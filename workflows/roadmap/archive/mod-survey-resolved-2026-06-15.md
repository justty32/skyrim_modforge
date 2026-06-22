# mod-survey 缺口 — 已撤銷的誤判項（2026-06-15 code 驗證）

← [roadmap/archive](README.md)

> **已凍結（archived）**：本檔收錄 2026-06-15 逐檔 code 驗證後判定「其實早就支援、撤銷此缺口」的誤判項。原由 survey agent 推斷為缺口，核對 `src/` 實際 builder 後確認既有功能已涵蓋，故撤銷。保留作脈絡（為何曾被當缺口 + 實際支援位置）。活檔 backlog（still-open partial / 真缺）見 [../mod-survey-gaps.md](../mod-survey-gaps.md)。內部連結容忍 stale。

## ✅ 已支援（原缺口判斷有誤，撤銷）

- **~~建立新 FLST + 填 ref~~** — 早有。`Generator.Build.Lists.cs:BuildFormLists()` 從 `spec.FormLists` 建新 FLST record；`Generator.Build.Lists.Wire.cs:WireFormLists()` 經 `Resolve(...)` 填 item，支援 in-spec editorId（自家 esp ref）與 vanilla `Plugin.esm:0xID`。
- **~~`placements[].linkedRef` 欄位（+ keyword 變體）~~** — 早有。`Generator.Build.PlacementRefs.cs:WireLinkedRefs()` 讀 `pl.LinkedRefs`、設 XLKR，且支援具名 keyword link（`link.KeywordOrReference.SetTo(...)`）。
- **~~`MagicEffectSpec` script-attach (VMAD)~~** — 用通用機制即可。`Generator.Build.Scripts.cs:AttachScripts()` 是 record-type-agnostic 的:反射任何 record 的 `VirtualMachineAdapter` 掛 `ScriptEntry`+typed property。MGEF 在 Mutagen 有可寫 VMAD，validator 無型別限制 → `scripts[]` 指向 MGEF editorId 今天就能用。**至多是文件缺口**（`MagicEffectSpec` 無專屬 script 欄位，但通用 `scripts[]` 已涵蓋）。
