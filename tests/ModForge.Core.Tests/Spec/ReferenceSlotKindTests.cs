using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using ModForge;
using Xunit;

namespace ModForge.Tests;

// THE GUARDRAIL FOR THE DEFECT THAT LOOKS PERFECT (Idea #24 referrer).
//
// `references[]` gives an EXISTING ref a label. Whether that label LOCKS ONTO THAT ONE OBJECT depends
// entirely on which package slot you put it in:
//   SingleRef target (sitTarget.target, …) → PackageTargetSpecificReference  → THAT ref, no other.
//   location         (sandbox.location, …) → LocationTarget + radius         → an AREA around that ref;
//                                            the engine picks any furniture/bed/food inside the radius.
// The second one builds green, dumps clean and warns about nothing — and the NPC sits in a DIFFERENT
// chair. So build prints an INFO note (never a warning: "wander near that chair" is a legal intent).
//
// Pinned here: the note fires exactly when a references[] LABEL lands in a location slot, never
// otherwise, and it changes NOTHING about what is built.
public class ReferenceSlotKindTests
{
    private const string Sandbox   = "Skyrim.esm:0x01C254";
    private const string SitTarget = "Skyrim.esm:0x0A9277";
    private const string Travel    = "Skyrim.esm:0x016FAA";
    private const string Chair     = "Skyrim.esm:0x0B9C04";   // CommonChair02 (FURN)
    private const string Label     = "sofia's chair";

    // A room + a chair placement + a references[] label on it. The package is left to the caller.
    private static ModSpec ChairSpec(Action<PackageSpec> slot, string template = Sandbox)
    {
        var s = new ModSpec { PluginName = "MFSlot.esp" };
        s.Cells.Add(new CellSpec { EditorId = "MFSlotRoom", Name = "Slot Room" });
        s.Placements.Add(new PlacementSpec
        {
            EditorId = "MFSlot_Chair", Base = Chair, Cell = "MFSlotRoom",
            Position = new Vec3 { X = 10f, Y = 20f, Z = 30f },
        });
        s.References.Add(new ReferenceSpec { Ref = "MFSlot_Chair", Label = Label, Base = Chair });
        var pk = new PackageSpec { EditorId = "MFSlotPkg", Template = template };
        slot(pk);
        s.Packages.Add(pk);
        s.Npcs.Add(new NpcSpec { EditorId = "MFSlotSofia", Name = "Sofia", Race = "Skyrim.esm:0x013746" });
        s.Npcs[0].Packages.Add("MFSlotPkg");
        return s;
    }

    // --- it fires: a label in a location slot ----------------------------------------------------

    [Fact]
    public void LabelInALocationSlot_IsAnInfoNote_NotAWarning()
    {
        var r = TestBuild.Ok(ChairSpec(p => p.Sandbox = new SandboxSpec { Location = Label, Radius = 128 }));

        // TestBuild.Ok already asserted zero WARNINGS — a note must never turn a clean build yellow.
        var note = Assert.Single(r.Notes);
        Assert.Contains(Label, note);
        Assert.Contains("sandbox.location", note);
        Assert.Contains("radius 128", note);
        Assert.Contains("AREA", note);                 // what actually happens
        Assert.Contains("DIFFERENT object", note);     // …and why it is not what you probably meant
        Assert.Contains("sitTarget.target", note);     // …and the slot that WOULD lock on
        Assert.StartsWith("  i ", note);               // info marker, not the "  ! " of a warning
    }

    [Fact]
    public void TheNoteChangesNothingThatIsBuilt()
    {
        // Pure hint: same records, same links, same slot payload as before the guardrail existed.
        var r = TestBuild.Ok(ChairSpec(p => p.Sandbox = new SandboxSpec { Location = Label, Radius = 128 }));
        Assert.Single(r.Notes);

        var chair = Assert.Single(r.Mod.EnumerateMajorRecords<IPlacedObjectGetter>());   // references[] authored nothing
        var pkg = r.Mod.Packages.Single(p => p.EditorID == "MFSlotPkg");
        var loc = Assert.IsAssignableFrom<IPackageDataLocationGetter>(pkg.Data[0]);
        var target = Assert.IsAssignableFrom<ILocationTargetGetter>(loc.Location!.Target);
        Assert.Equal(chair.FormKey, target.Link.FormKey);   // the label still resolves, exactly as before
        Assert.Equal(128u, loc.Location!.Radius);
    }

