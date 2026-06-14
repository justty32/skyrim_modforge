# 自己動手做一個 Open Animation Replacer 動畫替換（OAR 實作指南）

> 這是一份**動手教學**：跟著走完，你會得到一個能在遊戲裡「在指定條件下、給指定角色、替換掉某個 vanilla 動畫」的 OAR 替換包——**不需要 `.esp`**，整個交付物就是資料夾 + JSON + 你的 `.hkx`。
> 技術原理（OAR 在四層動畫堆疊裡的定位、為何是最高槓桿整合點）請看姊妹文件 [`integration-layer.md`](integration-layer.md) §5；動畫 `.hkx` 本體怎麼從 Blender/mocap 做出來（Havok 牆、retarget、win32↔amd64 轉換）見 [`havok-blender.md`](havok-blender.md)。本文**不重複**那兩塊，只在需要時連過去。
> 慣例：散文用繁體中文，所有 JSON key / condition 名 / 路徑 / EditorID 保留 English。
> 權威來源：OAR 官方 Nexus 頁（#92109，v3.1.5 規格）。

---

## 1. 總覽：要做出一個 OAR 替換，需要哪些拼圖

OAR 把「整合層」（讓動畫真的播得出來的那一層）從**改不得的 Havok behavior 狀態機**，降成**擺檔案 + 寫 JSON**。一個替換包由以下幾塊拼起來：

| # | 零件 | 放哪 | 做什麼 | 必要性 |
|---|------|------|--------|--------|
| A | **replacer mod 資料夾** | `Data\Meshes\OpenAnimationReplacer\<ModName>\` | 一個 mod 的容器；裡面裝一個或多個 submod | **必要** |
| B | **replacer-mod `config.json`** | 上面那層 | `{name, description}`——只標示這個 mod 叫什麼 | **必要** |
| C | **submod 資料夾** | `<ModName>\<SubmodName>\` | 一條替換規則的容器 | **必要** |
| D | **submod `config.json`** | submod 層 | `{name, description, priority, conditions[...]}` + 可選 feature flags——**真正的邏輯都在這** | **必要** |
| E | **替換用 `.hkx`** | submod 內、**重建 vanilla 路徑** | 與被替換 vanilla anim **同檔名**的你的動畫 | **必要** |
| F | **`user.json`**（可選） | submod 層 | 玩家端覆寫 config.json（name/description 除外），可安全刪除 | 可選 |

**心智模型**：**OAR 用「被替換 clip 的路徑/檔名」比對；在所有條件通過的 submod 中，套用 `priority` 最高的那個的 `.hkx`。** OAR 自己（`OpenAnimationReplacer.dll`）只提供「runtime 條件比對 + 換片 + blend」引擎，它**完全不改 behavior graph**——所以才不需要 `.esp`、不寫存檔、隨時可裝可移除。

最小可行產物（MVP）= **A + B + C + D（含一條 `IsActorBase(player)` 條件）+ E（一個替換 idle 的 `.hkx`）**。其餘（variants / presets / functions / 進階 flag / user.json）都是加值。

---

## 2. 前置需求

### 玩家端（執行時依賴）
- **Open Animation Replacer**（Nexus #92109）這個 SKSE plugin 必裝。
- 對應版本的 **SKSE64**、**Address Library**。
- **Animation Queue Fix**（除非啟用 skip-preload 實驗選項）、**Paired Animation Improvements**。
- **跑過一次 Nemesis 或 Pandora** 以建立 base behavior——這一步建立 behavior graph 的基底；**OAR 本身不加任何 behavior 編輯**，只在其上做條件式替換。（Linux/Wine 下 Nemesis 有 thread-race 問題，改用 Pandora；見 [`havok-blender.md`](havok-blender.md) 與 §6 linux-workflow。）

### 作者端（製作時用得到）
- 一個文字編輯器寫 JSON（出貨檔必須是**合法 JSON**——本指南範例裡的 `//` 註解是教學用，實檔要拿掉）。
- 你要替換進去的 `.hkx`：本指南**不負責**怎麼做出這個檔，那是 Havok/Blender 管線的事（見 [`havok-blender.md`](havok-blender.md)：retarget → win32 → serde-hkx 轉 amd64）。本文假設你已經有一個對齊 Skyrim skeleton、`hk_2010`、amd64（SE）的 `.hkx`。
- 若條件要引用某個 `.esp` 裡的 record（faction、keyword、actor base…），需要那個 plugin 的檔名 + 本地 FormID。

