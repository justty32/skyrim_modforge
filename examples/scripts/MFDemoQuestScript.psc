Scriptname MFDemoQuestScript extends Quest

; A trivial demo script — proves ModForge can drive the Papyrus compiler (Wine)
; on Linux end-to-end. The filename must match the Scriptname.

Actor Property PlayerRef Auto
Int Property GreetingCount Auto

Event OnInit()
    Debug.Trace("MFDemoQuestScript: forged by ModForge, compiled on Linux via Wine.")
EndEvent
