# Roadmap — 之後可做

← [INDEX](../INDEX.md)｜已完成的見 [feature-dev/landed](feature-dev/landed.md)、解碼依據見 [investigation/decode](investigation/decode/README.md)

**確定未來會做、但不確定何時**做的 backlog（比 [ideas](idea/ideas.md) 的「不確定要不要做」更篤定；非當前 in-flight——in-flight 在各工作流 session-log）。階梯：idea → **roadmap** → [spec](specs/README.md) → [plan](plans/README.md) → build。

---

## 待補清單（解碼浮現，按優先序）

1. **scene Dialog action 的 `Emotion`/`EmotionValue`** + 泛化 scene phase fragment（不只 PlayIdle，能跑 SetStage 等）：VIGILANT 演出靠 headtrack+emotion 取代 CAMS（78 cutscene、0 CAMS → CAMS 可延後）。
2. **worldspace LAND 高度圖**（自訂地圖地形，VIGILANT realm 的本體）、region-driven weather（REGN）—— 待先確認 ModForge worldspace builder 現況。
- **Scene 演出續做**：PlayIdle / 手勢動畫；camera shot（VIGILANT 證明可延後）。
- **多解 SM 事件**（SkillIncrease/Jail/Bribe…，須 conditions 才安全，見 [[dispatcher-magic-trigger]]）。
- **新 record**：Imagespace / Word of Power 等。（Music + Hazard 已落地，見 [feature-dev/landed](feature-dev/landed.md)。）

## 結構／工具

- **大檔拆分門檻改用 bytes**：現行 [DEV-GUIDE](../DEV-GUIDE.md) 用「300 行」當大檔標準，應改成 **4096 bytes**（更穩定、不受行長影響）。這會是一次全 src 大改（多檔需重拆），故延後——屆時更新 DEV-GUIDE「程式碼慣例」「結構整理原則」並掃一遍 `src/` 超標檔。

## 已有設計、待續

- **身份系統 ③ 聲望/行為追蹤**：需先定設計（GLOB 好感度系統是現成藍圖，見 sofia-patch 的 F6 分析）。in-flight 細節見 [feature-dev/session-log](feature-dev/session-log.md)。
