# Testing — 跑測試

← [INDEX](../INDEX.md)｜跨機開發/離線環境見 [dev-env.md](dev-env.md)

ModForge 只有一個測試專案：

```bash
dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj
```

大多數測試是純 .NET 的結構測試。它們可能引用外部 FormKey（如 `Skyrim.esm:0x013746`），但**不會開啟 `Skyrim.esm`**。

## 日常跑法（離線，任何機器）

日常迴歸排除需本機 `Skyrim.esm` 的測試——這些測試會 clone vanilla 模板或複製 vanilla cell/worldspace context，都已標記 `Category=RequiresSkyrim`：

```bash
dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "Category!=RequiresSkyrim"
```

離線機也可跑 `scripts/test-offline.sh`（同一條 `--filter "Category!=RequiresSkyrim"` 的封裝；`scripts/*.sh` 需經 bash，見 [dev-env.md](dev-env.md)）。

離線 suite 的 `VoiceLiveContractTests` 會用 production `Voice.GenerateWav()` 真正啟動同層
`../skyrim-voicegen/voicegen.py`，最末端才換成 deterministic fake Fish engine；不需語音模型。
測試建立當前平台 wrapper，並同時生成另一平台版本以釘死 wrapper 形狀。若 sibling checkout
或 Python runtime 不在場，該 case 會帶理由明示 skip；可用
`MODFORGE_VOICE_CONTRACT_PYTHON` 指定 Python executable。

## 重構護欄：golden hash（`scripts/golden-hash.sh`）

行為不變的重構（[refactor 工作流](refactor/README.md)）光靠上面的測試**不夠**：1107 個 test method 裡 965 個（87%）只走 `Generator.Build`/`Validate` 對記錄下斷言，內部怎麼重組它們一律看不見。`golden-hash.sh` 補這個洞——把 `examples/` 全部 build 一次，逐一輸出 `.esp`／`.seq` 的 SHA-256：

```bash
scripts/golden-hash.sh /tmp/before.txt        # 重構前（第二參數＝並行度，預設 4）
# ...重構...
scripts/golden-hash.sh /tmp/after.txt
diff /tmp/before.txt /tmp/after.txt && echo "byte-identical"
```

**任何一行變動＝輸出變了＝這次重構不是 behavior-preserving。** 143 支 spec ／ 197 個產物，離線機約 3 分鐘。

⚠️ **輸出跟機器綁定，不要 commit、不要跨機比對**：離線機沒有 `Skyrim.esm`，凡是指向 vanilla cell 的 placement 都會被 skip，產出的是縮水版 plugin，bytes 與 Manjaro 上同一支 spec 不同。永遠**在同一台機器上比 before/after**。

## RequiresSkyrim 跑法（需本機 Skyrim.esm）

標記 `Category=RequiresSkyrim` 的測試需要本機的 Skyrim Special Edition `Data` 資料夾（內含 `Skyrim.esm`）。生成器優先讀 `MODFORGE_SKYRIM_DATA`，未設時回退到本機 Steam 路徑：

```bash
export MODFORGE_SKYRIM_DATA="$HOME/.local/share/Steam/steamapps/common/Skyrim Special Edition/Data"
dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "Category=RequiresSkyrim"
```
