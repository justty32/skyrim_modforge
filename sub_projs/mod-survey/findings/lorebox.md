# LoreBox（item/spell 額外描述文字框架；SKSE C++ + SkyUI SWF 注入）

← [survey index](../index.md)

出處：<https://github.com/shazdeh/LoreBox>（開源，讀 source）｜Nexus 156534

| 項目 | 值 |
| --- | --- |
| 類型 | **工具 / 框架型（modder-facing）** — 讓 mod 作者替 item/spell 掛「額外描述文字」（lore tooltip），顯示於 inventory 系選單 |
| Plugin | **無 ESP**。純 `LoreBox.dll`（CommonLibSSE-NG SKSE）+ `lorebox_inject.swf`（由 `Fla/LoreBox.as` 編）+ `LoreBox.ini` |
| 依賴 | SKSE、**SkyUI**（吃它的 item-card keyword 曝露 + menu 結構 + `$` 翻譯 + `GlobalFunc`）；**無 PapyrusUtil/JContainers/MCM** |
| 規模 | 極小：`plugin.cpp` 77 行 + `LoreBox.as` 281 行；其餘 .cpp/.h 空殼 |
| 敘事價值 | 低（本身是顯示機制，不含內容；但**是投遞 lore/敘事文字到物品的載體**） |

## 是什麼（別望文生義）

不是「知識收集/書籍系統」。LoreBox = **給 modder 用的 tooltip 增強框架**：滑到（或鍵選）背包/容器/交易/法術/打造/贈禮選單裡的某個物品或法術時，若該物件掛了特定關鍵字，就彈出一塊**額外描述文字**（支援 HTML + 內嵌 `.dds` 圖片）。作者用它替武器/藥水/法術補「世界觀說明」而不動 vanilla 描述欄。

## 關鍵架構（三段，全是既有機制的巧妙組合）

**1. C++ 端只做 SWF 注入**（`plugin.cpp`）：監聽 `MenuOpenCloseEvent`；`LoreBox.ini` 的 `[Menus] sMenu=` 列出目標選單（Inventory/Container/Barter/Magic/Crafting/Gift）。選單一開 → 在其 GFx `_root` 上 `createEmptyMovieClip` 再 `loadMovie("lorebox_inject.swf")`。純顯示層 hook，**不碰任何 game record**。

**2. SWF 端讀 keyword → 翻譯字串**（`LoreBox.as`，核心 trick）：
- 掛進 SkyUI 的 itemList，聽 `itemHighlightChange` / `categoryChange`。
- 讀選中項的 `selectedEntry.keywords`（item）或 `effectKeywords`（magic effect）——**SkyUI 已把物件 keyword EDID 曝露到 item card**。
- 過濾 EDID 前綴 `LoreBox_` 的 keyword。對每個 `LoreBox_Foo`：`Placeholder_tf.text = '$LoreBox_Foo'` — 觸發 **Scaleform `$` 翻譯查找**。若翻譯檔裡有 `$LoreBox_Foo⟨TAB⟩⟨lore html⟩`，就取代成該文字並顯示；查不到就跳過。
- 支援文字內 `<img src="xxx.dds" width height>` → `MovieClipLoader` 內嵌載圖；tooltip 跟滑鼠或鍵選項定位、fade in/out（`iDelay` 可調）。

**3. 內容資料在哪 → 翻譯檔**：一條 lore 的實體 = SkyUI 翻譯檔（`Interface/Translations/<name>_<lang>.txt`）裡的一行 `$LoreBox_X⟨TAB⟩文字`。**替物品掛 lore 的完整配方＝(a) 建 EDID 以 `LoreBox_` 開頭的 KYWD → (b) 把該 KYWD 掛到 item / 或 spell 的 MGEF → (c) 翻譯檔加一行**。零腳本、零 ESP scripting，只有 record + 翻譯字串。

## 與 ModForge 的對照

同 PROTEUS 一樣是「約定驅動」，但 LoreBox 的「資料」是 **keyword EDID 命名約定 + SkyUI 翻譯字串**，內容產物落在 loose translation txt，而非 esp。這正是 ModForge（JSON→esp + loose files）能吃下的形狀：ModForge 可把 `loreText:` 這種 spec 欄位 macro 展開成「建 KYWD + 掛 keyword + 寫翻譯行」三件套。

## 結論

**對 ModForge：可生成（差一塊 loose-file 產出）。**
- **已可生成**：`LoreBox_*` KYWD 建立（`Generator.Build.LongTail.cs:71` `mod.Keywords.AddNew()`）+ 把 keyword 掛上 item / MGEF（Items/SPID/KID 都在用 keyword），這兩步今天就能生。DLL 與 SWF 是玩家端依賴（同 MCM Helper 模式，不生成、隨包宣告）。
- **一個新缺口 = SkyUI-style 翻譯檔輸出**：需寫出 `Interface/Translations/<plugin>_english.txt`，格式為 `$LoreBox_X⟨TAB⟩⟨html 文字⟩`。查 `src/`：ModForge 的 MCM `$TranslationKey` 只走 config.json 內嵌，**目前無任何路徑輸出這種 `$key<TAB>value` .txt loose file**（`McmGen.cs` / `Generator.Build.Mcm.cs` 無 Translations 輸出）。補上這個 emitter（順帶讓 MCM Helper 的 $-key 也能外部翻譯），LoreBox 配方即 100% 可生。
- **浮現原語**：item/spell spec 加 `loreText:`（值為 html，含選配 `<img .dds>`），macro 展開成 KYWD+attach+translation 行——低成本高辨識度的「物品世界觀文字」便利層。

**對 Sofia：無關**（純物品 tooltip 顯示層，與隨從系統無交集）。
</content>
</invoke>
