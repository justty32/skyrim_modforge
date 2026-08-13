using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace ModForge;

public static partial class Generator
{
    internal sealed partial class BuildContext
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

        // --- U10: warn when another installed plugin also overrides a mesh we override ----------------
        // A navmeshOverrides[] entry re-emits a vanilla NAVM under its own FormKey — a WHOLE-RECORD
        // override, and NAVM has no additive merge. So if another plugin overrides the same mesh, the two
        // clobber each other: whichever loads LAST replaces the other outright and its edits vanish with
        // no error. USSEP's navmesh fixes are the usual casualty. We cannot see the player's load ORDER
        // here (MasterCache reads one plugin at a time — there is no order), so we cannot name the winner;
        // but we CAN see who else touches the mesh, and that is the compatibility fact worth a build line.
        //
        // Warnings only, zero records — runs last with CheckNavmesh. OFFLINE-SAFE: reads plugins straight
        // from the Data folder, silent when none are there. Runs only when this spec actually overrides
        // navmesh (navmeshOverridden non-empty), so an ordinary build never pays the directory scan.
        public void CheckNavmeshOverrideClobbers()
        {
            if (!spec.Navmesh.Warnings || !spec.Navmesh.WarnNavmeshClobber) return;
            if (navmeshOverridden.Count == 0 || !Directory.Exists(skyrimData)) return;

            // The masters that OWN the meshes we override (usually just Skyrim.esm). A plugin can only
            // override one of these meshes if it masters that owner — a cheap header reject before we read
            // any records. An owner is the SOURCE of the mesh, never a clobber, so it is never flagged.
            var owners = navmeshOverridden.Select(fk => fk.ModKey.FileName.String)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var path in Directory.EnumerateFiles(skyrimData))
            {
                var ext = Path.GetExtension(path);
                if (!(ext.Equals(".esp", StringComparison.OrdinalIgnoreCase)
                   || ext.Equals(".esm", StringComparison.OrdinalIgnoreCase)
                   || ext.Equals(".esl", StringComparison.OrdinalIgnoreCase))) continue;

                var file = Path.GetFileName(path);
                if (string.Equals(file, mod.ModKey.FileName.String, StringComparison.OrdinalIgnoreCase)) continue; // us
                if (owners.Contains(file)) continue;                                 // the source master, not a clobber
                if (IsVanillaMaster(file) || IsCreationClubMaster(file)) continue;   // baseline, not the author's surprise

                List<FormKey>? hits = null;
                try
                {
                    using var other = SkyrimMod.CreateFromBinaryOverlay(new ModPath(path), SkyrimRelease.SkyrimSE);
                    // Header reject: a plugin that masters none of our owners cannot override our meshes.
                    if (!other.MasterReferences.Any(m => owners.Contains(m.Master.FileName.String))) continue;
                    foreach (var nm in other.EnumerateMajorRecords<INavigationMeshGetter>())
                        if (navmeshOverridden.Contains(nm.FormKey))
                            (hits ??= new()).Add(nm.FormKey);
                }
                catch { continue; }   // an unreadable/foreign plugin is not our problem — advisory pass, stay quiet

                if (hits is null) continue;
                var ids = string.Join(", ", hits.Take(6).Select(fk => $"0x{fk.ID:X6}"));
                if (hits.Count > 6) ids += $", … (+{hits.Count - 6})";
                Warn($"  ! navmesh: {hits.Count} NAVM record(s) this build overrides are ALSO overridden by '{file}' ({ids}). "
                   + "NAVM records do not merge — whichever plugin loads LAST replaces the other outright, so your "
                   + "override and its edits clobber each other (if that plugin carries a navmesh FIX, e.g. USSEP, you "
                   + "may silently revert it). Override only a vanilla mesh you truly need; if you keep it, order this "
                   + "plugin deliberately against that one.");
            }
        }
    }
}
