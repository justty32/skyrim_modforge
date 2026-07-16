# VIGILANT reconstruction redo

這裡是重新做的 VIGILANT 劇情/演出結構化整理區。

目標不是摘要，而是可追溯的 source-grounded reconstruction：

- 從 `game-data/mods/Vigilant/` 的原文抽取檔與 `Vigilant.esm` CLI 診斷出發。
- 保留 FormID、EditorID、record type、quest objective、topic prompt、INFO response。
- 原文主要用可點擊連結引用，不在整理稿裡整段複製。只有短詞、疑難片語、必要對照才直接摘錄。
- FormID、EditorID、Topic、INFO、BOOK、MISC 等代號也盡量連到抽取檔或本整理稿的對應位置。
- 翻譯時不吞行；必要時標註原文疑難，而不是用漂亮中文蓋掉來源。
- 場景演出要查 `SCEN`：host quest、aliases、phases、actions、timer、dialog topic。
- 不處理 Sofia 擴充台詞；本區只重建 VIGILANT 本體。

## Data Sources

- Extracted text:
  - `../game-data/mods/Vigilant/quests.md`
  - `../game-data/mods/Vigilant/dialogue.md`
  - `../game-data/mods/Vigilant/books.md`
  - `../game-data/mods/Vigilant/items.tsv`
  - `../game-data/mods/Vigilant/locations.tsv`
  - `../game-data/mods/Vigilant/npcs.tsv`
- ESM:
  - `/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`
- CLI:
  - `/home/lorkhan/repo/moddings/skyrim/projects/ModForge/src/ModForge.Cli/bin/Release/net10.0/ModForge.Cli.dll`
- Secondary references:
  - `references/zhihu-vigilant-review-notes.md` - Chinese player analysis/review notes supplied by user; use only as a verification roadmap, not as canonical source.
  - `references/video-transcript-notes.md` - consolidated notes from three user-supplied video transcripts; use only as a verification roadmap, not as canonical source.

## Standard Record Shape

```text
## <FormID> <EditorID> "<Name>"

Source:
- extracted: <path:line when available>
- cli: <command>
- record type: QUST / SCEN / DIAL / INFO / BOOK / ...

Quest:
- flags / priority / type / filter
- stages
- objectives and targets

Scene staging:
- host quest
- aliases
- phases
- actions
- scene-owned topics

Dialogue:
- topic FormID + EditorID + prompt
- INFO FormID + flags + conditions + VMAD if relevant
- original source link
- zh-TW translation

Notes:
- source-grounded only;
- mark inference explicitly;
- no freeform plot summary.
```

## Work Queue

1. `act-4-memory-07-marukh.md` - first vertical slice: quest + dialogue + SCEN staging. **DONE** (format template).
2. `act-4-memory-index.md` - index of all 13 memory quests + hub. **DONE**.
3. `act-4-memory-01..13-*.md` - one source-grounded slice per memory quest. **DONE (all 13, 2026-06-14)**.
4. **Cross-slice Open verification** — **DONE (2026-06-14)**. Resolved via `Vigilant.bsa` plaintext PSC (no pex decompiler needed; CLI has none): branch `choice → SetStage → CompleteQuest`, karma polarity (global `0x020B19F4 zzzCHKarma`), SCEN staging, and the `zzzCHMemoryGuide` hub (`42E0B1`, per-dream `qGuide.SetStage` + TraceON/OFF). Method + cache in index "Status". Residual `(unverified)` are CLI structural limits only (runtime alias fill / objective target refs — need a direct ESM QUST alias+CTDA dump).
5. **Acts 1-3 source-grounded verification** — now unblocked (Act 4 format + PSC-cache method stable). Apply the same workflow to the Act 1-3 slices' Open verification items.
6. **zh-TW re-sync** — the 繁中 mirror under `zh-TW/` diverged when EN slices `02`/`13` + the index were PSC-corrected (2026-06-14). Re-sync those before treating the mirror as current.
