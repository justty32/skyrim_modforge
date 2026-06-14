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

- ~~**大檔拆分門檻改用 bytes**~~ ✅ 已改（trigger-to-review、非硬上限；本質不可分可超標；archive/ 與 code-map/ 豁免）。**門檻分兩套**：`workflows/` 開發流程文檔 **8192 bytes**；`docs/` 使用手冊文檔 **300 行**；`src/`、`examples/` **300 行**。DEV-GUIDE 觸發 A + conventions 已同步。
  - **docs/ 拆檔（2026-06-14）**：`SPEC-world`(485)→ `SPEC-world`+`SPEC-worldspaces`；`SPEC-dialogue-quests`(653)→ `SPEC-dialogue`+`SPEC-quests`+`SPEC-identities`。tracked EN docs 零斷鏈。
  - ~~**待辦：zh-TW 鏡像重新對齊**~~ ✅ 已做（2026-06-14）：`docs/zh-TW/` 整批重譯並 1:1 鏡像 EN `docs/`——spec 移入 `spec/` 子夾、`SPEC-dialogue-quests`→dialogue+quests+identities、`SPEC-world`→world+worldspaces、新增 `SPEC-refs`、補 `local-skyrim-extraction`；`asset-pipelines/` 孤兒鏡像已刪（EN 正本在 `workflows/idea/`，不屬使用手冊）。逃出鏡像樹的連結補一層 `../`（zh-TW 深一層）；137 條鏡像內連結零斷鏈。`engine-internals` 標題保留英文（跨檔 anchor 目標）。html bundle 經 `generate.py` 重生（31 頁）。

  ### workflows 文檔拆檔（已調查，2026-06-14；待執行）

  一輪 per-workflow agent 審查已備好拆分地圖。**範圍決定**：此次只動 **`workflows/` 文檔**（`src/` 另排）。**豁免**：`*/archive/`（封存件凍結保脈絡、不在維護鏈）與 `common/code-map/`（CODE_MAP 是 code 鏡像，依**程式碼領域**而非 byte 分檔）一律**不套** 8192-byte 規則。

  待執行清單（按工作量）：
  - ~~**tooling.md**（9.6K）→ 升 L2：`tooling/` + README，按職責分 `env-vars` / `binaries` / `data-assets`~~ ✅ 已拆（`workflows/tooling/`）。
  - ~~**feature-dev/landed.md**（14K）→ `landed/` + INDEX，**對齊 CODE_MAP 五分法**（dialogue-quests / world / items-magic / npcs / infra）~~ ✅ 已拆（`landed/` + README index）。`infra.md` 5.1K 略超但維持 CODE_MAP 粒度（voice 濃縮句明細在 memory/git）。gotchas.md（5K）只需檔內分節，不拆（暫緩）。
  - ~~**plans/** 巨型多階段計畫 → 升 L4 拆 per-Task~~ ✅ **改決定**：已完成的 plan **不拆**，直接移 `plans/archive/`（凍結、不在維護鏈、不套門檻）。9 個現役 plan 全已落地 → 全移 archive；現役 plans/ 清空（待下個 in-flight 才有新 plan）。同理 **specs/** 9 份 design 也全移 `specs/archive/`（維持 spec↔plan 配對）。維護鏈外部連結（ideas / CODE_MAP.dialogue-quests 的 identity 引用）已改指 archive。
  - **idea/**：
    - ✅ `02` particle-vfx → `particle-vfx/`（L4，efsh-record-layer / particle-nif-wall）。
    - ✅ `04` map-scene → `map-scene/`（L4，layout-extraction / geometry / workflow-modforge）。
    - ✅ `05` animation → `animation/`（L4，havok-blender / integration-layer / linux-workflow-modforge）。
    - ✅ `voice-clone/01-engine-setup` → `voice-clone/engine-setup/`（按引擎：f5/chatterbox/gptsovits/fish-speech）。
    - **改判定：`01`/`03` 概覽 KEEP 不瘦身**——它們是連貫研究報告（≠混雜索引），已有「已展開子工作流 →」指標；硬瘦身會丟研究內容，套用「不可分敘事 KEEP」原則。
    - **`ideas.md`（18K）緩**：入口主檔，按主題拆風險高、價值低，暫不動。
    - 步驟檔（model-porting 01~10、voice-clone 02~06）**全 KEEP**（已是拆分結果）。
  - ~~**specs/** 現役 8 份 design 全 KEEP~~ ✅ 已隨 plans 一併移 `specs/archive/`（見上條；一份 spec/plan=一個整體不拆，完成即進凍結 archive）。
  - **investigation/decode/** 解碼筆記**全 KEEP**（單篇連貫）；真議題是 decode/ 是否按 mod 開子夾，建議**等下個 mod 解碼進來再分**。~~`notes-gemini-voice` 微超標且自述「處理完可刪」~~ ✅ 已刪。
  - **refactor**（L2，0 超標）免動。

  順手待辦：~~docs→workflows 殘留舊路徑 `docs/minor/ideas.md`、`docs/CODE_MAP.*.md`~~ ✅ 維護鏈上的 live 檔（model-porting/voice-clone 05、blender-layout）已校正指向 `workflows/common/code-map/`；**archive 內的舊路徑保持凍結**（歷史 build 指令、不在導航鏈，依封存慣例容忍 stale）。`docs/zh-TW/` 鏡像的同類舊路徑屬翻譯同步，另計。plan→spec 反向連結缺 `Design doc:` 行的也都在 archive、凍結。

## 已有設計、待續

- **身份系統 ③ 聲望/行為追蹤**：需先定設計（GLOB 好感度系統是現成藍圖，見 sofia-patch 的 F6 分析）。in-flight 細節見 [feature-dev/session-log](feature-dev/session-log.md)。
