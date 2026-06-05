Scriptname MFSE_ActivatorTrigger extends ObjectReference
{ ModForge reusable activator trigger. Attach this to any ACTI base and set TheKW: activating the
  object (pulling the lever, pressing the button) fires TheKW's story event through the universal
  dispatcher, with the activating actor as ref1. The same one-line pattern as MFSE_SpellTrigger,
  just a different entry point — proof that any Papyrus context wires to the Story Manager with zero
  per-mod glue. }

Keyword Property TheKW Auto

Event OnActivate(ObjectReference akActionRef)
    MFStoryEventDispatch.Fire(TheKW, akActionRef)
EndEvent
