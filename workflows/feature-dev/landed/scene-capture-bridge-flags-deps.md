# 已落地 — scene-capture-bridge：noHavokSettle／模式開關／依賴可見性

← [landed index](README.md)｜採集橋：[scene-capture-bridge.md](scene-capture-bridge.md)（含 🔴 部署事故，先讀）· [editor](scene-capture-bridge-editor.md) · [referrer](scene-capture-bridge-referrer.md) · [flags-deps](scene-capture-bridge-flags-deps.md)

## scene-capture-bridge — `noHavokSettle`（`sc pl py0`）IN-GAME PASS · 2026-07-13

「擺好的東西不要被物理彈飛」整條閉環：遊戲內 `sc pl py0` → 匯出 `"noHavokSettle": true` → build 寫 REFR record flag **`0x20000000`（DontHavokSettle）** → **裝進遊戲後真的生效**。判別靠 `ModForgeGoblets.zip` v2 的**懸空對照列**（學院庭院地板上方 128 units，6 顆銀杯，唯一變因是旗標）：實機 **3 顆帶旗標的浮在半空、3 顆不帶的掉到地上**。

**🧪 判讀教訓（可複用）**：**貼地靜止的物件驗不出這個旗標**——havok settle 對「已經躺在地面上」的東西本來就不做任何事，有無旗標都不動。第一版拿使用者原本那 8 顆銀杯（z 全在 −7744±7 ＝同一平面）當測資，實機「8 顆全在原位」是**無效判讀**，既非 PASS 也非 FAIL。**要驗這種「載入時才發生一次」的旗標，必須讓效果無法被靜態場景吸收**（懸空 ⇒ 浮著 vs 掉落，沒有第二種解釋）。另：**旗標不等於「推不動」**——`noHavokSettle` 只擋載入時的 settle，玩家照樣撿得起、撞得飛（vanilla 3791 筆 REFR 就是這行為）。

## scene-capture-bridge — 模式開關 `py`/`ed`/`pkc` ＋ referrer 殘項 · 使用者實機確認 2026-07-13

`noHavokSettle` 之外，同批的其餘遊戲內項目**使用者回報全數通過**（「後面應該都好了」，2026-07-13）：`sc pk ed1`／`sc pl ed1`（實例附魔：durable 附魔擺出來帶附魔、runtime ENCH 走匯出鑄造）、`sc pkc [Label]`（console 選取版滴管、大小寫保留）、`sc ed py0/py1`（編輯期物理，抽共用碼後的回歸）、跨存檔重開的 placed-ref／capture aim-source reacquire，以及 referrer 的剩餘三項（改 label 的大小寫保留與撞名擋下、拒收三類、重開讀檔撿回檔內目標）。⚠️ **這批是口頭驗收、無匯出檔佐證**（不像 `noHavokSettle`／perk／數值那三條有 json＋byte 對帳）——日後若發現哪項其實會壞，先回頭懷疑這裡。

## scene-capture-bridge — 依賴可見性（`Export requires` 鈕）驗收完成 · 2026-07-13

DLL 遊戲內報告 vs C# `build` 的 master 名單**跨端一致**（同 7 個 mod、link 數對得上），且**兩端都不把假依賴當依賴**——一變數實驗：刪掉某 mod 的 spell、只留它 14 筆 `activeEffects` → 該 mod 從 `requires.txt` **和 esp master list 同時消失**。細節與四項驗收見 [phases](../../plans/scene-capture-bridge/phases.md)「依賴可見性的遊戲內版」。
