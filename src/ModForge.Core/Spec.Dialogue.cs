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
// SCENE (SCEN) — two (or more) NPCs talking to EACH OTHER, not to the player. A vanilla Scene is
// hosted by a quest, its participants are the quest's ALIASES (not direct NPC refs), and it runs an
// ordered list of PHASES; in each phase one actor speaks a line via a Dialog ACTION that points at a
// Scene-subtype DialogTopic (Category=Scene, SNAM='SCEN') carrying the spoken response. ModForge emits
// the whole chain from this one spec entry: the host quest's QuestAliases (each `UniqueActor`-bound to
// the named NPC), the Scene's SceneActors (referencing those alias indices), one SceneePhase + one
// Dialog SceneAction + one Scene/SCEN DialogTopic+INFO per `phases[]` line.
//
// Authoring: name a `questEditorId` (a StartGameEnabled quest in this spec — the scene loads/plays only
// while its host quest runs, exactly like player dialogue), list the `actors` (each = an `aliasId` index
// + the `npc` editorId that fills it), then the ordered `phases` (each = which actor `speaker`s, the
// `lines` they say, and an `emotion`). The two NPCs must be PLACED near each other (a `placements[]`
// entry per NPC into the same cell) for the conversation to actually fire in-game.
public sealed class SceneSpec
{
    public string EditorId { get; set; } = "";
    public string QuestEditorId { get; set; } = "";        // host quest (must exist in spec; StartGameEnabled so the scene runs)
    public List<SceneActorSpec> Actors { get; set; } = new();
    public List<ScenePhaseSpec> Phases { get; set; } = new();
    // BeginOnQuestStart (default true): the scene auto-plays the moment its host quest starts — the
    // simplest "two NPCs chat on game load" trigger. StopQuestOnEnd stops the host quest when the
    // scene finishes (vanilla one-shot conversation scenes set both). Turn BeginOnQuestStart off to
    // trigger the scene from a script/package instead.
    public bool BeginOnQuestStart { get; set; } = true;
    public bool StopQuestOnEnd { get; set; }
    // Presence-gated AUTO-START (隨從在場偵測 + 互動 Scene). When set, the Scene does NOT auto-play on
    // quest start; instead the reusable `MFSceneBanterController` Papyrus script is attached to the host
    // quest and polls (chained RegisterForSingleUpdate): when the player is within range of BOTH actors
    // (+ optional LOS, not in combat, neither dead) and the cooldown has elapsed, it calls Scene.Start().
    // Followers stay near the player, so this fires repeatedly while travelling — the usable form of
    // "follower banter". Host quest must be StartGameEnabled (so the controller's OnInit arms).
    public SceneAutoStartSpec? AutoStart { get; set; }
    // Non-dialog scene beats (IDEAS §1b "NPC 劇情演出"): movement / timed pauses interleaved with the
    // spoken phases, so the scene becomes a visible performance (walk to a spot → wait → talk). Each
    // action runs an actor's AI package or a timer over a window of phase indices (see SceneActionSpec).
    // A phase referenced by an action may have empty `lines` (a pure BEAT phase — no spoken line).
    public List<SceneActionSpec> Actions { get; set; } = new();
    // Scene-level CTDA gate (optional): the scene only STARTS if ALL of these pass. e.g. gate an
    // autoStart/begin-on-quest-start banter on a GLOB flag (GetGlobalValue) or a quest stage
    // (GetStage). Uses the SHARED ConditionSpec / BuildCondition, wired in pass 2 (refs by editorId).
    public List<ConditionSpec> Conditions { get; set; } = new();
}
// One non-dialog scene beat (a SceneAction of Type Package or Timer, OR — for `idle` — a SceneAdapter
// phase fragment, no SceneAction at all; the spoken phases emit the Dialog actions automatically).
// EXACTLY ONE of (checked in this order):
//   * `idle` (a ref → an IDLE record) → a PLAYIDLE action: actor `actor` plays that idle animation when
//     phase `startPhase` begins (kneel/pray/gesture…), then returns to AI naturally. Implemented NOT as
//     a SceneAction but as a SceneAdapter per-phase begin fragment on the SCEN (`SF_<scene>.Fragment_N`
//     calling `<alias>.GetActorRef().PlayIdle(<idle>)`); `package` compiles + attaches it. Decoded from
//     vanilla SF_BardSongsBallad01Scene / SF_MQ201EscapeScene.
//   * `package` (a ref → an AI package: a `packages[]` entry in this spec, or an external
//     `<master>:0xFORMID`) → a PACKAGE action: actor `actor` runs that PACK across the phase window.
//     Movement = a Travel package whose destination is a placed marker; ambient activity = a Sandbox
//     package; etc. Decoded from vanilla dunTolvaldsCaveCrownScene / BardSongs* scenes.
//   * `timerSeconds` > 0 → a TIMER action: the scene waits this many seconds over the phase window
//     (no actor behaviour). Used between beats (vanilla bard scenes pause this way).
// The phase window is `startPhase`..`endPhase` (indices into `phases[]`); `endPhase` -1 = startPhase.
public sealed class SceneActionSpec
{
    public int Actor { get; set; }                  // aliasId (from actors[]) that performs the action
    public string Idle { get; set; } = "";           // ref → an IDLE record; non-empty = a PlayIdle action
                                                     // (SceneAdapter phase fragment, NOT a SceneAction)
    public string Package { get; set; } = "";       // ref → a PACK (spec packages[] editorId or <master>:0xID)
    public float TimerSeconds { get; set; }          // > 0 → a Timer action instead of a Package action
    public int StartPhase { get; set; }              // first phase index (into phases[]) the action spans
    public int EndPhase { get; set; } = -1;          // last phase index; -1 = same as StartPhase
}
// Tuning for a presence-gated Scene (see SceneSpec.AutoStart). All distances in game units, times in
// REAL seconds (timescale-independent). Defaults match a comfortable travelling-banter cadence.
public sealed class SceneAutoStartSpec
{
    public float TriggerDistance { get; set; } = 2048f;   // max distance from the player to EACH actor
    public bool RequireLineOfSight { get; set; }           // also require the player HasLOS both actors
    public float CooldownSeconds { get; set; } = 60f;      // min real seconds between plays
    public float PollSeconds { get; set; } = 5f;           // RegisterForSingleUpdate interval
    // When true, the controller makes the two actors fight each other (StartCombat both ways) the
    // moment the scene's dialogue finishes — "they come to blows after the argument". For a NON-lethal
    // tavern brawl mark the actors `essential` so the loser drops to bleedout instead of dying.
    public bool BrawlOnEnd { get; set; }