    [Fact]
    public void EveryLocationSlot_Notes_AndEverySingleRefSlot_DoesNot()
    {
        // One package per slot, all pointing at the same label — the note count IS the location-slot count.
        var s = new ModSpec { PluginName = "MFSlotAll.esp" };
        s.Cells.Add(new CellSpec { EditorId = "MFSlotRoom", Name = "Slot Room" });
        s.Placements.Add(new PlacementSpec { EditorId = "MFSlot_Chair", Base = Chair, Cell = "MFSlotRoom" });
        s.References.Add(new ReferenceSpec { Ref = "MFSlot_Chair", Label = Label, Base = Chair });

        s.Packages.Add(new PackageSpec { EditorId = "P_Sandbox",  Template = Sandbox,               Sandbox   = new SandboxSpec  { Location = Label } });
        s.Packages.Add(new PackageSpec { EditorId = "P_Sleep",    Template = "Skyrim.esm:0x019717", Sleep     = new SleepSpec    { Location = Label } });
        s.Packages.Add(new PackageSpec { EditorId = "P_Travel",   Template = Travel,                Travel    = new TravelSpec   { Place = Label } });
        s.Packages.Add(new PackageSpec { EditorId = "P_Escort",   Template = "Skyrim.esm:0x023B73", Escort    = new EscortSpec   { Destination = Label } });
        s.Packages.Add(new PackageSpec { EditorId = "P_Eat",      Template = "Skyrim.esm:0x019714", Eat       = new EatSpec      { Location = Label } });
        s.Packages.Add(new PackageSpec { EditorId = "P_UseMagicL",Template = "Skyrim.esm:0x0504F5", UseMagic  = new UseMagicSpec { Location = Label, Spell = "Skyrim.esm:0x043323" } });
        // …and the SingleRef slots, which lock on and must stay silent.
        s.Packages.Add(new PackageSpec { EditorId = "P_Sit",      Template = SitTarget,             SitTarget = new SitTargetSpec { Target = Label } });
        s.Packages.Add(new PackageSpec { EditorId = "P_Activate", Template = "Skyrim.esm:0x019B2D", Activate  = new ActivateSpec  { Target = Label } });
        s.Packages.Add(new PackageSpec { EditorId = "P_Follow",   Template = "Skyrim.esm:0x019B2C", Follow    = new FollowSpec    { Target = Label } });
        s.Packages.Add(new PackageSpec { EditorId = "P_Patrol",   Template = "Skyrim.esm:0x017723", Patrol    = new PatrolSpec    { Start = Label } });
        s.Packages.Add(new PackageSpec { EditorId = "P_EscortT",  Template = "Skyrim.esm:0x023B73", Escort    = new EscortSpec    { Target = Label, Destination = "MFSlot_Chair" } });
        s.Packages.Add(new PackageSpec { EditorId = "P_UseMagicT",Template = "Skyrim.esm:0x0504F5", UseMagic  = new UseMagicSpec  { Target = Label, Spell = "Skyrim.esm:0x043323" } });

        // Ok: a label resolves in ALL twelve slots (eat/useMagic used to be resolved before placements
        // and references[] existed, so those three warned "unresolved" here — see PackageTests).
        var notes = TestBuild.Ok(s).Notes;
        Assert.Equal(6, notes.Count);                    // exactly the six location slots
        foreach (var slot in new[] { "sandbox.location", "sleep.location", "travel.place",
                                     "escort.destination", "eat.location", "useMagic.location" })
            Assert.Contains(notes, n => n.Contains(slot));
        foreach (var slot in new[] { "sitTarget.target", "activate.target", "follow.target",
                                     "patrol.start", "escort.target", "useMagic.target" })
            // the SingleRef names appear only in the "use this instead" advice, never as the offending slot
            Assert.DoesNotContain(notes, n => n.Contains($"'P_{slot.Split('.')[0]}'"));
    }

    // --- the "area:" opt-out: author declares the region intent, the note goes away ---------------

