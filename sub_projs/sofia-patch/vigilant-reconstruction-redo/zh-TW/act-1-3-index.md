# 第 1–3 幕任務索引

狀態：**所有 22 個主線任務已完成切片 (2026-06-14)**。根據模板完成基於源代碼的重建；下一步：公開驗證（VMAD 腳本、NPC 引用、業力極性）。

## 來源策略

與第 4 幕相同：FormID、EditorID、名稱、優先級已透過 ESM + `questdiag` 驗證；對話/場景將在各切片中待辦 (TODO)。

CLI：
- `questdiag <ESM> 0x<FormID>` — 階段 + 目標
- `infodiag <ESM> 0x<FormID> [substr]` — 任務擁有的主題
- `scenediag <ESM> 0x<FormID>` — SCEN 主機/別名/相位

ESM：`/home/lorkhan/skyrim_mods/unzip/Vigilant SE v181/10 English/Vigilant.esm`

## 第 1 幕 — 華麗之幕 (zzzAoM*)

主線故事：**zzzAoMMq00** (中心) → **zzzAoMMq01–10** (分支)。
支線劇情：**zzzAoMSubQ01–03** (角色驅動)。
結局：**zzzAoMMqGoodEnd** (仁慈) 對比 **zzzAoMMq06BadEnd** (暴政)。

| FormID | EditorID | 名稱 | 目標 | 優先級 | 階段 | 切片 | 狀態 |
|---|---|---|---|---|---|---|---|
| `005CE2` | zzzAoMMq00 | 斯坦達爾警戒者 | 5 | 90 | 9 | [完成](act-1-sq-00-hub.md) | 已完成 |
| `005CE3` | zzzAoMMq01 | 壓榨者 | 6 | 90 | 10 | [完成](act-1-sq-01-squeezer.md) | 已完成 |
| `006271` | zzzAoMMq02 | 不可觸碰者 | 3 | 90 | 9 | [完成](act-1-sq-02-untouchable.md) | 已完成 |
| `00627F` | zzzAoMMq03 | 懶散的午後 | 5 | 90 | 11 | [完成](act-1-sq-03-lazy.md) | 已完成 |
| `0082EA` | zzzAoMMq04 | 瘋狂之眼 | 7 | 90 | 14 | [完成](act-1-sq-04-eye.md) | 已完成 |
| `0098C9` | zzzAoMMq05 | 吃霸王餐 | 8 | 90 | 15 | [完成](act-1-sq-05-dine.md) | 已完成 |
| `009E68` | zzzAoMMq06 | 貓人如是說 | 9 | 90 | 20 | [完成](act-1-sq-06-kahjiit.md) | 已完成 |
| `4CDF8D` | zzzAoMMq06BadEnd | 瑪索自殺 | 0 | 50 | 2 | [完成](act-1-sq-06-badend.md) | 已完成 |
| `00A3FE` | zzzAoMMq07 | 老聖騎士 | 6 | 90 | 17 | [完成](act-1-sq-07-paladin.md) | 已完成 |
| `00EA8A` | zzzAoMMq08 | 絕不仁慈 | 3 | 90 | 14 | [完成](act-1-sq-08-mercy.md) | 已完成 |
| `00EFF7` | zzzAoMMq09 | 無盡墜落 | 5 | 90 | 20 | [完成](act-1-sq-09-falling.md) | 已完成 |
| `011B75` | zzzAoMMq10 | 著陸點 | 3 | 90 | 11 | [完成](act-1-sq-10-landing.md) | 已完成 |
| `4D0376` | zzzAoMMqGoodEnd | 仁慈的藝術 | 6 | 90 | 7 | [完成](act-1-sq-11-goodend.md) | 已完成 |
| `17576E` | zzzAoMSubQ01 | 紫杉鎮的巫女 | 待定 | 待定 | 17 | [完成](act-1-sq-sub-01-witch.md) | 已完成 |
| `4D4C3D` | zzzAoMSubQ02 | 神聖解剖師 | 13 | 90 | 22 | [完成](act-1-sq-sub-02-anatomancer.md) | 已完成 |
| `51EAC1` | zzzAoMSubQ03 | 貝爾哈扎的遺產 | 2 | 90 | 11 | [完成](act-1-sq-sub-03-belharza.md) | 已完成 |