---

## 3. Step 1 — 規劃這個替換

動手擺檔案前，先把四件事想清楚：

1. **替換哪個 vanilla 動畫路徑**：例如 `Data\Meshes\actors\character\animations\male\mt_idle.hkx`（男性閒置）。你的 `.hkx` 最後要用**同檔名**（`mt_idle.hkx`）落在重建出的同一段相對路徑下。
2. **給誰**：全部角色？只玩家？某 race / faction？拿katana 的人？這決定 `conditions`。
3. **什麼條件**：上面那句翻成具體 condition（見 §6）。最常見的 MVP 是「只玩家」= `IsActorBase("Skyrim.esm", 0x000007)`。
4. **優先序**：和別的 OAR mod / DAR mod 撞同一個 clip 時誰勝。`priority` 數字大者勝。先給一個夠大的值（如 `100`）。

### 範例（本指南全程用它）

做一個叫 **`MyIdleSwap`** 的 replacer mod，內含一個 submod **`PlayerKatanaIdle`**：**當玩家裝備 katana 時**，把男性閒置動畫 `mt_idle.hkx` 換成自製的霸氣持刀站姿。

---

## 4. Step 2 — 資料夾結構

**2.0.0 起的關鍵事實**：OAR 區塊**可放在 `Data\Meshes` 底下任意位置**（舊資料說「必須在 `animations\` 下」已過時）。心智模型是「在原始動畫路徑中插入一個 `OpenAnimationReplacer\<Mod>\<Submod>\` 區塊以攔截它」。

要替換 `Data\Meshes\actors\character\animations\male\mt_idle.hkx`，最直觀的擺法是把 OAR 區塊放在 `Meshes` 之後、其餘 vanilla 路徑原樣重建：

```
Data\Meshes\OpenAnimationReplacer\
  MyIdleSwap\
     config.json                          ← replacer-mod 層 {name, description}
     PlayerKatanaIdle\
        config.json                       ← submod 層 {name, description, priority, conditions}
        actors\character\animations\male\
           mt_idle.hkx                     ← 你的動畫，檔名與 vanilla 完全相同
