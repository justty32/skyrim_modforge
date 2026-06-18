# 用 ModForge 生成 + 常見地雷/Checklist

← [oar-replacer-guide](oar-replacer-guide.md)

## 10. 用 ModForge 生成

### 為什麼這是最高槓桿整合點
整個 OAR 交付物——**資料夾樹 + 兩層 config.json（name/description/priority/conditions/variants/presets/functions）**——是**純確定性的 record+asset 產物**：沒有 Havok、沒有 `.esp`、沒有 behavior 編輯。這正是 ModForge 的主場（見 [`integration-layer.md`](../../../workflows/idea/asset-pipelines/animation/integration-layer.md) §5）。唯一非確定性的 `.hkx` 動畫本體屬另一條管線（Havok/Blender，見 [`havok-blender.md`](../../../workflows/idea/asset-pipelines/animation/havok-blender.md)），ModForge 只負責「擺檔 + 生 JSON + 接線」。

### 現在能生成（既有能力可重用）
- **資料夾樹**：`OpenAnimationReplacer\<Mod>\<Submod>\<vanilla 路徑>\` 是純路徑生成。
- **兩層 config.json**：name/description/priority/conditions 是純 JSON 序列化。
- **condition 模型天然契合 ModForge 既有 CTDA 支援**：OAR 的條件（`Plugin|FormID` 引用、AV/global/graph/static 比較、keyword EditorID、`negated`、巢狀 AND/OR）幾乎一對一對映 ModForge 既有的 CTDA condition 結構。把 ModForge 內部的 condition 模型**序列化成 OAR JSON 形狀**即可重用——比從零造條件系統便宜得多。

### 還缺的 generator
- **OAR submod 序列化器**：把「替換規格」轉成資料夾樹 + config.json。
- **CTDA → OAR condition 對映器**：把既有 condition 模型 emit 成 OAR 的 `{condition, requiredVersion, negated, ...}` 形狀（含 static/global/AV/graph 值型別與巢狀容器）。
- **`.hkx` 本體**：不在此 generator 範圍——交給 Havok/Blender 管線（[`havok-blender.md`](../../../workflows/idea/asset-pipelines/animation/havok-blender.md)），這裡只把成品擺進正確路徑。

### 未來 spec 欄位構想（proposal，非現況）

一個可能的 ModForge spec 片段長相（**僅為後續實作參考，非目前已支援**）：

```jsonc
// PROPOSAL — 尚未實作
{
  "animationReplacer": {
    "mod":  { "name": "My Idle Swap", "description": "..." },   // replacer-mod 層
    "submods": [
      {
        "name": "Player Katana Idle",
        "priority": 100,
        "replaces": "actors/character/animations/male/mt_idle.hkx", // 被替換的 vanilla 相對路徑
        "hkx": "build/anims/katana_idle.hkx",                       // 你提供的成品 .hkx（Havok 管線產）
        "conditions": [                                             // 對映既有 CTDA condition 模型
          { "condition": "IsActorBase", "form": "Skyrim.esm|0x000007" },
          { "condition": "IsWornHasKeyword", "keyword": "WeaponKatana" }
        ],
        "interruptible": true,                                      // 進階 flag
        "variants": [ "build/anims/v1.hkx", "build/anims/v2.hkx" ]  // 可選；生成 _variants_ 資料夾
      }
    ]
  }
}
```

ModForge generator 拿到後：建 `OpenAnimationReplacer\MyIdleSwap\` 樹、emit replacer-mod 與 submod 兩層 config.json、把 `conditions` 由內部模型序列化成 OAR 形狀、把 `hkx`/`variants` 擺進重建的 vanilla 路徑（變體放 `_variants_<name>\`）。**一句話分工**：資料夾 + JSON 由 ModForge 確定性產出；`.hkx` 動畫本體由 Havok/Blender 管線另計。

---

## 11. 常見地雷 / Checklist

**Do**
- `.hkx` 用**與 vanilla 完全相同的檔名**，落在 submod 內**重建的 vanilla 相對路徑**下。
- `priority` 寫在 **submod config.json**，撞片時靠它仲裁（**不**靠資料夾名——那是 DAR）。
- 出貨 config.json 是**合法 JSON**：拿掉所有 `//` 教學註解。
- 條件用對值型別（static / global / AV / graph）；keyword 用 **EditorID**（如 `WeaponKatana`）。
- 要即時反應（裝備一變就換）就開 **Interruptible**。
- 多 submod 共用動畫用 **override animation-folder name**，別各複製一份。
- 先用編輯器 **User 模式**（寫 user.json）試條件，確定後再用 **Author 模式**寫回 config.json。
- 用編輯器選 actor 看**條件燈號**除錯——哪條沒亮就改哪條。

**Don't**
- 別忘了**跑一次 Pandora**（2026 標準，Nemesis/FNIS 已 legacy）建 base behavior——OAR 不自己改 behavior，沒有基底就沒得替換。
- 別用**非英文字元**當資料夾名；別讓路徑超過 Windows **260 字元**上限（資料夾名取短）。
- 別以為 priority 看資料夾名——OAR 看 config.json 的 `priority`（這是與 DAR 的關鍵差異）。
- 別漏掉執行時前置：SKSE / Address Library / Animation Queue Fix / Paired Animation Improvements。
- 條件要引用某 `.esp` 的 record（faction / keyword / actor base）時，FormID 用 **`Plugin.esp|0xFormID`** 形式（load-order 無關），別寫裸 index。
- 別期待這份指南教你做 `.hkx`——動畫本體（Havok 牆、retarget、win32↔amd64）見 [`havok-blender.md`](../../../workflows/idea/asset-pipelines/animation/havok-blender.md)。
- 別在沒裝 OAR 的環境測——它是 SKSE plugin，缺了就完全不生效（且不報錯）。

---

> 原理深水區（OAR 在四層動畫堆疊的定位、為何最高槓桿、與 DAR/FNIS/Nemesis/Pandora 的關係）見 [`integration-layer.md`](../../../workflows/idea/asset-pipelines/animation/integration-layer.md) §5；`.hkx` 動畫本體的製作管線見 [`havok-blender.md`](../../../workflows/idea/asset-pipelines/animation/havok-blender.md)。本指南只負責「照著做就能裝出一個會動的 OAR 替換」。
