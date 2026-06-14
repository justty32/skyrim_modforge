# Animation §5 — Getting a custom animation to actually PLAY (the integration layer)

← [animation index](README.md)

The real deliverable. Three tiers, easiest→hardest:

### (a) Replace an existing animation (zero behavior edits)
Drop your `.hkx` at a **vanilla animation path** (e.g. `...\animations\mt_idle.hkx`). The graph already references that path → it plays your motion. **Pros:** no behavior editing, immediate. **Cons:** global override (every actor playing that idle now plays yours). Simplest win, perfect MVP.

### (b) IDLE record + existing behavior (what ModForge already does)
The graph exposes a finite set of **idle handles / animation events.** An **IDLE record** (`PlayIdle` / `Debug.SendAnimationEvent`) triggers a clip *through a handle the graph already has*. ModForge drives this via the SCEN SceneAdapter `PlayIdle` fragment. **Addressable space without touching behavior = the set vanilla already wires** (bows, gestures, furniture idles, the offset/IdleGive/IdleSilentBow family already decoded). You **cannot** introduce a genuinely new motion category this way — only ride existing handles (and, with (a), replace what a handle points at).

### (c) New animations via a framework (the modern answer)
To **add** animations without hand-editing Havok behavior, use a framework that *patches/generates the graph for you*:
- **FNIS** (legacy) / **Nemesis** (your baseline) — generate patched behavior `.hkx` from a mod-supplied list. Nemesis more capable but a Windows exe (Linux problem, [§6](linux-workflow-modforge.md)).
- **DAR (deprecated) → OAR (Open Animation Replacer)** — SKSE-plugin frameworks doing **condition-based replacement at runtime**: register a folder of replacement clips + a condition set, OAR swaps them in-engine.
- **Pandora Behaviour Engine+** — the modern, *cross-platform .NET* Nemesis/FNIS replacement ([§6](linux-workflow-modforge.md)).

**OAR is the pragmatic modern answer for a record-layer tool — its registration is pure folder + JSON, fully generatable, no `.esp` required.** OAR 是 SKSE plugin、開源、涵蓋 SE/AE/VR + 1.5.97，做**執行時條件式動畫替換**，內建遊戲內編輯器（Shift+O），隨時可裝可移除、不寫存檔。逐步動手做見姊妹文件 [oar-replacer-guide.md](oar-replacer-guide.md)；這裡只講「它是什麼、為何是最高槓桿整合點」。

**結構（2.0.0 起的關鍵更正）**：OAR 區塊**可放在 `Data\Meshes` 底下任何位置**，不再強制在 `animations\` 下（舊筆記的「must be under animations\」已過時）。心智模型是「在原始動畫路徑中插入一個 OAR 區塊以攔截它」。要替換 `Data\Meshes\actors\character\animations\male\mt_idle.hkx`，就放到：
```
Data\Meshes\OpenAnimationReplacer\<ModName>\
   config.json                 ← {name, description}  ← replacer-mod 層
   <SubmodName>\
      config.json              ← {name, description, priority, conditions[...], 可選 feature flags}
      user.json                ← 可選；覆寫 config.json（name/description 除外），可安全刪除
      actors\character\animations\male\mt_idle.hkx   ← 與被替換 vanilla anim 同檔名
```
（OAR 區塊也可插在路徑任意一點，效果相同。）

- **兩層 config.json**：replacer-mod 層只有 `{name, description}`；**submod 層**才有 `priority`（高者勝）+ **`conditions` 陣列**。**priority 寫在 config.json 裡，不靠資料夾名**（與 DAR 不同；資料夾可任意命名，避免非英文字元）。OAR 用被替換 clip 的路徑/檔名比對，套用條件通過者中 priority 最高的 submod。
- **conditions 比 DAR 更強**：每條有 `negated` + `requiredVersion`（部分還需另一 plugin）；數值比較值可為 **static 值 / global variable / Actor Value / behavior-graph variable**；**keyword 用 EditorID**（如 `WeaponKatana`）；容器條件 **OR / AND 可無限巢狀**。代表性條件：`IsActorBase(plugin,formID)`、`Random`、`IsEquippedType`、`IsWornHasKeyword`、`IsInFaction`、AV/global/graph 變數比較…完整清單在編輯器 tooltip。
- **user.json 覆寫 config.json**（name/description 除外）：玩家微調不動發行檔，可安全刪除。編輯器三模式：**Inspect / User（寫 user.json）/ Author（寫 config.json）**。
- **Variants（1.2.0+）**：子資料夾 `_variants_<animNameWithoutExt>`（如 `_variants_mt_idle`）放多個變體 hkx（檔名隨意，用 `1.hkx`/`2.hkx`），每個變體在編輯器設 **weight**；**Sequential mode（2.2.0+）**按序播放、可加 per-variant「Play once」。
- **Presets（2.2.0+）**：在 replacer-mod config 定義可重用的條件區塊，submod 用特殊 `PRESET` 條件引用（submod 只存 preset 名）——去重。
- **Functions**：submod 可在動畫開始/結束/指定動畫事件時觸發遊戲事件（function sets / multifunctions，multifunction 可帶條件集）。OAR 加了自訂動畫事件 **"OAR"**（免 behavior patch），帶 payload（如 `OAR.sound1`），讓動畫師不必盜用 vanilla 事件就能觸發 PlaySound 等。其他 SKSE plugin 可經 API 加自訂 function/condition。
- **進階 submod 設定**：Interruptible（持續輪詢條件→情境變化即時換+blend，小幅效能成本）、keep random results on loop/echo、share random across submod、自訂 blend time、忽略「No Triggers」flag、required project name（如 DefaultMale/DefaultFemale）、**override animation-folder name**（多 submod 共用同一份動畫免複製）。
- **前置**：需 SKSE、Address Library、Animation Queue Fix（除非用 skip-preload 實驗選項）、Paired Animation Improvements；**需 Nemesis 或 Pandora 跑一次**建立 base behavior，但 **OAR 本身完全不改 behavior**——只在其上做 runtime 條件式替換。**DAR 相容**：`DynamicAnimationReplacer` 資料夾原樣讀入並轉換，所有 DAR mod 成為一個叫 "Legacy" 的大 replacer-mod 的 submod，priority 沿用 DAR 的資料夾名規則。注意 Windows **260 字元路徑上限**，資料夾名要短。

**Can ModForge generate the OAR structure? Yes — 不僅可以，是整條動畫管線中槓桿最高的整合點。** 整個交付物（資料夾樹 + 兩層 `config.json`，含 name/description/priority/conditions/variants/presets/functions）是**純確定性的 record+asset 產物**——沒有 Havok、沒有 `.esp`、沒有 behavior 編輯。其 condition 模型（plugin|formID 引用、AV/global 比較、keyword EditorID、negated、巢狀 AND/OR）幾乎一對一對映 ModForge 既有的 CTDA condition 支援；唯一非確定性的 `.hkx` 動畫本體屬另一條管線（見 [havok-blender.md](havok-blender.md)）。**OAR 讓「整合層」從『改不得的 binary 狀態機』降為『生 JSON + 擺檔案』——這正是 ModForge 的主場。最高槓桿整合目標。**
