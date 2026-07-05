# improve_notes — 工作流體系改進意見（2026-07-02）

逐一讀過 WORKFLOWS.md、各工作流入口檔、DEV-GUIDE、SESSION-LOG、WAIT_USER 後的觀察。分層路由的骨架本身很健康（薄 CLAUDE.md → 派發表 → 入口檔，每層只指下一層），以下是骨架上長出來的偏差，按影響排序。

> **已處理**：§1（SESSION-LOG 清倉 + 條目格式）、§2（specs/plans 狀態欄去重）、§3（五個過期指標）已於 2026-07-05 完成並移除；以下保留尚未動工的 §4–§8。

---

## 4. testing.md：重複、誤導、語言不一致

- 內容與 `dev-env.md`「測試」段**重複維護同一組指令**，違反「durable 細節只放一層」。建議 testing.md 保留為權威（指令 + Category 語意 + MODFORGE_SKYRIM_DATA），dev-env 只留一行連結。
- 敘述順序誤導：「Tests that clone vanilla templates … are marked:」後面接的卻是**排除**它們的 `Category!=RequiresSkyrim` 指令，讀起來像標記方式。段落該重排：先講排除跑法（日常），再講 RequiresSkyrim 跑法（需 Skyrim.esm）。
- 全 repo 工作流文檔都是繁中，唯 testing.md 與 tooling/README.md 前半是英文——不影響功能，但統一語言可降低「這檔是不是另一個體系」的困惑。

## 5. 派發表（WORKFLOWS.md）觸發詞缺口

常見意圖沒有命中項，agent 得靠猜：

- 「**修 bug**」→ 應明列派 feature-dev（現在只有「開發/修改 feature」，修 bug 語感不同）。
- 「**打包出貨 / ship**」→ 流程在 dev-env.md，派發表沒入口。
- 「**同步 zh-TW / 重生 html**」→ SESSION-LOG 顯示這是高頻常態工作，但整個派發體系找不到它的流程文件入口（規則散在 conventions 的優先級序列與各 session 記錄裡）。值得給它一個明確的家（哪怕一個小單檔工作流：觸發時機、鏡像範圍、html 重生指令）。
- 「**記/查踩坑**」→ gotchas 有三處（common / feature-dev / investigation），歸類規則只藏在 common/README 的括號註記。建議在 common/gotchas.md 頂部放三行「哪類坑記哪裡」決策表。
- 兜底：表尾加一行「都不符 → 看 INDEX.md」。

## 6. dev-env.md 混進了 gotcha 內容，且機器表跟不上現實

- ship-voice 的「已知陷阱」（TIF 內聯編譯 spurious fail 的完整修法）與 LipGenerator wine crash 是典型踩坑內容，放在 dev-env 使其膨脹、又不在 gotchas 檢索路徑上。建議移 feature-dev/gotchas（外部工具聯動類正屬它），dev-env 留一行指標。
- 機器表只有「Manjaro 主力機／離線機」兩種，但這台是 Windows 11 + PowerShell（dev-env 末尾那句 PowerShell commit 注意事項洩露了它的存在）。建議機器表明列「離線機＝Windows」並補注意事項：`scripts/*.sh` 需經 bash、或直接給 `dotnet test --filter` 原生跨平台指令。

## 7. 超標檔案（DEV-GUIDE 觸發 A，該檢視拆分）

- `roadmap/mod-survey-gaps.md` 22.3 KB（門檻 8 KB 的近 3 倍；roadmap 不在豁免清單）。
- `roadmap/generation.md` 8.5 KB 剛過線。
- （`code-map/` 與現役 plan 依規則豁免，不列。）

## 8. 「spec」一詞三義（低優先，但對新 agent 是真實混淆源）

`workflows/specs/`（設計方案）、`docs/spec/`（JSON spec 欄位手冊）、`examples/*.json`（JSON spec 本體）共用一個詞。specs/README 已有語境自救，但反向沒有——建議在 `docs/spec/SPEC-index.md` 開頭加一行消歧義（「設計方案在 workflows/specs/，別搞混」），派發表的 spec 行也可括注。

---

### 建議動手順序（§1–§3 已完成，以下為剩餘項）

1. testing/dev-env 去重 + gotcha 搬家（§4、§6）。
2. 派發表補觸發詞 + gotchas 決策表（§5）。
3. roadmap 兩檔拆分視需要排程（§7），spec 消歧義順手做（§8）。
