Attempt 1 failed: You have exhausted your capacity on this model. Your quota will reset after 0s.. Retrying after 5382ms...
Based on my research into xEdit (TES5Edit) scripting for exporting placed references (REFR), here is the information you need, including a complete Pascal script and API details.

### 1. Custom xEdit Export Script (JSON/CSV)
You can save the following code as `Export_REFR_Data.pas` in your xEdit `Edit Scripts` folder. This script iterates through selected records and exports their FormID, EditorID, Base Form, Position, Rotation, and Scale.

```pascal
{
    Export Placed References (REFR) to JSON and CSV
    Includes: FormID, EditorID, Base Form, Position, Rotation, and Scale.
}
unit ExportREFR;

interface
    function Initialize: integer;
    function Process(e: IInterface): integer;
    function Finalize: integer;

implementation
    var
        slCSV, slJSON: TStringList;
        bFirst: boolean;

    function Initialize: integer;
    begin
        slCSV := TStringList.Create;
        slJSON := TStringList.Create;
        slCSV.Add('FormID,EditorID,BaseForm,PosX,PosY,PosZ,RotX,RotY,RotZ,Scale');
        slJSON.Add('[');
        bFirst := True;
        Result := 0;
    end;

    function Process(e: IInterface): integer;
    var
        sFormID, sEditorID, sBaseForm, sPosX, sPosY, sPosZ, sRotX, sRotY, sRotZ, sScale: string;
    begin
        if Signature(e) <> 'REFR' then Exit;

        sFormID := IntToHex(FixedFormID(e), 8);
        sEditorID := GetElementEditValue(e, 'EDID');
        sBaseForm := GetElementEditValue(e, 'NAME'); // Format: "Name [STAT:00123456]"
        
        // Position & Rotation (DATA block)
        sPosX := GetElementEditValue(e, 'DATA\Position\X');
        sPosY := GetElementEditValue(e, 'DATA\Position\Y');
        sPosZ := GetElementEditValue(e, 'DATA\Position\Z');
        sRotX := GetElementEditValue(e, 'DATA\Rotation\X');
        sRotY := GetElementEditValue(e, 'DATA\Rotation\Y');
        sRotZ := GetElementEditValue(e, 'DATA\Rotation\Z');
        
        // Scale (XSCL) - Defaults to 1.0 if not present
        sScale := GetElementEditValue(e, 'XSCL');
        if sScale = '' then sScale := '1.000000';

        // CSV Entry
        slCSV.Add(Format('%s,"%s","%s",%s,%s,%s,%s,%s,%s,%s', [
            sFormID, sEditorID, sBaseForm, sPosX, sPosY, sPosZ, sRotX, sRotY, sRotZ, sScale
        ]));

        // JSON Entry
        if not bFirst then slJSON[slJSON.Count - 1] := slJSON[slJSON.Count - 1] + ',';
        slJSON.Add('  {');
        slJSON.Add('    "formID": "' + sFormID + '",');
        slJSON.Add('    "editorID": "' + sEditorID + '",');
        slJSON.Add('    "baseForm": "' + sBaseForm + '",');
        slJSON.Add('    "position": {"x": ' + sPosX + ', "y": ' + sPosY + ', "z": ' + sPosZ + '},');
        slJSON.Add('    "rotation": {"x": ' + sRotX + ', "y": ' + sRotY + ', "z": ' + sRotZ + '},');
        slJSON.Add('    "scale": ' + sScale);
        slJSON.Add('  }');
        
        bFirst := False;
        Result := 0;
    end;

    function Finalize: integer;
    begin
        slJSON.Add(']');
        slCSV.SaveToFile(ProgramPath + 'Edit Scripts\Exported_Refs.csv');
        slJSON.SaveToFile(ProgramPath + 'Edit Scripts\Exported_Refs.json');
        AddMessage('Exported to: ' + ProgramPath + 'Edit Scripts\Exported_Refs.json');
        slCSV.Free; slJSON.Free;
        Result := 0;
    end;
end.
```

### 2. Community Scripts & Tools
*   **Mator’s Automation Tools:** A well-known suite of scripts that includes advanced "Quick Stats" and "Export" functionalities. [Nexus Mods Link](https://www.nexusmods.com/skyrim/mods/49373) (Works for SSE/Skyrim).
*   **xEdit-to-JSON:** Several developers have shared specialized scripts on GitHub for exporting records to JSON for web-based tools or game engine imports.
*   **Built-in "Export to CSV":** If you build reference info (right-click record -> "Build Reference Info"), you can right-click the **Referenced By** tab at the bottom and select "Export to CSV" without any scripting.

### 3. Key xEdit Scripting API for REFR
The following functions are essential for reading placed reference data:
*   **`GetElementEditValue(e, path)`**: Retrieves values exactly as they appear in the UI (e.g., `'DATA\Position\X'`).
*   **`Signature(e)`**: Returns `'REFR'` for placed references.
*   **`LinksTo(e)`**: If you have a `NAME` (base form) element, `LinksTo` returns the actual base record interface.
*   **`ReferencedByCount(e)` / `ReferencedByIndex(e, i)`**: Useful if you select a **Base Record** (like a tree) and want to find every single place it has been put in the world.

**Official Documentation:** [The Tome of xEdit - Scripting Functions](https://tes5edit.github.io/docs/13-Scripting-Functions.html)
