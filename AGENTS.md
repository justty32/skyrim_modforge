# AGENTS.md — 非 Claude agent 的入口

**本檔只是路由。** 這個 repo 的專案備忘是 **[CLAUDE.md](CLAUDE.md)**（Claude Code 會自動載入；其他工具讀不到，所以有這一份指過去）。兩份的規則完全一樣，**細節不在這裡重複寫**——重複的那一刻就開始腐爛。

> 本檔早期是 CLAUDE.md 的過時分身；改成路由的緣由已封存，見 [archive 索引](archive/README.md)。

## 先讀哪裡

- **要動手做某件事** → **[WORKFLOWS.md](WORKFLOWS.md)**：依意圖派發到對應工作流，再讀該工作流入口。
- **想看專案長怎樣** → **[INDEX.md](INDEX.md)**：repo 頂層結構地圖。
- **要碰原始碼** → [workflows/common/conventions.md](workflows/common/conventions.md)（程式碼慣例 + CODE_MAP 維護鏈）、導航走 [CODE_MAP](workflows/common/code-map/CODE_MAP.md)。
- **建置 / 測試 / 前置 / 出貨** → [workflows/dev-env.md](workflows/dev-env.md)（跨機：Manjaro 主力機 vs 離線機各能做什麼）。
- **要重構或整理結構** → [DEV-GUIDE.md](DEV-GUIDE.md)（被動參考，非日常）。

## 鐵律（always-on，任何工作流任何時候）

1. **重構/整理必須行為不變**——改完跑測試，離線至少 `Category!=RequiresSkyrim`（`./scripts/test-offline.sh`）。
2. **未經確認不 push、不開新工作**——commit 到 main 是慣例，push 先確認。
3. **durable 知識歸它所屬的那一層**，絕不往上堆到頂層檔案。本檔爛掉就是違反這條的下場。

## 分層思想

整個 repo 是一棵分層樹，每一層**只指向下一層、不存下層的細節**：

```
CLAUDE.md / AGENTS.md（最頂，路由）→ WORKFLOWS.md / INDEX.md → 各工作流入口 → 工作流內容 → 子工作流…
```

要某主題的細節，順著這棵樹往下走——不在本檔找，本檔不會有。
