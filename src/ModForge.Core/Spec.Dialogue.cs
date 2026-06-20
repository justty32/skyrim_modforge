namespace ModForge;

// --- Dialogue topics, INFO variants, CTDA conditions, banter, condition templates, script attach ---

// A dialogue topic: shown under QuestEditorId's branch; targets SpeakerNpcEditorId (GetIsID).
public sealed class DialogueSpec
{
    public string EditorId { get; set; } = "";
    public string QuestEditorId { get; set; } = "";
    public string SpeakerNpcEditorId { get; set; } = "";
    public string Prompt { get; set; } = "";
    public List<string> Responses { get; set; } = new();
    public string Emotion { get; set; } = "Neutral";   // Neutral|Anger|Disgust|Fear|Sad|Happy|Surprise|Puzzled — applied to all response lines
    public uint EmotionValue { get; set; } = 50;        // 0..100 intensity
    // Result fragment — Papyrus that runs when the line is PICKED (the INFO's OnEnd fragment). This is
    // the only way to *do* something on a dialogue choice (take gold, join the follower system, set a
    // stage). ResultScript is the fragment's Scriptname (must `Extends TopicInfo` and define
    // `Function Fragment_0(ObjectReference akSpeakerRef)`); ResultScriptSource is the .psc for `package`
    // to compile; ResultProperties bind that script's Auto properties (same shape as a ScriptAttachSpec).
    public string ResultScript { get; set; } = "";
    public string ResultScriptSource { get; set; } = "";
    public List<PropertySpec> ResultProperties { get; set; } = new();
    // Goodbye closes the dialogue menu after this line — vanilla recruit/dismiss lines all set it.
    public bool Goodbye { get; set; }
    // INFO (ENAM) behaviour flags. sayOnce: speak this line at most once ever (VIGILANT's most-used
    // INFO flag — one-shot story beats). walkAway: the NPC walks off after delivering it. random:
    // the engine random-picks among sibling INFOs whose conditions pass (line variety). invisibleContinue:
    // immediately continue to the next INFO in the chain without closing the menu. forceSubtitle: always
    // show the subtitle even if subtitles are off.
    public bool SayOnce { get; set; }
    public bool WalkAway { get; set; }
    public bool Random { get; set; }
    public bool InvisibleContinue { get; set; }
    public bool ForceSubtitle { get; set; }
    // --- dialogue TREE (branching conversations) ---
    // topLevel: a top-level menu option shown the moment you talk to the NPC. Set FALSE for a SUB-topic
    // that only appears once another line LINKS to it (default true → behaves like a normal top option).
    public bool TopLevel { get; set; } = true;
    // linkTo (ENAM): after THIS line plays, surface these dialogue topics as the next player choices —
    // each is another `dialogue` entry's editorId (its TOPIC), or a vanilla `<master>:0xFORMID` topic.
    // This is how a conversation branches: greeting → topic → response → linkTo:[follow-ups].
    public List<string> LinkTo { get; set; } = new();
    // previousDialog (PNAM): this INFO follows another INFO in a chain — its value is another `dialogue`
    // entry's editorId (resolved to that INFO). Used to order/chain responses within a flow.
    public string PreviousDialog { get; set; } = "";
    // Extra CTDA gates on the INFO (beyond the auto GetIsID speaker gate). e.g. only show a paid
    // recruit line when the player can afford it and isn't already following.
    public List<ConditionSpec> Conditions { get; set; } = new();
    // Variants (M組 — INFO array batch): declare MANY sibling INFOs under this ONE topic in a single
    // entry, instead of repeating the topic/speaker/conditions for each. Each variant becomes its own INFO
    // (Random flag, so the engine random-picks among those whose conditions currently pass) that SHARES
    // this entry's speaker gate + `conditions` + `useConditionTemplates` + `identity`, plus its own extra
    // `conditions` and own `responses`. This is the ambient-commentary generator: dozens of travel/location/
    // time/weather reaction lines on one shared gate (FCO's 265-line pain point). When `variants` is set and
    // this entry's own `responses` is EMPTY, no parent INFO is emitted (the entry is a pure batch header);
    // if `responses` is non-empty the parent line plays as one more sibling. Variants are line-variety only
    // — result fragments / setStage / linkTo stay on the parent entry.
    public List<DialogueVariantSpec> Variants { get; set; } = new();
    // Named condition templates (M組) to expand onto this INFO, by name from `conditionTemplates[]`.
    // The template's conditions are appended exactly like inline `conditions` (same BuildCondition path,
    // alias-aware). Lets many INFOs share one condition block — e.g. FCO's 265 commentary lines all
    // gated on the same location/state set. Expansion order: inline `conditions` first, then each named
    // template in listed order.
    public List<string> UseConditionTemplates { get; set; } = new();
    // Identity gating (lightweight class system). `identity`: only show this line when the PLAYER holds
    // that identity (GetInFaction(identity.faction) ≥ 1). `primaryIdentity`: same, PLUS exclude every
    // higher-priority identity (GetInFaction == 0) so only the top "primary" greeting fires. Both names
    // are identity ids from `identities[]`; they expand to CTDA at build (Generator.Build.Identity.cs).
    public string Identity { get; set; } = "";
    public string PrimaryIdentity { get; set; } = "";
    // SetPrimaryIdentity (optional): when the player picks this topic, MANUALLY OVERRIDE which identity NPCs
    // greet you as. The value is an identity id from `identities[]`, or "auto" (clear the override — back to
    // the highest-priority held identity). Generates a TIF result fragment that sets the MF_IdentityOverride
    // global; the MFIdentityController reads it. Pair with an `identity:` gate so the option only shows when
    // the player actually holds that identity.
    public string SetPrimaryIdentity { get; set; } = "";
    // Hello (default false): emit this line as the NPC's GREETING (Misc/Hello/HELO, NPC-initiated, no
    // player prompt) instead of a player-selectable Custom topic. Combine with `identity`/
    // `primaryIdentity` (or `conditions`) to make an NPC greet you differently by state — the engine
    // picks the highest-priority Hello whose conditions pass, falling back to the NPC's plain `greeting`.
    // `prompt` is ignored for a Hello (greetings have no menu line).
    public bool Hello { get; set; }
    // SetStage (optional, -1 = none): when the player picks this topic, advance the host quest to this
    // stage. In Skyrim a dialogue line sets a stage via an INFO RESULT FRAGMENT (a Papyrus snippet
    // `GetOwningQuest().SetStage(N)`). `package` emits a ready-to-compile TIF fragment scaffold; it
    // must be CK-compiled + bound to the INFO (structural only).
    public int SetStage { get; set; } = -1;
    // OpenBarter (default false): when the player picks this topic, open the BARTER/trade menu with the
    // speaking NPC (vanilla `Actor.ShowBarterMenu()`, no SKSE). Generates a TIF result fragment. The
    // speaker must be a vendor (a member of a Vendor-flagged faction with a merchant chest) for goods/gold
    // to appear. Pair with an `identity:` gate for an identity-specific "fellow merchant" trade option.
    public bool OpenBarter { get; set; }
    // SetGlobal (optional): when the player picks this topic, write or adjust one GlobalVariable.
    // Use `value` for flags/absolute reputation states (SetValue) or `delta` for counters (Mod).
    // The global is save-persisted and can be read by later `conditions` / `identity.activeWhen`
    // through GetGlobalValue.
    public DialogueSetGlobalSpec? SetGlobal { get; set; }
    // RewardItem (optional ref) + RewardCount: when the player picks this topic, give the player this
    // item/gold (vanilla `Game.GetPlayer().AddItem(item, count)`). Generates a TIF result fragment. Use
    // for quest rewards (e.g. gold on escort completion). RewardCount defaults to 1.
    public string RewardItem { get; set; } = "";
    public int RewardCount { get; set; } = 1;
    // EvaluateSpeakerPackages (default false): when picked, force the speaking NPC to re-evaluate its AI
    // packages (`Actor.EvaluatePackage()`) so a package newly enabled by this line's `setStage` (e.g. a
    // follow package gated on GetStage==N) activates immediately instead of on the next periodic re-eval.
    public bool EvaluateSpeakerPackages { get; set; }
    // Persist (optional, Idea #20): when picked, write nested per-Form state to a JContainers JFormDB
    // storage (the NPC you're talking to, or the player). Generates the JFormDB.solveXxxSetter calls into
    // this line's TIF result fragment. See PersistSpec / Generator.JContainers.cs.
    public PersistSpec? Persist { get; set; }
    // SyncPerks (optional, Idea #20): when picked, AddPerk/RemovePerk on the key actor from its stored
    // JFormDB node ranks (idempotent). Runs after `persist`. See SyncPerksSpec.
    public SyncPerksSpec? SyncPerks { get; set; }
    // StorageWrites (optional, J組): when picked, write lightweight per-Form scalar state via PapyrusUtil
    // StorageUtil into this line's TIF result fragment. `target` = "speaker" (the NPC you're talking to —
    // the default), "player", or "none"/"global". The save-managed, flat-KV counterpart to `persist` —
    // follower memory, cooldown timestamps, per-NPC flags. See StorageWriteSpec / Generator.StorageWrites.cs.
    public List<StorageWriteSpec> StorageWrites { get; set; } = new();
}
// One PapyrusUtil StorageUtil per-Form KV write (J組 — see DialogueSpec/StageSpec.StorageWrites). `key`
// is the StorageUtil string key; `target` is the Form the value hangs on (speaker | player | none/global —
// none = a process-global KV not tied to any Form). Set exactly one of int/float/str. `delta` (int/float
// only) emits Adjust{Int,Float}Value (atomic read-add-write) instead of Set; a string write has no delta.
public sealed class StorageWriteSpec
{
    public string Key { get; set; } = "";        // StorageUtil string key (e.g. "mymod_lastGreet")
    public string Target { get; set; } = "";       // speaker (default) | player | none | global
    public int? Int { get; set; }
    public float? Float { get; set; }
    public string? Str { get; set; }
    public bool Delta { get; set; }                // int/float only → Adjust…Value (read-add-write)
}

