Scriptname MFHireFollowerSetup extends Quest

; It.27 — the path that actually recruits a CUSTOM follower for free.
;
; The vanilla free-follow hire topic (DialogueFavorGenericFollowBranchTopic, probed with
; `modforge infodiag Skyrim.esm 0x0B0EE6`) gates each INFO on:
;   GetInFaction PotentialHireling(0x0BCC9A) == 0   <- so she must NOT be a paid hireling
;   GetIsVoiceType in VoicesFollowerNeutral          <- follower voice (FemaleEvenToned here)
;   GetInFaction CurrentFollowerFaction(0x05C84E)==0
;   GetInFaction PotentialFollowerFaction(0x05C84D)==1
;   GetRelationshipRank(player) >= 1
; Everything but the relationship is satisfied by static records. A *static* RELA to the player
; reads rank 0 at runtime (vanilla has ZERO player RELA — it is always script-set), so we set it
; here. Once set, "Follow me, I need your help" surfaces and she joins with no gold cost.

Actor Property SeraRef Auto   ; the placed follower (ACHR), bound by ModForge's object property

Event OnInit()
    If SeraRef
        ; 3 = Ally; any rank >= 1 satisfies the gate. Idempotent — safe to run once at game start.
        SeraRef.SetRelationshipRank(Game.GetPlayer(), 3)
        Debug.Trace("MFHireFollowerSetup: SeraRef relationship to player set to Ally (3).")
    EndIf
EndEvent
