# Step 5–7：variants + 進階 submod + 測試

← [oar-replacer-guide](oar-replacer-guide.md)

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

