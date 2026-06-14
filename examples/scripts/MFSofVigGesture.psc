Scriptname MFSofVigGesture Extends TopicInfo Hidden

; ModForge — Sofia "gesture while commenting" result fragment (v2 action demo).
; Replaces the auto setGlobal TIF: sets the say-once GLOB AND plays an idle animation
; on the speaker (Sofia), so she physically reacts as she delivers the line.
; Bound per-line via resultProperties: TargetGlobal = the line's once-flag GLOB,
; GestureIdle = a vanilla IdleAnimation chosen to fit the line's emotion.

GlobalVariable Property TargetGlobal Auto
Idle           Property GestureIdle  Auto

Function Fragment_0(ObjectReference akSpeakerRef)
    If TargetGlobal
        TargetGlobal.SetValue(1)
    EndIf
    Actor speaker = akSpeakerRef as Actor
    If speaker && GestureIdle
        speaker.PlayIdle(GestureIdle)
    EndIf
EndFunction
