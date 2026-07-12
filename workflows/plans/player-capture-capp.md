# Player capture — `sc capp <label>`（去 PROTEUS 化）

**狀態（2026-07-12 收工）**：**外貌路徑 ✅ 🎮 實機 PASS**（分身臉＝本人，含 `tintLayers` 戰紋——PROTEUS 拿不到的那層；落地句進 [landed/npcs](../feature-dev/landed/npcs.md)）。**仍待實機的兩條**：① **數值**（必須用**練過的角色**驗，白紙角色驗不出差別）；② **`isPlayer` ＋ 玩家 perk**（下面那顆 `As<PlayerCharacter>()` bug 已修，commit `eb6ae75`）。現行部署＝ DLL `dd7afd82`（含該修正），co-save SCCP v9。實機步驟與**對帳錨點**見 [wait_todo/ingame-tests.md](../../wait_todo/ingame-tests.md)「`sc capp` 直接吸玩家」。

## isPlayer 標示（2026-07-12 使用者拍板：照實輸出，不加 voice fallback）

實機發現玩家 base TESNPC **沒有 `voiceType`**（分身啞巴）。使用者定調：**不 fallback、不猜一個 vanilla voice**——但補一個「這筆是 player character」的標示，讓「啞巴」是可見的預期結果，不是靜默 bug。

- **DLL（SCCP v9）**：`Captures::NpcData.isPlayer`，`ReadNpc` 用 `actor->As<PlayerCharacter>()`（跟既有 perk 路線同一個 cast——`sc capp` 和點到玩家的 `sc capc` 都會標到）。`SceneExporter` 只在 `true` 時輸出 `"isPlayer": true`（同 `unique`/`essential` 的省略慣例）。`v≤8` 舊存檔缺省 `false`。
- **C#**：`CapturedNpcSpec.IsPlayer` → `Generator.ExpandCapturedNpcs` 帶到 `NpcSpec.IsPlayer`（純可見性欄位，不寫入任何 Mutagen 記錄欄）→ `BuildNpcs` 只在 `IsPlayer && VoiceType 空` 時 `Warn`（措辭「this is expected, not a bug」）。舊 json 缺欄位＝`false`＝行為不變。
- 契約文件：[specs/ingame-scene-export-design.md](../specs/ingame-scene-export-design.md) §④。測試：`CapturedNpcsTests.cs`（`Build_PlayerCaptureNoVoiceType_Warns` 等 5 個）。

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

## 原始計畫（2026-07-11 拍板）

## 動機

PROTEUS 能複製玩家臉，只因引擎把 chargen 資料寫在玩家的 TESNPC base（0x14 的 base 0x7）上——DLL 直接讀同一處即可，**不需要 PROTEUS 中介**。直接吸玩家還順帶解掉 PROTEUS 路線的三個已知缺陷：

| PROTEUS 路線缺陷 | 直接吸玩家 |
|---|---|
| clone 自報 level 1、數值 50/50/50 | 真等級 + 真 H/M/S + 18 技能（顯式 DNAM，見下） |
| 不寫 tintLayers | 玩家 base 的 tintLayers 直接讀（RaceMenu 寫入處；實吸驗證，見風險） |
| base/defaultOutfit 指向 esp 上的空殼 runtime 模板 → 裸體 | 無中介記錄；worn armour → 現有 OTFT 鑄造路線 |

## DLL 側（SCCP → v8）

1. **Console.cpp**：新增 `sc capp [label]`（吸玩家）；`sc capc [label]` 加選用 label。⚠️ label 要用**未 `Lower()` 的 raw2**（現有解析會把參數全轉小寫，label 需保留大小寫）。usage/helpString 更新。
2. **Captures.h/.cpp**：
   - `Entry` 加 `std::string label`；`CapturePlayer(label)` = `CaptureRef(RE::PlayerCharacter::GetSingleton() 的 ref, "player")` + label 落到 entry。`CaptureConsoleRef(label)` 同。
   - **玩家 perks**：玩家的 base TESNPC perk array 是空的——perk 在 `RE::PlayerCharacter` runtime data 的 `addedPerks`（`BSTArray<PerkRankData*>`，vcpkg header PlayerCharacter.h L817/L937 已驗）。ReadNpc 裡：actor 是 player → 走 addedPerks，否則照舊走 `npc->perks`。
   - **顯式數值（所有 actor 一律捕，不只玩家）**：`AsActorValueOwner()->GetBaseActorValue()` 取 kHealth/kMagicka/kStamina + 18 技能（AV 6..23，順序 = Mutagen `Skill` enum：OneHanded…Enchanting）。NpcData 加 `health/magicka/stamina` float + `skills` vector（18 或空）。
3. **SceneExporter.cpp**：capturedNpcs 條目加 `editorId`（= `"MFCap_" + sanitize(label)`，label 空則不輸出）、`health`、`magicka`、`stamina`、`skills[18]`。
4. **CoSave.cpp**：`kVerCaps = 8`；append label + hms + skills；load 以 `version <= 7` gate 舊格式。

## ModForge 消費側