// One INFO variant in a dialogue batch (M組 — see DialogueSpec.Variants). Becomes a sibling INFO under the
// parent topic with the Random flag. `responses` are its spoken line(s); `conditions` are its OWN extra
// CTDA gates (appended after the parent's shared gate + conditions + templates + identity). `emotion`/
// `emotionValue` default to the parent's when unset. `sayOnce` marks a one-shot variant (a story beat said
// at most once). Pure line-variety — no result fragment / setStage (those live on the parent entry).
public sealed class DialogueVariantSpec
{
    public List<string> Responses { get; set; } = new();
    public List<ConditionSpec> Conditions { get; set; } = new();
    public string Emotion { get; set; } = "";       // "" → inherit the parent entry's emotion
    public uint? EmotionValue { get; set; }           // null → inherit the parent entry's emotionValue
    public bool SayOnce { get; set; }
}
public sealed class DialogueSetGlobalSpec
{
    public string Global { get; set; } = "";   // ref -> GLOB
    public float? Value { get; set; }           // absolute SetValue
    public float? Delta { get; set; }           // relative Mod (counter increment/decrement)
}
// PROACTIVE banter — a line the NPC says UNPROMPTED (no player menu), the vanilla follower-comment
// pattern (see Skyrim.esm `HirelingIdles` 0x055DEB). All banter entries that share a (speaker, quest)
// are grouped into ONE ambient topic: Category=Misc, SNAM='IDLE', no branch, each entry an INFO with
// the Random flag so the engine random-picks among those whose `conditions` currently pass. The line
// only surfaces while the NPC has idle chatter enabled (an AI package with the AllowIdleChatter
// interrupt flag — e.g. a Sandbox package, or the vanilla follow package). Use `conditions` to make it
// situational (GetCurrentTime for night, IsInInterior, GetActorValuePercent for "I'm hurt", and the
// CurrentFollowerFaction gate for follower-only banter). Each entry's `responses` are spoken as one
// comment (multiple lines play in sequence). NOTE: this is ambient/idle banter — true *combat* shouts
// use a different subtype (Taunt/Attack), not yet supported.
public sealed class BanterSpec
{
    public string EditorId { get; set; } = "";          // optional — names the INFO group for diag/uniqueness
    public string QuestEditorId { get; set; } = "";       // host quest (must be StartGameEnabled, like dialogue)
    public string SpeakerNpcEditorId { get; set; } = ""; // who says it (auto GetIsID gate)
    public List<string> Responses { get; set; } = new(); // the spoken line(s) for this one comment
    public string Emotion { get; set; } = "Neutral";
    public uint EmotionValue { get; set; } = 50;
    public List<ConditionSpec> Conditions { get; set; } = new();  // situational gates (beyond the auto speaker gate)
}
// A CTDA condition (a static gate) usable on a dialogue INFO or an AI package. `function` picks the
// condition function; `param` is its form argument (a ref → faction/item/global/quest/npc); `comparison`
// + `value` are the numeric test; `runOn`/`reference` pick WHOSE value is read (Subject = the
// speaker/package owner; Reference = a named ref such as the player 0x14). `or` OR-chains with the next.
// Supported functions: GetInFaction, GetItemCount, GetGlobalValue, GetStage, GetIsID, GetRelationshipRank,
// GetActorValue / GetActorValuePercent (use `actorValue` instead of `param`; Percent is a 0..1 fraction),
// and the no-argument situational gates GetCurrentTime (game hour 0..24), IsInInterior, IsInCombat,
// GetRandomPercent (0..99 roll, for line variety).
public sealed class ConditionSpec
{
    public string Function { get; set; } = "";
    public string Comparison { get; set; } = ">=";   // == | != | > | >= | < | <=  (also accepts EqualTo/GreaterThan/… names)
    public float Value { get; set; }
    public string Param { get; set; } = "";           // the function's form argument (a ref — faction/item/global/perk/keyword/npc/race/…)
    public string ActorValue { get; set; } = "";       // the ActorValue name for GetActorValue/GetBaseActorValue (e.g. WaitingForPlayer, Destruction)
    public string ItemType { get; set; } = "";         // CastSource for GetEquippedItemType (Left | Right | Voice | Instant)
    public string RunOn { get; set; } = "Subject";     // Subject | Target | Reference | CombatTarget | ...
    public string Reference { get; set; } = "";        // the ref read when RunOn=Reference (e.g. player Skyrim.esm:0x000014)
    public string Alias { get; set; } = "";            // the alias NAME for GetIsAliasRef (resolved to the owning quest's alias index)
    public int Stage { get; set; } = -1;               // the stage INDEX for GetStageDone (a param, not the comparison value); -1 = unset
    // IsSceneActionComplete: the SCENE whose action is tested (a scene editorId ref; omit on a scene
    // completion/start condition to default to the OWNING scene) + the action's index in that scene.
    public string Scene { get; set; } = "";
    public int SceneActionIndex { get; set; } = -1;
    // GetVMQuestVariable / GetVMScriptVariable: the Papyrus property name read off the attached script
    // (e.g. ITH's quest-script "PlayerInDialogue"). `param` carries the quest (VM-quest) or object
    // whose script is read (VM-script); this is the property/variable string.
    public string VariableName { get; set; } = "";
    public bool Or { get; set; }                        // OR with the NEXT condition (default AND)
}
// A named, reusable condition block (M組). Referenced by a dialogue line's `useConditionTemplates`
// (by `name`); its `conditions` are appended to that INFO exactly like inline conditions. Lets many
// INFOs share one gate set (e.g. ambient-commentary lines all gated on the same location/state).
public sealed class ConditionTemplateSpec
{
    public string Name { get; set; } = "";
    public List<ConditionSpec> Conditions { get; set; } = new();
}
// Attach a compiled Papyrus script (by Scriptname) to a record (by editorId), with
// typed properties. type ∈ int|float|bool|string|object; object resolves ObjectEditorId.
public sealed class ScriptAttachSpec
{
    public string TargetEditorId { get; set; } = "";
    public string ScriptName { get; set; } = "";
    public string Source { get; set; } = "";   // optional .psc path (rel. to spec) for `package` to compile
    public List<PropertySpec> Properties { get; set; } = new();
}
public sealed class PropertySpec
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public int Int { get; set; }
    public float Float { get; set; }
    public bool Bool { get; set; }
    public string Str { get; set; } = "";
    public string ObjectEditorId { get; set; } = "";
}
