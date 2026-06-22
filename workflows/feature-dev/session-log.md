# feature-dev — session log（功能開發）

← [SESSION-LOG hub](../../SESSION-LOG.md)｜本工作流：[landed](landed/README.md) · [gotchas](gotchas.md)

**只放本工作流還沒完成的 in-flight / open 狀態**；完成即移除（→ [landed](landed/README.md) + git log）。

---

## 進行中 / open

- **身份系統 ③ 聲望/行為追蹤**：🧊 **不必做（2026-06-22 決定冷凍，等很有空時再做）**。JContainers/persist 早已摸透（前提解除），但暫時不排此功能。聲望/行為追蹤的資料結構（per-NPC 多維狀態）適合用 JContainers 而非單純 GLOB。GLOB 好感度系統是現成藍圖（見 sofia-patch F6 分析）；做時會順手帶出 J 組「arbitrary-ref target」（per-NPC 記資料）。其餘身份系統子項皆已落地（見 [landed](landed/README.md)）。roadmap 條目見 [roadmap](../roadmap/README.md)。