    // --- replay policy (controls WHEN/HOW OFTEN the presence gate re-fires; all AND with cooldown) ---
    // PlayOnce: play at most once ever (the controller stops polling after the single play). Use for a
    // one-shot encounter that should not loop.
    public bool PlayOnce { get; set; }
    // PlayHour: only play when the in-game hour is within ±PlayHourTolerance of this (0..24, circular).
    // -1 (default) = any time. e.g. PlayHour=12 → only around noon. Independent of the real-time cooldown.
    public float PlayHour { get; set; } = -1f;
    public float PlayHourTolerance { get; set; } = 1f;   // ± hours window around PlayHour
    // GateGlobal: a ref → a GlobalVariable (GLOB) used as a re-arm TOKEN. The scene only plays while the
    // global == 0; the controller SetValue(1) right after playing. Some OTHER generated content
    // (a dialogue result, quest fragment, alias script, another event) SetValue(0) to re-enable it.
    // This is the general "play once until something resets it" mechanism (build the GLOB in globals[]).
    public string GateGlobal { get; set; } = "";
}
// One participant in a scene: an alias INDEX (unique within the host quest, ≥0) plus the NPC that fills
// it. The alias is emitted on the host quest and `UniqueActor`-bound to `npc` (a ref → an in-spec NPC or
// a vanilla `<master>:0xFORMID`); the Scene's SceneActor references this `aliasId`.
public sealed class SceneActorSpec
{
    public int AliasId { get; set; }                        // alias index in the host quest (unique, ≥0)
    public string Npc { get; set; } = "";                   // ref → the NPC that fills this alias
    public string Name { get; set; } = "";                  // optional alias name (defaults to the npc editorId)
}
// One phase of a scene: actor `speaker` (an `aliasId` from `actors`) says `lines` (one or more spoken
// strings, played in sequence) with `emotion`/`emotionValue`. Phases play in list order.
public sealed class ScenePhaseSpec
{
    public int Speaker { get; set; }                        // the aliasId (from actors[]) who speaks this phase
    public List<string> Lines { get; set; } = new();        // the spoken line(s) for this phase
    public string Emotion { get; set; } = "Neutral";        // Neutral|Anger|Disgust|Fear|Sad|Happy|Surprise
    public uint EmotionValue { get; set; } = 50;            // 0..100 intensity
    // HEADTRACK / FACING — where the speaking actor's gaze points during this phase (the SceneAction's
    // HeadtrackActorID + FaceTarget/HeadtrackPlayer flags). headtrackActor: an actor `aliasId` = look at
    // that actor; -1 = look at no one (no headtrack); -2 (default) = the OTHER actor — the current
    // two-NPC default behavior. headtrackPlayer (default false): if true the speaker headtracks the
    // PLAYER (sets the HeadtrackPlayer flag, leaves HeadtrackActorID null) — mutually exclusive with a
    // non-default headtrackActor. faceTarget (default null = true): whether the FaceTarget flag is set;
    // explicit true/false overrides the default.
    public int HeadtrackActor { get; set; } = -2;
    public bool HeadtrackPlayer { get; set; }
    public bool? FaceTarget { get; set; }
    // Per-phase CTDA gates (optional). `startConditions`: the phase only PLAYS if all pass (else the
    // engine skips straight to the next phase). `completionConditions`: the phase ENDS once all pass
    // (a condition-driven advance, beyond "advance when the line finishes"). Both use the SHARED
    // ConditionSpec / BuildCondition and are wired in pass 2.
    public List<ConditionSpec> StartConditions { get; set; } = new();
    public List<ConditionSpec> CompletionConditions { get; set; } = new();
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
