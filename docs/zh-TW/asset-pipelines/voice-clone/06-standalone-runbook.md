# 06 — 獨立回家施工手冊（從這裡開始）

← [README](README.md) · 上一份：[05-modforge-integration.md](05-modforge-integration.md)

回家機器（Manjaro、16 GB VRAM）的複製貼上路徑。不動 ModForge 程式碼 —— 先手跑證明 pipeline（[05] 之後自動化）。每步證明一件事；當前步驟未跑通前別推進。需要*為什麼*時，連結會跳到細節文件。

> 開始前備好：一個 ModForge 建好的測試 plugin（`.esp`），含**一個 NPC**其 `VoiceType` 為 `MaleNord` 且有**3 句短對話**；MO2 + Skyrim SE 走 Proton；硬碟上有 Skyrim Voices BSA。

---

## 步驟 0 — 環境 sanity（~10 分鐘）

```bash
nvidia-smi                       # 看得到 GPU + CUDA runtime
python -c "import torch; print(torch.cuda.is_available())"   # venv 裝完 torch 後 → True
sudo pacman -S --needed ffmpeg sox git git-lfs
```
裝一個引擎 venv（[01] §2 —— 建議 F5-TTS）：
```bash
uv venv ~/voice-work/f5 && source ~/voice-work/f5/bin/activate
uv pip install torch torchaudio --index-url https://download.pytorch.org/whl/cu124
uv pip install f5-tts
```
**證明：** GPU + 引擎 import。→ 細節：[01](01-engine-setup.md)。

---

## 步驟 1 — 拿到參考聲音（~20 分鐘）

抽 ~10–20 個乾淨 `MaleNord` 行成 WAV，挑一個好的 5–15 秒 clip + 其逐字稿。
- 最省力：**Lazy Voice Finder** 走 Wine → 搜 voiceType `MaleNord` → 匯出 WAV（[02] §2 路線 A）。
- 存 `~/voice-work/refs/MaleNord_ref.wav` 並把其確切說的字寫進 `~/voice-work/refs/MaleNord_ref.txt`。

**證明：** 你有可克隆的參考。→ 細節：[02](02-voice-data.md)。

---

## 步驟 2 — 生成 3 句（~10 分鐘）

```bash
source ~/voice-work/f5/bin/activate
REF=~/voice-work/refs/MaleNord_ref.wav
RT="$(cat ~/voice-work/refs/MaleNord_ref.txt)"
mkdir -p ~/voice-work/out
i=1
for LINE in "Halt! You've violated the law." "Pay the fine or it's off to jail." "Wait... I know you."; do
  f5-tts_infer-cli --model F5TTS_v1_Base --ref_audio "$REF" --ref_text "$RT" \
    --gen_text "$LINE" --output_dir ~/voice-work/out/ 
  mv ~/voice-work/out/infer_cli_out.wav ~/voice-work/out/line_$i.wav   # 依實際輸出檔名調整
  i=$((i+1))
done
```
聽。若克隆漂移/破音，換個參考 clip（步驟 1）或用 Chatterbox A/B（[01] §3）再來怪 pipeline。

**證明：** 文字 → 對味 WAV。→ 細節：[01](01-engine-setup.md)。

---

## 步驟 3 — 正規化到遊戲格式（~2 分鐘）

```bash
for f in ~/voice-work/out/line_*.wav; do
  ffmpeg -y -i "$f" -ac 1 -ar 44100 -sample_fmt s16 "${f%.wav}_44k.wav"
done
```
**證明：** PCM 16-bit mono 44.1 kHz —— 安全遊戲內格式。→ 細節：[03](03-lip-and-audio-encoding.md) §1。

---

## 步驟 4 — 釘死檔名規則，再放鬆散 WAV（~40 分鐘 —— 成敗關鍵步）

這是最高風險、最高價值的一步。檔名錯 = 該行無聲、**無報錯**。

1. 抽 2–3 個 **vanilla** `.fuz` 檔名，在 **SSEEdit** 開其母 quest；反推 `(Quest)_(Topic)_(HexBaseID)_(LineNumber)` 每段怎麼導出（[04] §3）。
2. 在 SSEEdit 開**你**建的測試 plugin；讀你 NPC 的 quest EditorID、3 句的 INFO FormID 與 response index。
3. 手工拼出每個目標名，例 `MyQuest_MyTopic_000113C9_1.wav`。
4. 放正規化過的 WAV（用**純 `.wav` 副檔名**，先不 fuz）：
   ```
   <MO2 mod>/Sound/Voice/<YourPlugin.esp>/MaleNord/MyQuest_MyTopic_000113C9_1.wav
   ```
   …但依 [[mo2-reinstall-reverts-manual-pex]]，把它經你正常的打包流程組進**build zip / mod 資料夾**，別手改 live MO2 資料夾。

