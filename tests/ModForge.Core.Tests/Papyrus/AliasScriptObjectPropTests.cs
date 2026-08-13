using System.Linq;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// Two core fixes that unblock the living-adventurers macro (sub_projs/living-adventurers, design.md §6):
//  1. An ALIAS scriptProperties object prop can point at a placement/xmarker. Alias scripts fill in
//     BuildStandaloneQuestAliases (before placements build), so the resolution must be DEFERRED to
//     WireDeferredScriptObjectProps (after placements) — it used to silently warn + drop the prop.
//  2. A forced-alias ACHR is auto-persistent. A living NPC's ref is forced into an alias and MoveTo'd
//     around; if it isn't persistent the engine drops it. deferredForcedAliases now feeds deferredAnchorEds.
public class AliasScriptObjectPropTests
{
    // A StartGameEnabled controller quest with one alias forced-filled to a placed ACHR, carrying an
    // alias script whose object prop points at an xmarker placement (built AFTER the alias pass).
    private static ModSpec Spec()
    {
        var spec = new ModSpec { PluginName = "Test.esp" };
        spec.Cells.Add(new CellSpec { EditorId = "Room", Name = "Room" });
        spec.Npcs.Add(new NpcSpec { EditorId = "Liv", Name = "Liv", Greeting = "Hm." });
        spec.Placements.Add(new PlacementSpec { EditorId = "LivRef", Base = "Liv", Kind = "npc", Cell = "Room", Position = new Vec3 { X = 0, Y = 0, Z = 0 } });
        spec.Placements.Add(new PlacementSpec { EditorId = "Mark", Kind = "xmarker", Cell = "Room", Position = new Vec3 { X = 10, Y = 0, Z = 0 } });
        spec.Quests.Add(new QuestSpec
        {
            EditorId = "Ctrl", Name = "Ctrl", StartGameEnabled = true,
            Aliases =
            {
                new QuestAliasSpec
                {
                    Name = "A0",
                    Fill = "forced:LivRef",
                    Script = "MFTestAlias",
                    ScriptProperties = { new PropertySpec { Name = "Mark", Type = "object", ObjectEditorId = "Mark" } },
                },
            },
        });
        return spec;
    }

    [Fact]
    public void AliasScript_ObjectProp_ResolvesPlacementBuiltLater()
    {
        var r = TestBuild.Ok(Spec());                            // Ok() => zero warnings (no dropped prop)
        // The alias script's "Mark" object prop must be present AND point at the xmarker (not null/empty).
        var qad = (IQuestAdapterGetter)r.Mod.Quests.First(q => q.EditorID == "Ctrl").VirtualMachineAdapter!;
        var aliasScript = qad.Aliases.SelectMany(a => a.Scripts).First(s => s.Name == "MFTestAlias");
        var markProp = (IScriptObjectPropertyGetter)aliasScript.Properties.First(p => p.Name == "Mark");
        Assert.False(markProp.Object.FormKey.IsNull);            // deferred resolution filled it
        var marker = r.Mod.EnumerateMajorRecords<IPlacedObjectGetter>().Single(o => o.EditorID == "Mark");
        Assert.Equal(marker.FormKey, markProp.Object.FormKey);
    }

    [Fact]
    public void ForcedAliasRef_IsPersistent()
    {
        var r = TestBuild.Ok(Spec());
        var cell = r.Mod.Cells.SelectMany(b => b.SubBlocks).SelectMany(b => b.Cells).Single(c => c.EditorID == "Room");
        Assert.Contains(cell.Persistent, p => p.EditorID == "LivRef");   // forced-alias ACHR survives save/load
    }
}
