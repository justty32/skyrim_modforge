# Player capture `sc capp` — isPlayer 標示、base/runtime 前提修正、落地摘要

← [player-capture-capp](../player-capture-capp.md)（入口）

## isPlayer 標示（2026-07-12 使用者拍板：照實輸出，不加 voice fallback）

實機發現玩家 base TESNPC **沒有 `voiceType`**（分身啞巴）。使用者定調：**不 fallback、不猜一個 vanilla voice**——但補一個「這筆是 player character」的標示，讓「啞巴」是可見的預期結果，不是靜默 bug。

- **DLL（SCCP v9）**：`Captures::NpcData.isPlayer`，`ReadNpc` 用 `actor->As<PlayerCharacter>()`（跟既有 perk 路線同一個 cast——`sc capp` 和點到玩家的 `sc capc` 都會標到）。`SceneExporter` 只在 `true` 時輸出 `"isPlayer": true`（同 `unique`/`essential` 的省略慣例）。`v≤8` 舊存檔缺省 `false`。
- **C#**：`CapturedNpcSpec.IsPlayer` → `Generator.ExpandCapturedNpcs` 帶到 `NpcSpec.IsPlayer`（純可見性欄位，不寫入任何 Mutagen 記錄欄）→ `BuildNpcs` 只在 `IsPlayer && VoiceType 空` 時 `Warn`（措辭「this is expected, not a bug」）。舊 json 缺欄位＝`false`＝行為不變。
- 契約文件：[specs/ingame-scene-export-design.md](../../specs/ingame-scene-export-design.md) §④。測試：`CapturedNpcsTests.cs`（`Build_PlayerCaptureNoVoiceType_Warns` 等 5 個）。

## ⚠️ 前提修正：玩家的哪些東西在 base、哪些是 runtime（2026-07-12 實吸學到）

原計畫寫「玩家 chargen 就寫在 base TESNPC 上，DLL 直讀即可」——**只對了一半**：

| 資料 | 住在哪 | 怎麼讀 |
|---|---|---|
| **外貌**：race・headParts・tintLayers・faceMorphs・hairColor・faceTexture・**weight**・height | **base TESNPC**（`Skyrim.esm:0x000007`）。chargen/RaceMenu 直接改寫這筆記錄，**存檔帶著改過的版本** | `npc->...` 直讀（原計畫成立）。**實證**：磁碟上 Player base 的 `Weight=100`，實吸得到 chargen 的 `0.0` → 讀到的確實是存檔改寫版 |
| **perk** | **runtime**：`PlayerCharacter::GetPlayerRuntimeData().addedPerks`（base 的 perk array 是空的） | 已處理 |
| **level・H/M/S・18 技能** | **runtime actor value**。成長（升級加點、技能升級）是**堆在 permanent modifier 上**，base 值**永遠停在 chargen 起始表**（種族起始技能 15＋種族加成、100/100/100） | **`GetPermanentActorValue()`**——見下 |

**三個 AV 讀法的差別**（`Actor::avStorage` ＝ `base + modifiers[permanent|temporary|damage]`）：

| API | ＝ | 對分身合不合用 |
|---|---|---|
| `GetActorValue` | base＋permanent＋temporary−damage | ❌ 含藥水/裝備附魔/當下受傷——分身會繼承你剛好開著的 buff |
| `GetBaseActorValue` | base only | ❌ **玩家**只會拿到 chargen 出廠值（一般 NPC 反而是對的：引擎 load 時把 autocalc 結果寫進 base） |
| **`GetPermanentActorValue`** | base＋permanent | ✅ **兩種 actor 都對**：沒有 permanent modifier 的 NPC 讀起來＝base（Ancano 仍是 lvl 15 / 167-143-50），練過的玩家才拿得到真數字；且**不含**臨時 buff |

**踩坑警告（差點誤判）**：第一輪 export 的玩家 lvl 1 / 100-100-100 / 起始技能**看起來像 bug，其實是真值**——測試角色（Hatak）本來就是全新 1 級布萊頓（存檔 header：level 1、XP 0.0/100）。**沒練過的角色，base 讀法和 permanent 讀法吐出的數字一模一樣**，所以這個 bug **只能用練過的角色驗**（見 ingame-tests 第 0 步）。`level` 走 `Actor::GetLevel()`（runtime，一般 NPC 的等級縮放也靠它）不變。

## 落地摘要（2026-07-12）

| 計畫項 | 落地 |
|---|---|
| `sc capp [Label]` / `sc capc [Label]` | `Console.cpp`：label 取**未 `Lower()` 的 raw2**（`Trim()`，去空白與引號）。usage/helpString 更新 |
| 玩家＝一般 actor | `Captures::CapturePlayer` ＝ `CaptureRef(PlayerCharacter::GetSingleton(), "player", label)`——chargen 本來就在 base TESNPC（`Skyrim.esm:0x000007`），`ReadNpc` 原封不動就讀得到，**無 PROTEUS 中介** |
| 玩家 perks | `ReadNpc`：`actor->As<PlayerCharacter>()` → `GetPlayerRuntimeData().addedPerks`（玩家 base 的 perk array 是空的）；一般 NPC 照舊走 `npc->perks` |
| 顯式數值（**所有 actor**） | `AsActorValueOwner()->GetPermanentActorValue()`（**不是** `GetBase*`，見上節）：kHealth/kMagicka/kStamina ＋ AV 6..23 的 18 技能（＝Mutagen `Skill` enum 序，index 即映射） |
| label → editorId | `SceneExporter::AppendCaptures`：`editorId = "MFCap_" + sanitize(label)`（非 alnum → `_`），item/npc 兩段都吐 |
| co-save | `kVerCaps = 8`：entry 追加 `label`；NpcPayload 追加 H/M/S ＋ skills。v≤7 舊存檔照舊讀（欄位缺省 0） |
| C# 消費 | `CapturedNpcSpec`／`NpcSpec` 加 `Health/Magicka/Stamina/Skills`；`BuildNpcs` 寫 `PlayerSkills`（DNAM）；`ExpandCapturedNpcs` **優先序＝顯式數值 ＞ class autocalc**（有顯式值 → `AutoCalcStats=false`，class 仍帶）；兩處 validator 收邊界（skills 0\|18・0–255、H/M/S 0–65535）；`BuildNpcs` 對「顯式值 ＋ autoCalc 同開」`Warn` |
| `isPlayer` 標示（2026-07-12） | co-save `kVerCaps = 9`：`NpcData.isPlayer`（`actor->As<PlayerCharacter>()`）；`SceneExporter` 只在 true 時吐；C# `CapturedNpcSpec.IsPlayer` → `NpcSpec.IsPlayer`（純可見性，不寫記錄欄）→ `BuildNpcs` 對「`IsPlayer` 且無 `VoiceType`」`Warn`（非 fallback，見下節） |

**未做**：玩家物品欄全吸不過濾（等實機結果再決定要不要濾任務物品/金幣/鑰匙）。

---