5. **Spec.CapturedNpcs.cs**：`Health/Magicka/Stamina`（float，0 = 未知）+ `Skills List<int>`。`EditorId` 已存在——DLL 直接餵，現有「顯式 editorId 優先」邏輯即為 label 識別機制。
6. **Spec.Actors.cs NpcSpec**：加 `Health/Magicka/Stamina int`（0 = 不設）+ `Skills List<int>`（0 或 18 個）。
7. **Generator.Build.Actors.cs**：有顯式數值 → 寫 DNAM（Mutagen `Npc.PlayerSkills`：`Health/Magicka/Stamina` ushort + `SkillValues` dict——**欄位形狀實作時用 reflection 驗**，非 autocalc NPC 引擎直接讀 DNAM）。
8. **Generator.CapturedNpcs.cs**：優先序改為 **顯式數值 > class autocalc**——`AutoCalcStats = 有 class 且無顯式數值`；class/level 照舊帶（class 仍供 AI/訓練語意）。
9. **Validators**（captured + 手寫 NpcSpec 兩處）：skills 數量 0|18、每值 0–255（DNAM 是 byte）；H/M/S ≥0 且 ≤65535（DNAM ushort）。
10. **Tests**：DLL-shaped json 帶 editorId/stats/skills end-to-end；「有 class + 有顯式數值 → autocalc OFF、DNAM 落值」；「有 class 無數值 → autocalc ON」（不破既有 in-game 已驗路線）；validation 邊界。

## 風險／實吸待驗

- 玩家 **tintLayers** 是否真在 base TESNPC 上（RaceMenu 寫該處，vanilla chargen 待驗）——空了也只是回到 PROTEUS 同等水準，不劣化。
- 玩家 base **voiceType 可能為空** → 分身啞巴；先照實輸出，之後再考慮 fallback（如 MaleEvenToned）。
- 玩家**物品欄很大**（含任務物品、金幣、鑰匙）→ 先全吸（行為與 NPC 一致），嫌吵再加過濾。
- NPC 捕獲也改帶顯式數值後，舊 capture json（無 stats 欄位）走原 class-autocalc 路——欄位缺省 0 即自然向後相容。

## 量級估計

DLL ~150 行 + C# ~120 行 + tests；一個離峰時段可完。

---

## 🐞 踩坑：`As<RE::PlayerCharacter>()` 永遠回傳 nullptr（2026-07-12 實機抓到，已修）

上面 DLL 側第 2 點的「actor 是 player → 走 addedPerks」，第一版寫成 `actor->As<RE::PlayerCharacter>()`——**這個 cast 對任何 actor（包含玩家本人）都必定回傳 nullptr**。

**實機症狀**（`sc capp Hero`）：base 正確吸到 Player TESNPC（`Skyrim.esm:0x000007`），但 log 沒印 `PLAYER` 標記、匯出 json 也沒有 `"isPlayer": true`；perk 因此走進 `else` 分支讀 base 的 `BGSPerkRankArray`，**玩家真正點的 perk 一顆都吸不到**（當時使用者還沒點 perk，兩條路都是「沒有玩家 perk」，所以症狀被遮住了——真去點 perk 才會咬人）。

**真正原因（不是 clang-cl、不是 RTTI）**——`TESForm::As<T>()` 根本不是 `dynamic_cast`。CommonLibSSE `RE/F/FormTraits.h` 把它實作成 `switch (GetFormType())`，每個 case 都是：

```cpp
#define SKSE_FORMTRAITS(a_elem)                                         \
    case a_elem::FORMTYPE:                                              \
        if constexpr (std::is_convertible_v<const a_elem*, const T*>) { \
            return static_cast<const a_elem*>(this);                    \
        }                                                               \
        break
```

也就是：**用 FORM_TYPE 還原出「具體類別」，然後只肯往上（base）轉**。玩家的 ref form type 是 `kCharacter`，跟任何 NPC 一樣，而該 case 對應的具體類別是 **`Character`**——switch 裡**根本沒有 `PlayerCharacter` 的 case**（PlayerCharacter 沒有自己的 FORM_TYPE，標頭只是被 `#include` 進來）。於是 `As<PlayerCharacter>` 問的是 `Character*` → `PlayerCharacter*`，那是**向下轉型**，`is_convertible` 為 false → `break` → **靜默 nullptr**。編譯期就決定了，換 MSVC 也一樣。

**修法**：玩家身份用**單例指標比對**（DLL 其他地方——Aim/Editor/UI/Markers——本來就都用 `GetSingleton()`）：

```cpp
auto* player = RE::PlayerCharacter::GetSingleton();
auto* pc = (actor == player) ? player : nullptr;   // 不依賴任何 cast
```

`isPlayer` 與 perk 路徑一起修好。C# 消費端不必動。

**可推廣的判準（下次寫 `As<T>()` 前套一次）**：
> `x->As<T>()` 只有在 **T 是「x 的 FORM_TYPE 所對應的具體類別」本身或其 base** 時才會回傳非 null。**T 若在那個具體類別之下（更 derived）就一定是 nullptr，而且不會有任何警告。**

依此判準掃過全 DLL，其餘 4 處 `As<>` 全部安全（皆為 upcast 或 formtype 精確命中）：`Captures.cpp:64 As<RE::Actor>`（Actor 是 Character 的 base ✅）、`Palette.cpp:53 As<RE::TESBoundObject>`、`Palette.cpp:78 As<RE::TESEnchantableForm>`（皆為 WEAP/ARMO 的 base ✅；對非 bound form 回 null 正是預期的過濾）、`Palette.cpp:151`／`CoSave.cpp:79 As<RE::EnchantmentItem>`（EnchantmentItem 就是 `FormType::Enchantment` 的具體類別，精確命中 ✅）。**唯一「向下轉型」的就是壞掉的那一處。**
