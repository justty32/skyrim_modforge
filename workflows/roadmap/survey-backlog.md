# Roadmap — 通用框架/庫 survey backlog

← [roadmap](README.md)

> 「Skyrim 通用底座」這一層的調查缺口盤點（2026-06-15；觸發：盤 mod-survey 的框架/庫掌握度）。產出格式同既有 survey：`findings/<mod>.md` + 更新 [mod-survey/index.md](../../../../analysis/mod-survey/index.md)（框架型分類）。**survey 對「ModForge 缺什麼」一律標推斷、待 code 驗證**（同 index 末的 ⚠ 鐵律；那條規則不綁行號，index 改版也不會失效）。

> ✅ **2026-06-15 大批完成**：A 組（SkyPatcher / KID / FLM / SPID 深挖 / po3's Tweaks / MCM Helper / SRD）、B 組全部（PapyrusUtil / JContainers / BOS / AOS / CE / IWH+ITH）、C 組全部（SM 子系統 / PERK entry-point / MGEF VMAD / FLST 工廠 / Global-as-selector + linkedRef）已 survey；findings 已寫入 [mod-survey/index.md](../../../../analysis/mod-survey/index.md)。下方只列仍 open 的項目。

## 待調查

**全部需主力機**（要實際 mod 檔 + 能探 esp/讀 config）。

1. **Address Library for SKSE** — 幾乎所有 SKSE plugin 的硬依賴（跨版本記憶體定位）。純前置，但要弄懂「為何一堆 mod 列它當依賴」。本機有解壓：`~/skyrim_mods/unzip/AddressLibrary/`。
2. **needs/survival 框架**（Frostfall / Survival Mode 的需求系統本體）— Conditional Expressions finding 只提到 Frostfall effect，沒拆需求框架本身。本機無 Frostfall 檔，待取得後調查。
3. **DynDOLOD / xLODGen** — LOD 生成。歸 index 的「美術型尚未調查」，列此備忘、實際併美術型批次做。
4. ~~DAR（Dynamic Animation Replacer）~~ — **明確不做**（OAR 後繼已取代；action-system 已間接涵蓋舊格式）。
