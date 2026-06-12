# 語音克隆 → `.fuz` — 詳盡實作計劃

← 上層 landscape 調研：[../01-voice-cloning-fuz.md](../01-voice-cloning-fuz.md) · 資料夾索引：[../README.md](../README.md)

**本資料夾原本是「實作」計劃**（上層那份是「landscape 調研」）。
截至 2026-06-12，核心 ModForge `voicelines` 路徑已存在：它能檢查 built INFO、規劃
speaker/template/output path、shell out 到本機 TTS wrapper、用 Wine `xWMAEncode.exe` 編碼，並寫出
`.fuz` loose assets。剩餘工作是真模型設定、lip tooling、品質 QA、Skyrim/Proton 實機確認。

**計劃日期：** 2026-06-09。作者目標機器：**Manjaro Linux、16 GB VRAM NVIDIA 顯卡、CUDA、Wine/Proton 可用。** 個人單人遊玩用途；產出的語音資產一律不發布。

---

## 已鎖定的決策（2026-06-09 Q&A）

| 主題 | 決策 | 對本計劃的影響 |
|-------|----------|---------------------------|
| **主 TTS 引擎** | **分層** —— MVP 用零訓練的 **F5-TTS** 或 **Chatterbox**，**GPT-SoVITS** 當保真度／一致性升級。 | 各引擎都藏在**同一個可切換的契約**後面（`text + reference → wav`）。先從免訓練的零訓練起手；之後再為需要嚴格音色一致性的 NPC 加上 GPT-SoVITS 微調軌。見 [01-engine-setup.md](01-engine-setup.md)。 |
| **嘴型同步深度** | **「嘴型隨便亂動就好」** —— 不要求準確度，但嘴要*會動*（不要凍住）。 | 跳過準確度之爭。**Tier 1：FaceFXWrapper/Runalip 走 Wine**（若 Wine 配合，等於免費拿到正確嘴動）。**Tier 2 後備：原生 C# 寫的合成 envelope-driven `.lip`**（保證 Linux-native 的嘴動）。**Tier 0 基準：無 lip = 靜止嘴**（永遠可行）。見 [03-lip-and-audio-encoding.md](03-lip-and-audio-encoding.md)。 |
| **本計劃範圍** | **兩者** —— 先做獨立手跑 pipeline，再做 ModForge `voicelines` CLI step。 | [06-standalone-runbook.md](06-standalone-runbook.md) 是可複製貼上的回家施工手冊；[05-modforge-integration.md](05-modforge-integration.md) 是折進產生器的工程設計。 |

---

## 文件索引

| 檔案 | 涵蓋 | 何時需要 |
|------|----------------|------------------|
| [01-engine-setup.md](01-engine-setup.md) | Manjaro CUDA 前置；安裝 F5-TTS / Chatterbox / GPT-SoVITS；可切換引擎契約；VRAM 預算；調校旋鈕；決定性。 | 回家第一件事 —— 讓某個引擎能從文字產出克隆 WAV。 |
| [02-voice-data.md](02-voice-data.md) | 在 Linux 抽取 vanilla/follower voiceType 音檔；建立 reference clip（零訓練）vs 微調資料集（GPT-SoVITS）；正規化規格。 | 裝完引擎之後 —— 沒有參考聲音就無法克隆。 |
| [03-lip-and-audio-encoding.md](03-lip-and-audio-encoding.md) | 分層 `.lip` 計劃（無／Wine／合成 C#）；`.lip` 格式筆記與解碼計劃；`.xwm` 編碼；音訊正規化。 | 想讓嘴會動，且／或想要真 `.fuz`（而非鬆散 WAV）時。 |
| [04-fuz-and-filenames.md](04-fuz-and-filenames.md) | 原生 C# `.fuz` writer（byte layout + 草稿）；決定性的 CK-相符檔名規則與經驗驗證法；on-disk 路徑；MO2 zip 打包。 | 從「鬆散 WAV」進到打包 `.fuz` 時，以及任何檔名重要時（永遠）。 |
| [05-modforge-integration.md](05-modforge-integration.md) | Spec 設計（`voiceTemplate`、`NpcSpec.voiceTemplate`、`voiceLine`）；`voicelines` CLI step；`Generator.Build.Voice.cs`；shell-out + Wine plumbing；env vars；CODE_MAP/SPEC 落點。 | 歷史設計 + cross-check；實作現在已在 repo 裡。 |
| [06-standalone-runbook.md](06-standalone-runbook.md) | 確切的回家 MVP：複製貼上指令、一個 NPC / `MaleNord` / 3 句、端到端、驗證、再漸進強化。 | **第一天從這裡開始。** 需要時它會回連 01–04。 |

---

## 脊椎（每個 tier 共用）

```
ModForge emit INFO 對話行（文字 + voiceType）
        │
        ▼
[02] 參考聲音  ──►  [01] TTS 引擎  ──►  line.wav   （克隆，每行一個）
                                                  │
                                                  ├─ [03] 正規化（mono/16-bit；lip 用 16 kHz 副本）
                                                  ├─ [03]（選用）.lip  ── tier 0/1/2
                                                  ├─ [03]（選用）.wav → .xwm  （xWMAEncode/Wine）
                                                  └─ [04] 打包 → .fuz  （原生 C#）  或 直接出鬆散 .wav
                                                            │
                                                            ▼
                                  Data/Sound/Voice/<Plugin>/<VoiceType>/<CK-name>.fuz
                                                            │
                                          [04] 收進扁平 MO2 zip  （[05] package step）
```

