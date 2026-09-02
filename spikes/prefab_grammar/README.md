> [!CAUTION]
> **這是 spike，不進 ModForge 主管線，不生 esp、不碰遊戲、不碰 navmesh。**

# Prefab grammar spike

這個離線、確定性的實驗用 prefab JSON 與 seed 生成 block 佈局 JSON。

## 目錄結構

- `README.md` — 操作入口
- `NOTES.md` — 設計與接點
- `geometry.py` — 量化幾何
- `schema.py` — JSON 驗證
- `generator.py` — 佈局生成
- `cli.py` — 命令列入口
- `entrance_vault.json` — 雙出口入口間
- `corridor_straight.json` — 直走廊
- `corridor_long.json` — 長直走廊
- `corridor_corner_left.json` — 左轉角
- `corridor_corner_right.json` — 右轉角
- `corridor_tee.json` — T 字走廊
- `room_small.json` — 雙出口小房間
- `deadend_plug.json` — 死路封口

八個 prefab JSON 位於 `data/prefabs/`；測試檔 `test_*.py` 位於本目錄。

## 執行

在 `projects/ModForge/spikes/`：

```console
python -m prefab_grammar.cli --seed 1337
```

或在 `projects/ModForge/`：

```console
python spikes/prefab_grammar/cli.py --seed 1337
```

## 測試

在 `projects/ModForge/`：

```console
python -m unittest discover -s spikes/prefab_grammar -p "test_*.py" -v
```

## Prefab 幾何約定

座標使用 Skyrim game units，以 256 為格子單位。Bounding box 使用中心點與完整尺寸；connector 位於 bbox 表面，`facing` 朝 bbox 外側。所有 connector 的 `type` 統一為 `hall2`，讓 entrance 與 exit 依凍結對接公式互接。

設計說明與 ModForge 接點見 [NOTES.md](NOTES.md)；欄位與數學的凍結介面見 [CONTRACT.md](../../../../agentctl/handoffs/spike-2026-09-02/CONTRACT.md)。
