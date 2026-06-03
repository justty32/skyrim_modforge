<!-- 社交與任務模式 -->
# 食譜手冊 — 社交與任務

← [目錄](cookbook-index.md) | [lifelike 主頁](README.md)

## 「可對話的 NPC」（自訂玩家對話選項——遊戲內已確認 It.23）

為 NPC 設定一個 `greeting`、一個宿主任務，以及每個話題各一個 `dialogue` 條目。由此，建構程序會自動產生完整的原版鏈（Quest→DialogView→Branch→Topic→INFO + 一個 Hello），使話題能夠實際顯示。

```jsonc
{ "quests": [ { "editorId": "MF_TalkQuest", "name": "...", "startGameEnabled": true } ],
  "npcs": [
    { "editorId": "MF_Talker", "name": "Aldric", "race": "Skyrim.esm:0x013746",
      "voiceType": "Skyrim.esm:0x013AE6",            // 真實的語音類型——沒有語音的 NPC 不會打招呼
      "greeting": "Welcome. What brings you here?",   // 必填：產生讓他可對話的 Hello
      "factions": [ "Skyrim.esm:0x028172" ] } ],
  "dialogue": [
    { "editorId": "MF_AboutPlace", "questEditorId": "MF_TalkQuest", "speakerNpcEditorId": "MF_Talker",
      "prompt": "Tell me about this place.", "emotion": "Happy",
      "responses": [ "Everything here was forged on Linux.", "No Creation Kit needed." ] } ],
  "placements": [
    { "base": "MF_Talker", "cell": "Skyrim.esm:0x0133C6",
      "position": { "x": -350, "y": 180, "z": 0 } } ]   // 真實的室內座標，而非 (0,0,0)
}
```

以下三件事各自獨立地會導致靜默失敗——參見 [gotchas.md](gotchas.md)：
- **`greeting`** 必須設定，否則 NPC 無法對話（不會出現對話鏡頭）。
- **放置位置**必須是真實的室內座標——一個沒有套件的 NPC 位於 `(0,0,0)` 時會偏離導覽網格，你無法靠近它。
- **未配音的台詞**會一閃而過——請安裝 **Fuz Ro D-oh**（或附上靜音 `.fuz`），並開啟字幕。

## 「運作中的商人」（能買賣商品的店主——遊戲內已確認 2026-05-31）

商人 = 一個**標有 Vendor 旗標的 FACT**（包含交易時段 + 買賣類別清單 + 含金幣與存貨的商人箱），其成員 NPC 會被遊戲引擎視為店主。讓 NPC 可以對話，建構程序會自動加入 `JobMerchantFaction`，使原版通用的「I'd like to trade」話題得以顯示——**但前提是 `GetOffersServicesNow` 回傳 1**，這對生成的 NPC 有兩個不明顯的要求（商人必須在其商店內就位當班，且派系必須指定一個售賣區域的 CELL）。

```jsonc
{ "factions": [
    { "editorId": "MF_ShopFaction", "name": "ModForge General Goods",
      "vendor": {
        "startHour": 8, "endHour": 20, "buysStolen": false,
        "sellBuyList": "Skyrim.esm:0x06CB48",     // VendorItemsMisc（VendorItem-keyword FormList）
        "notSellBuyList": true,                    // NOT-sell 清單 -> 交易除此之外的所有物品（一般商品）
        "merchantContainer": "MF_ShopChestRef" } } ],   // -> 放置的箱子（如下）
  "containers": [
    { "editorId": "MF_ShopChest", "name": "Merchant Chest",
      "items": [ { "item": "Skyrim.esm:0x072AE7", "count": 1 },     // VendorGoldMisc（商人的金幣）
                 { "item": "Skyrim.esm:0x09AF0A", "count": 10 } ] } ],  // LItemMiscVendorMiscItems75（存貨）
  "npcs": [
    { "editorId": "MF_Shopkeeper", "name": "Marcurio the Merchant", "race": "Skyrim.esm:0x013746",
      "voiceType": "Skyrim.esm:0x013AE6", "unique": true,
      "factions": [ "MF_ShopFaction" ],
      "greeting": "Looking to buy?" } ],
  "cells": [ { "editorId": "MF_Shop", "name": "Trading Post", "template": "Skyrim.esm:0x0165A8" } ],
  "placements": [
    { "base": "MF_Shopkeeper", "cell": "MF_Shop", "position": { "x": 0, "y": 128, "z": 0 }, "persistent": true },
    { "editorId": "MF_ShopChestRef", "base": "MF_ShopChest", "cell": "MF_Shop",
      "position": { "x": 0, "y": 256, "z": 0 }, "persistent": true } ] }
```
（完整範例規格：`examples/vendor_spec.json`。）

**商人必須在其商店內就位當班**——這在**新**室內空間中需要：(a) 一個地板（`WRIntFloorSTMid01Large` `0x1044AA` 格板 + 一盞燈），以及 (b) 一個當班用的 **Sandbox** 套件（`0x01C254`，NearSelf）。

**已確認（2026-05-31）**：`coc MF_Shop`、`set GameHour to 12`，與 Marcurio 對話 → 以物易物選單開啟。

## 「NPC 的被動特技」（PERK——能力 + 入口點）

