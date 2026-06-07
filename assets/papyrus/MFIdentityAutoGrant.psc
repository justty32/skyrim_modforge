Scriptname MFIdentityAutoGrant extends Quest Hidden
{ ModForge auto-grant trigger. Joins the player to an identity's faction once a player ActorValue crosses
  a threshold — e.g. Dragonborn when DragonSouls >= 1 (the first absorbed dragon soul). Reads the AV in
  Papyrus via Actor.GetActorValue(name) (vanilla, no SKSE), so no event hook is needed. Factions[]/AvNames[]/
  Thresholds[] are parallel. Grants the FACTION signal only (identity greetings/gates then apply via the
  primary controller); abilities/perks on an auto-grant identity are not added here. One prebuilt .pex serves
  every generated mod — same embed/ship model as the other identity controllers. Polls so an absorb that
  happens mid-session is picked up; OnInit covers a save where the threshold is already met. }

Faction[] Property Factions   Auto   ; the identities' holding factions
String[]  Property AvNames    Auto   ; the ActorValue to read per faction (e.g. "DragonSouls")
Float[]   Property Thresholds Auto   ; grant once the player's AV >= this

Event OnInit()
    Check()
    RegisterForSingleUpdate(5.0)
EndEvent

Event OnUpdate()
    Check()
    RegisterForSingleUpdate(5.0)
EndEvent

Function Check()
    Actor p = Game.GetPlayer()
    Int i = 0
    While i < Factions.Length
        If Factions[i] && !p.IsInFaction(Factions[i]) && p.GetActorValue(AvNames[i]) >= Thresholds[i]
            p.AddToFaction(Factions[i])
        EndIf
        i += 1
    EndWhile
EndFunction
