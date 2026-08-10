Scriptname MFLivingNpcAlias extends ReferenceAlias
{ living-adventurers P1 — one living NPC's state + behaviour, held on a quest reference alias.
  The alias fill resolves the actor (in-spec placed ref, or an external follower's unique ref).
  archetype is a small int → a fixed branch in this script; adding a NEW NPC of an EXISTING
  archetype is pure data (a new alias + markers, no code). Per-NPC abstract progress is the
  DeedCount global; the shared MFLivingWorldController drives AdvanceSim()/Presence() on a single
  roster loop.

  NOTE on Markers: alias scriptProperties can't resolve a placement REFR by editorId (the placement
  isn't in the formKey table yet when aliases build), but it CAN resolve a FormList. So both the
  off-stage hold marker AND the anchors are passed as ONE FormList: index 0 = hold marker, indices
  1..N = the anchors this NPC frequents. (P2 should defer alias-prop resolution in core and pass
  these as plain ObjectReference props — see design.md gap note.) }

Int Property Archetype = 0 Auto
{ 0 = adventurer, 1 = mageApprentice (extend the switch for new life-types). }
FormList Property Markers Auto
{ index 0 = off-stage hold marker; indices 1..N = anchors (places he appears). }
GlobalVariable Property DeedCount Auto
{ Per-NPC abstract-sim progress; tavern rumor dialogue gates on GetGlobalValue >= N. }
Float Property ReclaimDelaySeconds = 30.0 Auto
{ Minimum real-time grace after the player loses sight of a dismissed follower. }
Float Property ReclaimDistance = 8192.0 Auto
{ While the dismissed actor's 3D is loaded within this distance, keep letting the follower package walk. }

int anchorIdx = 0
bool atAnchor = false
bool releasedToPlayer = false
Cell releasedInCell = None
float reclaimEligibleAt = 0.0

Event OnInit()
    Actor a = GetReference() as Actor
    ObjectReference hold = HoldMarker()
    if a && a.IsPlayerTeammate()
        releasedToPlayer = true          ; an existing save may enable this mod while the NPC is recruited
        releasedInCell = Game.GetPlayer().GetParentCell()
        reclaimEligibleAt = Utility.GetCurrentRealTime() + ReclaimDelaySeconds
    elseif a && hold
        a.MoveTo(hold)                   ; start off-stage
    endif
EndEvent

; --- Layer 1: abstract ghost-sim (no actor processed) ---
Function AdvanceSim()
    Actor a = GetReference() as Actor
    if a && a.IsPlayerTeammate()
        releasedToPlayer = true          ; recruited followers live with the player, not in the abstract sim
        releasedInCell = Game.GetPlayer().GetParentCell()
        reclaimEligibleAt = Utility.GetCurrentRealTime() + ReclaimDelaySeconds
        return
    elseif releasedToPlayer
        ; Dismissed but still visible to the player: keep the abstract clock paused until Presence()
        ; can reclaim the actor off-screen.
        return
    endif
    if DeedCount
        DeedCount.Mod(1.0)               ; "completed a contract" / "studied" — pure data
    endif
    int n = AnchorN()
    if n > 0
        anchorIdx = (anchorIdx + 1) % n  ; he moves on to his next haunt
    endif
    string nm = ""
    if a
        nm = a.GetDisplayName()
    endif
    Debug.Notification(nm + " " + ArchetypeVerb())
EndFunction

; --- Layer 2: materialize on co-location with the player ---
Function Presence()
    Actor a = GetReference() as Actor
    if a == None
        return
    endif
    if a.IsPlayerTeammate()
        releasedToPlayer = true          ; the follower system owns movement/packages while recruited
        releasedInCell = Game.GetPlayer().GetParentCell()
        reclaimEligibleAt = Utility.GetCurrentRealTime() + ReclaimDelaySeconds
        atAnchor = false
        return
    endif
    if releasedToPlayer
        ; Let the follower mod's dismissed package visibly walk the actor across doors/cells. Losing
        ; the actor for one poll is not enough: the player may immediately follow through the door.
        Actor player = Game.GetPlayer()
        Cell playerCell = player.GetParentCell()
        float now = Utility.GetCurrentRealTime()
        bool playerCanStillFollow = (playerCell == releasedInCell || playerCell == a.GetParentCell())
        if !playerCanStillFollow && a.Is3DLoaded() && a.GetDistance(player) <= ReclaimDistance
            playerCanStillFollow = true
        endif
        if playerCanStillFollow
            reclaimEligibleAt = now + ReclaimDelaySeconds
            return
        endif
        ; GetCurrentRealTime restarts with the process. A saved deadline from a previous run must not
        ; strand the actor forever; reset implausibly far-future values to one fresh grace period.
        if reclaimEligibleAt <= 0.0 || reclaimEligibleAt > now + (ReclaimDelaySeconds * 2.0)
            reclaimEligibleAt = now + ReclaimDelaySeconds
        endif
        if now < reclaimEligibleAt
            return
        endif
        ObjectReference releasedHold = HoldMarker()
        if releasedHold
            a.MoveTo(releasedHold)
        endif
        releasedToPlayer = false
        releasedInCell = None
        reclaimEligibleAt = 0.0
        atAnchor = false
    endif
    ObjectReference target = CurrentAnchor()
    bool playerHere = (target != None && Game.GetPlayer().GetParentCell() == target.GetParentCell())
    if playerHere && !atAnchor
        a.MoveTo(target)                 ; bring him on-stage to where he currently "is"
        a.EvaluatePackage()              ; kick his sandbox package so he doesn't just stand
        atAnchor = true
    elseif !playerHere && atAnchor
        ObjectReference hold = HoldMarker()
        if hold
            a.MoveTo(hold)               ; player left → send him off-stage
        endif
        atAnchor = false
    endif
EndFunction

ObjectReference Function HoldMarker()
    if Markers == None || Markers.GetSize() < 1
        return None
    endif
    return Markers.GetAt(0) as ObjectReference
EndFunction

int Function AnchorN()
    if Markers == None
        return 0
    endif
    int sz = Markers.GetSize() - 1       ; minus the hold marker at index 0
    if sz < 0
        return 0
    endif
    return sz
EndFunction

ObjectReference Function CurrentAnchor()
    int n = AnchorN()
    if n <= 0
        return None
    endif
    return Markers.GetAt(1 + (anchorIdx % n)) as ObjectReference
EndFunction

string Function ArchetypeVerb()
    if Archetype == 1
        return "pores over a tome at the College."
    endif
    return "completed another contract."
EndFunction
