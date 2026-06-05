Scriptname MFSE_AliasActivate extends ReferenceAlias
{ ModForge reusable ALIAS trigger. Attach to a quest alias (alias[].script) and set TheKW: activating
  WHATEVER ref currently fills the alias fires TheKW's story event through the universal dispatcher.
  Unlike a base-object trigger this works on a ref CREATED at runtime (createObject) or MATCHED at
  runtime (findMatching) — the alias travels with the ref. ref1 = the activating actor, ref2 = the
  aliased ref itself. Same one-line Fire() pattern as MFSE_ActivatorTrigger, a different attach point. }

Keyword Property TheKW Auto

Event OnActivate(ObjectReference akActionRef)
    MFStoryEventDispatch.Fire(TheKW, akActionRef, GetReference())
EndEvent
