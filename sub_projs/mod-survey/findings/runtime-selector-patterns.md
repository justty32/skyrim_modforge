# Runtime Selector Patterns — Finding

> 本文是兩種「執行期資料模式」的合一拆解：**Global-as-selector**（用 GlobalVariable 當跨系統共享狀態）與 **linkedRef 節點鏈**（用 XLKR 連接 placed ref 組成路線/觸發序列/目標集合）。材料來源：Immersive Interactions、Conditional Expressions、Animated Carriage 的 mod survey finding，以及 ModForge `src/` 實際 builder。
> 繁體中文行文；record 名/函數名/欄位名保留英文。

---

## 內容拆分

- [Global-as-selector：概念 + 語法](runtime-selector-global-concept-syntax.md) — 概念說明 + 各消費方（OAR/DAR/condition）語法
- [Global-as-selector：生命週期 + 評估](runtime-selector-global-lifecycle-eval.md) — 生命週期模式 + ModForge 生成評估
- [linkedRef：概念 + record 結構](runtime-selector-linkedref-structure.md) — 概念說明 + XLKR record 層結構
- [linkedRef：WireLinkedRefs + 設計模式](runtime-selector-linkedref-wiring.md) — 現有 WireLinkedRefs() 能力 + 設計模式

## 附錄：命名規則建議

### Global-as-selector 命名

| 用途 | 建議格式 | 範例 |
|------|----------|------|
| 動畫選擇器 | `<ModPrefix>_AnimState` | `MF_AnimState` |
| Busy gate | `<ModPrefix>_Busy` | `MF_Busy` |
| 玩家狀態中介 | `<ModPrefix>_Player<State>` | `MF_PlayerDrunk` |
| MCM 開關 | `<ModPrefix>_Enable<Feature>` | `MF_EnableGreetAnim` |
| 計數器/聲望 | `<ModPrefix>_<Counter>` | `MF_ReputationScore` |

### linkedRef 路線命名

| 用途 | 建議格式 | 範例 |
|------|----------|------|
| 路線整體 | `<ModPrefix>_Route_<Name>_<N>` | `MF_Route_TavernPatrol_01` |
| 路線起點 | 同上，後綴 `Start` | `MF_Route_TavernPatrol_Start` |
| 路線終點 | 同上，後綴 `End` | `MF_Route_TavernPatrol_End` |
| 具名 keyword | `kw<ModPrefix>_<LinkName>` | `kwMF_AltPath` |
