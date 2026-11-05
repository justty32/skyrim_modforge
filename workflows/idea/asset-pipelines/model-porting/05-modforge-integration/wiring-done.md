# 05 — ModForge Integration Design — §4–7 設定、接線、維護鏈、完成定義

← [入口](../05-modforge-integration.md)

## 4. Tool config (env vars, conditional)

Mirrors `Papyrus.cs`/`PapyrusOptions`:

| Env var | Points at | Missing → |
|---------|-----------|-----------|
| `MODFORGE_BLENDER` | `blender` binary | skip mesh conversion, warn (user-supplied nif still works) |
| `MODFORGE_COMPRESSONATOR` | `compressonatorcli` | skip dds compression, warn (or pass-through PNG — invalid, so warn loudly) |
| `MODFORGE_TEXCONV` | `texconv.exe` (Wine) | alternate to Compressonator; unset = use Compressonator |
| `MODFORGE_CKCMD` | `ck-cmd` (may carry `wine ` prefix) | only needed if `backend: ckcmd` |
| `MODFORGE_PYNIFLY_MANIFEST` | path to write the skinned hand-off list | only for `backend: pynifly` |

Each missing tool degrades gracefully to the next-lower capability with a warning, never hard-fails — the existing conditional-embed / conditional-tool stance.

---

## 5. Package + build-pipeline wiring

- `Assets.cs` already copies `Meshes`/`Textures` trees — confirm the glob covers `Meshes/<sub>/` and `Textures/<sub>/` where `importmesh` writes (it should; same trees as user-supplied assets).
- `Package.cs` flat MO2 assembly already handles `Meshes/`+`Textures/`; converted assets ride along. **No `.seq` interaction.**
- `StaticSpec.AlternateTextures` (already exists) lets one nif be reused with swapped texture sets per placement — useful for variant props without re-converting.

---

## 6. Maintenance-chain landing (when implemented, not now)

Per CLAUDE.md Workflow 1, on landing (this is research):
- **Code:** `Spec.Items.cs` (+ a `ModelSourceSpec` record), `Mesh.cs`, `convert.py`, `Program.Build.cs`, `examples/spec.schema.json` + `sample_spec.json`.
- **CODE_MAP:** add `Mesh.cs` to `CODE_MAP.infra.md`; `importmesh` into the CLI table; `modelSource` cross-ref into `CODE_MAP.world.md` (static/placement) and `CODE_MAP.items-magic.md` (weapon/armor models). Add a Tests row (`MeshPathTests`).
- **Docs:** `modelSource` field into `SPEC-world.md` / `SPEC-items-magic.md` (or a new `SPEC-assets.md` if it grows); `importmesh` into `for_agent_cli.md` + `SPEC-workflow.md`.
- New diag **`meshdiag <esp>`** (parallel to `lightdiag`/`identitydiag`) — verifies `Model`/texture path resolution from the built esp without the game. High value given the invisible-on-wrong-path failure mode.

---

## 7. What "done" looks like

`modforge importmesh spec.json` reads each `modelSource`, shells `convert.py` to produce a `NiTriShape` `.nif` + `.dds` at the deterministic Data-relative paths, `package` bundles them — with `MODFORGE_BLENDER` set, degrading gracefully (Compressonator/ck-cmd/pynifly) when other tools are unset. The runbook ([06]) is the spec for what this step automates; the skinned `pynifly` backend ([07]) is a documented manual hand-off, not yet automated.

---

### Sources
Internal conventions from `workflows/common/code-map/CODE_MAP.infra.md`, `src/ModForge.Core/Papyrus/Papyrus.cs`, `src/ModForge.Core/Assets.cs`, `src/ModForge.Core/Spec/Spec.Items.cs`, `src/ModForge.Cli/ModForge.Cli.csproj`. Engine/format facts: [01]–[04].
