# 程式碼慣例 + CODE_MAP 維護鏈（碼相關工作流共用）

← [common/README](README.md)｜[INDEX](../../INDEX.md)

碰原始碼的工作流（[feature-dev](../feature-dev/README.md) / [refactor](../refactor/README.md) / [specs](../specs/README.md) / [plans](../plans/README.md)）共用這套規矩。純文檔/調查類工作流（idea / investigation / tooling…）用不到。結構整理原則（被動、按需取用）在 [DEV-GUIDE](../../DEV-GUIDE.md)；always-on 鐵律在 [CLAUDE.md](../../CLAUDE.md)。

## 程式碼慣例

- `partial class` 按領域拆檔：CLI 是 `Program.cs` + `Diagnostics.*.cs` + `Package.cs`；Core 是 `Generator.Build.*.cs`
- 所有 src 檔案維持在 **300 行**以下（`partial class` 按領域拆）；`examples/` 同樣 300 行。（文檔拆檔門檻見 [DEV-GUIDE 結構整理原則](../../DEV-GUIDE.md)：**`workflows/` 文檔 8192 bytes、`docs/` 使用手冊 300 行**。）
- **Spec 欄位 breaking change**：新增欄位安全（optional，舊 example 不受影響）；**刪除或改名欄位**前必須先 `grep -r "舊欄位名" examples/`，找出所有受影響的 JSON 並在同一個 commit 裡一起更新。
- **新增 Spec 欄位後**：手動更新 `examples/spec.schema.json`（IDE autocomplete 用；無自動同步機制，允許偶爾落後，但 commit 前盡量補上）。

## CODE_MAP 維護鏈

程式碼導航 index 在 [code-map/CODE_MAP.md](code-map/CODE_MAP.md)（頂層）→ 五份子 index：

| 子 index | 涵蓋 |
|---------|------|
| `code-map/CODE_MAP.dialogue-quests.md` | quest / dialogue / scene / Story Manager / ScriptEvent / word wall |
| `code-map/CODE_MAP.world.md` | cell / placement / worldspace / region / leveled list / container / encounter zone |
| `code-map/CODE_MAP.items-magic.md` | weapon / armor / spell / magic effect / enchantment / perk / shout / long-tail |
| `code-map/CODE_MAP.npcs-packages.md` | NPC / faction / class / AI package / combat style / weather / climate |
| `code-map/CODE_MAP.infra.md` | CLI / build orchestrator / validate / package / Papyrus / translate / plugin I/O |

三個面向構成維護鏈：**程式碼（含 examples/ 與 assets/）→ CODE_MAP → 文檔**（HTML bundle 最低，只在明確要求時更新）。

`examples/*.json`、`examples/scripts/*.psc`、`examples/assets/`、`assets/papyrus/MFStoryEventDispatch.psc`、`spec.schema.json` 均視為**源碼**——功能變動時必須同步，不是次要的附屬物。

**優先級（衝突或時間不夠時，依序保持一致）：** 程式碼（含 examples + assets）> CODE_MAP > 文檔（`docs/spec/SPEC-*.md` / `docs/for_agent*.md`）> HTML

**CODE_MAP 與程式碼衝突時：以程式碼為準，立即修正 CODE_MAP。**

**日常規則：**
1. **修改前**：先讀 [code-map/CODE_MAP.md](code-map/CODE_MAP.md)，找到相關子 index，只讀清單中列出的檔案——不要讀無關領域的檔案。
2. **修改後**：若新增或刪除了 `.cs` 檔案，或某檔案的職責有顯著改變，必須同步更新對應子 index（含 Tests 欄）。
3. `.cs` 檔案本身不加「對應 CODE_MAP」的註釋（維護成本過高）；反向查找直接 `grep` CODE_MAP 文件。
