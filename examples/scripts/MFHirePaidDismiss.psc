Scriptname MFHirePaidDismiss Extends TopicInfo Hidden

; It.30 — result fragment for our OWN "we part ways" line. The vanilla dismiss can't release a
; self-managed follower (It.29), so we dismiss her the same way we hired her: clear the own
; FollowerFaction flag (which deactivates the Follow package) and drop teammate status. The recruit
; line — gated on NOT being in this faction — reappears, so she's re-hireable.

Faction Property FollowerFaction Auto   ; in-spec MF_PaidFollowerFaction

Function Fragment_0(ObjectReference akSpeakerRef)
    Actor speaker = akSpeakerRef as Actor
    If speaker == None
        Return
    EndIf
    speaker.RemoveFromFaction(FollowerFaction)
    speaker.SetPlayerTeammate(false)
    speaker.EvaluatePackage()   ; re-evaluate: Follow package now fails its condition -> she stops
    Debug.Notification("She parts ways with you.")
EndFunction
