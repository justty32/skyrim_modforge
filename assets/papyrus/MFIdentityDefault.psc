Scriptname MFIdentityDefault extends Quest Hidden
{ ModForge default-identity granter. Attached to a StartGameEnabled quest: on first init it adds the
  player to every default identity's holding faction and grants its standing abilities, so a baseline
  identity (e.g. Adventurer) is held from the start of the game — no book to read. Idempotent (skips
  factions/spells the player already has). One prebuilt .pex serves every generated mod — same
  embed/ship model as the dispatcher / scene controller / identity book. }

Faction[] Property Factions Auto   ; the default identities' holding factions
Spell[]   Property Grants   Auto   ; the default identities' standing abilities (optional)
Perk[]    Property Perks    Auto   ; the default identities' standing perks (optional)

Event OnInit()
    Actor p = Game.GetPlayer()
    Int i = 0
    While i < Factions.Length
        If Factions[i] && !p.IsInFaction(Factions[i])
            p.AddToFaction(Factions[i])
        EndIf
        i += 1
    EndWhile
    i = 0
    While i < Grants.Length
        If Grants[i] && !p.HasSpell(Grants[i])
            p.AddSpell(Grants[i], false)
        EndIf
        i += 1
    EndWhile
    i = 0
    While i < Perks.Length
        If Perks[i] && !p.HasPerk(Perks[i])
            p.AddPerk(Perks[i])
        EndIf
        i += 1
    EndWhile
EndEvent
