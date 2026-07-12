# scene-capture-bridge — 附錄：細摳記錄＋驗證清單

← [README](README.md)（現況導航）｜[phases](phases.md)（已落地實作記錄）｜[backlog](backlog.md)（未做項）

凍結參考：需求原文（2026-07-10 細摳）與未驗事項彙總。

---

# 附錄：細摳記錄（需求原文，2026-07-10）

## 細摳②：靜態物件 UX 定稿

「實現這些功能的程式碼就是那樣，**重點是操作方式**。」新增／修改（transform＋屬性：火把燃燒、門開關…）／刪除。

- **新增**：GUI 選物→指向法術落點（最簡）；或先放 marker→GUI 選「在那生成」。生成先出**綠色半透明輪廓**→幾秒或再施法→選單確認→OK 才實際生成。
- **刪除**：法術點選，或 GUI 列出 cell 內靜態物件（**我們新增的單獨一掛、最新在前**）。先變**紅色半透明輪廓**→確認。**色盲**：提供色彩調整（工具 esp 多套 EffectShader，面板切換）。
- **持續施法變體**（新增/刪除共用）：指哪打哪即時輪廓，收法選定最後命中者。
- **修改**：法術選中→**泛光**（勿半透明，與刪除區隔）→numpad 編輯＋屬性 GUI→`Enter`/`0` 結束。
- 設計後果：①「選中且動過」＝明示登記，解掉移動偵測問題；②屬性列表**只列能存活到 esp 的**（`PlacementSpec` 已有 Lock/Ownership/Count/InitiallyDisabled/EnableParent/LinkedRefs/Teleport；門「預設開啟」與火把燃燒待查，可能 enable-parent 對偶）；③輪廓/泛光 shader 機制待驗。

## 細摳③：動態物件＋檢視法術＋真刪除

- 動態物件同靜態流程，選中時**喪失物理**（光照渲染保留，細節實作時議）、結束回復。
- 檢視法術：持續施展顯示編輯痕跡，**四種：新增/修改/刪除/全部**；結束編輯後平時不顯示。
- **真刪除**：刪自己新增的＝徹底無痕（不進 removals、不留登記簿、檢視法術不顯示）。

## 細摳④：NPC/生物/leveled——全走 marker 流

「npc、生物這些暫時先不用弄太細，剩下都可以用同一套『指向性法術打 marker → 互動/GUI 改標籤 → 輸出 json → ModForge 讓 AI 在 marker 上擺東西』完成，甚至動態物件都可以先用這套。」leveled encounter 要（marker 流下 `placeatme` 抽選坑不存在；palette 直擺路徑的坑記錄於 git 歷史，日後富路徑再取用）。

---

# 驗證清單（彙總，未驗不寫進碼）

| 項 | 影響 | 驗法 |
|---|---|---|
| proxy 可見 base 的模型路徑 | P1 Task 0 | houseCARL/`find` 對 Skyrim.esm |
| `ExtraTextDisplayData` 可寫 | label 跨 session 免費復原 | grep header + 實機 |
| 各 hotkey scancode | P1/P2 | `GetIDCode()` log 實測 |
| 符文放置面限制 vs `bhkPickData` | P1 Task 4 | 工具 esp 試 + grep header |
| `TESObjectREFR::Delete()` 存在性 | 真刪除實作 | grep header（`Disable`/`Enable`/`IsDisabled` 已確認） |
| `SetMotionType(Keyframed)` C++ 對應 | P3 物理凍結 | grep header |
| EffectShader 綠/紅半透明＋泛光 | P2/P3 視覺 | 工具 esp 試 |
| 門「預設開啟」flag、火把燃燒的記錄表示 | P2 屬性列表 | houseCARL 讀 vanilla 記錄 |
| base=LVLN 的 placement 生成 | 富路徑 | grep `Generator.Build.Placements` |
| `data.location` 註解（authored vs live） | `SceneExporter.cpp` 待修註解 | 已知 `GetPosition()`≡`data.location`（`TESObjectREFR.h:405`） |
