Scriptname MFStoryEventDispatch extends Quest
{ ModForge generic story-event dispatcher. Compile ONCE; every generated mod ships this same .pex.
  The keyword and refs are runtime parameters, so one byte serves any mod and any story event.

  Fire(kw, ref1, ref2, loc) makes the Story Manager evaluate the ScriptEvent root: the SM branch
  whose "GetEventData Keyword GetIsID kw == 1" condition matches starts its template quest, and that
  quest's aliases pull ref1/ref2/loc (event slots R1/R2/L1). This is ModForge's universal custom entry. }

; Fire a script story event. akKeyword selects which SM branch handles it (via the branch's keyword
; filter). akRef1 -> R1, akRef2 -> R2, akLoc -> L1. Call as MFStoryEventDispatch.Fire(kMyKW, someRef).
Function Fire(Keyword akKeyword, ObjectReference akRef1 = None, ObjectReference akRef2 = None, Location akLoc = None) Global
    if akKeyword
        akKeyword.SendStoryEvent(akLoc, akRef1, akRef2)
    endif
EndFunction

; Synchronous variant: returns whether a quest actually started (handy for debug / conditional chains).
bool Function FireAndWait(Keyword akKeyword, ObjectReference akRef1 = None, ObjectReference akRef2 = None, Location akLoc = None) Global
    if akKeyword
        return akKeyword.SendStoryEventAndWait(akLoc, akRef1, akRef2)
    endif
    return false
EndFunction
