# Roadmap — 結構／工具（open 重構項）

← [roadmap](README.md)

只列**還沒做/已決定緩做**的結構整理項。已完成的整批（bytes 門檻改制、docs/ 拆檔、zh-TW 鏡像重對齊、workflows 文檔拆檔、plans/specs 移 archive…）已封存 → [refactor/archive](../refactor/archive/README.md)。拆法原則見 [DEV-GUIDE](../../DEV-GUIDE.md)。

## 緩做（已評估、暫不動）

- **`workflows/idea/ideas.md`（18K）** — 入口主檔，按主題拆**風險高、價值低**，暫不動。日後若它再膨脹或主題自然分群再拆。
- **`investigation/decode/` 是否按 mod 開子夾** — 解碼筆記目前全 KEEP（單篇連貫、不超標）；真議題是 decode/ 要不要按 mod 分子夾，**等下個 mod 解碼進來再決定**。
- **`feature-dev/gotchas.md`（5K）** — 只需檔內分節、不拆資料夾，暫緩。

## 待辦（殘留）

- （無）— `docs/zh-TW/` 鏡像舊路徑同步已完成：zh-TW `.md` 連結皆已指向 `workflows/common/code-map/` 且全數 resolve；`docs/zh-TW/html/for-agent.html` 兩條跨樹連結的 `../` 深度（少一層 `html/`）已補正。
