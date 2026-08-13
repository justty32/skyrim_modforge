using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        private int navPatchesBuilt;

        public void BuildNavPatches()
        {
            for (int i = 0; i < spec.NavPatches.Count; i++)
            {
                var np = spec.NavPatches[i];
                string who = $"navPatch[{i}]";
                if (!TryExternalRef(np.Cell, out var cellFk) || !TryExternalRef(np.Navmesh, out var navmFk))
                { Warn($"  ! {who}: cell and navmesh must both be <master>:0xFORMID refs — skipped"); continue; }

                string cellMaster = np.Cell[..np.Cell.IndexOf(':')].Trim();
                var cache = MasterCache(cellMaster);
                if (cache is null) continue; // offline-safe: an unavailable master is not a broken patch
                if (!cache.TryResolve<ICellGetter>(cellFk, out var vanillaCell))
                { Warn($"  ! {who}: cell '{np.Cell}' not found in {cellMaster} — skipped"); continue; }
                if ((vanillaCell.Flags & Cell.Flag.IsInteriorCell) == 0)
                { Warn($"  ! {who}: cell '{np.Cell}' is exterior; P3 MVP supports vanilla interiors only — skipped"); continue; }

                var vanillaNavm = vanillaCell.NavigationMeshes.FirstOrDefault(n => n.FormKey == navmFk);
                if (vanillaNavm is null)
                { Warn($"  ! {who}: navmesh '{np.Navmesh}' is not in cell '{np.Cell}' — skipped"); continue; }

                vanillaCellOverrides.TryGetValue(cellFk, out var targetCell);
                int existingIndex = -1;
                if (targetCell is not null)
                    for (int n = 0; n < targetCell.NavigationMeshes.Count; n++)
                        if (targetCell.NavigationMeshes[n].FormKey == navmFk) { existingIndex = n; break; }

                // Work only on a detached clone. A bad seam/geometry leaves the output NAVM untouched.
                var candidate = (existingIndex >= 0
                    ? targetCell!.NavigationMeshes[existingIndex]
                    : vanillaNavm).DeepCopy();
                if (!NavmeshPatch.TryApply(candidate, np.Polygon, np.Epsilon, out var error))
                { Warn($"  ! {who}: {error} — skipped without changing NAVM {np.Navmesh}"); continue; }

                // Do not even create the CELL override until geometry succeeds: a rejected patch emits
                // no stray parent record, not merely no partial vertices.
                if (targetCell is null)
                {
                    int before = vanillaCellOverrides.Count;
                    targetCell = VanillaCellOverride(np.Cell);
                    if (targetCell is null) continue;
                    if (vanillaCellOverrides.Count > before) vanillaCells++;
                }
                if (existingIndex >= 0) targetCell.NavigationMeshes[existingIndex] = candidate;
                else targetCell.NavigationMeshes.Add(candidate);
                navmeshOverridden.Add(navmFk); // whole-record clobber diagnostics apply to geometry patches too
                navPatchesBuilt++;
            }
        }
    }
}
