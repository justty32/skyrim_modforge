Scriptname MFIdentityBook extends ObjectReference Hidden
{ ModForge reusable identity-acquire book. Attach to a BOOK (the build binds the properties): reading
  the book joins the player to TheFaction (the identity's held signal), grants the standing ability,
  and optionally starts an acquire performance scene. With Toggle, reading it again leaves the identity.
  One prebuilt .pex serves every generated mod — same embed/ship model as the dispatcher/controller. }

Faction Property TheFaction Auto             ; the identity's holding faction (required)
Spell   Property GrantAbility Auto           ; optional standing ability added on join / removed on leave
Perk    Property GrantPerk Auto              ; optional standing perk added on join / removed on leave
Scene   Property AcquireScene Auto           ; optional performance started on acquire
Bool    Property Toggle = false Auto         ; reading again leaves the identity

Function OnRead()
    Actor p = Game.GetPlayer()
    Bool has = p.IsInFaction(TheFaction)
    If Toggle && has
        p.RemoveFromFaction(TheFaction)
        If GrantAbility
            p.RemoveSpell(GrantAbility)
        EndIf
        If GrantPerk
            p.RemovePerk(GrantPerk)
        EndIf
        Return
    EndIf
    If has
        Return                                ; already holds it — reading again is a no-op
    EndIf
    p.AddToFaction(TheFaction)
    If GrantAbility
        p.AddSpell(GrantAbility, false)
    EndIf
    If GrantPerk
        p.AddPerk(GrantPerk)
    EndIf
    If AcquireScene
        AcquireScene.Start()
    EndIf
EndFunction
