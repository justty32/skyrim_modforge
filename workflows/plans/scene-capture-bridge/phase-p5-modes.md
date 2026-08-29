# P5 — console 模式、動作鍵與持久化

← [phases index](phases.md)｜[backlog](backlog.md)

## 指令模型

- console 指令使用一字前綴：`sc mk`、`sc del`、`sc pk`、`sc pl`、`sc ed`、`sc cap`、`sc ref`、`sc off`。
- 形狀為 `sc <工具> <短參數>`；無參數切模式，有參數執行該工具設定或子命令。
- export 不佔動作鍵，只走 F1 面板 Export。
- 每個模式各有一格動作鍵，允許重複；同一時間只有目前模式消費事件。
- `edit` 的動作鍵選準星目標；numpad 是模式內操作，不是另一組全域快捷鍵。

## 動作鍵設定

- 遊戲內 rebind 已永久否決：SKSE Menu Framework 面板不暫停遊戲，抓鍵會與仍按著的 WASD 共用 input stream。
- 現行設定檔是 SKSE 資料夾下的 `SceneCaptureBridge.ini`；缺檔自動生成，值寫鍵名而非 scancode。
- Settings 頁提供 `reload keys from ini`；保留鍵在讀檔時拒收。
- 優先序：ini > co-save > F11 default。刪除 ini 某行後 reload，該模式須退回 co-save/default，不得保留 merge 殘值。
- DIK 名稱的單一表同時供 `KeyName(code)`／`KeyCode(name)` 使用；接受大小寫／空白變體與 `0x57`／`87` 逃生格式。

## co-save

- settings 與 Markers／Eraser／Overrides 等 registry 各自有 record version；loader 必須按 record 自身版本解析，不能因一項升版跳過整表。
- kPostLoadGame 的重建只補 runtime handle；不應改寫已保存的 authoring 資料。
- UI 顯示的鍵位來源要標示 `(ini)` 或 `(save / default)`，避免模式安靜地做不同的事。

## 輸入不變式

- `IsDown()` 只處理單發；`IsHeld()` 只交給 Editor／Preview 的 nudge。
- commit、cancel、select、per-axis revert 與模式動作鍵永遠單發。
- 長按有 0.35s 死區，從 8 steps/s 加速到 40 steps/s；相鄰 `heldDownSecs` 差值 >0.25s 時丟棄，避免暫停後瞬移。
- tap 與 hold 共用 `Nudge(ref, code, steps)`；scale clamp 為 `[0.05, 10]`。
