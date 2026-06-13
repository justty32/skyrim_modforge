# Testing

← [INDEX](../INDEX.md)｜跨機開發/離線測試見 [dev-env.md](dev-env.md)

ModForge has one test project:

```bash
dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj
```

Most tests are pure .NET structural tests. They may use external FormKeys like
`Skyrim.esm:0x013746`, but they do not open `Skyrim.esm`.

Tests that clone vanilla templates or copy vanilla cell/worldspace context are marked:

```bash
dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "Category!=RequiresSkyrim"
```

Those marked tests require a local Skyrim Special Edition `Data` folder containing
`Skyrim.esm`. The generator uses `MODFORGE_SKYRIM_DATA` when set, otherwise it falls
back to the local Steam path:

```bash
export MODFORGE_SKYRIM_DATA="$HOME/.local/share/Steam/steamapps/common/Skyrim Special Edition/Data"
dotnet test tests/ModForge.Core.Tests/ModForge.Core.Tests.csproj --filter "Category=RequiresSkyrim"
```
