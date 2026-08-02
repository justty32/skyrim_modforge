# AGENTS.md — 非 Claude agent 的入口

**本檔只是路由。** 這個 repo 的專案備忘是 **[CLAUDE.md](CLAUDE.md)**（Claude Code 會自動載入；其他工具讀不到，所以有這一份指過去）。兩份的規則完全一樣，**細節不在這裡重複寫**——重複的那一刻就開始腐爛。

> 2026-08-02：本檔原本是 CLAUDE.md 的一份 153 行分身（「Codex 專案備忘」），已經爛掉——指向五條不存在的路徑（`docs/CODE_MAP*.md`、`docs/IDEAS.md`、`docs/SPEC-*.md`，全都早就搬進 `workflows/` 與 `docs/spec/`）、宣稱測試數「259/260」（實際 1013）、把 `.pex` 前置寫成六條手打指令（早已被 `scripts/bootstrap-pex.sh` 取代）。內容逐段核對後確認**沒有任何一段是別處沒有的**，故整份改成路由。

## 先讀哪裡

- **要動手做某件事** → **[WORKFLOWS.md](WORKFLOWS.md)**：依意圖派發到對應工作流，再讀該工作流入口。
- **想看專案長怎樣** → **[INDEX.md](INDEX.md)**：repo 頂層結構地圖。
- **要碰原始碼** → [workflows/common/conventions.md](workflows/common/conventions.md)（程式碼慣例 + CODE_MAP 維護鏈）、導航走 [CODE_MAP](workflows/common/code-map/CODE_MAP.md)。
- **建置 / 測試 / 前置 / 出貨** → [workflows/dev-env.md](workflows/dev-env.md)（跨機：Manjaro 主力機 vs 離線機各能做什麼）。
- **要重構或整理結構** → [DEV-GUIDE.md](DEV-GUIDE.md)（被動參考，非日常）。

## 鐵律（always-on，任何工作流任何時候）

1. **重構/整理必須行為不變**——改完跑測試，離線至少 `Category!=RequiresSkyrim`（`./scripts/test-offline.sh`）。
2. **未經確認不 push、不開新工作**——commit 到 master 是慣例，push 先確認。
3. **durable 知識歸它所屬的那一層**，絕不往上堆到頂層檔案。本檔爛掉就是違反這條的下場。

## 分層思想

整個 repo 是一棵分層樹，每一層**只指向下一層、不存下層的細節**：

```
CLAUDE.md / AGENTS.md（最頂，路由）→ WORKFLOWS.md / INDEX.md → 各工作流入口 → 工作流內容 → 子工作流…
```

要某主題的細節，順著這棵樹往下走——不在本檔找，本檔不會有。
