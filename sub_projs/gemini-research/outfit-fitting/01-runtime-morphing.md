# Gemini Raw: Outfit Fitting Across Body Shapes (2024–2025 meta)

Query: modern solutions for outfit/clothing fitting across different NPC body shapes without manual BodySlide batch build.

---

In the modern Skyrim modding landscape (2024–2025), the focus has shifted from static "Batch Building" every outfit to **Runtime Morphing**, which allows every NPC to have a unique body shape while their clothes dynamically conform without clipping.

### 1. SKSE/Runtime Morphing Solutions

The primary way to achieve auto-morphing at runtime is through SKSE plugins that utilize the **ORefit algorithm** and `.tri` (morph) files.

*   **OBody NG (Next Generation)** - [Nexus 77016](https://www.nexusmods.com/skyrimspecialedition/mods/77016) | [GitHub: Aietos/OBody-NG](https://github.com/Aietos/OBody-NG) ✅ GitHub 驗證通過
    *   **How it works:** Intercepts game's character loading, applies RaceMenu morphs (`.tri` files) in real-time. Assign different BodySlide presets to every NPC via in-game menu.
*   **AutoBody AE** - [Nexus 61321](https://www.nexusmods.com/skyrimspecialedition/mods/61321) | [GitHub: RocketBun-OG/autoBodyAE](https://github.com/RocketBun-OG/autoBodyAE) ✅ GitHub 驗證通過
    *   **How it works:** "Set-and-forget" alternative. Uses `morphs.ini` to randomize/distribute body shapes by race/faction/location. ORefit algorithm, first for AE.

### 2. Technical Core: "Zeroed Sliders" & ORefit

1. Build body + all outfits in BodySlide with **Zeroed Sliders** preset (all 0) → neutral mesh
2. **Must check "Build Morphs"** → generates `.tri` files (morph deltas)
3. **ORefit algorithm** (built into OBody/AutoBody) → applies extra offsets ("Push-up"/"Compression") when outfit detected → realistic fabric interaction

### 3. CBBE 3BA vs. Community Overlays

| Feature | CBBE 3BA (Mesh-Based) | Community Overlays (Texture-Based) |
|---|---|---|
| Tech | 3D Geometry + Bones | RaceMenu Overlays (Skin Textures) |
| Fitting | Requires BodySlide + .tri | **Automatic** (follows UV map) |
| Clipping | High risk if morphs missing | **Impossible** (part of skin) |
| Best For | Armors, loose clothes, physics | Stockings, tattoos, body paint, tight suits |

Community Overlays [Nexus 22487](https://www.nexusmods.com/skyrimspecialedition/mods/22487) — zero mesh editing, "paints" clothing onto body shape.

### 4. Auto-Conversion Tools (Cross-Body)

*   **Outfit Studio "Conversion Reference"** (built into BodySlide [Nexus 201](https://www.nexusmods.com/skyrimspecialedition/mods/201)):
    *   `File → New Project → From Template` → select `CBBE to 3BA` etc.
    *   Vertex-proximity algorithm "Conforms" old mesh to new shape.
*   **AutoSlide (Python)** - [GitHub Gist: pl77/autoslide.py](https://gist.github.com/pl77/e03d58d2b25a3c3a5034684c7006c81b)
    *   Automates Outfit Studio GUI to batch-convert hundreds of outfits between body types.
    *   ⚠️ Gist URL 待驗
*   **nifly / NiflySharp** - [GitHub: ousnius/nifly](https://github.com/ousnius/nifly) ✅ GitHub 驗證通過
    *   Underlying C++ library for programmatic NIF vertex/bone weight editing.

### Modern Stack Summary

1. **Framework:** RaceMenu [Nexus 19080](https://www.nexusmods.com/skyrimspecialedition/mods/19080) + JContainers [Nexus 16495](https://www.nexusmods.com/skyrimspecialedition/mods/16495)
2. **Physics:** Faster HDT-SMP [Nexus 57339](https://www.nexusmods.com/skyrimspecialedition/mods/57339) + CBPC [Nexus 21224](https://www.nexusmods.com/skyrimspecialedition/mods/21224)
3. **Morphing:** OBody NG (manual) or AutoBody AE (randomized)
4. **Body:** CBBE 3BA [Nexus 30174](https://www.nexusmods.com/skyrimspecialedition/mods/30174)

---

**URL 驗證狀態（2026-06-15）**：
- ✅ GitHub: Aietos/OBody-NG, RocketBun-OG/autoBodyAE, ousnius/nifly — 全部真實
- ⚠️ GitHub Gist (pl77/autoslide.py)：Gist URL 需人工確認
- ⚠️ Nexus mod ID（curl 403 = 正常，需登入後在瀏覽器確認）
