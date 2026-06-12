# 04 — `.fuz` writer + 決定性檔名規則

← [README](README.md) · 上一份：[03-lip-and-audio-encoding.md](03-lip-and-audio-encoding.md) · 下一份：[05-modforge-integration.md](05-modforge-integration.md)

兩件事：native C# 寫 `.fuz` 容器，與算出引擎要求的**確切 CK-相符檔名**。檔名是成敗關鍵 —— 一個字元不符 = 該行無聲、無報錯。Wine 只用於 xWMA encoding 或 FaceFX lip generation。

---

## 1. `.fuz` 容器（已驗證 layout）

`.fuz` = 選用 `.lip` blob + `.xwm`（或 WAV）音訊，前面一個 12-byte header。讀自 suglasp 的 `convert_fuz_to_xwm.ps1`（在程式碼裡 parse fuz —— 權威、可重實作）：

| Offset | Bytes | 欄位 |
|--------|-------|-------|
| 0 | 4 | Magic `FUZE`（ASCII） |
| 4 | 4 | Version / unknown |
| 8 | 4 | `FuzLipSize` —— uint32，lip section 大小 |
| 12 | `FuzLipSize` | `.lip` 資料（size == 0 則整段省略） |
| 12 + FuzLipSize | rest | `.xwm`（或 WAV）音訊串流 |

所以 `audioLen = fileLength − 12 − FuzLipSize`。若 `FuzLipSize == 0`，音訊緊接 12-byte header 之後。

**Native C# writer（已落地於 voice pipeline）：**
```csharp
// versionBytes：從一個 vanilla .fuz 抓那 4 bytes 一次，hardcode（通常是個小常數）。
static byte[] WriteFuz(byte[] xwmOrWav, byte[]? lip)
{
    using var ms = new MemoryStream();
    using var w  = new BinaryWriter(ms);
    w.Write(Encoding.ASCII.GetBytes("FUZE")); // 0: magic
    w.Write(VersionBytes);                     // 4: 4-byte version（從 vanilla fuz 釘）
    w.Write((uint)(lip?.Length ?? 0));         // 8: FuzLipSize
    if (lip is { Length: > 0 }) w.Write(lip);  // 12: lip（zero 則省略）
    w.Write(xwmOrWav);                          // 音訊
    return ms.ToArray();
}
```
Zero-lip MVP：`WriteFuz(xwm, null)` → `FUZE` + version + `0x00000000` + xwm。**無第三方工具、無 Wine。** fake TTS + 真 xWMAEncode 已在本機產出 `.fuz`。

> 架構筆記：這跟 ModForge 各處姿態一致 —— 格式小且已驗證時就 native emit bytes（像 Mutagen records），只對大型不透明格式 shell-out。Native fuz writer 完全移除 Wine fuz-tool 依賴。

---

## 2. 檔名規則（最難的一步 —— 也是 ModForge 的超能力）

**On-disk 路徑：** `Data/Sound/Voice/<PluginName.esp>/<VoiceType>/<filename>.fuz`
- 第一段 = **plugin 檔名完全一致**（如 `MyMod.esp`）
- 第二段 = **voiceType EditorID**（如 `MaleNord`）

**檔名慣例（已確認形狀）：** `(Quest)_(Topic)_(HexBaseID)_(LineNumber)`
例 `MyQuest_MyTopic_000113C9_1.fuz`。它編碼：
- 母 **quest EditorID**
- **topic/INFO context** 字串
- INFO/response record 的 **8 位 hex FormID**
- INFO 內的 **1-based response index**

CK 自動生成這些，且**不可更改** —— 音檔必須完全相符否則引擎不播。

**為何這對 ModForge 免費：** ModForge *就是*產生器。它透過 Mutagen 指派 QUST EditorID、INFO FormID、response index。所以它已握有這檔名的每個輸入，**且能不開 Creation Kit 就決定性算出來。** 這是社群手動工作流最難的一步，而 ModForge 獨家具備條件搞定它。pipeline 其餘全是通用 plumbing；*這個*才是差異化。

---

## 3. 釘死確切規則（目前實作 + 待實機驗證）

目前 ModForge 已有可用的檔名規則：quest EditorID、topic context、INFO FormID、1-based response index。用 `voicediag` / `voicelines --plan` 可以在生成前列出每個 planned filename。仍值得用 vanilla `.fuz` 與 Skyrim/Proton 實機再確認長字串截斷、大小寫、空 topic 與非英數字元行為。

