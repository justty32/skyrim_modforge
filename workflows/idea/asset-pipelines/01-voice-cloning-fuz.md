# Voice-Cloning → Skyrim SE `.fuz` Voiced-Dialogue Pipeline

← index: [README.md](README.md) · **deep-dive 子工作流: [voice-clone/](voice-clone/README.md)**（逐步施工計畫）· related: [IDEAS.md §1](../ideas.md) (voice prereq), memory `voice-gen-interface-future`

**Research date:** 2026-06-08. Target: Skyrim SE on Manjaro Linux, MO2 under Proton, CUDA GPU available, Wine/Proton usable, **personal single-player use only**.

**Confidence note:** The file-format facts (§1, §5, §6) and the FaceFXWrapper / xVASynth tool facts (§3, §4) are well-corroborated; the `.fuz` layout is confirmed from source code. The biggest *uncertainty* is **Linux-native lip generation** — FaceFXWrapper is Windows-only and the `.lip` step is the one place the pipeline hits a wall (§4). Extrapolations are flagged inline.

**Status update 2026-06-12:** the core ModForge side has landed structurally. Current code has
`voiceTemplates[]`, `npcs[].voiceTemplate`, `voiceLine`, `voicediag`, `voicelines --plan`, a local
TTS wrapper contract, Wine `xWMAEncode.exe` path conversion, and a native `.fuz` writer. Fake TTS +
real xWMAEncode produced valid `.fuz` files locally. Real model setup, lip generation, QA, and
Skyrim/Proton in-game playback remain open.

## 內容

內容按主題分 3 檔放在 [01-voice-cloning-fuz/](01-voice-cloning-fuz/)，本檔是入口。

| 檔案 | 內容 |
|------|------|
| [format-and-engines.md](01-voice-cloning-fuz/format-and-engines.md) | §1 `.fuz` 格式與每句需求、§2 訓練素材擷取、§3 語音克隆／TTS 引擎（2026 現況） |
| [lip-xwm-pipeline.md](01-voice-cloning-fuz/lip-xwm-pipeline.md) | §4 `.lip` 嘴型牆、§5 `.xwm` 編碼、§6 組 `.fuz`、§7 端到端建議管線 |
| [integration-risks.md](01-voice-cloning-fuz/integration-risks.md) | §8 ModForge 整合點、§9 風險／坑／MVP 與來源 |
