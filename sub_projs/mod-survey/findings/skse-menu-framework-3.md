# SKSE Menu Framework 3（遊戲內嵌 Dear ImGui 選單框架；native C++ SKSE plugin）

← [survey index](../index.md)

**作者**：QTR-Modding（Thiago099）
**出處**（皆開源，讀 source）：
- 框架：https://github.com/QTR-Modding/SKSE-Menu-Framework-3
- 官方範例：https://github.com/QTR-Modding/SKSE-Menu-Framework-3-Example

| 項目 | 值 |
| --- | --- |
| 類型 | **框架型**：給 modder 用的**遊戲內 GUI 選單框架**，本體是 native `SKSEMenuFramework.dll`（CommonLibSSE-NG SKSE plugin，v3.4 / vcpkg 2.1.1）|
| 渲染 | **Dear ImGui**（bundled，含 `imgui_impl_win32` + `imgui_impl_dx11`）疊在遊戲 D3D11 上 |
| Plugin | **無 ESP**（純 SKSE DLL + loose 資產：fonts / themes / ini）|
| 消費者介面 | **純 C++ header API**（`resources/SKSEMenuFramework.h`）——消費者 mod **必須自己寫一支 SKSE plugin**。**無 Papyrus API、無資料驅動註冊** |
| 敘事價值 | 無 |

## 是什麼（別望文生義）

**不是** MCM 那種設定選單產生器，而是一個**通用遊戲內即時 GUI 畫布**：讓別的 mod 在遊戲畫面上疊自己的 ImGui 視窗／控件（按鈕、slider、input、table、tree、圖片、字型、圖示…整套 ImGui widget）。預設熱鍵 **F1** 開一個「Mod Control Panel」主視窗，各消費者 mod 把自己的頁面掛進去；也能開獨立浮動視窗、螢幕 HUD overlay。定位＝Skyrim 版的「按 F1 叫出開發者/工具面板」。

## 架構（讀 `framework/src/`）

- **渲染後端**：`Hooks.cpp` hook `IDXGISwapChain::Present`（vtable）+ `SetWindowLongPtrA` 換 `WndProc`（`WndProcHook::thunk`）攔輸入；ImGui 用官方 win32+dx11 backend。純疊加，不碰 Scaleform。
- **註冊一個選單**（消費者端，見下「介面」）：`SetSection` → `AddSectionItem(名稱, RenderFn)` 掛頁面；`AddWindow(RenderFn, pauseGame)` 開獨立視窗；`AddHudElement` 掛 overlay；`AddInputEvent` 攔按鍵。Render 函式是 `void __stdcall()`，每幀被呼叫，內部直接呼 ImGui。
- **控件型別**：**整套 Dear ImGui**（`ImGuiMCP::` 命名空間轉發）——Button/InputText/SliderInt/ColorEdit4/PlotLines/BeginTable/BeginChild/Image/MenuBar… 無自訂控件抽象層，就是 raw ImGui。
- **input/焦點**：WndProc 攔截；`AddInputEvent` callback 回傳 `bool` 決定是否吞掉該輸入（block）。`IsAnyBlockingWindowOpened()` 判斷玩家是否正被選單佔用。
- **開關熱鍵**：`SKSEMenuFramework.ini [General] ToggleKey`（預設 f1）+ `ToggleMode`（SinglePress/DoublePress）+ gamepad 熱鍵；`SetHotkeyEnabled(bool)` API 動態開關。
- **暫停/游標互動**：`GameLock.cpp` 用 `RE::Main::freezeTime` 凍結時間；ini `FreezeTimeOnMenu` / `BlurBackgroundOnMenu` 可調；**non-blocking 視窗**（`AddWindow(fn,false)` 或 `BlockUserInput=false`）不暫停遊戲、玩家仍可操作（overlay/HUD 用途）。
- **其他**：主題（`SKSEMenuFrameworkThemes/*.json` 熱插拔）、字型（`SKSE/Plugins/Fonts/*.ttf|otf` + 同名 `.json` 配大中小三尺寸，`PushFont`）、Font Awesome 圖示、多語系字型範圍（中/日/韓/西里爾/泰/土耳其 ini 開關 + 翻譯檔）、`LoadTexture`（SVG/DDS/PNG…）貼圖、開關選單 `Event`（kOpenMenu/kCloseMenu/kBeforeRender/kAfterRender）。