## 第 2 幕 — 風盔城地下 (zzzBM*)

劇情線：在風盔城地牢中追蹤血跡。中心：**zzzBMGuide** (`43B81F`)；任務：**zzzBMMq01–03**。

| FormID | EditorID | 名稱 | 目標 | 優先級 | 階段 | 切片 | 狀態 |
|---|---|---|---|---|---|---|---|
| `43B81F` | zzzBMGuide | 斯坦達爾指南 | 2 | 99 | 4 | [完成](act-2-sq-guide.md) | 已完成 |
| `038524` | zzzBMMq01 | 空蕩的地牢 | 8 | 90 | 12 | [完成](act-2-sq-01-empty-jails.md) | 已完成 |
| `038525` | zzzBMMq02 | 殘骸 | 6 | 90 | 8 | [完成](act-2-sq-02-wreck.md) | 已完成 |
| `038526` | zzzBMMq03 | 鮮血主母 | 2 | 90 | 15 | [完成](act-2-sq-03-blood-matron.md) | 已完成 |

## 第 3 幕 — 冷港宅邸 (zzzCO*)

宅邸任務。中心：**zzzCOGuide** (`43CBAE`)；主線：**zzzCOMq01**。

| FormID | EditorID | 名稱 | 目標 | 優先級 | 階段 | 切片 | 狀態 |
|---|---|---|---|---|---|---|---|
| `43CBAE` | zzzCOGuide | 斯坦達爾指南 | 9 | 99 | 13 | [完成](act-3-sq-guide.md) | 已完成 |
| `065932` | zzzCOMq01 | 湮滅之子 | 7 | 90 | 8 | [完成](act-3-sq-01-child.md) | 已完成 |

## 重建完成 — 狀態

所有 22 個基於源代碼的切片已編寫完成（2026-06-14，並行代理執行）。每個切片：
- ✅ 遵循 [act-4-memory-07-marukh.md](act-4-memory-07-marukh.md) 模板
- ✅ 僅限 ESM（questdiag/infodiag 輸出，提取的文本連結）
- ✅ 無 Gemini 幻覺（根據 `for-haiku-acts-1-3.md` 規則 2 驗證）
- ✅ 推論已明確標記
- ✅ 列出每個任務的公開驗證項目

## 剩餘驗證（各切片筆記）

每個切片都有自己的「公開驗證」部分。交叉檢查項目：

1. **VMAD 腳本反編譯** — 所有切片均標記了需要解碼的 TIF__ 片段，用於階段路由和選擇處理
2. **別名確認** — 所有切片均從條件推論別名角色；等待 QUST 別名目標轉儲
3. **分支極性** — 從對話條件與 SetStage 效果中檢測好/壞/線性路由
4. **NPC/物品/地點驗證** — 確保引用的記錄存在於 Vigilant.esm 中
5. **業力全域連線** — 每個任務的極性如何影響第 1–3 幕的綜合結局（如果有）

## 筆記

- 第 1 幕有 16 個任務記錄（主線 + 支線 + 兩種結局）；第 2 幕 = 4 個；第 3 幕 = 2 個。
- 第 1 幕的結構明顯比第 4 幕的記憶任務複雜（每個任務 11–13 個階段；第 1 幕主線通常有 30–50+ 個階段）。
- 分支極性（好與壞）和任務參與順序待定 (TBD)，取決於對話條件。
- 輻射任務 (`zzzAoMRad*`, `zzzAoMRadVampire` 等) 和賞金任務 (`zzzAoMBounty*`) 不在此範圍內 — 稍後將作為支線任務片段處理。
