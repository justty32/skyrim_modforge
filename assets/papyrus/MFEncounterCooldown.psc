Scriptname MFEncounterCooldown extends Quest
{ ModForge reusable anti-spam cooldown for Story-Manager encounter quests (A組 #6, the EE_WITimeout
  pattern). Attached to a ChangeLocation (or other SM) encounter quest that declares `cooldownHours`.
  The SM still auto-starts the quest when its event + locationFilter conditions pass; this script runs
  on start and, if the last firing was too recent, immediately Stop()s the quest before any stage /
  objective shows — so the same encounter can't re-trigger within the window. One prebuilt .pex serves
  every generated mod; ModForge wires LastFired (a per-quest GLOB) + CooldownHours. }

GlobalVariable Property LastFired Auto
{ Per-quest float GLOB storing the game time (in days, GetCurrentGameTime) of the last firing. 0 = never. }
Float Property CooldownHours = 12.0 Auto
{ Minimum in-game hours between firings. EE's default is 12. }

Event OnInit()
    CheckCooldown()
EndEvent

; Quests started by the Story Manager fire OnInit on each start; re-evaluate the cooldown there.
Function CheckCooldown()
    float now = Utility.GetCurrentGameTime()           ; days since game start
    float last = LastFired.GetValue()
    if last > 0.0 && (now - last) < (CooldownHours / 24.0)
        Stop()                                          ; still on cooldown — abort this encounter
        return
    endif
    LastFired.SetValue(now)                             ; stamp this firing
EndFunction
