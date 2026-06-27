Scriptname MFLivingNpcAlias extends ReferenceAlias
{ living-adventurers (Idea #23) — one living NPC's state + behaviour, held on a quest reference alias.
  The alias fill resolves the actor (an in-spec placed ref, or an external follower's unique ref). The
  shared MFLivingWorldController drives AdvanceSim()/Presence() on a single roster loop.

  Two layers (the abstract-ghost-sim + materialize architecture):
   - AdvanceSim() advances off-stage progress (DeedCount) and rotates which anchor he "is" at — no actor
     is processed while the player is elsewhere.
   - Presence() teleports the persistent ref onto the current anchor when the player is co-located, and
     back to the off-stage HoldMarker when not.

  archetype is a small int → a fixed branch here; adding a NEW NPC of an EXISTING archetype is pure data
  (the livingNpcs: macro emits another alias + markers, no code). One prebuilt .pex serves every mod;
  the macro wires the properties (HoldMarker/Anchors are object props the macro binds to placements). }

Int Property Archetype = 0 Auto
{ 0 = adventurer, 1 = mageApprentice, 2 = merchant, 3 = herbalist, 4 = priest, 5 = bandit(hostile). }
ObjectReference Property HoldMarker Auto
{ Off-stage holding spot; the NPC sits here, frozen/unprocessed, while the player is elsewhere. }
FormList Property Anchors Auto
{ ObjectReference markers this NPC frequents; AdvanceSim rotates through them, Presence materialises at one. }
GlobalVariable Property DeedCount Auto
{ Per-NPC abstract-sim progress; tavern rumor dialogue gates on GetGlobalValue >= N. }

int anchorIdx = 0
bool atAnchor = false

Event OnInit()
    Actor a = GetReference() as Actor
    if a && HoldMarker
        a.MoveTo(HoldMarker)             ; start off-stage
    endif
EndEvent

; --- Layer 1: abstract ghost-sim (no actor processed) ---
Function AdvanceSim()
    if DeedCount
        DeedCount.Mod(1.0)               ; "completed a contract" / "studied" / "ran a trade route" — pure data
    endif
    int n = AnchorN()
    if n > 0
        anchorIdx = (anchorIdx + 1) % n  ; he moves on to his next haunt
    endif
    Actor a = GetReference() as Actor
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
    ObjectReference target = CurrentAnchor()
    bool playerHere = (target != None && Game.GetPlayer().GetParentCell() == target.GetParentCell())
    if playerHere && !atAnchor
        a.MoveTo(target)                 ; bring him on-stage to where he currently "is"
        a.EvaluatePackage()              ; kick his sandbox package so he doesn't just stand
        atAnchor = true
    elseif !playerHere && atAnchor
        if HoldMarker
            a.MoveTo(HoldMarker)         ; player left → send him off-stage
        endif
        atAnchor = false
    endif
EndFunction

int Function AnchorN()
    if Anchors == None
        return 0
    endif
    return Anchors.GetSize()
EndFunction

ObjectReference Function CurrentAnchor()
    int n = AnchorN()
    if n <= 0
        return None
    endif
    return Anchors.GetAt(anchorIdx % n) as ObjectReference
EndFunction

string Function ArchetypeVerb()
    if Archetype == 1
        return "pores over a tome at the College."
    elseif Archetype == 2
        return "turned a tidy profit on the road."
    elseif Archetype == 3
        return "gathered rare reagents in the wild."
    elseif Archetype == 4
        return "kept the shrine's vigil."
    elseif Archetype == 5
        return "pulled off another raid."
    endif
    return "completed another contract."
EndFunction
