using System.Collections.Generic;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace ModForge;

public static partial class Generator
{
    private sealed partial class BuildContext
    {
        // --- pass 1: NPC AI patches. Override an EXISTING NPC and swap its AI packages (the new list is
        // wired in pass 2). A faithful override deep-copies the whole record (a Skyrim override REPLACES
        // the master record, so we must carry everything forward — name/stats/inventory/factions — then
        // change only the packages in pass 2). Skyrim.esm is LOCALIZED, but MasterCache now opens it with
        // the vanilla English strings provisioned, so DeepCopyIn resolves the NPC's real name headless.
        // (Players run the English game + a translation mod on top — we ship the English name inline.) ---
        public void BuildNpcPatches()
        {
            foreach (var p in spec.NpcPatches)
            {
                if (!TryResolveTemplate<INpcGetter>(p.OverrideOf, out var src) || src is null)
                { Warn($"  ! npcPatch '{p.OverrideOf}' could not resolve the existing NPC (set MODFORGE_SKYRIM_DATA; ref must be <master>:0xFORMID) — skipped"); continue; }
                try
                {
                    var r = new Npc(src.FormKey, SkyrimRelease.SkyrimSE);
                    r.DeepCopyIn(src);   // deep-copy keeps the FormKey → override; copies name/stats/etc.
                    mod.Npcs.Add(r);
                    npcPatchesByRef[p.OverrideOf] = r;
                }
                catch (System.Exception ex)
                {
                    Warn($"  ! npcPatch '{p.OverrideOf}' override failed: {ex.GetType().Name}: {ex.Message} — skipped");
                }
            }
        }

        // --- pass 2: apply the patched package list onto the override NPC (after in-spec PACKs built). ---
        public void WireNpcPatchPackages()
        {
            foreach (var p in spec.NpcPatches)
            {
                if (!npcPatchesByRef.TryGetValue(p.OverrideOf, out var r)) continue;
                var resolved = new List<IFormLinkGetter<IPackageGetter>>();
                foreach (var pkgRef in p.Packages)
                    Resolve($"npcPatch '{p.OverrideOf}' package", pkgRef, fk => resolved.Add(new FormLink<IPackageGetter>(fk)));
                if (resolved.Count == 0) continue;
                // r.Packages is populated by DeepCopyIn (every NPC has a package list, possibly empty).
                switch ((p.Mode ?? "replace").Trim().ToLowerInvariant())
                {
                    case "prepend":
                        for (int i = resolved.Count - 1; i >= 0; i--) r.Packages.Insert(0, resolved[i]);
                        break;
                    case "append":
                        foreach (var l in resolved) r.Packages.Add(l);
                        break;
                    default: // replace
                        r.Packages.Clear();
                        foreach (var l in resolved) r.Packages.Add(l);
                        break;
                }
            }
        }
    }
}