兩種特技形態，均可透過 `npcs[].perks` 附加到 NPC（角色在遊戲開始時獲得）：

```jsonc
{ "magicEffects": [
    { "editorId": "MF_IronHideMgef", "archetype": "ValueModifier", "actorValue": "DamageResist",
      "castType": "ConstantEffect", "targetType": "Self",
      "flags": [ "Recover", "NoArea", "NoDuration", "HideInUI" ] }
  ],
  "spells": [
    { "editorId": "MF_IronHideAbility", "name": "Iron Hide", "spellType": "Ability",
      "castType": "ConstantEffect", "targetType": "Self",
      "effects": [ { "magicEffect": "MF_IronHideMgef", "magnitude": 50 } ] }
  ],
  "perks": [
    // (a) 能力特技——授予上方的持續效果 SPEL
    { "editorId": "MF_IronHidePerk", "name": "Iron Hide", "numRanks": 1,
      "effects": [ { "kind": "ability", "spell": "MF_IronHideAbility" } ] },
    // (b) 入口點特技——揮劍時攻擊傷害 +20%，單手武器達 30 時可用
    { "editorId": "MF_DeadlyStrikesPerk", "name": "Deadly Strikes", "numRanks": 1,
      "conditions": [
        { "function": "GetBaseActorValue", "actorValue": "OneHanded",
          "comparison": "GreaterThanOrEqualTo", "value": 30 } ],
      "effects": [
        { "kind": "entryPoint", "entryPoint": "ModAttackDamage", "function": "Multiply", "value": 1.2,
          "conditions": [
            { "function": "WornHasKeyword", "param": "Skyrim.esm:0x01E711",   // WeapTypeSword
              "comparison": "EqualTo", "value": 1 } ] } ] }
  ],
  "npcs": [
    { "editorId": "MF_PerkGuard", "name": "Hardened Guard", "race": "Skyrim.esm:0x013746",
      "perks": [ "MF_IronHidePerk", "MF_DeadlyStrikesPerk" ] }
  ] }
```

- 透過 `perkdiag <Skyrim.esm> entrypoints` 和 `perkdiag <Skyrim.esm> 0x079343`（Armsman20）確認入口點名稱及可參考的原版結構。
- **玩家特技**並非僅靠資料記錄即可：需透過 Papyrus 的 `AddPerk` 呼叫（一個 `scripts` 任務片段）來授予。
- 這些在結構上與原版特技完全相同；但修改值是否真的影響遊戲內的戰鬥數值，需要實際啟動 Skyrim 才能確認。完整範例：[`../../examples/perk_spec.json`](../../examples/perk_spec.json)。

## 「多階段任務」（階段 + 日誌記錄 + 目標 + 對話設定階段）

一個會**推進**的任務：在各階段間推進、在每個階段寫入日誌文字、依階段顯示/完成目標，並在最終階段關閉任務。

```jsonc
{ "quests": [ {
    "editorId": "MF_ErrandQuest", "name": "A Forged Errand",
    "startGameEnabled": true, "priority": 60,
    "stages": [
      { "index": 10, "logEntry": "Joren asked me to retrieve his lost hammer." },
      { "index": 20, "logEntry": "I agreed to help. Time to search the riverbank." },
      { "index": 30, "logEntry": "I returned the hammer. Done.", "completeQuest": true } ],
    "objectives": [
      { "index": 10, "text": "Agree to help Joren", "showStage": 10, "completeStage": 20 },
      { "index": 20, "text": "Find Joren's hammer",  "showStage": 20, "completeStage": 30 } ] } ],
  "dialogue": [
    { "editorId": "MF_AgreeToHelp", "questEditorId": "MF_ErrandQuest", "speakerNpcEditorId": "MF_Joren",
      "prompt": "I'll find your hammer.", "responses": [ "Good. It's by the mill." ],
      "setStage": 20 } ] }                                 // 選擇此選項後任務從 10 → 20
```

**`package` 自動處理 Papyrus——無需 CK（遊戲內已確認 It.36 2026-06-02）。**
它會生成、編譯並以 VMAD 附加所有內容：

- `Scripts/Source/MF_ErrandQuest_Stages.psc` — 每個階段各有 `Fragment_Stage_XXXX_Item00000()`，用於顯示/完成目標。引擎在 `SetStage()` 觸發時按名稱呼叫此函式。
- `Scripts/Source/TIF_MF_AgreeToHelp.psc` — `extends TopicInfo Hidden`，帶有明確的 `Quest Property OwningQuest Auto`（綁定至任務的 FormKey），`OnBegin` 呼叫 `OwningQuest.SetStage(20)`。**請勿使用 `GetOwningQuest()`——對 StartGameEnabled 任務回傳 None**（見 gotchas.md）。
- 編譯後的 `.pex` 複製至 `Scripts/`，VMAD 附加至 QUST 和 INFO。
- `setStage` 對話行上自動加入 `GetStage(quest) < 20` 條件，使 Joren 在玩家已選取後不再重複觸發。

`package` 指令需要 `~/tools/papyrus-compiler`（Linux 原生；備用方案為 Wine/CK）。對話仍然只在遊戲**載入**時才會登錄——見 [gotchas.md](gotchas.md)。
