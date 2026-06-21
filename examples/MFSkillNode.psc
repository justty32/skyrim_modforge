Scriptname MFSkillNode extends ObjectReference
{ ModForge self-contained in-world skill node. Click the node to learn its ability;
  gated on a prerequisite node. ZERO external-mod dependency — only vanilla types. }

Spell property nodeAbility auto
{ The ability spell granted to the player when this node is learned. }

GlobalVariable property rankGlobal auto
{ This node's learned state: 0 = not learned, 1 = learned. }

GlobalVariable property prereqGlobal auto
{ Optional: the prerequisite node's rankGlobal. Leave UNSET on a root node (always learnable). }

GlobalVariable property pointsGlobal auto
{ The shared skill-point pool global. }

String property nodeName = "this node" auto
{ Display name used in the on-screen notifications. }

Event OnActivate(ObjectReference akActionRef)
    if akActionRef != Game.GetPlayer()
        return
    endIf
    if rankGlobal.GetValueInt() >= 1
        Debug.Notification(nodeName + " is already learned.")
        return
    endIf
    if prereqGlobal && prereqGlobal.GetValueInt() < 1
        Debug.Notification("Locked. Learn the node below it first.")
        return
    endIf
    if pointsGlobal.GetValueInt() < 1
        Debug.Notification("No skill points available.")
        return
    endIf
    Game.GetPlayer().AddSpell(nodeAbility, false)
    rankGlobal.SetValueInt(1)
    pointsGlobal.SetValueInt(pointsGlobal.GetValueInt() - 1)
    Debug.Notification("Learned: " + nodeName + " — " + pointsGlobal.GetValueInt() + " point(s) left")
EndEvent
