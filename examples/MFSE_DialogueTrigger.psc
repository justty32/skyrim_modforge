Scriptname MFSE_DialogueTrigger extends TopicInfo Hidden
{ ModForge reusable dialogue trigger. Wire it as a dialogue line's result script (resultScript +
  resultScriptSource + a TheKW resultProperty): picking the line fires TheKW's story event through
  the universal dispatcher, with the player as ref1 and the speaker NPC as ref2. Same one-line
  Fire() as the spell/activator triggers — the NPC-gives-a-quest entry point with zero per-mod glue. }

Keyword Property TheKW Auto

Function Fragment_0(ObjectReference akSpeakerRef)
    MFStoryEventDispatch.Fire(TheKW, Game.GetPlayer(), akSpeakerRef)
EndFunction