    [Fact]
    public void AreaPrefix_OnALabelInALocationSlot_SilencesTheNote()
    {
        // "area:sofia's chair" = "I MEAN a region here" — the exact question the note asks is answered,
        // so it must not fire (StripAreaPrefix in the builder still resolves the label — see below).
        var r = TestBuild.Ok(ChairSpec(p => p.Sandbox = new SandboxSpec { Location = "area:" + Label, Radius = 128 }));
        Assert.Empty(r.Notes);
    }

    [Fact]
    public void AreaPrefix_StillResolvesTheLabel_ToTheSameFormKeyAsTheBareLabel()
    {
        // The prefix only changes intent/notes, never the built payload: "area:sofia's chair" binds the
        // SAME LocationTarget(chair) + radius as the bare "sofia's chair" did in TheNoteChangesNothing.
        var r = TestBuild.Ok(ChairSpec(p => p.Sandbox = new SandboxSpec { Location = "area:" + Label, Radius = 128 }));
        var chair = Assert.Single(r.Mod.EnumerateMajorRecords<IPlacedObjectGetter>());
        var pkg = r.Mod.Packages.Single(p => p.EditorID == "MFSlotPkg");
        var loc = Assert.IsAssignableFrom<IPackageDataLocationGetter>(pkg.Data[0]);
        var target = Assert.IsAssignableFrom<ILocationTargetGetter>(loc.Location!.Target);
        Assert.Equal(chair.FormKey, target.Link.FormKey);   // prefix stripped, label resolved as normal
        Assert.Equal(128u, loc.Location!.Radius);
    }

    [Fact]
    public void AreaPrefix_OnAPlainVanillaRef_ResolvesToThatRef()
    {
        // "area:" is a location-slot modifier, not label-only: an author can prefix any ref to make the
        // region intent explicit. Here a vanilla FormID keeps resolving after the prefix is stripped.
        var s = ChairSpec(p => p.SitTarget = new SitTargetSpec { Target = Label }, SitTarget);
        s.Packages.Add(new PackageSpec
        {
            EditorId = "MFSlotWander", Template = Sandbox,
            Sandbox = new SandboxSpec { Location = "area:Skyrim.esm:0x0D1991", Radius = 512 },
        });
        var r = TestBuild.Ok(s);
        Assert.Empty(r.Notes);   // a vanilla ref never noted anyway; still clean with the prefix
        var pkg = r.Mod.Packages.Single(p => p.EditorID == "MFSlotWander");
        var loc = Assert.IsAssignableFrom<IPackageDataLocationGetter>(pkg.Data[0]);
        var target = Assert.IsAssignableFrom<ILocationTargetGetter>(loc.Location!.Target);
        Assert.Equal(FormKey.Factory("0D1991:Skyrim.esm"), target.Link.FormKey);
    }

    // --- it stays quiet: the negative cases (this is what stops it being noise) --------------------

    [Fact]
    public void LabelInASingleRefSlot_SaysNothing()
    {
        // The whole point of the primitive, correctly used. No note.
        var r = TestBuild.Ok(ChairSpec(p => p.SitTarget = new SitTargetSpec { Target = Label }, SitTarget));
        Assert.Empty(r.Notes);
    }

    [Fact]
    public void VanillaFormIdInALocationSlot_SaysNothing()
    {
        // A location slot pointing at a plain vanilla ref is the ORDINARY area case ("sandbox around this
        // marker") — the author never claimed to care about one specific object. Never nag about it.
        var s = ChairSpec(p => p.SitTarget = new SitTargetSpec { Target = Label }, SitTarget);
        s.Packages.Add(new PackageSpec
        {
            EditorId = "MFSlotWander", Template = Sandbox,
            Sandbox = new SandboxSpec { Location = "Skyrim.esm:0x0D1991", Radius = 512 },
        });
        Assert.Empty(TestBuild.Ok(s).Notes);
    }

