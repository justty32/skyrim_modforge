# Player capture `sc capp` — 踩坑：`As<RE::PlayerCharacter>()` 永遠回傳 nullptr

← [player-capture-capp](../player-capture-capp.md)（入口）

## 🐞 踩坑：`As<RE::PlayerCharacter>()` 永遠回傳 nullptr（2026-07-12 實機抓到，已修）

上面 DLL 側第 2 點（[original-plan](original-plan.md)）的「actor 是 player → 走 addedPerks」，第一版寫成 `actor->As<RE::PlayerCharacter>()`——**這個 cast 對任何 actor（包含玩家本人）都必定回傳 nullptr**。

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
