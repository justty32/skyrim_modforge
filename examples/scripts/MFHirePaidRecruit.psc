Scriptname MFHirePaidRecruit Extends TopicInfo Hidden

; It.28 — the result fragment for a CUSTOM paid-recruit dialogue line.
;
; The vanilla paid-hireling recruit (HirelingQuestTopic1) can't recruit a custom NPC: its recruit
; INFOs are each hardcoded GetIsID==<a specific vanilla mercenary> (probe: `modforge infodiag
; Skyrim.esm 0x0BCC84`). So we author our OWN topic (gated on GetIsID==our speaker, which always
; passes) and do the gold transaction + follower-join here, in Papyrus, where control flow belongs.
;
; Joining = AddToFaction(CurrentFollowerFaction 0x05C84E) + SetPlayerTeammate(true). That faction is
; what the GENERIC vanilla follow/wait/dismiss dialogue keys on (dismiss INFO[17] of 0x05C80C gates
; only on CurrentFollowerFaction + follower voice + PotentialHireling==0 — no GetIsID), so once joined
; the whole vanilla command suite works for free.
;
; Properties are bound by ModForge from the spec's resultProperties (no Game.GetForm guesswork).

MiscObject Property Gold001 Auto                 ; Skyrim.esm:0x00000F
Faction    Property CurrentFollowerFaction Auto  ; Skyrim.esm:0x05C84E
Int        Property GoldCost Auto                ; 500
Int        Property RelRank Auto                 ; 3 = Ally

Function Fragment_0(ObjectReference akSpeakerRef)
    Actor speaker = akSpeakerRef as Actor
    Actor player = Game.GetPlayer()
    If speaker == None || player == None
        Return
    EndIf
    If speaker.IsPlayerTeammate()
        Return   ; already following — line is cosmetic at this point
    EndIf
    If player.GetItemCount(Gold001) < GoldCost
        Debug.Notification("You need " + GoldCost + " gold to hire her.")
        Return
    EndIf
    player.RemoveItem(Gold001, GoldCost)
    speaker.SetRelationshipRank(player, RelRank)
    speaker.AddToFaction(CurrentFollowerFaction)
    speaker.SetPlayerTeammate(true)
    speaker.EvaluatePackage()
    Debug.Notification("She joins you as a follower.")
EndFunction
