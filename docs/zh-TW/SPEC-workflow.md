<!-- CLI 工作流程 -->
# ModForge 規格說明 — 工作流程

← [目錄](SPEC-index.md)

## 工作流程

```bash
dotnet run --project src/ModForge.Cli -- validate myspec.json          # 先行檢查
dotnet run --project src/ModForge.Cli -- build    myspec.json out.esp   # 僅建置插件
dotnet run --project src/ModForge.Cli -- package  myspec.json OutModDir # esp + 編譯後的腳本 -> MO2 資料夾
```
`package` 輸出 `OutModDir/<pluginName>` + `Scripts/*.pex` + `Scripts/Source/*.psc`。

**自然語言 → 規格：** 向 AI 代理人（Claude Code）描述需求；代理人根據本文件 / `../examples/spec.schema.json`（依 `for_agent.md`）輸出規格，執行 `validate`（自動修正問題），再執行 `build`/`package`。此代理人驅動循環**即是** NL→規格層——工具本身不含 LLM API（原本規劃的 `describe` 指令已取消），因此無需設定任何 API 金鑰或提供商。

## 尚未涵蓋（可在 `ModForge.Core` 的 `Generator.Build` + 規格類別中擴充）
世界放置現已涵蓋新建室內場景、原版室內場景，**以及室外/世界空間場景**（透過 `worldspace` + 世界座標），ModForge 現在也能**建立**新的世界空間（WRLD）+ 地區（REGN）——見 [SPEC-world](SPEC-world.md)（僅限記錄層；地形/LOD/導航網格仍在 CK 端）。Refs（spec 內部或 `<master>:0xFORMID`）與 `find` 指令是參照外部 form 的基礎積木。其餘缺口為長尾記錄類型/欄位及 CK 端的地形/LOD/導航網格創作——記錄端的模式相同：新增一個規格類別 + 在 `Build` 中新增一個迴圈。
