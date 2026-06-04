Scriptname MFSE_TestTrigger extends Quest
{ Minimal Script Event end-to-end test: on init, fire MFSE_TestKW's story event with the player as
  akRef1. The SM ScriptEvent branch (keyword-filtered) should then start MFSE_Target with Target=player. }

Keyword Property TheKW Auto

Event OnInit()
    TheKW.SendStoryEvent(None, Game.GetPlayer())
EndEvent
