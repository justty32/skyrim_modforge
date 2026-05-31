Scriptname MFFollowerFollow Extends TopicInfo Hidden

; It.31 — "follow me again": clear WaitingForPlayer, re-enabling the Follow package.

Function Fragment_0(ObjectReference akSpeakerRef)
    Actor speaker = akSpeakerRef as Actor
    If speaker
        speaker.SetActorValue("WaitingForPlayer", 0.0)
        speaker.EvaluatePackage()
    EndIf
EndFunction
