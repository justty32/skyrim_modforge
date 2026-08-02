# scene-capture-bridge — 已移出為獨立 repo

**2026-08-02 起本資料夾只是導引。** SKSE DLL 原始碼、CMake/vcpkg 建置、CI 全在同層的獨立 repo：

```
../../../scene-capture-bridge/             ← 本機：~/repo/moddings/skyrim/projects/scene-capture-bridge
```

→ [該 repo 的 README](../../../scene-capture-bridge/README.md)（操作模型／`sc` 模式制／面板）｜[BUILD.md](../../../scene-capture-bridge/BUILD.md)｜[src/](../../../scene-capture-bridge/src/)

**未帶 commit 歷史**（使用者決定）——舊歷史查 ModForge 的 `git log -- sub_projs/scene-capture-bridge`。

## 與 ModForge 的關係（不變）

靠 **scene.json 協議**對接、不整合：DLL 走訪 cell → 反解耐久 `<plugin>:0xLOCALID` → 吐 `scene.json`（＝一份合法 `ModSpec`）→ `dotnet run --project src/ModForge.Cli -- build scene.json` 生 patch esp。

**契約權威留在 ModForge**：[workflows/specs/ingame-scene-export-design.md](../../workflows/specs/ingame-scene-export-design.md)。該 repo 只擁有 output 形狀，生成端全在這裡。

計畫與驗收：[workflows/plans/scene-capture-bridge/](../../workflows/plans/scene-capture-bridge/README.md)｜[landed/world.md](../../workflows/feature-dev/landed/world.md)｜殘項 [wait_todo/ingame-tests.md](../../wait_todo/ingame-tests.md)——**都留在 ModForge**。

## 兩件跨 repo 的事

- **部署**：走 `../scene-capture-bridge/scripts/deploy.sh`，**不要手打 `cp`**（遊戲跑著時 `cp` 覆寫載入中的 DLL → 無聲暴斃，見 [dev-env.md](../../workflows/dev-env.md)）。
- **工具 esp**：`SceneCaptureTools.esp` 由 ModForge 自己 build（dogfood）——
  `dotnet run --project src/ModForge.Cli -- build ../scene-capture-bridge/tools-spec.json <out>/SceneCaptureTools.esp`
