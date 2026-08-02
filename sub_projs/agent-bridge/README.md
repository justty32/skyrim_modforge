# agent-bridge — 已移出

**2026-08-02 起本資料夾只是導引。** 內容全在：

```
projects/agent-bridge/          ← 本機：~/repo/moddings/skyrim/projects/agent-bridge
```

→ [新位置的 README](../../../agent-bridge/README.md)

## 是什麼

AI 全自動 mod QA 迴圈的兩端：遊戲內 SKSE C++23 DLL（`127.0.0.1:5099` HTTP：`/ping`、`/state`、`/console`）+ Linux 端 `client/`（`mo2ctl.py` 免 GUI 裝卸 mod、`qa_runner.py` 跑 `qa.json`、`qa_mcp.py` MCP server）。

## 留在 ModForge 的

計畫與結論在**工作區**：[`workflows/plans/ai-ingame-qa-loop.md`](../../../../workflows/plans/ai-ingame-qa-loop.md)（不在 ModForge）。ModForge 這邊沒有它的對接程式碼——它是測試治具，透過 console 與 `/state` 斷言驗 ModForge 生出來的 mod。

---

**未帶 commit 歷史**（使用者決定）——舊歷史查 `git log -- sub_projs/agent-bridge`。
