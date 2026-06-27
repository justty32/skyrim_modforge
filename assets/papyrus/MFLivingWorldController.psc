Scriptname MFLivingWorldController extends Quest
{ living-adventurers P1 — the shared world controller (one per generated mod).
  Attached to a startGameEnabled host quest whose reference aliases each hold one living NPC
  (MFLivingNpcAlias). Runs ONE game-time tick and ONE real-time presence poll for the whole
  roster — cost does NOT scale per-NPC the way per-alias RegisterForSingleUpdate would.

  Layer 1 (abstract ghost-sim): OnUpdateGameTime → every alias advances its off-stage progress.
  Layer 2 (materialize): OnUpdate → every alias teleports its NPC in/out based on co-location.
  Chained single-update idiom (no persistent OnUpdate loop → no save bloat), per MFSceneBanterController. }

Float Property SimIntervalHours = 4.0 Auto
{ In-game hours between abstract deeds for every living NPC. }
Float Property PollInterval = 5.0 Auto
{ Real seconds between presence checks. }
Int Property AliasCount = 0 Auto
{ How many living-NPC aliases (indices 0..AliasCount-1) this quest carries. }

Event OnInit()
    RegisterForSingleUpdateGameTime(SimIntervalHours)
    RegisterForSingleUpdate(PollInterval)
EndEvent

Event OnUpdateGameTime()
    int i = 0
    while i < AliasCount
        MFLivingNpcAlias a = GetAlias(i) as MFLivingNpcAlias
        if a
            a.AdvanceSim()
        endif
        i += 1
    endwhile
    RegisterForSingleUpdateGameTime(SimIntervalHours)
EndEvent

Event OnUpdate()
    int i = 0
    while i < AliasCount
        MFLivingNpcAlias a = GetAlias(i) as MFLivingNpcAlias
        if a
            a.Presence()
        endif
        i += 1
    endwhile
    RegisterForSingleUpdate(PollInterval)
EndEvent
