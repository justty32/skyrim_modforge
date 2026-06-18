# 對 ModForge 的評估

← [sound-record-distributor](sound-record-distributor.md)

## 五、對 ModForge 的評估

### 現有 ModForge 音效支援（確認自 src/）

ModForge 已能在 esp 內設定：

| 類型 | 支援狀態 |
| --- | --- |
| Sound Descriptor（SNDR）建立 | ✅ `BuildSounds()` + `WireSounds()` |
| SNDR Category + OutputModel | ✅ 有預設值 |
| Weapon PickUp / PutDown | ✅ `WireSounds()` |
| MiscItem PickUp / PutDown | ✅ `WireSounds()` |
| Activator ActivationSound / LoopingSound | ✅ `WireSounds()` |
| Music Track（MUST）+ Music Type（MUSC） | ✅ 獨立建構路徑 |
| NPC 語音（Voice） | ✅ 完整 voice gen pipeline |

ModForge **尚未**在 esp 內設定的音效欄位（推斷，未驗 spec schema）：

| 類型 | 備注 |
| --- | --- |
| Weapon Attack / Equip / ImpactDataSet 等完整欄位 | WireSounds 只寫了 PickUp/PutDown |
| ArmorAddon Footstep Set | 可能需要 |
| MGEF 六個音效 slot | MagicEffect 音效未見 wire |
| PROJ / EXPL / EFSH / ALCH 音效 | 可能走 vanilla template copy |
| Region 音效（RDSA） | 未見 |

### esp-side 設音效 vs SRD 分發：取捨

**直接在 esp 設定（ModForge 現行做法）**：
- 優點：自含 mod、無額外依賴（不需 SRD SKSE plugin）、load order 清晰
- 優點：自建 NPC 的 ARMA footstep、自建 WEAP 的 attack sound 直接寫 record 最簡單
- 缺點：若需要「讓外部 mod 的武器也有我的音效」就必須做 patch（衝突）

**SRD 分發**：
- 優點：不寫 esp patch、無 load order 衝突、音效 mod 對玩家 mod 友善
- 適合場景：你是「音效作者」，想讓你的 SNDR 替換外部 mod（AOS、ISC 等）的武器音效
- **不適合**：ModForge 自建 NPC/武器的情況——自建 form 直接在 esp 設音效更直接

### 結論

**ModForge 自建 NPC / WEAP / ARMO 時，優先在 esp 內設定音效**（現行做法正確）。SRD 的主要價值是「audio mod 對別人 mod 的無 patch 覆蓋」，不在 ModForge 的核心生成路徑上。

有一個潛在擴充方向：若 ModForge 日後支援「音效 patch config 生成」（讓玩家的 NPC/武器吃上 ISC 等音效而無 patch），可輸出 `_SRD.yaml` 作為配套產物。這是 **roadmap 等級的 feature**，目前不是缺口。

### 依賴注意事項

- SRD 本身：需要 SKSE（已是 ModForge mod 的基礎前置）
- EditorID lookup（Region/SNDR 以外的 form）：額外需要 **po3 Tweaks**（po3's Tweaks）
- 如果只引用 FormID（`Plugin.esp|0xID` 格式）則不需 po3 Tweaks

---

