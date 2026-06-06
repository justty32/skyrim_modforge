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

float lastPlayed = 0.0
bool scenePlaying = false

Event OnInit()
    RegisterForSingleUpdate(PollInterval)
EndEvent

Event OnUpdate()
    Poll()
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
    scenePlaying = true        ; so Poll() can detect the end and fire BrawlOnEnd
EndFunction

Actor Function GetActor(int aliasIndex)
    ReferenceAlias ra = GetAlias(aliasIndex) as ReferenceAlias
    if ra == None
        return None
    endif
    return ra.GetReference() as Actor
EndFunction
