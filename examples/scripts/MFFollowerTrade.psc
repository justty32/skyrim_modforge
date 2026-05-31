Scriptname MFFollowerTrade Extends TopicInfo Hidden

; It.31 — "let me see what you're carrying": open her inventory for give/take. Mirrors vanilla
; DialogueFollowerTradeTopic (flags=0, not Goodbye — the trade menu opens over the dialogue).

Function Fragment_0(ObjectReference akSpeakerRef)
    Actor speaker = akSpeakerRef as Actor
    If speaker
        speaker.OpenInventory(true)
    EndIf
EndFunction