    [Fact]
    public void PlainPlacementEditorIdInALocationSlot_SaysNothing()
    {
        // Same reasoning: an in-spec placement editorId in sandbox.location is just "wander near my
        // marker". It is the LABEL — the declaration "this specific object matters" — that makes the
        // location slot suspicious.
        var s = ChairSpec(p => p.SitTarget = new SitTargetSpec { Target = Label }, SitTarget);
        s.Placements.Add(new PlacementSpec { EditorId = "MFSlot_Marker", Base = "Skyrim.esm:0x000034", Cell = "MFSlotRoom" });
        s.Packages.Add(new PackageSpec
        {
            EditorId = "MFSlotWander", Template = Sandbox,
            Sandbox = new SandboxSpec { Location = "MFSlot_Marker", Radius = 512 },
        });
        Assert.Empty(TestBuild.Ok(s).Notes);
    }

    [Fact]
    public void NoReferencesSection_SaysNothing()
    {
        var s = ChairSpec(p => p.Sandbox = new SandboxSpec { Location = "MFSlot_Chair" });
        s.References.Clear();
        Assert.Empty(TestBuild.Ok(s).Notes);
    }

    // --- anti-rot: the slot table must keep covering every package ref field ----------------------

    [Fact]
    public void PackageRefSlots_ClassifiesEveryStringFieldOfEveryPackageSubSpec()
    {
        // If a new package template adds a `target` / `location` field and nobody classifies it, the
        // SingleRef-vs-Location table silently stops being the truth — and this guardrail (and the docs
        // that quote it) rot. Fail here instead: every string an author can put a ref into must be in
        // PackageRefSlots, as SingleRef, Location, or an explicit NotAPlacedRef.
        var table = TableEntries();
        var missing = new List<string>();

        foreach (var f in StringFieldsOf(typeof(PackageSpec), ""))
            if (!table.Contains(f))
                missing.Add(f);

        Assert.True(missing.Count == 0,
            "PackageRefSlots (src/ModForge.Core/PackageRefSlots.cs) does not classify: " + string.Join(", ", missing)
            + "\n  → add each as SingleRef (PackageTargetSpecificReference — locks onto ONE ref),"
            + "\n    Location (LocationTarget + radius — an AREA), or NotAPlacedRef (not a placed-ref slot).");
    }

    [Fact]
    public void PackageRefSlots_ReadsTheRealSlotsOffASpec()
    {
        // The table's accessors must actually be wired to the fields they name (a copy-paste lambda
        // pointing at the wrong property would classify the wrong slot and the note would lie).
        var pk = new PackageSpec
        {
            Sandbox   = new SandboxSpec   { Location = "sandbox.location" },
            Sleep     = new SleepSpec     { Location = "sleep.location" },
            Travel    = new TravelSpec    { Place = "travel.place" },
            Escort    = new EscortSpec    { Destination = "escort.destination", Target = "escort.target" },
            Eat       = new EatSpec       { Location = "eat.location" },
            UseMagic  = new UseMagicSpec  { Location = "useMagic.location", Target = "useMagic.target" },
            SitTarget = new SitTargetSpec { Target = "sitTarget.target" },
            Activate  = new ActivateSpec  { Target = "activate.target" },
            Follow    = new FollowSpec    { Target = "follow.target" },
            Patrol    = new PatrolSpec    { Start = "patrol.start" },
        };
        foreach (var e in TableRows().Where(r => r.Kind != PackageSlotKind.NotAPlacedRef))
            Assert.Equal(e.Path, e.Get(pk));
    }

    // Reflection over the ModForge sub-spec object graph hanging off PackageSpec (Sandbox/Sleep/…/
    // Schedule) — every `string` property, as the dotted path an author writes in JSON.
    private static IEnumerable<string> StringFieldsOf(Type t, string prefix)
    {
        foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var path = prefix + char.ToLowerInvariant(p.Name[0]) + p.Name[1..];
            if (p.PropertyType == typeof(string)) yield return path;
            else if (p.PropertyType.Namespace == "ModForge" && p.PropertyType.IsClass)
                foreach (var nested in StringFieldsOf(p.PropertyType, path + "."))
                    yield return nested;
        }
    }

    // PackageRefSlots is internal to ModForge.Core; the test project sees it via InternalsVisibleTo.
    private static IEnumerable<PackageRefSlot> TableRows() => PackageRefSlots.All;
    private static HashSet<string> TableEntries() => TableRows().Select(r => r.Path).ToHashSet(StringComparer.Ordinal);
}