```

要點：
- **submod 內部，被替換路徑要按 vanilla 原樣重建**（`actors\character\animations\male\mt_idle.hkx`），OAR 就是靠這段相對路徑 + 檔名來比對它替換的是哪個 clip。
- **OAR 區塊可插在路徑任意一點**也行（例如插在 `male\` 前），效果相同；放在 `Meshes\` 後最易讀。
- **資料夾名（`MyIdleSwap` / `PlayerKatanaIdle`）可任意命名**——priority 不靠資料夾名（這點與 DAR 不同），但**避免非英文字元**，且注意 Windows **260 字元路徑上限**（資料夾名取短）。
- **DAR 相容**：放進 `DynamicAnimationReplacer` 資料夾的東西 OAR 會原樣讀入並轉換（成為一個叫 "Legacy" 的大 replacer-mod 的 submod，priority 沿用 DAR 資料夾名規則）。新作品一律用 OAR 結構，不用 DAR。

---

## 5. Step 3 — 寫 config.json（兩層逐欄）

### 5.1 replacer-mod 層 `config.json`

`MyIdleSwap\config.json`——只標示這個 mod：

```jsonc
{
  "name": "My Idle Swap",                  // 在遊戲內編輯器裡顯示的 mod 名
  "description": "Condition-based idle replacements."
}
```

（presets 也定義在這一層——見 §6 末。）

### 5.2 submod 層 `config.json`（核心）

`MyIdleSwap\PlayerKatanaIdle\config.json`——真正的邏輯：

```jsonc
{
  "name": "Player Katana Idle",            // 編輯器顯示的 submod 名
  "description": "Cool katana stance for the player when a katana is equipped.",
  "priority": 100,                         // 數字大者勝；和別的 submod 撞同一 clip 時用它仲裁
  "conditions": [                          // 全部通過才套用這個 submod（見 Step 4 詳解）
    {
      "condition": "IsActorBase",
      "requiredVersion": "1.0.0.0",
      "Actor base": "Skyrim.esm|0x000007"  // 只對玩家（Player actor base）
    },
    {
      "condition": "IsEquipped",
      "requiredVersion": "1.0.0.0",
      "negated": false,
      "Form": "Skyrim.esm|0x0001397E",     // 範例：某把武器；實務可改用 keyword 條件
      "Left hand": false
    }
  ]
}
```

**逐欄要點**：
- **`name` / `description`**：給遊戲內編輯器顯示用；**這兩欄 `user.json` 不能覆寫**（其餘都能）。
- **`priority`**：整數，高者勝。OAR 對同一被替換 clip，挑出**所有條件通過**的 submod 裡 priority 最高那個套用。撞優先序就調這個數字。
- **`conditions`**：陣列，**全部為真**才算這個 submod 命中（要「或」邏輯用 §6 的容器條件）。每條至少有 `condition`（條件名）與 `requiredVersion`；多數還有自己的參數欄位（如 `IsActorBase` 的 `Actor base`）。
- 出貨前**拿掉所有 `//` 註解**（OAR 讀的是嚴格 JSON）。

> 實務上不必手寫——遊戲內編輯器（Author 模式，§9）會幫你生成這份 JSON。但理解它的 schema 才能讓 ModForge 程式化產生（§10）。

---

## 6. Step 4 — 條件系統

OAR 的條件 = DAR 全部條件 + 許多新增。這是它真正的威力所在。

**每條條件的通用欄位**：
- **`condition`**：條件名（如 `IsActorBase`、`Random`、`IsEquippedType`、`CompareValues`…）。
- **`requiredVersion`**：此條件起作用的最低 OAR 版本（編輯器會填）。
- **`negated`**（可選，預設 false）：反轉這條的真假。
- 部分條件還**需要另一個 plugin** 才可用（編輯器會標示）。

**數值比較值可以是四種型別之一**（這點是 OAR 的關鍵彈性）：
- **static value**：寫死的常數。
- **global variable**：引用一個 GLOB（`Plugin.esp|FormID`）。
- **Actor Value**：某個 AV（如 health、某 skill）。
- **behavior-graph variable**：behavior graph 裡的變數。

**Keyword 用 EditorID 指定**（如 `WeaponKatana`），不必查 FormID。

**容器條件可無限巢狀**：用 **OR** / **AND** 容器把子條件包起來，組出任意布林邏輯。例如「(玩家 AND 裝katana) OR (是某 faction 成員)」。

**代表性條件**（完整清單在編輯器 tooltip，這裡列類別）：
- 身分／陣營：`IsActorBase("Plugin.esm", 0xFormID)`、`IsInFaction`、`IsRace`…
- 裝備／物品：`IsEquipped`、`IsEquippedType`、`IsWornHasKeyword`（keyword 用 EditorID）…
- 數值比較：把 AV / global / graph variable / static 互比（大於、等於…）。
- 隨機：`Random`（配 variants 做隨機變體）。
- 巢狀：`OR` / `AND` 容器。
- **`PRESET`**：引用 replacer-mod config 裡定義好的條件區塊（見下）。

**PRESET（2.2.0+，去重利器）**：在 **replacer-mod 層 config.json** 定義可重用的條件區塊，submod 用一條特殊 `PRESET` 條件引用它（submod 只存 preset 名，內容住在 replacer-mod config）。多個 submod 共用同一組複雜條件時，改一處即可。

