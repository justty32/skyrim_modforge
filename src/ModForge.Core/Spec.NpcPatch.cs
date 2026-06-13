using System.Collections.Generic;

namespace ModForge;

// Patch an EXISTING (vanilla / other-master) NPC by overriding it and changing its AI package list.
// `overrideOf` is the existing NPC ref "<master>:0xFORMID" (e.g. Carlotta = Skyrim.esm:0x013B99).
// `packages` are PACK refs (in-spec editorId or vanilla "<master>:0xFORMID") forming the new schedule.
// `mode`: replace = use ONLY our packages; prepend/append = keep the NPC's existing packages and add
// ours before/after (package order matters — specific time/place packages should sit above the broad
// sandbox fallback). BuildNpcPatches deep-copies the existing NPC (a Skyrim override REPLACES the whole
// record, so name/stats/factions are carried forward) and only the package list is swapped in pass 2.
// The real English name resolves because MasterCache provisions the vanilla STRINGS (see that method).
public sealed class NpcPatchSpec
{
    public string OverrideOf { get; set; } = "";
    public List<string> Packages { get; set; } = new();
    public string Mode { get; set; } = "replace";   // replace | prepend | append
}
