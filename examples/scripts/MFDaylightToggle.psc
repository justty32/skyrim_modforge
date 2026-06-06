Scriptname MFDaylightToggle extends ActiveMagicEffect
{Castable toggle: add/remove the constant Daylight ability on the caster.
 State is the presence of the ability on the actor (so it survives save/reload for free).}

Spell Property MFDaylightActive Auto

Event OnEffectStart(Actor akTarget, Actor akCaster)
    if akCaster.HasSpell(MFDaylightActive)
        akCaster.RemoveSpell(MFDaylightActive)   ; already on -> turn off
    else
        akCaster.AddSpell(MFDaylightActive, false) ; off -> turn on (silent, no UI message)
    endif
EndEvent