```jsonc
// replacer-mod 層 config.json 裡：
{
  "name": "My Idle Swap",
  "description": "...",
  "presets": [
    {
      "name": "PlayerOnly",
      "conditions": [
        { "condition": "IsActorBase", "requiredVersion": "1.0.0.0",
          "Actor base": "Skyrim.esm|0x000007" }
      ]
    }
  ]
}
// submod 層 config.json 裡，用 PRESET 引用：
// "conditions": [ { "condition": "PRESET", "preset": "PlayerOnly" }, ... ]
```

---

## 7. Step 5 — variants（隨機／序列變體）

要讓同一個 clip 有多個變體（隨機挑或按序播），用 **`_variants_` 子資料夾**（1.2.0+）。

在被替換 clip 的同層，建一個 `_variants_<animNameWithoutExt>` 資料夾（去掉副檔名），裡面放變體 `.hkx`（**檔名隨意**，慣例 `1.hkx`、`2.hkx`…）：

```
PlayerKatanaIdle\actors\character\animations\male\
   _variants_mt_idle\          ← 注意：對應 mt_idle.hkx，去掉 .hkx
      1.hkx
      2.hkx
      3.hkx
```

- **weight**：每個變體的權重在**遊戲內編輯器**設（決定隨機被選機率）。
- 尊重 submod 設定的 **keep random on loop/echo**（迴圈/回放時是否重抽）與 **share random results**（整個 submod 共用同一次隨機結果）。
- **Sequential mode（2.2.0+）**：改成**按順序播放**而非隨機；可給每個變體 **「Play once」** flag；sequence/history 在 clip 一段時間沒被觸發後重置。

---

## 8. Step 6 — 進階 submod 設定 + functions

這些都是 submod 層的可選 feature flag（在編輯器設，落到 config.json）：

**進階替換行為**：
- **Interruptible**：持續輪詢條件，情境一變就**立刻換片 + blend**（不等動畫播完）；有小幅效能成本。做「裝武器瞬間切站姿」這種即時反應要開它。
- **keep random results on loop/echo**：迴圈時不重抽變體。
- **share random results**：整個 submod 共用一次隨機結果。
- **custom blend time on interrupt**：被打斷換片時的過場時間。
- **ignore "No Triggers" clip flag**：忽略某些 clip 的 No-Triggers 標記。
- **required project name**：限定 behavior project（如 `DefaultMale` / `DefaultFemale`）。
- **override animation-folder name**：讓多個 submod **共用同一份動畫檔**而不必各複製一份——省空間、好維護。

**Functions（在動畫事件上觸發遊戲事件）**：
- submod 可指定在**動畫開始 / 結束 / 某個指定動畫事件**時觸發遊戲事件（如 PlaySound）。
- **function sets / multifunctions**：multifunction 可帶一個條件集——子 function 只在條件通過時才跑。
- **OAR 自訂動畫事件 `"OAR"`**：OAR 加了一個自訂動畫事件（**免 behavior patch**），帶 payload（如 `OAR.sound1`），讓動畫師**不必盜用 vanilla 事件**就能掛 function 觸發音效等。
- 其他 SKSE plugin 可經 OAR 的 API 加**自訂 function / condition**。

---

## 9. Step 7 — 測試（遊戲內編輯器）

OAR 的測試主力是**遊戲內編輯器**（預設 **Shift+O** 開）：

1. **檔案落位檢查**：`Data\Meshes\OpenAnimationReplacer\MyIdleSwap\config.json` 與 `PlayerKatanaIdle\config.json` 都是**合法 JSON**（拿掉教學註解），`.hkx` 在重建的 vanilla 相對路徑下、檔名與 vanilla 相同。
2. **進遊戲、開編輯器（Shift+O）**，找到你的 mod → submod。三種模式：
   - **Inspect**：唯讀檢視，不寫任何檔。
   - **User**：改動寫進 **`user.json`**（覆寫 config.json，name/description 除外）——拿來臨時試條件最安全。
   - **Author**：改動寫回 **`config.json`**（作者發行檔）。
