Scriptname MFSE_AdvanceStage extends ReferenceAlias
{ ModForge reusable ALIAS trigger that ADVANCES ITS OWN QUEST. Attach to a quest alias (alias[].script)
  and set Stage: activating WHATEVER ref currently fills the alias runs SetStage(Stage) on the owning
  quest. Pairs with a stage that completes/closes an objective (objective.completeStage) so the player
  sees a quest progress + complete in the journal. Works on a ref CREATED at runtime (createObject) or
  MATCHED at runtime (findMatching) — the alias travels with the ref, so no base-object script needed.
  This is the "completion half" of journal progression: startUpStage shows the opening objective on
  quest start, this advances/finishes it on a player action. }

Int Property Stage = 20 Auto
{ The quest stage to set when the aliased ref is activated. }

Event OnActivate(ObjectReference akActionRef)
    GetOwningQuest().SetStage(Stage)
EndEvent
