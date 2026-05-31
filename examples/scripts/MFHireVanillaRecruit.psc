Scriptname MFHireVanillaRecruit Extends TopicInfo Hidden

; It.32 — paid recruit that hands the NPC to the VANILLA follower system. After this, vanilla's own
; trade / wait / "follow me" / "let's part ways" dialogue manages her (and any follower-manager mod —
; AFT/EFF/NFF — picks her up automatically, because she's a normal vanilla follower). The only custom
; parts are our recruit trigger and the gold transaction; DialogueFollowerScript.SetFollower does the
; rest: sets the player relationship, SetPlayerTeammate, and crucially ForceRefTo the follower alias
; (which is what carries the follow package and adds CurrentFollowerFaction). Her custom CombatStyle and
; any sandbox/lifelike packages are untouched — they just yield to the alias's follow package while she's
; actively trailing you, and resume when she's dismissed or told to wait.

MiscObject Property Gold001 Auto             ; Skyrim.esm:0x00000F
Int        Property GoldCost Auto            ; 500
Quest      Property DialogueFollower Auto    ; Skyrim.esm:0x0750BA (runs DialogueFollowerScript)

Function Fragment_0(ObjectReference akSpeakerRef)
    Actor speaker = akSpeakerRef as Actor
    Actor player = Game.GetPlayer()
    If speaker == None || player == None || DialogueFollower == None
        Return
    EndIf
    ; The dialogue CTDA already gates on gold>=cost and PlayerFollowerCount==0, but guard anyway.
    If player.GetItemCount(Gold001) < GoldCost
        Debug.Notification("You need " + GoldCost + " gold to hire her.")
        Return
    EndIf
    player.RemoveItem(Gold001, GoldCost)
    (DialogueFollower as DialogueFollowerScript).SetFollower(speaker)
    Debug.Notification("She joins you as a follower.")
EndFunction
