# 已落地 — items / magic（record builders）

← [landed index](README.md)｜對應 [CODE_MAP.items-magic](../../common/code-map/CODE_MAP.items-magic.md)

- **GlobalVariable (GLOB)**：`GlobalSpec`（short/long/float + constant）。
- **Projectile (PROJ) + Explosion (EXPL)**：自訂法術飛行彈+爆，鏈 EXPL←PROJ←MGEF←SPEL。
- **MGEF 擴充**（2026-06-13）：`archetype:"Script"`（VMAD 掛 ActiveMagicEffect 腳本，走通用 `scripts[]{targetEditorId:<mgef>}`）+ **DualValueModifier**（`secondActorValue`/`secondActorValueWeight`，一法術扣兩條 AV）。**Health+Stamina in-game 確認 2026-06-13**；**踩坑**：Concentration+Aimed 需 `castingArt`+`projectile` 否則 CTD。
- **FormList (FLST)**（2026-06-13，offline）：`formLists[]`（editorId + items 任意 record ref，順序保留）。FLST **無**獨立 `GetIsInList`，走既有 `*OrList` param（GetItemCount/GetEquipped/GetIsVoiceType/GetInWorldspace 收 FormList）。
- **Hazard (HAZD)**（2026-06-13，offline 完整、未實機）：`hazards[]`（model/radius/lifetime/targetInterval/limit/spell/flags + light/sound/imad/impactDataSet）。兩種用法：①法術噴出（MGEF `archetype:"SpawnHazard"` + `association`，複用既有 MGEF wiring）②放置（`placements[].base` 是 HAZD 或 `kind:"hazard"`→`PlacedHazard`）。見 `SPEC-magic.md § hazards`、`CODE_MAP.items-magic.md`、`examples/hazard.json`。
- **Music (MUSC + MUST)**（2026-06-13，offline 完整、未實機）：`musicTracks[]`（MUST：SingleTrack→`.xwm`／Palette→子軌池／SilentTrack + loop）+ `music[]`（MUSC：flags/priority/`duckingDecibel`(正 dB 0–655)/tracks）。掛 `cells[].music` + `worldspaces[].music`（後者沿用既有 wire）。音檔 loose asset 走 `assets`。見 `SPEC-world.md § music`、`CODE_MAP.items-magic.md`、`examples/music.json`。**踩坑**：`duckingDecibel` 負值記憶體 OK 但 CLI build 寫檔 range-check（0–655）會炸。
