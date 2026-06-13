namespace ModForge;

// --- Dialogue: quests, topics, CTDA conditions, and Papyrus script attachment -----------

public sealed class QuestSpec
{
    public string EditorId { get; set; } = "";
    public string Name { get; set; } = "";
    public List<ObjectiveSpec> Objectives { get; set; } = new();
    // Quest STAGES (QSDT). A stage is an integer milestone (10/20/30…) the quest can be SET to. Each
    // stage optionally writes a journal LOG ENTRY (QLOG text) and can carry a CompleteQuest/FailQuest
    // flag (closes/fails the quest when reached). Objectives display/complete as a stage is set —
    // wire that with the objective's `showStage`/`completeStage`. Setting a stage at runtime is done
    // by a generated quest fragment script (see `package`); a dialogue line can set one via
    // `dialogue[].setStage`. Stage indices must be unique and ascending.
    public List<StageSpec> Stages { get; set; } = new();
    // StartGameEnabled (default true): the quest auto-starts on game load, which is REQUIRED for any
    // dialogue it hosts to be loaded/evaluated. A quest that never runs = its dialogue never surfaces.
    public bool StartGameEnabled { get; set; } = true;
    public byte Priority { get; set; } = 50;   // higher wins when multiple quests offer dialogue to the same NPC
    // Quest TYPE (DNAM) — which JOURNAL tab the quest groups under. CRITICAL: a quest with type=None
    // does NOT appear in the player's journal at all (it is a background/controller quest), so its
    // stage LOG ENTRIES never show and `setstage` produces no on-screen update. Leave empty for the
    // smart default: a quest that carries journal content (any objective, or a stage with log text)
    // defaults to "SideQuest" so it shows; a pure dialogue/controller quest (no objectives, no log
    // text) stays None to avoid cluttering the journal. Set explicitly to override:
    // None|MainQuest|MageGuild|ThievesGuild|DarkBrotherhood|CompanionQuests|Misc|Daedric|SideQuest|
    // CivilWar|Vampire|Dragonborn. "Misc" shows as a one-line Miscellaneous entry (no quest page);
    // "SideQuest" gives it its own page under Side Quests with objectives.
    public string Type { get; set; } = "";
    // Story Manager：宣告此 quest 可被某遊戲事件動態啟動（radiant 量產的底座）。有此塊時 build 會
    // 自動產生 SMBN→SMQN 把它掛到原版事件根下，並強制清除 StartGameEnabled（SM 啟動，不開局自跑）。
    public QuestStoryEventSpec? StoryEvent { get; set; }
    // SM 啟動時要填的 alias。fill="fromEvent:<slot>" 拿事件 ref，"forced:<ref>" 填寫死 ref。
    public List<QuestAliasSpec> Aliases { get; set; } = new();
}
// One quest stage (QSDT). `index` is the stage number (set with SetStage). `logEntry` (optional) is
// the journal text shown when the quest reaches this stage (a QuestLogEntry / QLOG). `completeQuest`
// (QuestLogEntry.Flag.CompleteQuest) closes the quest when this stage is reached; `failQuest` fails
// it. `conditions` (optional) gate WHICH log entry of the stage applies (CTDA on the QLOG) — the
// SHARED ConditionSpec / BuildCondition (e.g. function GetStage, param the quest ref).
public sealed class StageSpec
{
    public ushort Index { get; set; }
    public string LogEntry { get; set; } = "";
    public bool CompleteQuest { get; set; }
    public bool FailQuest { get; set; }
    // StartUpStage (QSDT "Start Up Stage" flag): the engine AUTO-runs SetStage to this stage the moment
    // the quest starts — no external SetStage needed. This is how a Story-Manager-triggered quest shows
    // its first journal log entry / displays its opening objective on start (the stage's log entry shows
    // engine-natively; an objective bound via `showStage` displays through the auto stage→objective
    // fragment). At most one start-up stage per quest. Without one, an SM-started quest sits silently at
    // stage 0 until something else sets a stage (a dialogue line, or an alias-script SetStage).
    public bool StartUpStage { get; set; }
    public List<ConditionSpec> Conditions { get; set; } = new();
}
// One quest objective (QOBJ). `index` is the objective number; `text` is the journal display text.
// `showStage`/`completeStage` (optional) link the objective to stages: the generated quest fragment
// SetObjectiveDisplayed at `showStage` and SetObjectiveCompleted at `completeStage`. -1 (the default)
// means "not stage-linked" — leave both unset for a static objective the quest script drives itself.
public sealed class ObjectiveSpec
{
    public ushort Index { get; set; }
    public string Text { get; set; } = "";
    public int ShowStage { get; set; } = -1;       // stage index that displays this objective (-1 = none)
    public int CompleteStage { get; set; } = -1;   // stage index that completes this objective (-1 = none)
    // QSTA targets: the alias(es) the compass/map arrow points at. The marker follows whatever the
    // alias is filled with at runtime — an actor (mark a person) or a location/ref (mark a place).
    // Several targets = several QSTA (vanilla "any of X/Y/Z"). Wired by WireObjectiveTargets once the
    // quest's aliases exist.
    public List<ObjectiveTargetSpec> Targets { get; set; } = new();
}
// One QSTA on an objective. `alias` is an alias NAME on the SAME quest (resolved to its alias index).
// `compassIgnoresLocks` sets the QSTA flag so the compass marker shows through locked doors.
// `conditions` are per-target CTDA gates (the marker only shows while they pass), built via the
// shared BuildCondition().
public sealed class ObjectiveTargetSpec
{
    public string Alias { get; set; } = "";
    public bool CompassIgnoresLocks { get; set; }
    public List<ConditionSpec> Conditions { get; set; } = new();
}
// A dialogue topic: shown under QuestEditorId's branch; targets SpeakerNpcEditorId (GetIsID).
public sealed class DialogueSpec
{
    public string EditorId { get; set; } = "";
    public string QuestEditorId { get; set; } = "";
    public string SpeakerNpcEditorId { get; set; } = "";
    public string Prompt { get; set; } = "";
    public List<string> Responses { get; set; } = new();
    public string Emotion { get; set; } = "Neutral";   // Neutral|Anger|Disgust|Fear|Sad|Happy|Surprise — applied to all response lines
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
    // Extra CTDA gates on the INFO (beyond the auto GetIsID speaker gate). e.g. only show a paid
    // recruit line when the player can afford it and isn't already following.
    public List<ConditionSpec> Conditions { get; set; } = new();
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
    public bool Or { get; set; }                        // OR with the NEXT condition (default AND)
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
