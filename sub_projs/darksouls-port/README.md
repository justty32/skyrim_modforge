# darksouls-port — DS1 地圖移植成 Skyrim world

← [sub_projs](../README.md)

把本機 Dark Souls Remastered（v1.04）的地圖移植成 Skyrim worldspace，用 ModForge spec 管線出 esp。**首個目標：北方不死院（m18_01_00_00）**。

- **規劃**：[plan.md](plan.md)（素材盤點 / 工具鏈 / 技術牆 / P0–P3 分階段）
- **類型**：消費者 + 基石聯動（吃 [model-converter](../model-converter/README.md) 的 glTF↔NIF 能力，反向 glTF→NIF 是它的既定 roadmap 方向）
- **狀態**：🔵 規劃完成，待 P0 spike（單塊 map piece 端到端）
- **IP 鐵律**：移植出的資產**僅本機個人使用、絕不發佈**（Nexus / 任何公開渠道都是紅線）。repo 只 commit 工具與 spec；抽出的 FLVER/DDS/NIF 產物一律 gitignore（見 [.gitignore](.gitignore)）。
