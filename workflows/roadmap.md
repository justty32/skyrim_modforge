# Roadmap — 之後可做

← [INDEX](../INDEX.md)｜已完成的見 [feature-dev/landed](feature-dev/landed/README.md)、解碼依據見 [investigation/decode](investigation/decode/README.md)

**確定未來會做、但不確定何時**做的 backlog（比 [ideas](idea/ideas.md) 的「不確定要不要做」更篤定；非當前 in-flight——in-flight 在各工作流 session-log）。階梯：idea → **roadmap** → [spec](specs/README.md) → [plan](plans/README.md) → build。

---

## 待補清單（解碼浮現，按優先序）

1. **scene Dialog action 的 `Emotion`/`EmotionValue`** + 泛化 scene phase fragment（不只 PlayIdle，能跑 SetStage 等）：VIGILANT 演出靠 headtrack+emotion 取代 CAMS（78 cutscene、0 CAMS → CAMS 可延後）。
2. **worldspace LAND 高度圖**（自訂地圖地形，VIGILANT realm 的本體）、region-driven weather（REGN）—— 待先確認 ModForge worldspace builder 現況。
- **Scene 演出續做**：PlayIdle / 手勢動畫；camera shot（VIGILANT 證明可延後）。
- **多解 SM 事件**（SkillIncrease/Jail/Bribe…，須 conditions 才安全，見 [[dispatcher-magic-trigger]]）。
- **新 record**：Imagespace / Word of Power 等。（Music + Hazard 已落地，見 [feature-dev/landed](feature-dev/landed/README.md)。）

## 結構／工具

- **大檔拆分門檻改用 bytes**：現行 [DEV-GUIDE](../DEV-GUIDE.md)／[common/conventions](common/conventions.md) 用「300 行」當大檔標準，應改成 **4096 bytes**（更穩定、不受行長影響）。屆時更新 DEV-GUIDE「結構整理原則」+ conventions「程式碼慣例」，並掃 `src/` 超標檔（src 是此門檻的**原始**適用對象）。

  ### workflows 文檔拆檔（已調查，2026-06-14；待執行）

  一輪 per-workflow agent 審查已備好拆分地圖。**範圍決定**：此次只動 **`workflows/` 文檔**（`src/` 另排）。**豁免**：`*/archive/`（封存件凍結保脈絡、不在維護鏈）與 `common/code-map/`（CODE_MAP 是 code 鏡像，依**程式碼領域**而非 byte 分檔）一律**不套** 4096 規則。

  待執行清單（按工作量）：
  - ~~**tooling.md**（9.6K）→ 升 L2：`tooling/` + README，按職責分 `env-vars` / `binaries` / `data-assets`~~ ✅ 已拆（`workflows/tooling/`）。
  - ~~**feature-dev/landed.md**（14K）→ `landed/` + INDEX，**對齊 CODE_MAP 五分法**（dialogue-quests / world / items-magic / npcs / infra）~~ ✅ 已拆（`landed/` + README index）。`infra.md` 5.1K 略超但維持 CODE_MAP 粒度（voice 濃縮句明細在 memory/git）。gotchas.md（5K）只需檔內分節，不拆（暫緩）。
  - **plans/** 巨型多階段計畫 → 各成 `plans/<功能>/` + index（升 L4，沿天然 Task/子系統切點）：lighting-pipeline 54K、spec-refs-env 41K、quest-markers 31K(A/B/C 層)、playidle 26K。中型 hazard/music/weather/voice-annotation 為 BORDERLINE（看是否容忍超標）；identity-mvp 8K KEEP。
  - **idea/**：概覽 `01`/`03` 瘦身成「survey + 指向已展開子夾」的導航頁；`02`/`04`/`05` 按內容拆（04 map-scene、05 animation 興趣最高、最可能升 L4 子夾）；`voice-clone/01-engine-setup` 12K 可按引擎再分。步驟檔（model-porting 01~10、voice-clone 02~06）**全 KEEP**（已是拆分結果）。`ideas.md` 18K 按 idea 主題分類拆，但它是入口主檔、可緩。
  - **specs/** 現役 8 份 design **全 KEEP**（一份 spec=一個整體，硬拆傷讀）。
  - **investigation/decode/** 9 份解碼筆記**全 KEEP**（單篇連貫）；真議題是 decode/ 是否按 mod 開子夾，建議**等下個 mod 解碼進來再分**。`notes-gemini-voice` 微超標且自述「處理完可刪」→ 確認後刪或移 archive。
  - **refactor**（L2，0 超標）免動。

  順手待辦：plan→spec 反向連結只有 3/8 顯式（lighting/weather/hazard/music/quest-markers 缺 `Design doc:` 行）；docs→workflows 殘留的舊路徑 `docs/minor/ideas.md`、`docs/CODE_MAP.*.md` 仍散見於部分 design 本體與 archive（未動，待全 repo 校正時一併處理）。

## 已有設計、待續

- **身份系統 ③ 聲望/行為追蹤**：需先定設計（GLOB 好感度系統是現成藍圖，見 sofia-patch 的 F6 分析）。in-flight 細節見 [feature-dev/session-log](feature-dev/session-log.md)。
