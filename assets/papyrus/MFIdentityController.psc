Scriptname MFIdentityController extends Quest Hidden
{ ModForge primary-identity controller. Maintains the MF_PrimaryIdentity global = the player's CURRENT
  primary identity code: the manual Override (if set and the player still holds it) else the highest-priority
  HELD identity. Greetings / gates read MF_PrimaryIdentity (one GetGlobalValue == code CTDA) instead of a
  brittle faction-exclusion chain, and the override lets the player choose their primary. Factions[]/Codes[]
  are parallel arrays, sorted by priority DESCENDING. One prebuilt .pex serves every generated mod — same
  embed/ship model as the dispatcher / scene controller / identity book. Recomputes on init and on a light
  poll (so a book-driven faction change is picked up); a dialogue option that sets Override is reflected on
  the next poll. }

GlobalVariable Property Primary  Auto   ; OUT: the resolved current primary identity code (0 = none)
GlobalVariable Property Override Auto   ; IN:  the manual override code (0 = auto); set by a dialogue option
Faction[]      Property Factions Auto   ; identities' holding factions, sorted by priority DESC
Int[]          Property Codes    Auto   ; the matching identity codes (parallel to Factions)

Event OnInit()
    Recompute()
    RegisterForSingleUpdate(3.0)
EndEvent

Event OnUpdate()
    Recompute()
    RegisterForSingleUpdate(3.0)
EndEvent

Function Recompute()
    Actor p = Game.GetPlayer()
    Int ov = Override.GetValue() as Int
    If ov != 0
        Int oi = IndexOfCode(ov)
        If oi >= 0 && Factions[oi] && p.IsInFaction(Factions[oi])
            Primary.SetValue(ov)            ; honour the manual override while it's still held
            Return
        EndIf
    EndIf
    Int i = 0
    While i < Factions.Length               ; auto: first held in priority order (array is priority DESC)
        If Factions[i] && p.IsInFaction(Factions[i])
            Primary.SetValue(Codes[i])
            Return
        EndIf
        i += 1
    EndWhile
    Primary.SetValue(0)                      ; nothing held
EndFunction

Int Function IndexOfCode(Int code)
    Int i = 0
    While i < Codes.Length
        If Codes[i] == code
            Return i
        EndIf
        i += 1
    EndWhile
    Return -1
EndFunction