**ModForge 的獨家超能力：** 因為 ModForge *指派* QUST EditorID、INFO FormID、response index，它能**不靠 Creation Kit 就決定性算出確切的 CK-相符目標檔名**。這正是社群手動工作流最難的一步，這裡卻是免費的。其餘全是繞著它的 plumbing。（見 [04](04-fuz-and-filenames.md)。）

---

## 建構順序（最小可證明切片優先）

每一步只證明一件新事、可獨立測試。前一個未在遊戲內確認前，不要加下一個 tier。

1. **鬆散 WAV，無 lip、無 xwm、無 fuz。** 一個 NPC、`MaleNord`、3 句短的。把純 WAV 丟到確切 CK-風格路徑。證明**檔名映射 + 克隆品質 + 打包** —— 整條脊椎 —— **零 Wine 依賴**。（[06](06-standalone-runbook.md)）
2. **打包 `.fuz`（zero-lip）。** WAV → 原生 C# 寫的 `FUZE` + version + `0x00000000` + xwm/wav。證明 C# fuz writer。（[04](04-fuz-and-filenames.md)）
3. **加 `.xwm`。** 走 Wine 用 `xWMAEncode.exe` 編碼。證明 Wine 音訊路徑並縮小檔案。（[03](03-lip-and-audio-encoding.md)）
4. **加 lip（嘴動）。** Tier 1 走 Wine 的 FaceFXWrapper/Runalip；若 Wine 失敗，Tier 2 合成 envelope `.lip`。（[03](03-lip-and-audio-encoding.md)）
5. **GPT-SoVITS 保真度軌。** 任何零訓練克隆跨多行會漂移的 NPC，微調後切換該語音的引擎。（[01](01-engine-setup.md)、[02](02-voice-data.md)）
6. **ModForge `voicelines` CLI step。** 已結構性實作。先用 `voicediag` / `voicelines --plan`
   檢查，再實際生成。剩餘工作：真 TTS 模型安裝 + 實機播放確認。（[05](05-modforge-integration.md)）

---

## 風險、未決問題、回家需實測的點

這些是計劃在 Windows 公司桌前**無法定論**的點 —— 標出來讓你刻意去測，而不是假設。

- **檔名規則必須逐 byte 釘死。** 一個字元不符 = 該行無聲、無報錯。先抽 2–3 個 vanilla `.fuz` 檔名，確認 ModForge 的檔名產生器逐段完全重現。（[04](04-fuz-and-filenames.md) §「釘死規則」）。記憶 [[vanilla-nif-paths-must-be-verified]] 是同類「錯路徑＝隱形、無報錯」陷阱。
- **FaceFXWrapper 在 Wine 下未證實。** 它用 MemoryModule 在記憶體載入 CK DLL —— 這是 Wine 下最可能爆的部分。把 Tier 1 當成「試一下，~15 分鐘時限」；若不乖就直接退到 Tier 2（合成）或 Tier 0（無 lip）。（[03](03-lip-and-audio-encoding.md)）
- **`.lip` 確切 byte layout 尚未捕捉到這裡**（兩個權威 wiki 都 403 擋自動抓取）。若走 Tier 2，在實作時用瀏覽器讀 `fallout.wiki/wiki/LIP_File_Format`，**並**對 hex-diff 幾個抽出來的 vanilla `.lip` 來釘。已知事實記在 [03](03-lip-and-audio-encoding.md) §「`.lip` 格式」。
- **ffmpeg 產不出 Bethesda 合法 `.xwm`。** 用 `xWMAEncode.exe`（Wine）或出鬆散 WAV。別信 ffmpeg 的 xWMA *編碼器*。（[03](03-lip-and-audio-encoding.md)）
- **零訓練克隆會漂移**（跨多行／長行）。預留一道正規化 + QA pass；GPT-SoVITS 微調軌存在的理由正是給漂移不可接受的語音。（[01](01-engine-setup.md)）
- **`FonixData.cdf`**（只有用 CK/FaceFX 路徑才需要）是 Bethesda 財產 —— 從自己的 CK 安裝複製，絕不發布。
- **xVASynth headless-on-Linux** 維持*不選*（headless recipe 無文件）。只當「canonical 角色聲音」逃生口保留。（[01](01-engine-setup.md) §「已拒絕／延後」）
- **MO2 重裝會還原手 patch 的檔** —— 一律重建進 zip，絕不在 MO2 mod 資料夾裡手改。（記憶 [[mo2-reinstall-reverts-manual-pex]]。）
- **實機測試是手動的** —— 你自己跑遊戲（記憶 [[ingame-test-workflow]]）；計劃的結構檢查（`*diag`、路徑/檔名驗證）先做，真實 MO2/Proton 後做。

---

## 法務／倫理護欄

僅限個人、單人、不發布。**不要**發布克隆語音資產 —— 配音員與 Bethesda 權利皆適用，且 `FonixData.cdf` 是 Bethesda 財產。所有產出音訊留在本機。此約束貫穿整個 asset-pipelines 資料夾。

---

*狀態：實作計劃於 2026-06-09 依 2026 web 調研 + ModForge 既有能力草擬。引擎 API（F5-TTS CLI/Python、Chatterbox Python、GPT-SoVITS）與 `.fuz` layout 為具體事實；上面行內標註的點需在家用機器實測確認。原始檔以英文撰寫以對齊 sibling 報告 01–05；此為其 zh-TW 鏡像。*
