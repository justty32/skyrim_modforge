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

float lastPlayed = 0.0

Event OnInit()
    RegisterForSingleUpdate(PollInterval)
EndEvent

Event OnUpdate()
    TryBanter()
    RegisterForSingleUpdate(PollInterval)   ; re-arm the next poll (no persistent OnUpdate loop)
EndEvent

Function TryBanter()
    if BanterScene == None || BanterScene.IsPlaying()
        return
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
EndFunction

Actor Function GetActor(int aliasIndex)
    ReferenceAlias ra = GetAlias(aliasIndex) as ReferenceAlias
    if ra == None
        return None
    endif
    return ra.GetReference() as Actor
EndFunction
