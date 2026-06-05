Scriptname MFSE_SpellTrigger extends ActiveMagicEffect
{ ModForge reusable magic-effect trigger. Attach this to any MGEF and set TheKW: casting the spell
  that carries the effect fires TheKW's story event through the universal dispatcher, with the caster
  as ref1 and the effect's target as ref2. Routes through MFStoryEventDispatch.Fire — the single
  chokepoint every ModForge custom entry shares, so one trigger script wires any spell to the Story
  Manager with zero per-mod Papyrus. }

Keyword Property TheKW Auto

Event OnEffectStart(Actor akTarget, Actor akCaster)
    MFStoryEventDispatch.Fire(TheKW, akCaster, akTarget)
EndEvent
