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

## RequiresSkyrim 跑法（需本機 Skyrim.esm）

標記 `Category=RequiresSkyrim` 的測試需要本機的 Skyrim Special Edition `Data` 資料夾（內含 `Skyrim.esm`）。生成器優先讀 `MODFORGE_SKYRIM_DATA`，未設時回退到本機 Steam 路徑：

```bash
export MODFORGE_SKYRIM_DATA="$HOME/.local/share/Steam/steamapps/common/Skyrim Special Edition/Data"
dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "Category=RequiresSkyrim"
```
