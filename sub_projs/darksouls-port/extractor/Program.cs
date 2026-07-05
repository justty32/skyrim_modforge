using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using SoulsFormats;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;

// DsExtractor — Dark Souls (Remastered) map asset extractor for the ModForge
// darksouls-port sub-project. Reads DCX/BND/BXF/FLVER/TPF/MSB via SoulsFormats
// (JuicerMV.SoulsFormats NuGet fork). Structural spike — see README.md.
//
// Coordinate convention: glTF output stays in DS native space (Y-up, metres).
// NO Z-up / x70 scaling here — that belongs on the NIF side of the pipeline.

if (args.Length == 0)
{
    Console.Error.WriteLine("usage:");
    Console.Error.WriteLine("  msb-dump    <msb>          <out.json>");
    Console.Error.WriteLine("  flver2gltf  <flver.dcx>    <outdir>");
    Console.Error.WriteLine("  tpf-extract <tpfbhd>       <outdir> [--filter <substr>]");
    return 1;
}

try
{
    switch (args[0])
    {
        case "msb-dump":   return MsbDump(args[1], args[2]);
        case "flver2gltf": return Flver2Gltf(args[1], args[2]);
        case "tpf-extract":
            string? filter = null;
            int fi = Array.IndexOf(args, "--filter");
            if (fi >= 0 && fi + 1 < args.Length) filter = args[fi + 1];
            return TpfExtract(args[1], args[2], filter);
        default:
            Console.Error.WriteLine($"unknown subcommand: {args[0]}");
            return 1;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine("ERROR: " + ex);
    return 2;
}

// ---------------------------------------------------------------------------
// msb-dump : list every MSB part (type/name/model/position/rotation/scale)
// ---------------------------------------------------------------------------
static int MsbDump(string msbPath, string outJson)
{
    MSB1 msb = MSB1.Read(msbPath);

    var records = new List<PartRecord>();
    void Collect(string type, IEnumerable<MSB1.Part> parts)
    {
        foreach (var p in parts)
            records.Add(new PartRecord(
                type, p.Name, p.ModelName,
                new[] { p.Position.X, p.Position.Y, p.Position.Z },
                new[] { p.Rotation.X, p.Rotation.Y, p.Rotation.Z },
                new[] { p.Scale.X, p.Scale.Y, p.Scale.Z }));
    }

    var pp = msb.Parts;
    Collect("MapPiece",         pp.MapPieces);
    Collect("Object",           pp.Objects);
    Collect("Enemy",            pp.Enemies);
    Collect("Player",           pp.Players);
    Collect("Collision",        pp.Collisions);
    Collect("Navmesh",          pp.Navmeshes);
    Collect("DummyObject",      pp.DummyObjects);
    Collect("DummyEnemy",       pp.DummyEnemies);
    Collect("ConnectCollision", pp.ConnectCollisions);

    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outJson))!);
    File.WriteAllText(outJson, JsonSerializer.Serialize(new
    {
        source = Path.GetFileName(msbPath),
        partCount = records.Count,
        parts = records,
    }, new JsonSerializerOptions { WriteIndented = true }));

    Console.WriteLine($"MSB: {msbPath}");
    Console.WriteLine($"wrote {records.Count} parts -> {outJson}");
    Console.WriteLine("counts by type:");
    foreach (var g in records.GroupBy(r => r.type).OrderByDescending(g => g.Count()))
        Console.WriteLine($"  {g.Key,-18} {g.Count()}");
    return 0;
}

