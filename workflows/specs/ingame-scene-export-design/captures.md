# 遊戲內場景匯出 — 場景匯出不含 NPC、captures 獨立檔、`sc capp` 直接吸玩家

← [ingame-scene-export-design](../ingame-scene-export-design.md)（入口）

### ✅ 拍板（2026-07-12，使用者）：**場景匯出不含 NPC** ＋ **captures 拆成獨立檔**

推翻 2026-07-10「actor 與物件一律進 `placements[]`」那條（上面 §「⚠️ 採集橋輸出必須是合法 ModSpec」（[contract-overrides](contract-overrides.md)）第一點）。**DLL 端已落地**（`SceneExporter.cpp`，DLL crc `65f53a93`）。三條契約變更：

**① cell 匯出＝純場景/物件，actor 一律不出。** `ExportCell`/`ExportAll` 掃描時遇到 actor ref 直接跳過（計入 `actorsExcluded`，只進 log/面板統計），**`placements[]` 不再出現 `kind:"npc"` 的條目**。NPC 交給 ModForge 按 **`annotations[]`（marker）** 去擺——marker 帶 position/rotation/scale/label/kind/note，足以指定「這裡放一隻山羊，面向這邊」。理由：把 NPC 塞進 cell 掃描要處理 dynamic actor base、PROTEUS 外貌、role 標記…太麻煩，而 marker＋ModForge `NpcSpec` 這條路本來就已實機驗過。
- ⚠️ 這**不影響** `capturedNpcs[]`：真的要「複製這個 NPC」時走 `sc cap`（擷取器，帶完整外貌/perk/inventory/身份），那是**明示**採集，不是掃描產物。
- ⚠️ ModForge 端**零改動**：少一種輸入條目而已（`PlacementSpec.Kind = "npc"` 路徑照舊存在，手寫 spec 仍可用）。

**② `capturedItems[]` / `capturedNpcs[]` 從場景檔移出，走自己的檔。** 面板 Export 頁／Captures 頁各有一顆 **`Export captures`** 鈕 → 寫 `captures_<YYYYMMDD-HHMM>.json`（只含這兩段）。場景匯出檔**不再帶**這兩段。
- **仍是合法 ModSpec**：`CapturedItems`/`CapturedNpcs` 都是 `ModSpec` 成員（`Spec.cs:107`、`Spec.CapturedNpcs.cs`），所以 `build captures_20260712-1830.json out.esp` 單獨吃得下，**ModForge 端零改動**。要一次生成場景＋擷取物，就 build 兩份、或人工把兩個 json 併起來。
- 理由：擷取到的定義是**跨 cell 的資料庫**（一把附魔劍不屬於你站的那間房），混在場景檔裡會讓「這份檔＝這個地方」的語意變髒；而且每次 export 場景都重覆吐一整包 NPC 外貌資料。

**③ 匯出檔名帶場景＋時間戳**（連續 export 不再互相覆蓋）：
```
scene-export_<cell EditorID 或 worldspace+grid>_<YYYYMMDD-HHMM>.json   # Export player cell
scene-export_all-<玩家所在>_<YYYYMMDD-HHMM>.json                        # Export all (loaded cells)
captures_<YYYYMMDD-HHMM>.json                                          # Export captures
```
interior 用 cell 的 EditorID（如 `WhiterunBanneredMare`）；exterior 用 worldspace EditorID ＋ cell grid（如 `Tamriel_x5y-3`）。名稱 sanitize 成 `[A-Za-z0-9._-]`（其餘字元→`_`，截 48 字）；同分鐘同場景再匯出＝加 `-2`/`-3` 後綴，**永不覆蓋**。⚠️ 下游 agent 別再寫死 `scene-export.json`——**取該資料夾最新一份**（或使用者指定的那份）。

---

### ✅ 拍板＋落地（2026-07-12）：`sc capp` **直接吸玩家**（去 PROTEUS 化）＋ 顯式數值＋label→editorId

