# Gemini Raw: Universal NPC Beautification (2024–2025 meta)

Query: Skyrim SE/AE universal NPC overhaul mods that automatically cover ALL NPCs including mod-added ones, technical mechanisms.

---

In 2024–2025, the Skyrim modding scene has shifted from "static" replacers (which require a patch for every single mod) to **dynamic frameworks** and **automated patchers**. These allow you to beautify every NPC in your load order—including those added by DLC-sized quest mods—without manual intervention.

### 1. The "Big Three" Universal Base Mods
If you want a baseline that covers everyone (including many mod-added NPCs), these are the gold standards. They provide the "FaceGen" (the 3D mesh and texture for the head) that makes NPCs look modern.

*   **[Modpocalypse NPCs](https://www.nexusmods.com/skyrimspecialedition/mods/54422)**:
    *   **Coverage**: Over 3,000 NPCs. Most comprehensive "catch-all" mod available.
    *   **Mod-Added NPCs**: Author has patches for almost every major mod (Interesting NPCs, Wyrmstooth, LotD, etc.).
    *   **Look**: High Poly Heads + KS Hairdos.
*   **[Nordic Faces](https://www.nexusmods.com/skyrimspecialedition/mods/40658)**:
    *   **Coverage**: All vanilla NPCs and many mod-added ones via "Vanilla+" approach.
    *   **Technical**: High-res skins + optimized vanilla meshes. Stable for massive load orders.
*   **[NVICO](https://www.nexusmods.com/skyrimspecialedition/mods/48821)**:
    *   **Coverage**: Comprehensive vanilla. High-poly meshes + realistic skin.

### 2. Dynamic Frameworks (2025 Meta) ← 核心

*   **[SkyPatcher](https://www.nexusmods.com/skyrimspecialedition/mods/106659)**:
    *   **Mechanism**: SKSE plugin. Patches NPC records *in memory* at game start. Can swap hair/eyes/skin for ALL NPCs (including mod-added) via config files. 100% conflict-free (doesn't touch .esp).
    *   ⚠️ ID 修正（2026-06-15 Gemini 驗證）：原始 ID 106614 實為「The New Dragon」mod；SkyPatcher 真實 ID = **106659**。⚠️ 待人工瀏覽器驗證。
*   ~~**FaceGen Unbound (TofteModding/FaceGen-Unbound)**~~: ❌ **HALLUCINATED — GitHub 404 + Gemini 雙重確認。**
    *   **真實對應工具**：**[Face Discoloration Fix](https://www.nexusmods.com/skyrimspecialedition/mods/42441)**（Exit-9B，Nexus 42441，[GitHub: Exit-9B/Face-Discoloration-Fix](https://github.com/Exit-9B/Face-Discoloration-Fix)）
    *   **機制**：Hook `BSFaceGenManager::PrepareFaceGen`（Address Library ID 26143/SE + 26684/AE），偵測缺少 FaceGen 資料時呼叫 `GenerateFaceGen` 在記憶體即時生成頭部 mesh + tint 貼圖。自動覆蓋所有 NPC（含 mod-added）。業界標準暗臉修正工具。
    *   ⚠️ Nexus 42441 / GitHub Exit-9B/Face-Discoloration-Fix — 待人工瀏覽器驗證。
*   **[SPID (Spell Perk Item Distributor)](https://www.nexusmods.com/skyrimspecialedition/mods/36869)**:
    *   **Mechanism**: Injects hair/skin onto NPCs globally via Keywords/Factions. Hits every mod-added NPC automatically.

### 3. Body: Universal BodySlide

*   **[OBody Next](https://www.nexusmods.com/skyrimspecialedition/mods/77016)** / **[AutoBody](https://www.nexusmods.com/skyrimspecialedition/mods/61321)**:
    *   **Mechanism**: SKSE batch BodySlide preset assignment. Randomly assigns preset to every NPC (vanilla or mod-added) at spawn.

### 4. Orchestrators (Automated Patching)

*   **[EasyNPC](https://www.nexusmods.com/skyrimspecialedition/mods/52313)**: Standalone app. Scans load order, merges per-character mods into one plugin, fixes dark face bug.
*   **[NPC Plugin Chooser 2](https://www.nexusmods.com/skyrimspecialedition/mods/139598)**: EasyNPC 的新世代繼承者（Gemini 報告，⚠️ 待人工驗證）。
*   **[Synthesis Face Fixer / Facefixer](https://github.com/mutagen-modding/Synthesis)**: Synthesis 框架內的 Facefixer patcher，掃 load order 把 NPC overhaul 的視覺 records 推到末尾避免被覆蓋。

### 2025 Universal Stack Summary
1. Skin/Body: BnP Skin + OBody Next
2. Base: Modpocalypse NPCs (+ patches)
3. Hero layer: Dibella's Blessing (F) + Sons of Nirn (M)
4. Glue: EasyNPC merge
5. Fail-safe: Face Discoloration Fix (SKSE)

---

---

**URL 驗證狀態（2026-06-15 Gemini 二次核查）**：
- ✅ FaceGen Unbound：❌ HALLUCINATED（Gemini 雙重確認）
- ✅ Face Discoloration Fix (42441, Exit-9B)：Gemini 確認真實存在 ⚠️ 待人工瀏覽器驗證
- ⚠️ SkyPatcher：真實，但 ID 應為 **106659**（非 106614）⚠️ 待人工驗證
- ⚠️ NPC Plugin Chooser 2 (139598)：Gemini 報告 ⚠️ 待人工驗證
- ⚠️ 其餘 Nexus mod ID 需人工確認（curl 403 = 正常）
