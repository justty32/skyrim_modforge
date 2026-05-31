Scriptname MFHirePaidRecruit Extends TopicInfo Hidden

; It.28/It.30 — result fragment for a CUSTOM paid-recruit dialogue line.
;
; Self-contained follower state: we do NOT touch the vanilla CurrentFollowerFaction / DialogueFollower
; quest, because that quest only dismisses followers IT registered — a manually-faction'd NPC gets the
; vanilla "you're dismissed" line but is never actually released (It.29 in-game bug). Instead an OWN
; faction (FollowerFaction = MF_PaidFollowerFaction) is the "is my follower" flag: the Follow package and
; the recruit/dismiss lines all gate on it, and MFHirePaidDismiss clears it. SetPlayerTeammate makes her
; fight for you and obey commands; the Follow package (gated on the faction) makes her physically trail.
;
; Properties are bound by ModForge from the spec's resultProperties.

MiscObject Property Gold001 Auto          ; Skyrim.esm:0x00000F
Faction    Property FollowerFaction Auto  ; in-spec MF_PaidFollowerFaction
Int        Property GoldCost Auto         ; 500
Int        Property RelRank Auto          ; 3 = Ally

Function Fragment_0(ObjectReference akSpeakerRef)
    Actor speaker = akSpeakerRef as Actor
    Actor player = Game.GetPlayer()
    If speaker == None || player == None
        Return
    EndIf
    ; The dialogue CTDA already gates on gold>=cost and not-already-following, but guard anyway.
    If speaker.IsInFaction(FollowerFaction)
        Return
    EndIf
    If player.GetItemCount(Gold001) < GoldCost
        Debug.Notification("You need " + GoldCost + " gold to hire her.")
        Return
    EndIf
    player.RemoveItem(Gold001, GoldCost)
    speaker.SetRelationshipRank(player, RelRank)
    speaker.AddToFaction(FollowerFaction)
    speaker.SetPlayerTeammate(true)
    speaker.EvaluatePackage()
    Debug.Notification("She joins you as a follower.")
EndFunction