3. **選一個 actor 看條件燈號**：編輯器會對選中的 actor **即時顯示每條 condition 通過與否**——這是除錯條件的主力（哪條沒亮就知道哪裡寫錯）。
4. **觸發那個動畫**（讓玩家進入閒置 / 裝上 katana），看是否換成你的 `.hkx`；條件不該命中的角色不該被換。
5. 試完用 **User 模式**寫的 `user.json` 可直接刪掉還原。

> OAR 隨時可裝可移除、不寫存檔，所以反覆試錯成本很低——這是它相對於 esp-based 整合的一大優勢。

---

## 10. 用 ModForge 生成

### 為什麼這是最高槓桿整合點
整個 OAR 交付物——**資料夾樹 + 兩層 config.json（name/description/priority/conditions/variants/presets/functions）**——是**純確定性的 record+asset 產物**：沒有 Havok、沒有 `.esp`、沒有 behavior 編輯。這正是 ModForge 的主場（見 [`integration-layer.md`](integration-layer.md) §5）。唯一非確定性的 `.hkx` 動畫本體屬另一條管線（Havok/Blender，見 [`havok-blender.md`](havok-blender.md)），ModForge 只負責「擺檔 + 生 JSON + 接線」。

### 現在能生成（既有能力可重用）
- **資料夾樹**：`OpenAnimationReplacer\<Mod>\<Submod>\<vanilla 路徑>\` 是純路徑生成。
- **兩層 config.json**：name/description/priority/conditions 是純 JSON 序列化。
- **condition 模型天然契合 ModForge 既有 CTDA 支援**：OAR 的條件（`Plugin|FormID` 引用、AV/global/graph/static 比較、keyword EditorID、`negated`、巢狀 AND/OR）幾乎一對一對映 ModForge 既有的 CTDA condition 結構。把 ModForge 內部的 condition 模型**序列化成 OAR JSON 形狀**即可重用——比從零造條件系統便宜得多。

### 還缺的 generator
- **OAR submod 序列化器**：把「替換規格」轉成資料夾樹 + config.json。
- **CTDA → OAR condition 對映器**：把既有 condition 模型 emit 成 OAR 的 `{condition, requiredVersion, negated, ...}` 形狀（含 static/global/AV/graph 值型別與巢狀容器）。
- **`.hkx` 本體**：不在此 generator 範圍——交給 Havok/Blender 管線（[`havok-blender.md`](havok-blender.md)），這裡只把成品擺進正確路徑。

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
- 別忘了**跑一次 Nemesis/Pandora** 建 base behavior——OAR 不自己改 behavior，沒有基底就沒得替換。
- 別用**非英文字元**當資料夾名；別讓路徑超過 Windows **260 字元**上限（資料夾名取短）。
- 別以為 priority 看資料夾名——OAR 看 config.json 的 `priority`（這是與 DAR 的關鍵差異）。
- 別漏掉執行時前置：SKSE / Address Library / Animation Queue Fix / Paired Animation Improvements。
- 條件要引用某 `.esp` 的 record（faction / keyword / actor base）時，FormID 用 **`Plugin.esp|0xFormID`** 形式（load-order 無關），別寫裸 index。
- 別期待這份指南教你做 `.hkx`——動畫本體（Havok 牆、retarget、win32↔amd64）見 [`havok-blender.md`](havok-blender.md)。
- 別在沒裝 OAR 的環境測——它是 SKSE plugin，缺了就完全不生效（且不報錯）。

---

> 原理深水區（OAR 在四層動畫堆疊的定位、為何最高槓桿、與 DAR/FNIS/Nemesis/Pandora 的關係）見 [`integration-layer.md`](integration-layer.md) §5；`.hkx` 動畫本體的製作管線見 [`havok-blender.md`](havok-blender.md)。本指南只負責「照著做就能裝出一個會動的 OAR 替換」。
