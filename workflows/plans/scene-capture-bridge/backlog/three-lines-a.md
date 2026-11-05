# scene-capture-bridge backlog — 2026-08-20 三條線全開：A 施法選單 ＋ 物品欄式預覽 ＋ 最愛

← [backlog](../backlog.md)（入口）

## 🗺️ 2026-08-20 使用者拍板：三條線全開

三條出自使用者同一次提問（「施法叫出物品欄式選單」「導航網格能不能在遊戲中編輯」「還要能調物件屬性——火把燃燒與否、門開關與否」）。**C 是 2026-07-14 就記在下面 ①（[open-directions](open-directions.md)）、當時排在 ② 之後、一直沒動工的那條**，這次一併重開。

**建議順序：A 的兩顆 spike（半天，會決定 A 剩下的值不值得做）→ B（一輪，值最高且無未知數）→ C（最長，動工前先挑第一批屬性）。**

---

### A. 施法叫出選單 ＋ 物品欄式預覽 ＋ 最愛

**訴求原文（2026-08-20）**：「使用一個技能後，跳出一個跟玩家物品欄一樣的介面，每個可擺放的東西都是其中的一個物品，跟原版一樣可以預覽其狀況，並且可以加入最愛。」

**這是 2026-07-14 那題的第二次提出**（見下面 ②，[open-directions](open-directions.md)）。拆成四件事，**三件已有答案、只有一件要做**：

| 成分 | 判定 |
|---|---|
| **技能觸發** | ✅ **要做，小工**。DLL sink `RE::TESSpellCastEvent`（拿得到 caster ＋ spell FormID）就能把施法接到既有模式切換，**零 Papyrus**。esp 多一顆 SPEL/MGEF。與 `sc <mode>` ＋ ini 動作鍵並存，不取代 |
| **「跟物品欄一樣的介面」** | ❌ **維持否決**（見下 ②，理由不變：STAT/TREE/FURN/ACTI/MSTT 進不了 inventory）。繞道評估見下框——**評估過了，不做** |
| **「跟原版一樣可以預覽」** | ✅ **這才是訴求的核心**，且有便宜版＝下面 ② 的 `Inventory3DManager` spike。**升格為 A 的主線任務** |
| **「加入最愛」** | ✅ **已經有了，叫 Palette**（目錄是全部、palette 是你留下來的那幾個，落盤跨存檔）。Browser 的 `add to palette` 就是「加入最愛」——**只要改名＋Palette 頁加星號置頂排序**，不是新功能 |

> #### 🔬 繞道評估：proxy MISC ＋ runtime 換模型（**2026-08-20 評估完畢，不做**）
>
> 唯一能真的用上 vanilla 物品欄的路：esp 生一批 `MISC` 空殼，DLL 在 runtime 改寫每個空殼的 `TESModel::model` 與 `TESFullName`（po3 / Base Object Swapper 那套手法）成目錄項目的 nif 與名字。物品欄收得下 MISC ⇒ item card 的 3D 預覽會渲染真正的 static、vanilla 最愛也能用。
>
> **技術上可行，但買到的不如失去的**：
>
> | | 現行 Browser | proxy MISC ＋ 物品欄 |
> |---|---|---|
> | 搜尋 | 空白分隔 AND，比對 name＋**model path**＋id（搜 "mountain" 直接命中 `Landscape\Mountains\*.nif`）| **沒有**。vanilla 物品欄沒搜尋框，SkyUI 也只有分類排序 |
> | 型別／plugin 過濾 | 兩個下拉 | **沒有**。全部是 MISC ⇒ 全擠在同一個「雜項」堆（form type 無法 runtime 改） |
> | 預覽 fit | 世界內 ghost，真尺寸真光照，auto-scale 到約九分之一螢幕 | item card 的距離/縮放是 INI 固定值算的，山脈 nif（10000 單位）會爆框或縮成一個點，**要逐項自動 fit** |
> | form 預算 | 0 | 每個「同時列出」的項目一顆 MISC。browsing pool 可用 runtime dynamic form（不跨存檔、每 session 重建），但**最愛要穩定 ⇒ 得 authored form ⇒ esp 要生幾百到幾千筆**；recycle proxy 會讓最愛跟著插槽跑而不是跟著物件 |
>
> **🔴 決定性的那一格是搜尋。** Browser 的索引之所以建在 model path 上，是因為 **SSE runtime 拿不到 EditorID**（`GetFormEditorID()` 對 STAT/ACTI/FURN 一律回空字串），而 model path 其實是更好的鍵。換成物品欄＝把整個編輯器最有價值的 affordance 丟掉，換一個好看的框。**⇒ 不做。若以後真的想要 vanilla 選單的手感（手把翻頁、大圖），先跑一顆 spike：一顆寫死的 proxy MISC ＋ 一個山脈 nif ＋ 開 InventoryMenu，只看「item card 渲不渲得出來、什麼比例、轉不轉得動」——這一顆答掉 80% 的風險。**

**任務**

- **A1〔離線 spike，半天〕面板內真 3D 預覽**：`RE::Inventory3DManager::LoadInventoryItem(TESBoundObject*, ExtraDataList*)`，**STAT 也是 `TESBoundObject`**。用一顆寫死的 STAT 試，只答一個問題：**它綁不綁 item-3D render layer？** 能脫離物品欄單獨畫進 ImGui 面板就成立。不成立 ⇒ ghost 已經夠用，A 只剩 A2/A3。
- **A2〔小〕技能觸發**：sink `TESSpellCastEvent` → 進 place 模式 ＋ 開 ghost（＝ Browser `preview here` 的同一條路）。SPEL/MGEF 由 ModForge 生（`SceneCaptureTools.esp`）。⚠️ 法術自己會有施法特效/dynamic ref，要確認不會被 `ExportCell` 撈進 `placements[]`——**ghost 的哨兵機制（`ExtraTextDisplayData`）是現成的參考**。
- **A3〔小〕最愛語意**：`add to palette` 改名為「加入最愛」；Palette 頁加星號／置頂排序。**零新狀態**（Palette 本來就落盤跨存檔）。
- 🎮 驗收：施法 → 選單/ghost 起來 → 擺一個 → 匯出仍是零外洩。

---