**證明（遊戲內、手動）：** 走 MO2/Proton 啟動，觸發對話 → **音訊播放、有字幕、嘴靜止。** 這驗證檔名映射 + 克隆品質 + 打包 —— 整條脊椎 —— **零 Wine 依賴**。→ 細節：[04](04-fuz-and-filenames.md)。

> 若某行無聲：幾乎一定是檔名（段落大小寫/截斷/topic）。從 vanilla 範例重推。這就是步驟 4.1 存在的理由。

---

## 步驟 5 — 打包 `.fuz`（zero-lip）（~30 分鐘）

鬆散 WAV → `.fuz`。手跑時，用工具（Yakitori 走 Wine）或一個拋棄式腳本實作 [04] §1：
```
FUZE + <4 個 version bytes 來自 vanilla fuz> + 0x00000000 + <xwm 或 wav bytes>
```
命名 `..._1.fuz`（同規則、`.fuz` 副檔名）。遊戲內重測。

**證明：** fuz 容器可用、你的 version bytes 對。（這邏輯之後成為 [05] 的 `WriteFuz`。）→ 細節：[04](04-fuz-and-filenames.md) §1。

---

## 步驟 6 — 加 `.xwm`（選用，~15 分鐘）

```bash
wine xWMAEncode.exe ~/voice-work/out/line_1_44k.wav ~/voice-work/out/line_1.xwm
```
把 `.xwm` 打包進 fuz 取代 WAV。重測。

**證明：** Wine 音訊路徑 + 較小檔案。（ffmpeg 做不到 —— [03] §4。）→ 細節：[03](03-lip-and-audio-encoding.md) §4。

---

## 步驟 7 — 讓嘴會動（~15 分鐘時限，然後決定）

你只要*嘴動*，不要準確（[03] §2）：
```bash
ffmpeg -y -i ~/voice-work/out/line_1.wav -ac 1 -ar 16000 -sample_fmt s16 line16.wav
wine FaceFXWrapper.exe Skyrim USEnglish /path/FonixData.cdf line16.wav line16.wav out.lip "Halt! You've violated the law."
```
- **Wine 下可跑** → 把 `out.lip` 打包進 fuz（`FuzLipSize` = lip 長度）。嘴會動、準確、免費。完成。
- **Wine 下失敗** → 接受靜止嘴（Tier 0），或做 native 合成 envelope `.lip`（Tier 2，[03] §2/§3）—— 之後的 coding 任務，非 blocker。

**證明：** lip 嘴動端到端（或確認退到 Tier 0/2）。→ 細節：[03](03-lip-and-audio-encoding.md) §2。

---

## 步驟 8 — 升級保真度軌（僅在零訓練漂移時）

若跨多/長行 F5/Chatterbox 克隆飄，為該語音微調 **GPT-SoVITS**（[01] §4、[02] §3b）並用 `--engine gptsovits` 重生該 NPC 的行。下游全部（步驟 3–7）不變 —— 只有步驟 2 的引擎換掉。

---

## 步驟 9 — 交給 ModForge

步驟 1–7 成為可靠手動 recipe 後，依 [05] 實作 `voicelines` CLI step + `Generator.Build.Voice.cs`。手冊*就是* spec：每個手動步驟對映 [05] §2 的一個 pipeline stage。

---

## 速查 —— 整個 MVP 一螢幕

```
0  venv + f5-tts                                          → 引擎 import
1  Lazy Voice Finder → MaleNord_ref.wav (+ .txt)          → 參考聲音
2  f5-tts_infer-cli  × 3 句                                → line_N.wav
3  ffmpeg -ac 1 -ar 44100 -sample_fmt s16                 → 遊戲格式 wav
4  SSEEdit → 推 CK 檔名 → 經打包放置                       → 音訊播放（靜止嘴）  ★ 脊椎證明
5  打包 FUZE+ver+0x0+audio                                → .fuz 可用
6  wine xWMAEncode                                        → 較小檔案
7  wine FaceFXWrapper（15 分鐘時限）                       → 嘴會動（或 Tier 0/2）
8  （若漂移）GPT-SoVITS 微調                               → 一致語音
9  實作 voicelines step                                   → ModForge 自動化
```
★ 步驟 4 是證明 ModForge 獨家優勢（決定性 CK 檔名）、且帶無聲失敗風險的那步 —— 給它最多心力。
