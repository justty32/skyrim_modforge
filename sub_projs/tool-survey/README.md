# tool-survey — Skyrim 模組工具調查

← [sub_projs/README.md](../README.md)

調查 Skyrim 模組製作生態中的**工具**（非 mod content，而是製作工具、SKSE plugin 框架、編輯器、patcher 等），評估其與 ModForge 的關係與潛在用途。

**性質**：agent 工作區 — 原始 findings 放 `findings/`，Gemini 原始輸出放 `../gemini-research/tool-survey/`，確認過的結論才往上搬（roadmap / WAIT_USER 等）。

Repo 本體 clone 至 `repos/`（gitignored，shallow clone）：

```
repos/
  SkyrimIngameEditor/   (Jonahex)
  TES5Edit/             (TES5Edit team)
  F4RefToBlender/       (6ooflames)
  BodySlide-and-Outfit-Studio/ (ousnius)
  OBody/                (Aietos, 舊版 Papyrus)
  OBody-NG/             (Aietos, 新版 SKSE C++)
```

---

## Findings

| 工具 | 類型 | 狀態 | 摘要 |
|------|------|------|------|
| [skyrim-ingame-editor](findings/skyrim-ingame-editor.md) | SKSE plugin + EspGenerator | ✅ 完整調查 | 遊戲內即時 Weather/Cell/ImageSpace/LGTM 編輯；EspGenerator 已支援 Reference（IPlacedGetter）匯出；**擴展路徑清楚**（見 roadmap generation.md #3） |
| TES5Edit | Delphi GUI 編輯器 | 📄 Gemini raw | xEdit：record 查看/清理/衝突解決/Pascal 腳本；`wbDefinitions.pas` 定義 record binary 佈局 |
| F4RefToBlender | Python Blender 腳本 | 📄 Gemini raw | CK reference data + PyNifly → Blender 3D 場景重建；了解 reference 資料流用 |
| BodySlide-and-Outfit-Studio | C++ wxWidgets GUI | 📄 Gemini raw | NIF 服裝/身體 mesh 編輯器；BodySlide 滑桿自訂 + Batch Build；Outfit Studio mesh 編輯 + skinning |
| OBody-NG | SKSE C++ plugin | 🔍 原始碼讀取 | JSON config 按 NPC FormID/plugin/faction/race 分配 BodySlide preset；ORefit 自動貼合服裝 |
| OBody | Papyrus + ESP | — | 舊版（Papyrus-based）；已被 OBody-NG 取代 |

---

## 調查方法

1. git clone --depth=1 → `repos/`（直接讀原始碼，比 Gemini 準確）
2. Gemini CLI（聯網）補充 README / release 資訊
3. 重點：工具做什麼、架構怎麼運作、與 ModForge 的交集／差異