## 給消費者 mod 的介面（重點：純 C++，非資料驅動）

消費者**不連結** DLL，而是 include 一份 header shim：`GetModuleHandleW(L"SKSEMenuFramework")` + `GetProcAddress` 動態取每個 export（`AddSectionItem`/`AddWindow`/`LoadTexture`…），`IsInstalled()` 檢查 DLL 存在即 graceful skip。**最小註冊流程**（`example/src/UI.cpp` + `plugin.cpp`）：

```cpp
// plugin.cpp — 標準 CommonLibSSE-NG SKSE 進入點
SKSEPluginLoad(const SKSE::LoadInterface* skse) {
    SKSE::Init(skse);
    UI::Register();
    return true;
}
// UI.cpp
void UI::Register() {
    if (!SKSEMenuFramework::IsInstalled()) return;   // 沒裝就跳過
    SKSEMenuFramework::SetSection(MOD_NAME);          // 用 mod 名當分區
    SKSEMenuFramework::AddSectionItem("Add Item", Example1::Render); // 掛一頁
}
void __stdcall UI::Example1::Render() {              // 每幀呼叫，內部直接寫 ImGui
    ImGuiMCP::InputScalar("form id", ...);
    if (ImGuiMCP::Button("Search")) LookupForm();
    // 可直接呼 RE:: 遊戲 API，例如 player->AddObjectToContainer(...)
}
```

Render 函式裡能直接呼 CommonLib `RE::` 遊戲 API（範例：查 FormID → 加物品進玩家背包），所以它是「GUI + 直接操控遊戲物件」的組合。**沒有 JSON/ini 註冊選單這條路**——所有選單邏輯都是編譯進消費者 DLL 的 C++。

## 依賴 / 前置

- **本體**：SKSE64、CommonLibSSE-NG（`alandtse/CommonLibVR` ng 分支，同時涵蓋 SE/AE/VR）、Address Library、D3D11。vcpkg 拉 imgui 生態 + spdlog/simpleini/nanosvg/directxtk/nlohmann-json 等。
- **消費者 mod**：需 SKSE + CommonLibSSE-NG **自建 build 環境**（vcpkg + CMake + MSVC）寫 native plugin；runtime 只軟依賴本框架 DLL（沒裝就 `IsInstalled()` 跳過）。
- 與其他 QTR 元件無強耦合；本體自包含。

## 與其他 UI 路線對比

| 路線 | 技術 | 定位 |
| --- | --- | --- |
| **SKSE Menu Framework** | 原生 **Dear ImGui**（D3D11 hook）| 開發者/工具面板、即時控件、debug/editor 型 GUI；modder 寫 C++ |
| SkyUI / MCM（+ MCM Helper）| Flash / Scaleform `.swf` | 設定選單、貼合原生 UI 風格；MCM Helper 可 JSON 資料驅動 |
| UIExtensions | Scaleform 自訂選單（環選/列表）| 玩家互動選單，仍走 Flash |
| LoreBox | Scaleform loadMovie 注入 | 往既有 SkyUI 選單塞 tooltip |

差異核心：**ImGui = 程式碼即介面（immediate mode，C++）**，開發極快、控件豐富、但外觀非原生風、且**每個消費者都要出一支 DLL**；Scaleform 路線貼原生風、可 Papyrus/資料驅動、但做自訂控件痛苦。

## 對 ModForge：純參考（非可生成）— idea #7 / #24 定位

⚠️ **界線（比照 [proteus.md](proteus.md) 對閉源 native 的判定，只是這個是開源）**：ModForge 是 **build-time JSON→esp 生成器**，產物是 Bethesda record（+ 附帶 `.pex`/loose 資產）。SKSE Menu Framework 是 **runtime C++ GUI 框架**，它的「選單」是**編譯進消費者 DLL 的 ImGui C++ 程式碼**，**沒有任何 record、沒有資料驅動註冊面**。因此：