**步驟（≈30 分鐘，在信任任何生成檔名前）：**
1. 抽一把某已知 quest 的 **vanilla** `.fuz` 檔名（Lazy Voice Finder，或解 Voices BSA，[02]）。
2. 在 **SSEEdit** 開那 quest 的 QUST/DIAL/INFO 看真實 EditorID、INFO FormID、response index。
3. 反推映射：確認每段檔名怎麼導出（Topic 用 DIAL EditorID 還是 INFO？截到 N 字？大/小寫？空 topic 占位？）。
4. 寫個小 ModForge 單元測試：餵已知 quest/INFO/index → 斷言它逐字重現抽出的 vanilla 名。
5. 對一個**多 response** 的 INFO 重做（確認 1-based 行索引）、與一個**空/自動 topic** 的。

確認後把釘死的規則記回此處。這是整個功能最高價值的驗證。

---

## 4. 鬆散 WAV vs 打包 `.fuz`（每次 build）

兩者都遊戲內可播。引擎接受 WAV/XWM/FUZ 當語音檔。

| 輸出 | 嘴 | 需 Wine | 硬碟 | 何時用 |
|--------|-------|-------------|------|----------|
| 鬆散 `.wav`（44.1k/16/mono） | 靜止 | 否 | 大 | **MVP** —— 零 Wine 證脊椎 |
| `.fuz`（zero-lip、內含 WAV） | 靜止 | 否 | 大 | native fuz writer 已證、仍無 Wine |
| `.fuz`（zero-lip、內含 xwm） | 靜止 | 僅 xwm | 小 | xwm 路徑已證 |
| `.fuz`（lip + xwm） | 會動 | xwm（+lip 若 Tier 1） | 小 | 完整結果 |

檔名規則（§2/§3）**不論容器都相同** —— 只變副檔名/內容。所以檔名釘死一次，容器自由換。

---

## 5. 打包進 MO2 zip / mod folder

Voice files 是 loose assets，不嵌入 ESP/ESM。`package` 只會複製 `--assets` 或 `spec.assets` 提供的 `Sound/...` 樹，不會自動尋找另一個 build directory 旁邊的 voice output。輸出樹：
```
<zip root>/
  MyMod.esp
  Sound/Voice/MyMod.esp/MaleNord/MyQuest_MyTopic_000113C9_1.fuz
  ...
```
無 `.seq` 互動。依記憶 [[mo2-reinstall-reverts-manual-pex]]，一律重建進 zip —— 絕不在 live MO2 mod 資料夾手放檔，重裝會被還原。

可靠順序：

```bash
# Option A: 先 package 到最終 mod folder，再直接對其中 plugin 產 voice files
dotnet run --project src/ModForge.Cli -- package spec.json OutModDir
dotnet run --project src/ModForge.Cli -- voicelines spec.json OutModDir/MyMod.esp

# Option B: staging 產 voice，再把 staging 的 Sound/ 當 assets 打包
dotnet run --project src/ModForge.Cli -- build spec.json Staging/MyMod.esp
dotnet run --project src/ModForge.Cli -- voicelines spec.json Staging/MyMod.esp
dotnet run --project src/ModForge.Cli -- package spec.json OutModDir --assets Staging
```

---

## 6.「完成」長什麼樣

- C# `WriteFuz` 已存在，unit tests 覆蓋 FUZE header；fake-TTS + 真 xWMA generation 已產出 `.fuz`。
- `voicediag` / `voicelines --plan` 已能在 generation 前列出每個 INFO 的確切路徑。
- 剩餘：真 TTS 模型、FaceFX/lip、Skyrim/Proton 實機播放確認。

這些檢查用來加固已實作的 [05] `voicelines` step 與 [06] 的實機 runbook。

---

### 來源
fuz layout：suglasp `convert_fuz_to_xwm.ps1`、[Fallout Wiki FUZ File](https://fallout.wiki/wiki/FUZ_File)。檔名/路徑：[CK Wiki「generate voice files by batch」](https://ck.uesp.net/wiki/How_to_generate_voice_files_by_batch)、[Beyond Skyrim Voice Line Implementation](https://wiki.beyondskyrim.org/wiki/Arcane_University:Voice_Line_Implementation)。
