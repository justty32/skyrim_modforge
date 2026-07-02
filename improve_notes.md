# improve_notes — 工作流體系改進意見（2026-07-02）

逐一讀過 WORKFLOWS.md、各工作流入口檔、DEV-GUIDE、SESSION-LOG、WAIT_USER 後的觀察。分層路由的骨架本身很健康（薄 CLAUDE.md → 派發表 → 入口檔，每層只指下一層），以下是骨架上長出來的偏差，按影響排序。

---

## 1. SESSION-LOG.md 已嚴重超標，且違反自己的 open-only 規則（最大單一改進點）

- **現況**：23.6 KB ≈ 門檻 8192 bytes 的 3 倍。檔頭自己寫了「膨脹就拆 → `session_logs/`」的觸發條款，但一直沒觸發。
- **更根本的問題**：檔頭說「只放還沒完成的」，實際條目大量是**已完成敘事**——2026-06-20 的多條 ✅ IN-GAME CONFIRMED、已修完 bug 的完整過程、mod 調查批的全部結論。這些照規則該濃縮進 `feature-dev/landed/` 與 git log，SESSION-LOG 每條只留 open 尾巴。living-adventurers 一條就 ~2500 字，其中 open 的其實只有最後一句「剩主力機 package + 實機」。
- **為什麼重要**：SESSION-LOG 是每個 session 開頭最可能被整讀的檔，越肥每次啟動越貴，而且 open 項被淹在 done 敘事裡反而難找。
- **建議**：① 立即做一次清倉——done 部分移 landed/ 或直接刪（git log 已有）；② 給條目定強制格式：「**一行 open 狀態 + 指向細節的連結**」，細節（設計決策、修了什麼）落到該工作流或 sub_proj 的文件裡；③ 完成即整條刪除，不留「✅ 已確認」條目。

## 2. 規劃管線的狀態欄多處重複，已實際漂移

- **證據**：worldspace-editor 的狀態同時活在 specs README 現役表、plans README 現役表、SESSION-LOG（Idea #19 條）、wait_todo/worldspace-editor.md 至少四處。而且**已經漂了**：`specs/README.md:20` 還寫「待出 plan」，但 `plans/README.md:17` 早已列出該 plan 且 Task 1–6 落地。
- **建議**：狀態只留一個 source of truth（建議 plans 表；未出 plan 的留 specs 表），其他地方**只留連結、不留狀態欄**。specs 表的狀態欄可整個刪掉——「在現役夾＝現役、在 archive/＝落地」已由檔案位置隱含。
- **同類重複**：specs 與 plans 兩份 README 各有一段幾乎相同的「命名不含日期 + 落地即 archive」規則文字，改一處必須記得改另一處；可抽成一段、另一邊留一行指標。

## 3. 一批過期指標／自相矛盾敘述（低成本、一次修完）

1. `WORKFLOWS.md:38`「單檔工作流（tooling / roadmap / testing）」——**tooling 與 roadmap 都已是資料夾型**（tooling/ 4 檔、roadmap/ 6 檔 + archive）。順帶建議：這種「逐一點名」的正面清單每次升級都會過期，改成描述性規則（單檔＝還沒長成資料夾者），點名交給上面的派發表。
2. `workflows/tooling/README.md:21`「See CLAUDE.md『前置步驟』」——前置步驟早已搬到 `dev-env.md`，CLAUDE.md 已無此段。
3. `workflows/feature-dev/README.md:26` 連結文字寫 `landed.md`、實指 `landed/README.md`——landed 已升級成資料夾，標籤沒跟上。
4. idea #20 雙入口不一致：`ideas.md` 索引直接指 `sub_projs/inworld-skill-tree/`，但 `workflows/idea/inworld-skill-tree.md` 仍存在、且被 progression-combat-overhaul.md 與 sub_proj README 反向連結。要嘛索引恢復指 idea 檔、要嘛 idea 檔內容併進 sub_proj README 後刪除。
5. `WAIT_USER.md`「各工作流的待你項」段永遠是「目前無」——實際待驗項全在 `wait_todo/` 分類檔裡。這段只增加一個「要記得更新」的位置，建議刪除。

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

### 建議動手順序

1. SESSION-LOG 清倉 + 定條目格式（§1）——收益最大。
2. 一次修完 §3 的五個過期指標 + §2 的狀態欄去重（都是純文檔行為不變修正）。
3. testing/dev-env 去重 + gotcha 搬家（§4、§6）。
4. 派發表補觸發詞 + gotchas 決策表（§5）。
5. roadmap 兩檔拆分視需要排程（§7），spec 消歧義順手做（§8）。
