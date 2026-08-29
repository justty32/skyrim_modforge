# P1 — marker 與 `annotations[]`

← [phases index](phases.md)｜[README](README.md)｜[backlog](backlog.md)

玩家在世界裡放具名 marker，面板管理後匯出成 advisory `annotations[]`；agent 讀座標、姿態、標籤與筆記，再 author 真正的生成欄位。

## 資料模型

```cpp
MarkerEntry {
    std::uint32_t seq;
    std::string   label;
    std::string   kind;
    std::string   note;
    RE::NiPoint3  position;
    RE::NiPoint3  rotation;
    float         scale;
    std::string   cellOrWs;
    RE::ObjectRefHandle proxy;
}
```

- `seq` 單調遞增；`navmesh` 等有序用途依賴它。
- `note`／`navmesh` 是建議性；`mapMarker`／`vfx`／`tag` 仍由 agent 翻成 `mapMarkers[]`／`hazards[]`／`tags[]`，bridge 不自行展開。
- 座標與姿態在放置／編輯 commit 時取定，不依賴 proxy 後續物理位置。
- interior 用 cell-local 座標；exterior 用 world-space 座標；rotation 輸出度數。

## ModForge 契約

- `AnnotationSpec { Label, Kind, Note, Position, Rotation, Scale, Cell, Worldspace, Seq }` 對應 `ModSpec.Annotations`。
- build 不生成任何記錄，只輸出 `N annotation(s) (advisory, not built)`。
- 無 annotations 的 spec 生成位元不變。
- 外部 NPC base 若被 agent author 成 placement，必須明示 `kind: "npc"`；自動 NPC 判定只認 in-spec base。

## 放置與生命週期

- 動作鍵以 `bhkPickData`／`bhkWorld::PickObject` 射線放在瞄準命中點；`GetWorldScale`、`BSReadLockGuard`、eye+120 起點、range 4096，無命中退回玩家腳下。
- proxy 是 editor chrome。`ExportCell` 必須先以 handle／哨兵排除，再處理 disabled/dynamic 判斷。
- 面板支援 label、kind、note、所在 cell、刪除；E 啟動 marker 可開獨立編輯窗。
- 改名同步寫 `TESObjectREFR::SetDisplayName(BSFixedString,bool)`；save/load 後可由顯示名與 co-save pending 資料重建。
- kPostLoadGame 延後掃描當前 cell；proxy handle 解不回時以位置 ≤16 units 配對 `g_pending`，保留 label/kind/note。
- 刪除 marker 必須銷毀 proxy 並移出登記簿，不得留下 `removals[]`。
- proxy 的 3D 未就緒時，`FreezeDeferred` 最多重試 60 幀，待 `Get3D()` 後凍結。

## 工具 esp 與驗收

- `SceneCaptureTools.esp` 由 ModForge dogfood 產生；它是編輯工具，不是輸出 mod 的依賴。
- bridge 在工具 esp 缺席時仍須有 vanilla fallback，動作鍵不可完全依賴工具 esp。
- 驗收：放置 interior／exterior marker、編輯 label/kind/note/姿態、save/load 後重建、匯出後 `validate` 零問題，且 proxy 不進 `placements[]`。
