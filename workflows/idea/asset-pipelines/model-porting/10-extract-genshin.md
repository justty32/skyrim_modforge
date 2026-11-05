# 10 — Extracting from Genshin Impact (3DMigoto frame-dump + GIMI)

← [README](README.md) · related: [02-source-mesh-prep.md §5](02-source-mesh-prep.md), [03-materials-textures.md](03-materials-textures.md), [07-skinned-characters.md](07-skinned-characters.md)

Genshin Impact is a **Unity** title, and its asset bundles are **encrypted** — so the clean Unity extractors (AssetRipper, stock AssetStudio) can't read the meshes directly. That makes Genshin the **hardest and least Linux-clean of the three game sources** here: [08] Dark Souls is native/pure-Python (cleanest), [09] WuWa is middle (Windows-side extract, native convert), and Genshin is the wall. The canonical route isn't a file unpacker at all — it's a **3DMigoto frame-dump** of the running game captured with **GIMI** (GI-Model-Importer), then reconstructed in Blender. Given your **dual-boot**, the path of least resistance mirrors [09]: **frame-dump on the Windows side, reconstruct in Blender (native Manjaro), resume the spine.**

> Legal (unchanged): you own the game; converted assets stay **local**, never redistributed. Genshin's bundles are encrypted — extracting your own copy for personal use is the line we stay on.

> **Anti-cheat / ToS warning (read this first).** Genshin runs a **kernel-level anti-cheat** (`mhyprot`) on Windows. Injecting tooling (3DMigoto) into the **live online client** is against HoYoverse's Terms of Service and carries a real **ban risk**. State this plainly to yourself before you start: this is the riskiest source in the set, and the risk is account-level, not just technical.

## 內容

內容按主題分 2 檔放在 [10-extract-genshin/](10-extract-genshin/)，本檔是入口。

| 檔案 | 內容 |
|------|------|
| [route.md](10-extract-genshin/route.md) | §1 為何 Genshin 最難、§2 3DMigoto + GIMI 正規路線、§3 雙系統開機 |
| [traps-handoff.md](10-extract-genshin/traps-handoff.md) | §4 靜態 vs 角色、§5 坑、§6 交回 model-porting 主線、§7 完成定義 |
