namespace ModForge;

// --- Scene (SCEN): two-or-more NPCs talking to / performing for EACH OTHER (not the player) ---
// Split out of Spec.Dialogue.cs (the SceneSpec family); shares ConditionSpec (in Spec.Dialogue.cs).

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
// One non-dialog scene beat (a SceneAction of Type Package or Timer, OR — for `idle`/`setStage` — a
// SceneAdapter phase fragment; the spoken phases emit the Dialog actions automatically).
// EXACTLY ONE of (checked in this order):
//   * `idle` (a ref → an IDLE record) → a PLAYIDLE action: actor `actor` plays that idle animation when
//     phase `startPhase` begins (kneel/pray/gesture…), then returns to AI naturally. Implemented NOT as
//     a SceneAction but as a SceneAdapter per-phase begin fragment on the SCEN (`SF_<scene>.Fragment_N`
//     calling `<alias>.GetActorRef().PlayIdle(<idle>)`); `package` compiles + attaches it. Decoded from
//     vanilla SF_BardSongsBallad01Scene / SF_MQ201EscapeScene.
//   * `setStage` → a restricted phase-begin fragment that calls Quest.SetStage(stage). `quest` may be
//     omitted to target this Scene's host quest. This deliberately does not expose arbitrary Papyrus.
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
    public SceneSetStageSpec? SetStage { get; set; }  // phase-begin Quest.SetStage; null = none
    public string Package { get; set; } = "";       // ref → a PACK (spec packages[] editorId or <master>:0xID)
    public float TimerSeconds { get; set; }          // > 0 → a Timer action instead of a Package action
    public int StartPhase { get; set; }              // first phase index (into phases[]) the action spans
    public int EndPhase { get; set; } = -1;          // last phase index; -1 = same as StartPhase
}

public sealed class SceneSetStageSpec
{
    public string Quest { get; set; } = "";          // empty = the Scene's host quest; otherwise quest ref
    public int Stage { get; set; } = -1;              // sentinel distinguishes omitted from valid stage 0
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
    public string Emotion { get; set; } = "Neutral";        // Neutral|Anger|Disgust|Fear|Sad|Happy|Surprise|Puzzled
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

// The ModSpec fields that carry the DTOs above.
public sealed partial class ModSpec
{
    public List<SceneSpec> Scenes { get; set; } = new();
}
