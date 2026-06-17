Scriptname MFEncounterCooldown extends Quest
{ ModForge reusable anti-spam cooldown for Story-Manager encounter quests (A組 #6, the EE_WITimeout
  pattern). Attached to a ChangeLocation (or other SM) encounter quest that declares `cooldownHours`.
  The SM auto-starts the quest when its event + locationFilter conditions pass; the quest's generated
  <quest>_Stages startUpStage fragment then calls `(self as MFEncounterCooldown).TryFire()` BEFORE any
  spawn / objective. TryFire() returns False (and the fragment Stop()s the quest) when the last firing
  was too recent — so the same encounter can't re-trigger within the window.

  WHY a fragment and not OnInit: OnInit fires only ONCE per quest lifetime, but the SM relaunches the
  same quest on every qualifying event — so an OnInit cooldown would be evaluated once and never again.
  The startUpStage fragment runs on EVERY start, which is where the cooldown must be re-checked.
  One prebuilt .pex serves every generated mod; ModForge wires LastFired (a per-quest GLOB) + CooldownHours. }

GlobalVariable Property LastFired Auto
{ Per-quest float GLOB storing the game time (in days, GetCurrentGameTime) of the last firing. 0 = never. }
Float Property CooldownHours = 12.0 Auto
{ Minimum in-game hours between firings. EE's default is 12. }

; Called from the owning quest's startUpStage fragment on every quest start. Returns True if the
; encounter may fire now (and stamps this firing), False if still on cooldown (caller should Stop()).
bool Function TryFire()
    float now = Utility.GetCurrentGameTime()           ; days since game start
    float last = LastFired.GetValue()
    if last > 0.0 && (now - last) < (CooldownHours / 24.0)
        return false                                    ; still on cooldown — caller aborts this encounter
    endif
    LastFired.SetValue(now)                             ; stamp this firing
    return true
EndFunction