- **ModForge 無可生成成分**：生不出 ImGui 視窗（那是 C++ 不是 record/JSON），也沒有像 MCM `config.json` 那樣的中介宣告可讓 ModForge 產出。連消費者的註冊都是 `SKSEPluginLoad` 裡的 C++ 呼叫。
- **idea #7（遊戲內嵌互動 UI）**：這**正是** #7 缺的那塊「原生即時 GUI 畫布」——但只能當**技術選型參考**。若 #7 要走 ImGui 路線，等於 ModForge 得**隨產物附一支預建/需消費者自寫的 SKSE plugin**（同 Tundra/Honed Metal controller `.pex` 的「須附 native code」判定，只是這裡是 DLL 不是 Papyrus）。ModForge 本身不生 GUI。
- **idea #24（遊戲內編輯器需要 GUI：擺物/選 record/存快照面板）**：**它能當 #24 的 UI 層——但代價是 native**。這框架的能力**天生對得上 #24 需求**：range 3D 世界的即時控件、`RE::` 直接讀寫遊戲物件、FormID 查找（範例已示範查 form + 操作）、非暫停 overlay（邊擺物邊看世界）、table/tree 選 record、按鈕存快照。**技術上它就是 #24 面板該用的東西**。但 ModForge **不能「生成」這個編輯器**——得**手寫一支專用 SKSE plugin**（用此框架畫 UI，把「擺物/選 record/存快照」邏輯寫成 C++，快照再吐回 ModForge 的 JSON→esp 管線）。即：**#24 的 GUI 層 = 一個獨立 native 子專案（消費此框架），不是 ModForge 生成目標**。與 AnnoRim 筆記裡「#24 快照該吐 placement 產物格式」呼應——此框架負責「在遊戲內採集」，ModForge 負責「把採集結果生成 esp」。

**對 Sofia**：無關。

## 結論

開源、可讀 source 的**遊戲內 Dear ImGui 選單框架**（D3D11 hook + WndProc 攔輸入 + freezeTime 暫停），消費者以純 C++ header（GetProcAddress shim）註冊選單/視窗/HUD/輸入 hook，**無 ESP、無 Papyrus、無資料驅動**。對 ModForge：**純參考**（生成器域外）。**價值＝它是 idea #7/#24「遊戲內原生互動 GUI／編輯器面板」在技術上最現成的答案**，但落地路徑是「**寫一支消費此框架的 SKSE plugin**」而非 ModForge 生成——ModForge 端最多只在旁邊接「編輯器快照 JSON → esp」的既有管線。

---

## 為何不是 `sse-imgui`（2026-07-10 實測排除）

[ryobg/sse-imgui](https://github.com/ryobg/sse-imgui) 看起來是同類東西，**在 AE 上不能用**：

- 它的功能靠相依鏈 `sse-imgui → sse-gui → sse-hooks`（DLL 內字串 `Accepted SSEGUI interface v`、`.refptr.ssegui`，執行期經 SKSE messaging 取 SSE-GUI 介面）。三者都停在 2020 年、鎖 SE 1.5.97。
- `sse-imgui.dll` 只匯出舊式 `SKSEPlugin_Query` / `SKSEPlugin_Load`，**沒有 `SKSEPlugin_Version`**；import `msvcrt.dll`、字串含 `Mingw-w64 runtime failure`。本機 runtime 是 SKSE **1.6.1170（AE）**。
- 對照：`SKSEMenuFramework.dll` 匯出 `SKSEPlugin_Version`（v3.12.0），CommonLibSSE-NG 建置，upstream 2026-07 仍在動。

## 落地（2026-07-10）：`scene-capture-bridge` 就是它的消費者 plugin

本 finding 原本的結論是「#24 的 GUI 層＝一個獨立 native 子專案」。**不必獨立**——[`sub_projs/scene-capture-bridge`](../../scene-capture-bridge/README.md) 已經是一支編得過、實機驗過的 CommonLibSSE-NG SKSE plugin，面板直接長在它上面（`src/UI.cpp`）。

- 消費者 header `resources/SKSEMenuFramework.h` vendored 到 `extern/SKSEMenuFramework/`（LGPL-2.1；**離線機必須能 build**，故不用 `file(DOWNLOAD)`）。header 自足，只要 `windows.h` + std。
- **軟相依**：`IsInstalled()` 是 `GetModuleHandleW(L"SKSEMenuFramework")` 探測。編出來的 DLL import 表仍只有 5 個系統 DLL、無此框架的 import name → 沒裝框架的玩家照樣拿到 F10 hotkey，且動態連結符合 LGPL。
