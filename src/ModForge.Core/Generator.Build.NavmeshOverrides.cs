using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // --- navmeshOverrides[]: re-emit a vanilla NAVM from our plugin, unchanged ----------------
        // The contract, the reasoning and the iron rule ("never renumber a triangle") are in
        // Spec.NavmeshOverrides.cs. Here we only do the copy.
        //
        // The copy is a plain DeepCopy of the master's NavigationMesh under its OWN FormKey, dropped
        // into the cell override we build anyway for placements/navcuts. That last part is the whole
        // reason this file does not use Mutagen's TryResolveContext(...).GetOrAddAsOverride(mod): that
        // would let Mutagen invent its own CELL/WRLD parent overrides, and ModForge's WRLD override is
        // not a naive deep-copy — it deliberately carries LandDefaults (or the world floods), EDID +
        // RNAM + TNAM/UNAM (or the world map renders white/corrupt), the persistent TopCell WITH its
        // record-header flags (or Tamriel CTDs), and deliberately DROPS OFST (absolute byte offsets
        // into Skyrim.esm — transplanting them corrupts our file). Two mods' worth of scar tissue live
        // in CopyWorldspaceEnv/WorldspaceOverride; routing through ExteriorCell() reuses all of it and
        // guarantees there is exactly ONE override object per cell/worldspace in the output.
        //
        // NAVI is untouched on purpose (U4): the mesh keeps its FormID, so vanilla's NVMI entry still
        // points at it. Nothing here feeds WriteNaviInfoMap.
        private readonly HashSet<FormKey> navmeshOverridden = new();
        private int navmeshOverridesBuilt;

        public void BuildNavmeshOverrides()
        {
            for (int i = 0; i < spec.NavmeshOverrides.Count; i++)
            {
                var no = spec.NavmeshOverrides[i];
                string who = $"navmeshOverride[{i}]";

                // Exterior: worldspace + grid (explicit x/y, or the cell containing `position`).
                if (!string.IsNullOrWhiteSpace(no.Worldspace))
                {
                    if (!LooksExternalRef(no.Worldspace))
                    { Warn($"  ! {who}: worldspace '{no.Worldspace}' must be a vanilla <master>:0xFORMID ref (an in-spec worldspace's navmesh is ours already — there is nothing to override) — skipped"); continue; }
                    if (!TryExternalRef(no.Worldspace, out var wsFk)) { Warn($"  ! {who}: worldspace '{no.Worldspace}' is not a <master>:0xFORMID ref — skipped"); continue; }
                    if (!TryGrid(no, out int cx, out int cy))
                    { Warn($"  ! {who}: needs `x`+`y` (cell grid coords) or a `position` inside the cell — skipped"); continue; }

                    var master = no.Worldspace[..no.Worldspace.IndexOf(':')].Trim();
                    var src = FindMasterExteriorCell(master, wsFk, cx, cy);   // null offline, or ungenerated grid
                    if (src is null) continue;                                // OFFLINE-SAFE: unknown -> emit nothing
                    var dst = ExteriorCell(no.Worldspace, cx, cy);
                    if (dst is null) continue;
                    CopyNavmeshes(src, dst, no.Navmesh, $"{who} (worldspace {no.Worldspace} cell {cx},{cy})");
                    continue;
                }

                // Interior: a vanilla cell by FormID.
                if (!LooksExternalRef(no.Cell))
                { Warn($"  ! {who}: needs a vanilla `cell` (<master>:0xFORMID) or a `worldspace` + grid — an in-spec cell has no vanilla navmesh to override — skipped"); continue; }
                if (!TryExternalRef(no.Cell, out var cellFk)) { Warn($"  ! {who}: cell '{no.Cell}' is not a <master>:0xFORMID ref — skipped"); continue; }
                var cellMaster = no.Cell[..no.Cell.IndexOf(':')].Trim();
                var cache = MasterCache(cellMaster);
                if (cache is null) continue;                                  // OFFLINE-SAFE: no master -> emit nothing
                if (!cache.TryResolve<ICellGetter>(cellFk, out var vanilla))
                { Warn($"  ! {who}: cell '{no.Cell}' not found in {cellMaster} — skipped"); continue; }

                int before = vanillaCellOverrides.Count;
                var target = VanillaCellOverride(no.Cell);                    // warns + refuses on an exterior cell
                if (target is null) continue;
                if (vanillaCellOverrides.Count > before) vanillaCells++;
                CopyNavmeshes(vanilla, target, no.Navmesh, $"{who} (cell {no.Cell})");
            }
        }

        // Deep-copy every NAVM of `src` (or just `only`, if given) into `dst` under the SAME FormKey.
        // Same FormKey = an override, so the engine's NAVI entry, every neighbour mesh's EdgeLink and
        // every door portal that names this mesh all keep pointing at it. No geometry is touched: the
        // vertex array, the triangle array (indices UNCHANGED — the iron rule), the opaque NavmeshGrid
        // blob, the cover table and the record-header flags all come across verbatim. `navdiag` proves
        // it by byte-comparing the written NVNM against the master's.
        private void CopyNavmeshes(ICellGetter src, ICell dst, string only, string who)
        {
            FormKey? onlyFk = null;
            if (!string.IsNullOrWhiteSpace(only))
            {
                if (!TryExternalRef(only, out var f))
                { Warn($"  ! {who}: navmesh '{only}' is not a <master>:0xFORMID ref — skipped"); return; }
                onlyFk = f;
            }

            int copied = 0;
            foreach (var nm in src.NavigationMeshes)
            {
                if (onlyFk is { } want && nm.FormKey != want) continue;
                if (!navmeshOverridden.Add(nm.FormKey)) { copied++; continue; }   // listed twice — copy once
                dst.NavigationMeshes.Add(nm.DeepCopy());                          // same FormKey => override
                navmeshOverridesBuilt++;
                copied++;
            }

            if (copied == 0)
                Warn(onlyFk is null
                    ? $"  ! {who}: that cell has no navmesh at all — nothing to override"
                    : $"  ! {who}: navmesh '{only}' is not in that cell — nothing to override");
        }

        // The exterior grid cell a navmeshOverride targets: explicit x+y, else the cell `position` is in.
        private static bool TryGrid(NavmeshOverrideSpec no, out int cx, out int cy)
        {
            if (no.X is { } x && no.Y is { } y) { cx = x; cy = y; return true; }
            if (no.Position is { } p) { cx = PosToGrid(p.X); cy = PosToGrid(p.Y); return true; }
            cx = cy = 0; return false;
        }
    }
}
