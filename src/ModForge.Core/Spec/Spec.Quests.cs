namespace ModForge;

// --- Quest / stage / objective / spawn spec DTOs (the host records dialogue & scenes hang on) ---

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
    // Optional dynamic spawn (F組 #3): on quest start, spawn N of `form` near the player on valid
    // navmesh. Attaches the reusable MFDynamicSpawn quest script. See SpawnSpec.
    public SpawnSpec? Spawn { get; set; }
}
// Dynamic near-player spawn (F組 #3 — the EE NavmeshTester trick, self-snapping form). On quest start
// the MFDynamicSpawn script places `count` copies of `form` (an ActorBase or LeveledNpc) at a random
// offset (minDistance..maxDistance units) around the player, then toggles EnableAI so each snaps to the
// nearest navmesh point — a legal, walkable spawn with no pre-placed cell markers. The trigger is the
// owning quest starting (Story-Manager-launched or StartGameEnabled); pair with locationFilter/cooldownHours
// for a rate-limited, location-aware encounter.
public sealed class SpawnSpec
{
    public string Form { get; set; } = "";          // ref → ActorBase (NPC_) or LeveledNpc (LVLN) to spawn
    public int Count { get; set; } = 1;             // how many to spawn (>=1)
    public float MinDistance { get; set; } = 1500f; // nearest spawn offset from the player (units)
    public float MaxDistance { get; set; } = 4000f; // farthest spawn offset
    public bool SnapToNavmesh { get; set; } = true; // EnableAI toggle to snap each spawn to navmesh
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
    // JContainers JFormDB writes / perk sync performed when the quest REACHES this stage (the stage
    // fragment, extends Quest — Idea #20 Phase 0). A stage has no `akSpeakerRef`, so the key must be
    // "player" or an arbitrary ref (NOT "speaker"). Same shape as the dialogue-line persist/syncPerks
    // (see Generator.JContainers.cs); use it to bank state on a stage milestone rather than a dialogue pick.
    public PersistSpec? Persist { get; set; }
    public SyncPerksSpec? SyncPerks { get; set; }
    // Globals to bind to THIS quest instance when this stage runs (gather/count radiant quests). The
    // generated stage fragment calls UpdateCurrentInstanceGlobal(<global>) so objective text like
    // "<Global=ItemTotal> bandits" shows per-instance numbers — letting one quest template run many
    // times at once with different counts. Optionally seeds each global first (random range / fixed
    // value). Decoded from Missives' StartUpStage fragment (SetValue(Utility.RandomInt) + Update…).
    public List<InstanceGlobalSpec> InstanceGlobals { get; set; } = new();
    // Plain GlobalVariable writes performed when the quest REACHES this stage (K組): the generated stage
    // fragment emits `<global>.SetValue(value)`. First-class spec sugar for "set a flag/counter global on
    // a stage milestone" — previously only doable by hand-writing a fragment or via a dialogue TIF. Unlike
    // `instanceGlobals` this does NOT call UpdateCurrentInstanceGlobal (it's a global write, not an
    // instance binding). For an SM-driven quest the write runs in the OnStory<Event> handler (the stage
    // fragment doesn't fire for SM quests — in-game 2026-06-19), same routing as persist.
    public List<GlobalWriteSpec> GlobalWrites { get; set; } = new();
    // PapyrusUtil StorageUtil per-Form KV writes performed when the quest REACHES this stage (J組). The
    // generated stage fragment emits StorageUtil.Set/Adjust{Int,Float,String}Value. A stage has no
    // akSpeakerRef, so `target` must be "player" or "none"/"global" (NOT "speaker"). Lightweight, save-
    // managed counterpart to `persist` (JContainers JFormDB); see StorageWriteSpec / Generator.StorageWrites.cs.
    public List<StorageWriteSpec> StorageWrites { get; set; } = new();
}
// One plain global write in a stage fragment (see StageSpec.GlobalWrites). `value` is required.
public sealed class GlobalWriteSpec
{
    public string Global { get; set; } = "";       // GLOB editorId (declare in spec.globals) or a vanilla ref
    public float Value { get; set; }               // → <global>.SetValue(value)
}
// One global bound to the quest instance in a stage fragment (see StageSpec.InstanceGlobals).
public sealed class InstanceGlobalSpec
{
    public string Global { get; set; } = "";       // GLOB editorId (declare in spec.globals) or a vanilla ref
    public int? RandomMin { get; set; }             // with RandomMax → SetValue(Utility.RandomInt(min,max)) first
    public int? RandomMax { get; set; }
    public float? Value { get; set; }               // else if set → SetValue(value) first
    // RandomMin/Max and Value both unset → bind only (UpdateCurrentInstanceGlobal without changing value).
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

// The ModSpec fields that carry the DTOs above.
public sealed partial class ModSpec
{
    public List<QuestSpec> Quests { get; set; } = new();
}
