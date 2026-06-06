Scriptname MFDaylightVisionEffect extends ActiveMagicEffect
{Carried by the constant Daylight ability: while active, apply the daylight imagespace
 (whole-view brightening); remove it when the ability is dispelled. On save/reload the
 constant effect re-runs OnEffectStart, so the brightening is restored automatically.}

ImageSpaceModifier Property MFDaylightIMAD Auto

Event OnEffectStart(Actor akTarget, Actor akCaster)
    MFDaylightIMAD.ApplyCrossFade(0.5)   ; fade the daylight wash in over 0.5s
EndEvent

Event OnEffectFinish(Actor akTarget, Actor akCaster)
    MFDaylightIMAD.Remove()              ; clear the imagespace when toggled off
EndEvent
