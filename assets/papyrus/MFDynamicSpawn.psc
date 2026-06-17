Scriptname MFDynamicSpawn extends Quest
{ ModForge reusable DYNAMIC near-player spawn (F組 #3, the EE NavmeshTester trick). Attached to a
  quest declaring `spawn`. SpawnNow() places `Count` copies of `SpawnForm` (an ActorBase or LeveledNpc)
  at a random offset around the player and snaps each to the nearest navmesh point (EnableAI toggle) —
  a legal, walkable spawn with no pre-placed cell markers. One prebuilt .pex serves every generated mod;
  ModForge wires the properties.

  TRIGGER: the owning quest's generated <quest>_Stages startUpStage fragment calls SpawnNow() via
  `(self as MFDynamicSpawn).SpawnNow()` — NOT OnInit. OnInit fires only ONCE per quest lifetime, so an
  SM-relaunched encounter would spawn only on its very first start; a startUpStage fragment runs on
  EVERY quest start (SM-launched or StartGameEnabled), which is what a repeatable encounter needs.
  Pair with a ChangeLocation storyEvent + locationFilter + cooldownHours for a rate-limited encounter. }

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

Function SpawnNow()
    Debug.Notification("MF: SpawnNow entered, count=" + Count)   ; DIAG (remove after verify)
    Actor player = Game.GetPlayer()
    if !player || !SpawnForm
        Debug.Notification("MF: SpawnNow ABORT — no player or SpawnForm is None")   ; DIAG
        return
    endif
    int i = 0
    while i < Count
        float ang = Utility.RandomFloat(0.0, 360.0)
        float dist = Utility.RandomFloat(MinDistance, MaxDistance)
        ObjectReference spawned = player.PlaceAtMe(SpawnForm, 1)
        if spawned
            ; Shove to a random offset around the player, spawned 128 units ABOVE so the actor free-falls
            ; onto whatever ground is there. This is far more reliable than a navmesh "snap": a flat z=0
            ; offset buries the actor inside any rising terrain (→ stuck/invisible), whereas dropping from
            ; just above always lands them on the surface. dist 0 → leave exactly at the player.
            if dist >= 1.0
                spawned.MoveTo(player, dist * Math.Sin(ang), dist * Math.Cos(ang), 128.0)
            endif
            Actor a = spawned as Actor
            if SnapToNavmesh && a
                a.EnableAI(false)
                a.EnableAI(true)        ; wakes the actor so it paths onto navmesh after landing
            endif
        endif
        i += 1
    endwhile
EndFunction
