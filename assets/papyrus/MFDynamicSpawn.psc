Scriptname MFDynamicSpawn extends Quest
{ ModForge reusable DYNAMIC near-player spawn (F組 #3, the EE NavmeshTester trick). Attached to a
  quest declaring `spawn`. On quest start (OnInit) it places `Count` copies of `SpawnForm` (an
  ActorBase or LeveledNpc) at a random offset around the player and snaps each to the nearest navmesh
  point (EnableAI toggle) — a legal, walkable spawn with no pre-placed cell markers. One prebuilt .pex
  serves every generated mod; ModForge wires the properties. Pair with a ChangeLocation storyEvent +
  locationFilter + cooldownHours for a rate-limited, location-aware encounter. }

Form Property SpawnForm Auto
{ The ActorBase (NPC_) or LeveledNpc (LVLN) to spawn. }
Int Property Count = 1 Auto
{ How many to spawn. }
Float Property MinDistance = 1500.0 Auto
{ Nearest spawn offset from the player (units). }
Float Property MaxDistance = 4000.0 Auto
{ Farthest spawn offset from the player (units). }
Bool Property SnapToNavmesh = true Auto
{ Toggle EnableAI on each spawn so it snaps to the nearest navmesh point (legal walkable spot). }

Event OnInit()
    SpawnNow()
EndEvent

Function SpawnNow()
    Actor player = Game.GetPlayer()
    if !player || !SpawnForm
        return
    endif
    int i = 0
    while i < Count
        float ang = Utility.RandomFloat(0.0, 360.0)
        float dist = Utility.RandomFloat(MinDistance, MaxDistance)
        ; place at the player, then shove to a random offset around them
        ObjectReference spawned = player.PlaceAtMe(SpawnForm, 1)
        spawned.MoveTo(player, dist * Math.Sin(ang), dist * Math.Cos(ang), 0.0)
        Actor a = spawned as Actor
        if SnapToNavmesh && a
            a.EnableAI(false)
            a.EnableAI(true)        ; re-enabling AI snaps the actor to the nearest navmesh point
        endif
        i += 1
    endwhile
EndFunction
