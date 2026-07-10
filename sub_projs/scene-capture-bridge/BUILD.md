# Build — SceneCaptureBridge

SKSE plugin (C++23, CommonLibSSE-NG). Build architecture adapted from
[justty32/my_skyrim_plugin_1](https://github.com/justty32/my_skyrim_plugin_1)
(build stack only; plugin logic is our own).

## 先決條件

- **Windows path**: VS 2022（"Desktop development with C++"）+ [vcpkg](https://github.com/microsoft/vcpkg)（`bootstrap-vcpkg.bat` 過、`VCPKG_ROOT` 指向 clone）+ CMake ≥ 3.21 + Ninja。
- **Linux (Manjaro) compile-verify path**: clang-cl + lld-link + [xwin](https://github.com/Jake-Shadle/xwin)（`xwin --accept-license splat --output ~/.xwin-cache`）+ vcpkg（`VCPKG_ROOT`）。名義上「只做編譯驗證」，但 **2026-07-10 實測此路徑的產物在遊戲內載入正常**（`skse64.log`：`plugin SceneCaptureBridge.dll (...) loaded correctly`；import 表只有 KERNEL32/ole32/VERSION/USER32/SHELL32，靜態 CRT 無 vcredist 相依）。**出貨仍走 Windows CI**（未驗過 address-library 對不同遊戲版本的行為），但 clang-cl 產物可以直接拿去實機測，不必等 CI。
- **必要 overlay**：`ports/`（`commonlibsse-ng-fork` 的 `fix-clang-delete.patch` 是 clang-cl 編 CommonLibSSE-NG 的必要修補；`directxtk` 的 registry 版在 `x64-windows-skse-clang` 下編不過）。`CMakeLists.txt` 需 `find_package(directxtk CONFIG REQUIRED)`。
- **改過 preset / `vcpkg.json` 後必須 `rm -rf build/release-clang-cl-linux`**：stale `CMakeCache.txt` 會讓 `vcpkg.cmake` 跳過 chainload toolchain，clang-cl 就不帶 `/winsysroot`，錯誤訊息長得像「編譯器壞了」。
- deps 由 vcpkg manifest 拉：`commonlibsse-ng-fork`（Monitor221hz registry）+ `nlohmann-json`。

## Windows（MSVC）

```powershell
cmake --preset build-release-msvc; if ($?) { cmake --build build/release-msvc }
```

產出 `build/release-msvc/SceneCaptureBridge.dll`（靜態 CRT，不依賴 vcredist）。
Debug 把 `release-msvc` 換 `debug-msvc`。改過 `vcpkg.json` / triplet → 先 `Remove-Item -Recurse -Force build` 再 configure（避免舊 CRT cache 的 LNK2038）。

## Manjaro（clang-cl 跨編譯，僅驗證）

```bash
cmake --preset build-release-clang-cl-linux && cmake --build build/release-clang-cl-linux
```

## 驗證 standalone

```powershell
dumpbin /dependents build\release-msvc\SceneCaptureBridge.dll
```
不該出現 `MSVCP140.dll` / `VCRUNTIME140*.dll`（出現 → 砍 `build/` 重來）。CI 自動跑此檢查。

## 打包（MO2 zip）

```powershell
scripts\pack.ps1                      # → dist\SceneCaptureBridge-0.0.1.zip
```

## 本機狀態

離線機（無 MSVC/vcpkg/ninja）**不能編譯**——只搭骨架、寫碼。實際 build-verify 待主力機（clang-cl）或 GitHub CI（windows-latest）。見 [WAIT_USER](../../WAIT_USER.md)。
