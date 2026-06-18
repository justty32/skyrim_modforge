# 總覽 + 前置 + 規劃 + 資料夾結構

← [oar-replacer-guide](oar-replacer-guide.md)

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
- **跑過一次 Pandora** 以建立 base behavior（2026 標準；Nemesis/FNIS 已 legacy）——建立 behavior graph 基底；**OAR 本身不加任何 behavior 編輯**，只在其上做條件式替換。（Pandora＝.NET 8 開源跨平台、讀 Nemesis/FNIS mod；Linux 目前建議 Proton-wrap、headless 待驗；見 [`havok-blender.md`](../../../workflows/idea/asset-pipelines/animation/havok-blender.md) 與 §6 linux-workflow §6.0。）

### 作者端（製作時用得到）
- 一個文字編輯器寫 JSON（出貨檔必須是**合法 JSON**——本指南範例裡的 `//` 註解是教學用，實檔要拿掉）。
- 你要替換進去的 `.hkx`：本指南**不負責**怎麼做出這個檔，那是 Havok/Blender 管線的事（見 [`havok-blender.md`](../../../workflows/idea/asset-pipelines/animation/havok-blender.md)：retarget → win32 → serde-hkx 轉 amd64）。本文假設你已經有一個對齊 Skyrim skeleton、`hk_2010`、amd64（SE）的 `.hkx`。
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

