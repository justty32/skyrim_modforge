# 遊戲內場景匯出 — `references[]` 兩類目標、persistent 處置、build 順序、DLL 端與範例

← [ingame-scene-export-design](../ingame-scene-export-design.md)（入口）

**兩類目標（backlog 🔑 洞察，語意不同）：**

| | (乙) 檔內相依 | (甲) 外部既有 ref |
|---|---|---|
| `ref` | 同檔 `placements[]` 的 **editorId** | `<master>:0xFORMID` |
| 誰擁有它 | **我們**（玩家 `sc pl` 擺的，exporter 寫進 placements[]） | vanilla / 他 mod |
| persistent | build **強制** persistent（0x400 ＋ cell 的 Persistent group；機制同 linkedRefs target / package anchor） | 看它自己 |
| 🔴 坑 | 無——**乾淨路徑** | vanilla 場景物件多為 **temporary** → 不是可靠的 specific-reference 目標 |
| 離線可測 | ✅ | ❌（要 master link cache 查 0x400） |

**🔴 persistent 坑的處置（甲路徑）**：build 用 master link cache 解出該 ref，檢查 record header 的 **0x400 persistent flag**：

- 有 → 無警告，label 直接指它。
- 沒有 → **明確警告**（temporary ref；quest alias 的 specific-reference fill / package SingleRef target 可能撐不過 save/load），label 仍指它（不靜默丟）。
- `anchor` ＝逃生門，**ModForge 在該點放自己的 persistent 錨點**：
  - `"marker"` → 該座標生一支 **persistent XMarkerHeading**（0x400），**label 綁到 marker**。適用「只需要一個*地點*」（sandbox / travel / patrol 錨點）。
  - `"replace"` → 用 `base` 在該點生**我們自己的 persistent 複製品**（同 base/transform），**label 綁到複製品**，並把 vanilla 原件**自動加進 removals**（disable＋深埋，避免兩張椅子疊在一起）。適用「錨點必須*就是那個物件*」（坐**那張**椅子）。
  - 乙路徑上設 `anchor` ＝ validate 報錯（本來就是我們的、本來就 persistent）。

**Build 順序**（`Build.cs`）：`BuildPlacements` → `BuildMapMarkers` → **`BuildReferences`** → `BuildOverrides` → `BuildRemovals` → 全部 wire 步驟。必須在 placements 之後（檔內目標要先存在）、在 wire 之前（label 才解析得到）。`references[]` 為空時**不生任何記錄**（行為不變）。

**✅ DLL 端已補齊（2026-07-12，DLL crc `112be269`）**：`sc ref` / `sc ref <Label>` / `sc refc [Label]` ＋ referrer 模式（`src/Referrer.{h,cpp}`）＋ References 面板頁（`src/UI.References.cpp`）＋ exporter 吐 `references[]`。**(乙) 檔內相依的實作**：`AppendPlacements` 掃到的 ref 若是某個 referrer 的目標（identity ＝ ObjectRefHandle——dynamic ref 沒有耐久 id 可比），就在該 placement 上蓋一個穩定 editorId `MFRef_<sanitize(label)>_<seq>`（seq 隨 co-save 故跨匯出穩定），`references[].ref` 指它；`AppendReferences` **跑在 AppendPlacements 之後**，且**只吐這次匯出真的有出 placement 的那些**（cell 沒掃到／物件被擦掉／跨重啟 handle 撿不回 ⇒ 跳過＋warn，絕不吐一個檔內找不到的 editorId）。外部目標照記耐久 id ＋ base ＋座標/rotation/scale；**`anchor` DLL 一律不填**（留白＝`none`，選擇權在 ModForge/agent）。拒收 marker proxy（editor chrome，本就被 `ExportCell` 排除）、自家 dynamic actor（cell 匯出不含 actor）、重複 label（label 是全域名字空間）。

**範例**：
- **`examples/referrer-chair-anchor.json` ＝ 權威範例／實機價值證明**（2026-07-12 交付 `~/skyrim_mods/mine/ModForgeReferrerChair.zip`）。乙路徑端到端＋**對照組**：vanilla WhiterunBreezehome（**必須是 vanilla cell——自建內裝沒 navmesh，NPC 不會動**）裡擺**兩張一模一樣的 `CommonChair01F`**，`references[]` **只命名其中一張**，Sofia 的 **SitTarget** package 拿 label 當 slot-16 SingleRef 錨點。誘餌椅**更近、擋在必經路上**，所以「她坐了某張椅子」不可能被誤讀成「她坐了被命名的那張」。離線證據：命名的 REFR `0x808` flag=`0x400` ＋ Persistent group；誘餌 `0x807` flag=`0x0` ＋ Temporary group（**兩者只差一個 label**）；package slot 16 ＝ `PackageTargetSpecificReference(0x808)`，**不需要 quest alias**，與 vanilla `CaravanACamp1Sit` 同形。
- `examples/scene-references.json`＝**(甲) 外部路徑 ＋ `anchor` 逃生門**的示範（Skulvar 的鋤頭：temporary → 警告）。它的乙路徑那半也已改用 `sitTarget`（原本用 `sandbox.location`，見上面 🔴（[references](references.md)） —— 那份寫法 build 全綠卻證明不了任何事，是誤導後續 agent 的樣板）。

測試：`tests/ModForge.Core.Tests/Build/ReferencesTests.cs`。
