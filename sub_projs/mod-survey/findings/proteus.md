# PROTEUS（character build/save manager；native-DLL + JSON 驅動）

← [survey index](../index.md)

| 項目 | 值 |
| --- | --- |
| 類型 | **框架型**（角色 build 存取 / 多角色 / NPC 生成管理），核心是 **native `Proteus.dll`（556KB SKSE plugin）** |
| Plugin | `PROTEUS.esp`（3.4.0）+ 一堆相容 patch esp（Odin/Mysticism/Vigilant/EDM…）|
| 規模 | quests=31 npcs=154 items=5 magic=121 books=0 loc=31；無 BSA（loose files）|
| 敘事價值 | 無 |

## 是什麼

存檔/還原整個角色「build」（外觀+技能+法術+裝備+物品）、開分身多角色、生成/控制 NPC、傳送、輪迴任務等。核心邏輯在 **native DLL**，Papyrus（~14 支 `Proteus*.pex`）只是膠水，UI 走自帶 **UILib**（`UILIB_1_ListMenu.swf` / `TextInputMenu.swf`）+ MCM。

## 關鍵架構：DLL 讀外部 JSON 模板（與 ModForge 形成有趣對照）

`Data/Scripts/Proteus JSON/` 有 6 個 JSON 模板，DLL 讀它們來 serialize/deserialize 角色資料：
`Proteus_Character_GeneralInfo / _Character_Skills / _NPC_GeneralInfo / _Armor / _Weapon / _Spell _Template.json`

→ **PROTEUS = runtime JSON 序列化角色狀態**（遊戲中存讀）；**ModForge = build-time JSON 生成 esp**。方向相反，但都用「JSON schema 描述 Skyrim 物件」。PROTEUS 的角色/裝備/法術 JSON schema 可作 ModForge 日後若要做「角色 preset 匯入」的欄位參考，但**核心是閉源 native code，無可生成成分**。

## 結論

對 ModForge：**忽略**（純消費型框架，核心 native，無 record 生成借鏡）。唯一可留檔的是它的 JSON-template-driven 角色 schema 作為對照參考。對 Sofia：無關。
</content>
