# 07 — 蒙皮角色（後置設計）

← [README](README.md) · 上一份：[06-standalone-runbook.md](06-standalone-runbook.md)

靜態脊椎（[02]–[06]）今天可跑。蒙皮角色/防具**在此設計但後置**——你選了靜態先做。本章點名牆與穿牆路，使你真的開始時形狀已定。標題：**這裡你重開機進 Windows 用 PyNifly。**

---

## 1. 蒙皮為何是另一種動物

靜態是幾何 + 材質 + 簡單碰撞。蒙皮 mesh 多三件難事：
1. 有確切骨名的 **Skyrim 骨架**（`NPC Spine [Spn1]`、`NPC L UpperArm [LUpr]`、…）。
2. **Skin weights**——每頂點 ≤4 骨、正規化。
3. 帶 body-part partitions 的 **`BSDismemberSkinInstance`**（讓遊戲能隱藏/斷肢的 flags）。

NifTools addon 做靜態、不好好做蒙皮-SSE。**PyNifly** 三者都正確做（skin、partitions、`_0`/`_1` weight nif、完整 `BSTriShape`）——且**只有 Windows**。你的雙系統讓這成乾淨重開機，非脆弱 Wine 硬幹（[01] §2）。

---

## 2. 管線（Blender，再重開機匯出）

多數活在 Blender（原生 Manjaro）；只有最終 nif 匯出需 Windows。

```
[Manjaro / Blender]
  1. 匯入來源 mesh + 其骨架（[02] / 遊戲解包器）
  2. 重定向來源骨架 → Skyrim 骨架   ← 牆 1（per-source bone map）
  3. 把 weights transfer 到 Skyrim 骨架（Outfit Studio Copy-Bone-Weights）
  4. clamp ≤4 weights/vertex、正規化
  5. 存 .blend
[重開機 → Windows / Blender + PyNifly]
  6. 開 .blend、建 BSDismemberSkinInstance partitions
  7. PyNifly 匯出蒙皮 SSE .nif（skin + partitions + weights）
  8. 複製 .nif 回 Manjaro build 樹
[Manjaro]
  9. 貼圖 → .dds（[03]）、路徑進 build 樹（[04] §4-§5）
 10. handoff 到動畫 .hkx                  ← 牆 2（Havok — 另一條管線）
```

---

## 3. 牆 1 — 骨架重定向（人類判斷步）

來源骨架（Genshin 的 Unity humanoid、WuWa 的 UE skeletal、FromSoft 綁骨）在骨名、數量、朝向、rest pose 都與 Skyrim 不同。

- **每種來源骨架建一次 source→Skyrim bone-name map。** Genshin `Bip001 Spine` → Skyrim `NPC Spine [Spn1]` 等。這 map 首次是*人類活*；套它可批量——「寫一次映射、重用」哲學（IDEAS §13/§14）。
- **工具：** Blender Rigify/retarget addon，或手動 constraint-based 重定向。防具只需貼合身體時，常跳過完整重定向、直接從參考身體做 weight-transfer（§4）。
- **輸出：** mesh 擺在 / parent 到 Skyrim 骨架。

這是*半*自動化：per-rig map 手動、套它可腳本。ModForge 能 per `sourceType` 存 map 並在 `convert.py` 套，但 author 它是離線人類活。

---

## 4. 牆 1b — weight transfer（多半機械）

**Outfit Studio → Shape → Copy Bone Weights** 從參考身體（CBBE/UNP/vanilla）到你的 mesh。這是標準防具 refit 流——多半點點按按，且匯入時設預設 `BSDismember` partitions。Outfit Studio 有「Building on Linux」路（原生）或 Wine 跑（[01] §3）；兩者皆可因為這是匯出前。

transfer 後：clamp 到 ≤4 weights/vertex（Blender weight 工具或 Outfit Studio）、正規化、sanity-check 無頂點總權重為零（→ 遊戲內爆炸）。

---

## 5. 牆 2 — 動畫（`.hkx`，此處範圍外）

綁好的 mesh 仍需 `.hkx` 才會動（idle、走、攻）。那是**另一條管線**——見 survey [`../05-animation-pipeline.md`](../05-animation-pipeline.md)（OAR/serde-hkx/Pandora）。本章止於「mesh 蒙皮到 Skyrim 骨架、partitions 合法、PyNifly 匯出」。若重用 vanilla 骨架 + race，角色用既有 Skyrim 動畫動——所以首個角色你可能根本不需新 `.hkx`。

---

## 6. ModForge 整合（落地時）

`modelSource.backend: pynifly` 分支（[05] §2）是**刻意手動交接**，非自動：
- `importmesh` 偵測 `backend: pynifly`、做它能做的 Blender 側準備、寫一份 **manifest**（`MODFORGE_PYNIFLY_MANIFEST`）列出要 Windows 側匯出什麼。
- 你重開機、對 manifest 跑 `pynifly_export.py`、複製結果回來。
- `package` 接著像任何 mesh 般打包蒙皮 nif。

完全自動的蒙皮路需 PyNifly 在 Linux headless 可呼叫——它不是。雙系統 + manifest 是誠實設計：自動化到 Windows-only 接縫前的一切，把接縫做成文件化的一行重開機步驟。

NPC wiring（race、head parts、`WNAM`/skin）重用既有 `NpcSpec` 機制——蒙皮 nif 只是 NPC 或防具 record 透過 `Model` 欄位指向的身體/防具 mesh。

---

## 7.「完成」長什麼樣（當你動手時）

- 一種來源骨架的 source→Skyrim bone map，存好可重用。
- 一個角色/防具 mesh：重定向、weight-transfer（≤4/頂點）、`BSDismember` partitions、**PyNifly 匯出蒙皮 SSE nif**、遊戲內在 vanilla 骨架上渲染（用既有 Skyrim 動畫動）。
- `pynifly` manifest 交接文件化為一個 build step。

然後——且僅當想要自訂動畫角色時——跨進 `.hkx` 管線（survey [05]）。

---

### 來源
[PyNifly（GH BadDogSkyrim — 蒙皮/partitions/weights、只有 Windows）](https://github.com/BadDogSkyrim/PyNifly) · [Outfit Studio — Copy Bone Weights + Building on Linux（GH ousnius wiki）](https://github.com/ousnius/BodySlide-and-Outfit-Studio/wiki/Copying-bone-weights) · [Beyond Skyrim — Rigging in Outfit Studio](https://wiki.beyondskyrim.org/wiki/Arcane_University:Rigging_in_Outfit_Studio) · `BSDismemberSkinInstance` / 80-bone-partition 上限（Beyond Skyrim NIF Data Format）。動畫 handoff：survey [`../05-animation-pipeline.md`](../05-animation-pipeline.md)。
