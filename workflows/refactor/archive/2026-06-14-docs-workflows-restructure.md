# docs→workflows 大重構批次（2026-06-14，已完成）

← [refactor/archive](README.md)

> 封存記錄、凍結。本批次原列於 `roadmap.md` 的「結構／工具」段，roadmap 升 L2（`roadmap/` 資料夾）時把**已完成項**移來此處；roadmap 只保留 open 重構項（見 [roadmap/structure-tooling.md](../../roadmap/structure-tooling.md)）。內部路徑為當時狀態，容忍 stale。

## 大檔拆分門檻改用 bytes ✅

trigger-to-review、非硬上限；本質不可分可超標；archive/ 與 code-map/ 豁免。**門檻分兩套**：`workflows/` 開發流程文檔 **8192 bytes**；`docs/` 使用手冊文檔 **300 行**；`src/`、`examples/` **300 行**。DEV-GUIDE 觸發 A + conventions 已同步。

## docs/ 拆檔（2026-06-14）✅

`SPEC-world`(485)→ `SPEC-world`+`SPEC-worldspaces`；`SPEC-dialogue-quests`(653)→ `SPEC-dialogue`+`SPEC-quests`+`SPEC-identities`。tracked EN docs 零斷鏈。

## zh-TW 鏡像重新對齊（2026-06-14）✅

`docs/zh-TW/` 整批重譯並 1:1 鏡像 EN `docs/`——spec 移入 `spec/` 子夾、`SPEC-dialogue-quests`→dialogue+quests+identities、`SPEC-world`→world+worldspaces、新增 `SPEC-refs`、補 `local-skyrim-extraction`；`asset-pipelines/` 孤兒鏡像已刪（EN 正本在 `workflows/idea/`，不屬使用手冊）。逃出鏡像樹的連結補一層 `../`（zh-TW 深一層）；137 條鏡像內連結零斷鏈。`engine-internals` 標題保留英文（跨檔 anchor 目標）。html bundle 經 `generate.py` 重生（31 頁）。

> 殘留：`docs/zh-TW/` 鏡像的舊路徑同步（翻譯同步、另計）仍 open，已留在 [roadmap/structure-tooling.md](../../roadmap/structure-tooling.md)。

## workflows 文檔拆檔（已調查 + 已執行，2026-06-14）✅

一輪 per-workflow agent 審查備好拆分地圖。**範圍**：只動 `workflows/` 文檔（`src/` 另排）。**豁免**：`*/archive/`（封存件凍結保脈絡、不在維護鏈）與 `common/code-map/`（CODE_MAP 是 code 鏡像，依**程式碼領域**而非 byte 分檔）一律**不套** 8192-byte 規則。

已執行：

- **tooling.md**（9.6K）→ 升 L2：`tooling/` + README，按職責分 `env-vars` / `binaries` / `data-assets`。✅
- **feature-dev/landed.md**（14K）→ `landed/` + INDEX，**對齊 CODE_MAP 五分法**（dialogue-quests / world / items-magic / npcs / infra）。✅ `infra.md` 5.1K 略超但維持 CODE_MAP 粒度（voice 濃縮句明細在 memory/git）。gotchas.md（5K）只需檔內分節、不拆（暫緩，已留在 roadmap）。
- **plans/** 巨型多階段計畫 → **改決定**：已完成的 plan 不拆，直接移 `plans/archive/`（凍結、不在維護鏈、不套門檻）。9 個現役 plan 全已落地 → 全移 archive；現役 plans/ 清空（待下個 in-flight 才有新 plan）。同理 **specs/** 9 份 design 也全移 `specs/archive/`（維持 spec↔plan 配對）。維護鏈外部連結（ideas / CODE_MAP.dialogue-quests 的 identity 引用）已改指 archive。✅
- **idea/**：`02` particle-vfx → `particle-vfx/`（L4，efsh-record-layer / particle-nif-wall）✅；`04` map-scene → `map-scene/`（L4，layout-extraction / geometry / workflow-modforge）✅；`05` animation → `animation/`（L4，havok-blender / integration-layer / linux-workflow-modforge）✅；`voice-clone/01-engine-setup` → `voice-clone/engine-setup/`（按引擎：f5/chatterbox/gptsovits/fish-speech）✅。
  - **改判定：`01`/`03` 概覽 KEEP 不瘦身**——連貫研究報告（≠混雜索引），已有「已展開子工作流 →」指標；硬瘦身會丟研究內容，套「不可分敘事 KEEP」原則。
  - `ideas.md`（18K）**緩**（已留在 roadmap）；步驟檔（model-porting 01~10、voice-clone 02~06）**全 KEEP**（已是拆分結果）。
- **specs/** 現役 8 份 design 全 KEEP → 已隨 plans 一併移 `specs/archive/`（一份 spec/plan=一個整體不拆，完成即進凍結 archive）。✅
- **investigation/decode/** 解碼筆記**全 KEEP**（單篇連貫）；真議題是 decode/ 是否按 mod 開子夾，**等下個 mod 解碼進來再分**（已留在 roadmap）。`notes-gemini-voice` 微超標且自述「處理完可刪」→ 已刪。✅
- **refactor**（L2，0 超標）免動。

順手待辦：docs→workflows 殘留舊路徑 `docs/minor/ideas.md`、`docs/CODE_MAP.*.md` → 維護鏈上的 live 檔（model-porting/voice-clone 05、blender-layout）已校正指向 `workflows/common/code-map/`。✅ **archive 內的舊路徑保持凍結**（歷史 build 指令、不在導航鏈，依封存慣例容忍 stale）。`docs/zh-TW/` 鏡像的同類舊路徑屬翻譯同步、另計（仍 open）。plan→spec 反向連結缺 `Design doc:` 行的也都在 archive、凍結。
