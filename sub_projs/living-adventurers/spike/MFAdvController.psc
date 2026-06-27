Scriptname MFAdvController extends Quest
{ living-adventurers SPIKE controller — proves the two-layer "abstract ghost-sim + materialize on
  co-location" loop with ONE named adventurer.

  Layer 1 (abstract sim): a game-time tick advances the adventurer's abstract progress (DeedCount)
  while he is OFF-STAGE — no actor is processed. This stands in for "went to a dungeon and cleared it".
  Layer 2 (materialize): a real-time presence poll teleports the SINGLE persistent adventurer ref into
  the inn when the player is there, and back to an off-stage holding marker when the player leaves.
  Because the cast is named (one persistent ref), materialization is a MoveTo in/out — NOT a LVLN
  spawn — so there is no spawn/despawn churn and no duplicate/zombie actors.

  Idiom mirrors MFSceneBanterController: chained RegisterForSingleUpdate (no persistent OnUpdate loop,
  which bloats saves). Tavern rumor is data-driven elsewhere: dialogue INFOs gated on DeedCount. }

Actor Property Adventurer Auto
{ The persistent adventurer ref (a PLACED ACHR, bound by objectEditorId — not the NPC_ base). }
ObjectReference Property InnMarker Auto
{ Where the adventurer is teleported to materialize when the player enters the inn. }
ObjectReference Property HoldMarker Auto
{ Off-stage holding spot; the adventurer sits here (frozen, unprocessed) while the player is elsewhere. }
GlobalVariable Property DeedCount Auto
{ Abstract-sim progress counter. The tick increments it; tavern rumor dialogue reads it via GetGlobalValue. }
Float Property SimIntervalHours = 2.0 Auto
{ In-game hours between abstract deeds. Tune; console `set MFLA_DeedCount to N` to test rumors instantly. }
Float Property PollInterval = 5.0 Auto
{ Real seconds between presence checks. }

bool atInn = false

Event OnInit()
    if Adventurer != None && HoldMarker != None
        Adventurer.MoveTo(HoldMarker)            ; start off-stage
    endif
    RegisterForSingleUpdateGameTime(SimIntervalHours)
    RegisterForSingleUpdate(PollInterval)
EndEvent

; --- Layer 1: abstract ghost-sim (runs whether or not the adventurer is loaded) ---
Event OnUpdateGameTime()
    AdvanceSim()
    RegisterForSingleUpdateGameTime(SimIntervalHours)
EndEvent

Function AdvanceSim()
    if DeedCount != None
        DeedCount.Mod(1.0)                       ; "completed another contract" — pure data, no actor
    endif
    Debug.Notification("Kjeld the Wanderer completed another contract.")
EndFunction

; --- Layer 2: materialize the named ref on co-location ---
Event OnUpdate()
    Presence()
    RegisterForSingleUpdate(PollInterval)        ; re-arm; no persistent loop
EndEvent

Function Presence()
    if Adventurer == None || InnMarker == None || HoldMarker == None
        return
    endif
    bool playerAtInn = (Game.GetPlayer().GetParentCell() == InnMarker.GetParentCell())
    if playerAtInn && !atInn
        Adventurer.MoveTo(InnMarker)             ; player just arrived → bring him on-stage
        atInn = true
    elseif !playerAtInn && atInn
        Adventurer.MoveTo(HoldMarker)            ; player left → send him off-stage
        atInn = false
    endif
EndFunction