// ---------------------------------------------------------------------------
// flver2gltf : DCX -> FLVER2 -> glTF 2.0 (.gltf + .bin). Each FLVER mesh
// becomes one glTF mesh/primitive. Material name = referenced diffuse texture
// filename. Geometry kept in DS native space (Y-up, metres).
// ---------------------------------------------------------------------------
static int Flver2Gltf(string flverPath, string outDir)
{
    FLVER2 flver = FLVER2.Read(flverPath);      // auto-decompresses DCX
    Directory.CreateDirectory(outDir);
    string stem = Path.GetFileName(flverPath);
    foreach (var ext in new[] { ".flver.dcx", ".dcx", ".flver" })
        if (stem.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
        { stem = stem[..^ext.Length]; break; }

    var scene = new SceneBuilder();
    var matCache = new Dictionary<string, MaterialBuilder>();
    var referencedTextures = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

    long totalVerts = 0, totalTris = 0;
    var bbMin = new Vector3(float.MaxValue);
    var bbMax = new Vector3(float.MinValue);
    int meshOut = 0;

    for (int mi = 0; mi < flver.Meshes.Count; mi++)
    {
        var mesh = flver.Meshes[mi];
        if (mesh.Vertices.Count == 0 || mesh.FaceSets.Count == 0) continue;

        // Full-detail faceset only (skip LOD / motion-blur duplicates).
        var faceSet = mesh.FaceSets.FirstOrDefault(fs => fs.Flags == FLVER2.FaceSet.FSFlags.None)
                      ?? mesh.FaceSets[0];
        List<int> indices = faceSet.Triangulate(false, false); // strip->list handled internally
        if (indices.Count < 3) continue;

        // Diffuse texture name for this mesh's material.
        string diffuse = "";
        if (mesh.MaterialIndex >= 0 && mesh.MaterialIndex < flver.Materials.Count)
        {
            var mat = flver.Materials[mesh.MaterialIndex];
            var tex = mat.Textures.FirstOrDefault(t =>
                          t.Type.Contains("Diffuse", StringComparison.OrdinalIgnoreCase) ||
                          t.Type.Contains("Albedo", StringComparison.OrdinalIgnoreCase))
                      ?? mat.Textures.FirstOrDefault();
            if (tex != null && !string.IsNullOrEmpty(tex.Path))
                diffuse = Path.GetFileName(tex.Path.Replace('\\', '/'));
            foreach (var t in mat.Textures)
                if (!string.IsNullOrEmpty(t.Path))
                    referencedTextures.Add(Path.GetFileNameWithoutExtension(t.Path.Replace('\\', '/')));
        }
        string matName = diffuse.Length > 0 ? diffuse : $"mat_{mesh.MaterialIndex}";
        if (!matCache.TryGetValue(matName, out var material))
        {
            material = new MaterialBuilder(matName).WithDoubleSide(true);
            matCache[matName] = material;
        }

        var mb = new MeshBuilder<VertexPositionNormal, VertexTexture1, VertexEmpty>($"{stem}_mesh{mi}");
        var prim = mb.UsePrimitive(material);

        VertexBuilder<VertexPositionNormal, VertexTexture1, VertexEmpty> ToVB(FLVER.Vertex v)
        {
            Vector3 pos = v.Position;
            Vector3 nrm = v.Normal;
            Vector2 uv = v.UVs.Count > 0 ? new Vector2(v.UVs[0].X, v.UVs[0].Y) : Vector2.Zero;
            if (pos.X < bbMin.X) bbMin.X = pos.X;
            if (pos.Y < bbMin.Y) bbMin.Y = pos.Y;
            if (pos.Z < bbMin.Z) bbMin.Z = pos.Z;
            if (pos.X > bbMax.X) bbMax.X = pos.X;
            if (pos.Y > bbMax.Y) bbMax.Y = pos.Y;
            if (pos.Z > bbMax.Z) bbMax.Z = pos.Z;
            return new VertexBuilder<VertexPositionNormal, VertexTexture1, VertexEmpty>(
                new VertexPositionNormal(pos, nrm), new VertexTexture1(uv));
        }

        for (int i = 0; i + 2 < indices.Count; i += 3)
        {
            int a = indices[i], b = indices[i + 1], c = indices[i + 2];
            if (a == b || b == c || a == c) continue; // degenerate guard
            prim.AddTriangle(ToVB(mesh.Vertices[a]), ToVB(mesh.Vertices[b]), ToVB(mesh.Vertices[c]));
            totalTris++;
        }
        totalVerts += mesh.Vertices.Count;
        scene.AddRigidMesh(mb, Matrix4x4.Identity);
        meshOut++;
    }

    var model = scene.ToGltf2();
    string gltfPath = Path.Combine(outDir, stem + ".gltf");
    model.SaveGLTF(gltfPath);

    // Sidecar: texture stems referenced by this FLVER (feeds tpf-extract --filter).
    File.WriteAllText(Path.Combine(outDir, stem + ".textures.json"),
        JsonSerializer.Serialize(referencedTextures, new JsonSerializerOptions { WriteIndented = true }));

    Vector3 size = (meshOut > 0) ? bbMax - bbMin : Vector3.Zero;
    Console.WriteLine($"FLVER: {flverPath}");
    Console.WriteLine($"  flver meshes={flver.Meshes.Count} materials={flver.Materials.Count}");
    Console.WriteLine($"  emitted glTF meshes={meshOut} vertices={totalVerts} triangles={totalTris} indices={totalTris * 3}");
    Console.WriteLine($"  bbox min=({bbMin.X:F2},{bbMin.Y:F2},{bbMin.Z:F2}) max=({bbMax.X:F2},{bbMax.Y:F2},{bbMax.Z:F2})");
    Console.WriteLine($"  bbox size (metres) = ({size.X:F2} x {size.Y:F2} x {size.Z:F2})");
    Console.WriteLine($"  referenced textures: {string.Join(", ", referencedTextures)}");
    Console.WriteLine($"  wrote {gltfPath} (+ .bin, + .textures.json)");
    return 0;
}

// ---------------------------------------------------------------------------
// tpf-extract : BXF3 (tpfbhd + tpfbdt) -> each TPF -> DDS files, dropped as-is.
// ---------------------------------------------------------------------------
static int TpfExtract(string bhdPath, string outDir, string? filter)
{
    string bdtPath = Path.ChangeExtension(bhdPath, ".tpfbdt");
    if (!File.Exists(bdtPath))
        bdtPath = bhdPath.Replace(".tpfbhd", ".tpfbdt");
    if (!File.Exists(bdtPath)) { Console.Error.WriteLine($"missing bdt: {bdtPath}"); return 1; }

    BXF3 bxf = BXF3.Read(bhdPath, bdtPath);
    Directory.CreateDirectory(outDir);

    int tpfCount = 0, ddsCount = 0, skipped = 0;
    foreach (var file in bxf.Files)
    {
        if (!TPF.Is(file.Bytes)) { skipped++; continue; }
        TPF tpf = TPF.Read(file.Bytes);
        tpfCount++;
        foreach (var tex in tpf.Textures)
        {
            if (filter != null && !tex.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                continue;
            byte[] dds = tex.Headerize();       // reconstruct full DDS header
            string name = tex.Name;
            if (!name.EndsWith(".dds", StringComparison.OrdinalIgnoreCase)) name += ".dds";
            File.WriteAllBytes(Path.Combine(outDir, Path.GetFileName(name)), dds);
            ddsCount++;
        }
    }

    Console.WriteLine($"BXF3: {bhdPath}");
    Console.WriteLine($"  files={bxf.Files.Count} tpf={tpfCount} skipped(non-TPF)={skipped}");
    Console.WriteLine($"  wrote {ddsCount} DDS -> {outDir}" + (filter != null ? $" (filter='{filter}')" : ""));
    return 0;
}

record PartRecord(string type, string name, string model, float[] position, float[] rotation, float[] scale);
