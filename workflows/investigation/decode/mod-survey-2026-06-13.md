# 下載 mod 盤點 — ModForge 解碼參考價值（2026-06-13）

盤點 `~/Downloads` 與 `~/skyrim_mods` 的 Nexus 下載,從**「能否當 ModForge 的 known-good 解碼樣本／技術借鏡」**角度分級。大多數是 SKSE DLL / UI / 引擎修正(實際遊玩配置,無解碼價值);真正有 ESP 內容可參考的只有幾個。

**解碼方法（記憶體安全鐵律）**:Linux 限制單 process 記憶體、超限會被終止 → **只 `unrar e`/直接讀單顆小 esp/esm,不解 BSA、不載 Skyrim.esm(250MB)**;Mutagen `CreateFromBinaryOverlay`(lazy)讀小檔安全。21MB 的 Vigilant.esm overlay 可、但 group 用 `.Take`/summarize 勿全 materialize。

## Tier 1 — 高解碼價值(豐富 ESP/ESM,直接對應 roadmap)

| Mod | 內容 | 對 ModForge 的價值 | 解碼狀態 |
|-----|------|------------------|---------|
| **VIGILANT** | `Vigilant.esm` 21MB + 1.6G BSA | quest/scene/自訂 worldspace 巨型 mod。已解碼 worldspace/story/magic/scene-dialogue 四份 | ✅ 已解碼(見下參考檔)|
| **Sofia Follower** | `SofiaFollower.esp` 635KB + BSA | 全語音隨從框架(30 quest/28 scene/1135 INFO/57 GLOB)。隨從擴充最佳模板 | ✅ 已解碼 + 內容索引 |
| **AI Overhaul** | `AI Overhaul.esp` 2.2MB | 424 NPC override + 744 package = NPC 日程堆疊。對應 vanilla NPC AI patch | ✅ 已解碼 |
| **RDO**(Relationship Dialogue Overhaul) | `Overhaul.esp` + BSA | 巨量 dialogue/INFO 重排。對應 dialogue conditions、shared/faction dialogue、voice-type targeting | ⬜ 未解碼(候選)|
| **Moons And Stars** | `MoonsAndStars.esp` + po3_MoonMod.dll | 天空/天氣/imagespace/climate overhaul。對應「weather/IMGS 掛 region 未做」 | ⬜ 未解碼(候選)|
| **Alternate Start** | `life.esp` + BSA | quest start、Tamriel worldspace override(本 session 用過當持久 cell 參考)| ◻ 部分(map-fix 用過)|

## Tier 2 — 技術/設定思路參考(多為 SKSE config 分發,非 ESP 記錄)

- **SPID / KID / FLM / Sound Record Distributor / Base Object Swapper** — 「不改 ESP、用 config 把 spell/keyword/sound/object 分發到大量 vanilla 記錄」框架。ModForge 是反向(生 ESP),但若日後想做「大規模套用/分發」,這些 distribution 設計值得借鏡。
- **Use Or Take / Dynamic Things Alternative** — 活化/取物行為(po3 Papyrus + BOS config)。

## Tier 3 — 無解碼價值(你的實際遊玩配置)

SkyUI / SkyHUD / TrueHUD / moreHUD / RaceMenu / MCM Helper+Unlocked / Bethini、Engine Fixes / CrashLogger / Scrambled Bugs / Bug Fixes / Actor Limit Fix、PapyrusUtil / JContainers / po3 Tweaks+Extender / Papyrus Tweaks+Ini、ConsoleUtil / Base Object Swapper / AnimObject Swapper、SmoothCam / Display Tweaks / FPS Stabilizer / Better Jumping、Particle Patch(235M 純 mesh)/ Assorted Mesh Fixes、中文翻譯…——全是 SKSE 庫/UI/引擎修正/貼圖/翻譯,沒有可供 ModForge 學的 ESP 記錄結構。

**例外**:**USSEP**(Unofficial Patch)雖是巨型修正,但本 session 已用它當 **Tamriel 持久 cell(0xD74）的 known-good 比對參考**(解出 worldspace override 必帶 TopCell + 0x00040400 記錄旗標,見 [[worldspace-override-must-carry-topcell]] / quest-markers 地圖修復)。

## 本 session 產出的解碼/計畫/可行性參考檔（完整清單）

- 隨從:`../sofia-patch/`（獨立消費者專案，`README.md` 索引）→ `reference/follower-decode-2026-06-13.md`、`plans/expansion-plan-2026-06-13.md`、`reference/sofia-personality.md`
- NPC 日程:`ai-overhaul-decode-2026-06-13.md`、`ai-overhaul-expansion-plan-2026-06-13.md`
- VIGILANT:`vigilant-worldspace-decode`、`vigilant-story-decode`、`vigilant-magic-decode`、`vigilant-scene-dialogue-audit`（皆 `-2026-06-13.md`）
- 工作流:`blender-layout-feasibility-2026-06-13.md`、本檔
- 待解碼候選:RDO、Moons And Stars（見 Tier 1）

跨 mod 浮現的「ModForge 待補」清單見 `CLAUDE.md`「之後可做」。