推翻下面「NPC 來源」節（[npc-source](npc-source.md)）的**路徑 A（拓印玩家＝消費 PROTEUS clone）**：PROTEUS 能複製玩家的臉，只因引擎把 chargen 寫在**玩家的 base TESNPC**（`Skyrim.esm:0x000007`）上——DLL 直讀同一處即可，**不需要中介**。DLL 端已落地（crc `f8afc170`，co-save SCCP v8）；計畫全文＋落地摘要見 [plans/player-capture-capp.md](../../plans/player-capture-capp.md)。契約變更三條：

**① `sc capp [Label]` ＝把玩家當一般 actor 擷取** → 一筆 `capturedNpcs[]`（外貌/perk/裝備/擺位全帶）。`base` 出來是 `Skyrim.esm:0x000007`（advisory，ModForge 只 MINT 不 override）。順手解掉 PROTEUS 路線三個缺陷：clone 自報 level 1／50-50-50、不寫 tintLayers、defaultOutfit 是空殼 → 裸體。玩家 perk 讀 `PlayerCharacter::addedPerks`（玩家 base 的 perk array 是空的），一般 NPC 照舊讀 base。

**② `capturedNpcs[]` 新增顯式數值欄（所有 actor 都吐，不只玩家）**：
```jsonc
"health": 320.0, "magicka": 150.0, "stamina": 210.0,   // base actor values（引擎真正跑的數字）
"skills": [41, 42, ...]                                // 18 個，引擎 AV 6..23 序
```
skills 的順序 ＝ `OneHanded, TwoHanded, Archery, Block, Smithing, HeavyArmor, LightArmor, Pickpocket, Lockpicking, Sneak, Alchemy, Speech, Alteration, Conjuration, Destruction, Illusion, Restoration, Enchanting` ＝ **Mutagen `Skill` enum 序**，index 即映射。**ModForge 消費優先序＝顯式數值 ＞ class autocalc**：有顯式值就寫 DNAM 且 `autoCalcStats` 關（autocalc 只是拿 class+level 估，載入時還會覆蓋掉寫死的值）；沒有才走舊的 class-autocalc 路（**舊 capture json 因此原樣相容**，欄位缺省 0）。

**③ `capturedItems[]` / `capturedNpcs[]` 可帶 `editorId`（label 機制）**：`sc capp <Label>` / `sc capc <Label>` 的標籤 → `editorId: "MFCap_<sanitised label>"`（非 alnum → `_`）。ModForge 既有的「顯式 editorId 優先」規則即為身份機制——**同一個 label 再吸一次＝同一筆記錄**（不會多生一個）。⚠️ label 走**未 `Lower()` 的 raw 參數**（`sc` 的參數解析會全轉小寫；`pkc`/referrer 的標號同此坑）。

**④ `capturedNpcs[]` 加 `isPlayer` 標示（2026-07-12 使用者拍板：照實輸出，沒有就沒有）**：實機發現玩家的 base TESNPC **沒有 `voiceType`**（分身會啞巴）。使用者定調**不加 fallback、不猜一個 vanilla voice 塞進去**——但要標「這筆是玩家」。DLL 判定用 `actor->As<PlayerCharacter>()`（`ReadNpc` 既有的 perk 路線同一個 cast，`sc capp` 和點到玩家的 `sc capc` 都會標到；co-save `SCCP v9`，`v≤8` 舊存檔缺省 `false`）：
```jsonc
"isPlayer": true   // 只在 true 時輸出（同 unique/essential 的省略慣例）
```
ModForge 消費：`isPlayer` 是**純可見性欄位**，不寫入任何記錄欄（不生成、不 fallback voiceType）。`Generator.ExpandCapturedNpcs` 把它帶到 `NpcSpec.IsPlayer`；`BuildNpcs` **只在 `IsPlayer && VoiceType 空`時 `Warn`**（語氣是「這是預期的、不是 bug」）——有 voiceType、或不是玩家、或舊 json 缺欄位（＝`false`）都不印，行為完全不變。

---
