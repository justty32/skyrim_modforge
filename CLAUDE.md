# ModForge — Claude Code 專案備忘

ModForge = **JSON spec → Skyrim `.esp` 生成工具**（AI-agent 友善）。本檔是**最頂層路由器**：只指向下一層，**durable 細節一律不寫這裡**。

## 開發環境

跨機開發：**Manjaro 主力機**（完整，含實機測試 / Wine / CK / 語音）與**離線機 fresh clone**（無 Skyrim / Wine / 遊戲，只做離線開發與測試）。build / 測試 / 前置 / 出貨指令與**各機能做什麼**全在 **[workflows/dev-env.md](workflows/dev-env.md)**。

因為跨機，**不用 Claude 本機 memory**——需要記憶的一律寫進 repo 檔案（歸到所屬工作流那一層）。

## 先讀哪裡

- **使用者要你動手做某件事** → **[WORKFLOWS.md](WORKFLOWS.md)**：依使用者意圖派發到對應工作流，再讀該工作流入口。
- **想看專案長怎樣** → **[INDEX.md](INDEX.md)**：repo 頂層結構地圖。

## 分層思想（本專案的組織原則）

整個 repo 是一棵**分層樹**，每一層**只指向下一層、不存下層的細節**：

```
CLAUDE.md（本檔，最頂）→ WORKFLOWS.md / INDEX.md → 各工作流入口 → 工作流內容 → 子工作流…
```

- **README**＝初入一個資料夾**先讀的入口／導引**；**INDEX**＝**描述該資料夾頂層結構**的索引。小資料夾兩者合一，大了才分出獨立 INDEX。
- **durable 知識歸到它所屬的那一層／那個工作流**，絕不往上堆——所以 CLAUDE.md 才這麼薄。要某主題的細節，順著上面的樹往下走，不在本檔找。
- **鐵律（always-on，任何工作流任何時候都遵守）**：① 重構/整理必須**行為不變**（改完跑測試，離線至少 `Category!=RequiresSkyrim`）；② **未經確認不 push、不開新工作**（commit 到 master 是慣例，push 先確認）；③ 各工作流的具體流程在它自己的 README，不在頂層。
- **[DEV-GUIDE.md](DEV-GUIDE.md) 是被動參考**（結構整理原則 + 四級成長軌跡）——**只在你要重構/整理結構時才取用**，不貫穿日常每個動作（性質類似 zh-tw / html：需要時才拿出來，不是 always-on 憲法）。只在**碰原始碼**時適用的**程式碼慣例 + CODE_MAP 維護鏈**在 [common/conventions](workflows/common/conventions.md)。

## 外部工具（主力機專屬）

**Gemini CLI**（`gemini -p "..."`，headless 模式）可做：
- ✅ 聯網搜尋（知識截止後的最新資訊、GitHub 狀態、社群工具）
- ✅ 長文本關鍵字搜尋 / 定向摘取
- ⚠️ 資訊壓縮（有風險，幻覺率高）
- ❌ 程式碼準確性、具體 FormID/API 事實（必須人工驗證）

**使用準則**：① 逐一跑，不要同時開 6 個（rate limit）；② 生出的 URL、工具名、版本號**必須驗**（Gemini 捏造習慣嚴重）；③ 原始輸出存 `sub_projs/gemini-research/`，確認過的結論才搬進正式 finding。

## 主工作流（進度與待測）

事情告一段落、因應需求結束、或臨時中止時 → 把**還沒完成**的活狀態記到進度；需要**使用者親自驗證**的（遊戲實機、外部工具實跑、需權限/本機環境）→ 記到待測。兩者都**只列 open**，完成即移除、不留已完成清單。

- **進度** → [SESSION-LOG.md](SESSION-LOG.md)
- **待測** → [WAIT_USER.md](WAIT_USER.md)
