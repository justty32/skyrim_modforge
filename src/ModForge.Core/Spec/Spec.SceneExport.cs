namespace ModForge;

// In-game scene export (Idea #24 §D). Give an already-existing EXTERNAL npc — a PROTEUS clone of the
// player, or a standalone-follower base — a job ROLE, and the build macro-expands it into that NPC's
// conditioned greeting + AI behaviour. This is NOT the player-facing IdentitySpec (a FACT the PLAYER
// joins to gate PLAYER dialogue); it tags a specific NPC. It differs from SettlementSpec.ResidentSpec
// (whose `Npc` is an in-spec NpcSpec) by keying on an external base-NPC ref and by carrying dialogue.
//
// `npc` is the base NPC_ ref the greeting/behaviour attaches to — an in-spec NpcSpec editorId OR an
// external "<plugin>.esp:0xFORMID" (a captured clone / follower base). The role macro keys GetIsID and
// the NpcPatch on it; place the actor in the world with a companion `placements[]` entry (kind:npc).
//
// `role` selects a template (currently only "blacksmith"); an unknown role is warned and skipped
// (no silent drop). `backstory` is a few lines of context that seed the greeting text (hand-authored
// in the slice; a later pass can hand it to the #17 AI dialogue pipeline).
//
// Expansion (Generator.ExpandNpcRoles, a pass-0 macro like ExpandSettlements) adds — for blacksmith —
// a shared StartGameEnabled host QUST, a Hello DialogueSpec (GetIsID npc), and an NpcPatch appending a
// sandbox package (editor-location fallback = sandbox where the actor stands). Vendor service is not
// yet expanded (NpcPatch cannot add faction membership — see design doc §D).
public sealed class SceneNpcRoleSpec
{
    public string Npc { get; set; } = "";        // base NPC ref: in-spec editorId OR <plugin>.esp:0xFORMID
    public string Role { get; set; } = "";        // role template key (slice: "blacksmith")
    public string Backstory { get; set; } = "";   // context seeding the greeting (hand-authored in slice)
}

// The ModSpec fields that carry the DTOs above.
public sealed partial class ModSpec
{
    public List<SceneNpcRoleSpec> NpcRoles { get; set; } = new(); // in-game scene export: tag an external NPC with a job role (Idea #24 §D; Spec.SceneExport.cs) — macro-expands to a host quest + conditioned greeting + sandbox package. NOT the player-facing Identities.

    // Guard so the npc-role macro-expansion (Generator.ExpandNpcRoles) runs at most once. Not serialized.
    [System.Text.Json.Serialization.JsonIgnore] internal bool NpcRolesExpanded { get; set; }
}
