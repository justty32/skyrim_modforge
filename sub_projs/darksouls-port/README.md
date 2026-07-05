# darksouls-port — DS1 地圖移植成 Skyrim world

← [sub_projs](../README.md)

把本機 Dark Souls Remastered（v1.04）的地圖移植成 Skyrim worldspace，用 ModForge spec 管線出 esp。**首個目標：北方不死院（m18_01_00_00）**。

- **規劃**：[plan.md](plan.md)（素材盤點 / 工具鏈 / 技術牆 / P0–P3 分階段）
- **工具**：[extractor/](extractor/README.md)（DsExtractor — MSB/FLVER/TPF → JSON/glTF/DDS，C# + SoulsFormats）
- **類型**：消費者 + 基石聯動（吃 [model-converter](../model-converter/README.md) 的 glTF↔NIF 能力，反向 glTF→NIF 是它的既定 roadmap 方向）
- **狀態**：🟡 **P0 離線全鏈完成、待實機**（2026-07-05）：extractor（MSB/FLVER/TPF）+ 碰撞（DSR hkx 47/47 解通 → 元件式凸分解 57 hulls）+ gltf2nif（BSTriShape+材質+bhkConvexVerticesShape）→ `p0/ds_port_p0_spec.json` → `DSPortP0.zip` 已交付 `~/skyrim_mods/mine/`。驗收三段（看得到/貼圖對/站得上去）見 [wait_todo/ingame-tests](../../wait_todo/ingame-tests.md)；③ 過＝碰撞路線 A 定案、開 P1
- **IP 鐵律**：移植出的資產**僅本機個人使用、絕不發佈**（Nexus / 任何公開渠道都是紅線）。repo 只 commit 工具與 spec；抽出的 FLVER/DDS/NIF 產物一律 gitignore（見 [.gitignore](.gitignore)）。
