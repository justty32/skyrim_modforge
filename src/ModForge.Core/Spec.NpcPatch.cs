using System.Collections.Generic;

namespace ModForge;

// Patch an EXISTING (vanilla / other-master) NPC by overriding it and changing its AI package list.
// `overrideOf` is the existing NPC ref "<master>:0xFORMID" (e.g. Carlotta = Skyrim.esm:0x0001A6A0).
// `packages` are PACK refs (in-spec editorId or vanilla "<master>:0xFORMID") forming the new schedule.
// `mode`: replace = use ONLY our packages; prepend/append = keep the NPC's existing packages and add
// ours before/after (package order matters — specific time/place packages should sit above the broad
// sandbox fallback). The override deep-copies the existing NPC (keeping name/stats/etc.) and only swaps
// the package list — the same GetOrAddAsOverride/DeepCopyIn pattern ModForge uses for vanilla cells.
public sealed class NpcPatchSpec
{
    public string OverrideOf { get; set; } = "";
    public List<string> Packages { get; set; } = new();
    public string Mode { get; set; } = "replace";   // replace | prepend | append
}
