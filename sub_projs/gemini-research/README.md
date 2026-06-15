# gemini-research — Gemini CLI 搜尋原始輸出

Gemini CLI（聯網搜尋）生成的**原始研究素材**。品質參差不齊（幻覺嚴重），純粹存放原始輸出供人工篩選（「屎裏掏金」）。

**結構**：每個主題開一個子資料夾，每個搜尋 query 一個 `.md` 檔。

## 子資料夾

| 資料夾 | 對應主題 |
|--------|---------|
| [npc-beautification/](npc-beautification/) | 通用 NPC 美化（自動覆蓋 mod 角色） |

## 使用原則

- **不信任**任何沒有 URL 或具體工具名的說法
- **URL 要驗**（Gemini 會捏造 URL）
- 有價值的結論手動整理進正式 finding（`sub_projs/mod-survey/findings/` 或 `workflows/idea/`）
