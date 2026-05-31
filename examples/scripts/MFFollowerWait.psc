Scriptname MFFollowerWait Extends TopicInfo Hidden

; It.31 — "wait here": set the WaitingForPlayer ActorValue (the same flag vanilla uses). The Follow
; package gates on WaitingForPlayer==0, so once set she stops following and holds position.

Function Fragment_0(ObjectReference akSpeakerRef)
    Actor speaker = akSpeakerRef as Actor
    If speaker
        speaker.SetActorValue("WaitingForPlayer", 1.0)
        speaker.EvaluatePackage()
    EndIf
EndFunction
