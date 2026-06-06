Scriptname MFSceneBanterController extends Quest
{ ModForge reusable presence-gated Scene controller.
  Attached to a host quest when a scene declares `autoStart`. Polls (chained
  RegisterForSingleUpdate) and starts the bound Scene whenever the player is co-present with both
  actor aliases. One prebuilt .pex serves every generated mod; ModForge wires the properties. }

Scene Property BanterScene Auto
{ The scene this controller starts. }
Int Property ActorAliasA = 0 Auto
{ Alias index (on THIS quest) holding the first scene actor. }
Int Property ActorAliasB = 1 Auto
{ Alias index holding the second scene actor. }
Float Property TriggerDistance = 2048.0 Auto
{ Max distance (units) from the player to EACH actor for the scene to fire. }
Float Property PollInterval = 5.0 Auto
{ Seconds between presence checks. }
Float Property Cooldown = 60.0 Auto
{ Min REAL seconds between scene plays. }
Bool Property RequireLOS = false Auto
{ Also require the player to have line of sight to both actors. }
Bool Property BrawlOnEnd = false Auto
{ When the scene's dialogue finishes, make the two actors fight each other (StartCombat both ways).
  Mark the actors `essential` for a non-lethal tavern brawl. }

; --- replay policy (all AND-ed onto the Cooldown) ---
Bool Property PlayOnce = false Auto
{ Play at most once, ever; the controller stops polling after the single play. }
Float Property PlayHour = -1.0 Auto
{ Only play within +/-PlayHourTolerance of this in-game hour (0..24, circular). -1 = any time. }
Float Property PlayHourTolerance = 1.0 Auto
{ +/- hours window around PlayHour. }
GlobalVariable Property Gate Auto
{ Re-arm token: only play while Gate == 0; SetValue(1) right after playing. Another event SetValue(0)
  re-enables it. None = no gate. }

float lastPlayed = 0.0
bool scenePlaying = false
bool played = false

Event OnInit()
    RegisterForSingleUpdate(PollInterval)
EndEvent

Event OnUpdate()
    Poll()
    ; one-shot hygiene: once it has played and the scene has finished, stop polling for good
    if PlayOnce && played && !scenePlaying
        return
    endif
    RegisterForSingleUpdate(PollInterval)   ; re-arm the next poll (no persistent OnUpdate loop)
EndEvent

Function Poll()
    if BanterScene == None
        return
    endif
    if BanterScene.IsPlaying()
        scenePlaying = true       ; remember it ran so we can react when it ends
        return
    endif
    if scenePlaying
        scenePlaying = false      ; the scene just finished this poll
        if BrawlOnEnd
            StartBrawl()
        endif
        return
    endif
    TryBanter()
EndFunction

Function StartBrawl()
    Actor a = GetActor(ActorAliasA)
    Actor b = GetActor(ActorAliasB)
    if a == None || b == None || a.IsDead() || b.IsDead()
        return
    endif
    a.StartCombat(b)
    b.StartCombat(a)
EndFunction

Function TryBanter()
    ; --- replay policy gates ---
    if PlayOnce && played
        return
    endif
    if Gate != None && Gate.GetValue() != 0.0
        return                                 ; gated off until another event SetValue(0)
    endif
    if PlayHour >= 0.0 && HourDistance(CurrentHour(), PlayHour) > PlayHourTolerance
        return                                 ; outside the time-of-day window
    endif
    float now = Utility.GetCurrentRealTime()
    if (now - lastPlayed) < Cooldown
        return
    endif
    Actor a = GetActor(ActorAliasA)
    Actor b = GetActor(ActorAliasB)
    if a == None || b == None || a.IsDead() || b.IsDead()
        return
    endif
    Actor player = Game.GetPlayer()
    if a.GetDistance(player) > TriggerDistance || b.GetDistance(player) > TriggerDistance
        return
    endif
    if a.IsInCombat() || b.IsInCombat() || player.IsInCombat()
        return
    endif
    if RequireLOS && (!player.HasLOS(a) || !player.HasLOS(b))
        return
    endif
    BanterScene.Start()
    lastPlayed = now
    played = true
    scenePlaying = true        ; so Poll() can detect the end and fire BrawlOnEnd
    if Gate != None
        Gate.SetValue(1.0)     ; arm the token; another event resets it to 0 to re-enable
    endif
EndFunction

; Current in-game hour 0..24 (GetCurrentGameTime is days as a float; fraction * 24 = hour).
float Function CurrentHour()
    float gt = Utility.GetCurrentGameTime()
    int days = gt as int
    return (gt - days) * 24.0
EndFunction

; Circular distance between two hours (0..12).
float Function HourDistance(float h1, float h2)
    float d = h1 - h2
    if d < 0.0
        d = -d
    endif
    if d > 12.0
        d = 24.0 - d
    endif
    return d
EndFunction

Actor Function GetActor(int aliasIndex)
    ReferenceAlias ra = GetAlias(aliasIndex) as ReferenceAlias
    if ra == None
        return None
    endif
    return ra.GetReference() as Actor
EndFunction
