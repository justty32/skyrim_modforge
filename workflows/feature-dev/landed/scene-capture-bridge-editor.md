# 已落地 — scene-capture-bridge 編輯器：P7/P8／匯出三改／輸入與面板／Browser ghost

← [landed index](README.md)｜採集橋：[scene-capture-bridge.md](scene-capture-bridge.md)（含 🔴 部署事故，先讀）· [editor](scene-capture-bridge-editor.md) · [referrer](scene-capture-bridge-referrer.md) · [flags-deps](scene-capture-bridge-flags-deps.md)

## P7/P8 編輯器 backlog 一輪 + 匯出三改 · IN-GAME 2026-07-12

一輪實測**全過**（使用者：「測了，都沒問題」）。DLL 這輪最終 `c5049c78`。

**P7**（模式制之後的操作性補完）：`sc delc`（console 選取擦除，actor 正確拒絕）、`sc del/pk/ed er0/er1`（該模式動作鍵準星↔射線切換，取代「numpad * 專用鍵」）、`sc ed ax`（純旋轉子模式：4/6 yaw、1/3 pitch、7/9 roll）、編輯匕首 marker 位置（numpad 0 commit 更新登記簿、**不**進 overrides）、palette `save/load to file`、`Export all (loaded cells)`、co-save SETT v3 設定還原。

**P8**：marker 模型換**鐵匕首**（劍尖視覺化朝向）；marker 記錄**完整朝向＋大小** → `annotations[]` 帶 `rotation{x,y,z}`＋`scale`（實機 export 驗到 `rotation.z=56.9`、`scale=1.1`、`note`）；numpad 5 per-mode。

**匯出三改**（同日做、同日過）：
1. **檔名帶場景＋時間**：`scene-export_<cell EditorID 或 worldspace_x<X>y<Y>>_<YYYYMMDD-HHMM>.json`，同分鐘再匯出加 `-2`，**永不覆蓋**（實機驗到 `scene-export_Tamriel_x26y25_20260712-0957.json` / `-0957-2.json` / `all-Tamriel_...`）。⚠️ 下游別再寫死 `scene-export.json`。
2. **captures 拆獨立檔**：`Export captures` 鈕 → `captures_<時間>.json`，只含 `capturedItems[]`＋`capturedNpcs[]`；**場景匯出檔不再帶這兩段**。兩者都是 `ModSpec` 成員，故單獨 `build` 吃得下（C# 端零改動）。
3. **Scope 反轉**：`ExportCell`/`ExportAll` 掃到 actor 直接跳過（計 `actorsExcluded`），`placements[]` 不再有 `kind:"npc"`。NPC 改走 marker（`annotations[]`）或 `sc cap`。

**後續修正**（同日，使用者第二輪反饋）：旋轉子模式的歸零鍵改 **per-axis 還原**（2=pitch / 5=yaw / 8=roll，各自還原成**進編輯前的該軸原值**，不是設 0、不是全軸）；palette 的 `load from file` 明確「載入的排最上面」＋新增 **`replace from file`**（清空再載入，含「檔案不存在就完全不動」的防呆）。順帶統一了 palette json 檔內順序＝面板順序。

## 採集橋輸入/面板三件（動作鍵 `.ini`、面板欄位一致化、numpad 長按）· 使用者實機確認 2026-07-14

- **動作鍵改走 `SceneCaptureBridge.ini`**：遊戲內 rebind 抓鍵**兩次實機失敗後整條移除**——面板不暫停遊戲，抓鍵永遠在跟玩家手上還按著的 WASD 搶同一條輸入串流。檔案沒有那條賽道可輸（沒有 armed 狀態、沒有 input sink、沒有時序）。ini 贏過 co-save、保留鍵拒收、缺檔自動生成（log 實證 `KeyIni: applied 7 bind(s)`）。
- **面板欄位一致化（`UI.Fields`）**：六個面板各抄一份「buffer 與 registry 靜默分叉」的錯 ⇒ **結構在漏**，收成單一擁有者（非編輯中每幀從 registry 回種 ⇒ 面板結構上不可能說謊；Enter／apply／點走都提交）。四本 registry ＋ 六頁面板補齊 label／note。
- **`sc ed` numpad 長按持續作用**：`IsDown()` 只有按下那一幀為真（料一直都在，是被那一行丟掉的）⇒ 改吃 `IsHeld()`；**只有 nudge 鍵重複**，commit／cancel／select／per-axis revert／動作鍵**維持單發**；0.35s 死區 ＋ 8→40 steps/s 加速；frameDelta 取自兩幀 `heldDownSecs` 的差、>0.25s 的跳躍直接丟（否則暫停/讀取後會把整段空窗補成一次瞬移）。tap 與 hold 走同一個 `Nudge(steps)` 本體 ⇒ 不可能各自漂移。（2026-07-14 這套時鐘抽成共用 `Numpad.h`，給 ghost 預覽一起用。）

## Browser 目錄 ＋ 世界內 ghost 預覽（＝CK 的 Object Window）· IN-GAME 2026-07-14

**要擺山脈不必再去世界上找一座來滴管吸**：面板 Browser 頁列出 load order 的**全部可擺放 base**（實機 **26949 筆 / 31 plugin / 19 type**），預覽**就是世界本身**——選中誰，牠就站在你的瞄準點，真尺寸真光照。ghost 是 **place 模式的擺放游標**（不變式：**ghost 存在 ⟺ `sc pl` ＋ `gh1` ＋ 有東西被選中**；Browser 點一筆＝切到 place 模式並釘住它）。numpad 可轉可縮不可位移（**瞄準就是定位**，與 `sc ed ax` 同一套手感／同一個長按時鐘 `Numpad.h`）；新 ghost 依 **OBND ＋ 預覽距離**自動縮到約**螢幕九分之一**（只縮不放，numpad 0 回真實大小）。commit 與 `sc pl` **同一條擺放路徑**，匯出契約與 ModForge C# 端**零改動**。

三個值得記住的引擎真相（都是實機打出來的）：

1. **「借用 Skyrim 物品欄 UI」＝死路**（原始提案）。物品欄只吃**可攜帶** form type；**STAT/TREE/FURN/ACTI（山脈、樹、家具、建築）進不了 inventory**——連 FULL name 都沒有（STAT 記錄＝EDID/OBND/MODL），沒 icon、沒重量價值，ItemCard/SkyUI 的資料源整套對不上。**最需要的那一類正好不支援。**
2. **SSE runtime 沒有 EditorID**：`TESForm::GetFormEditorID()` 預設 `{ return ""; }`（CELL/WRLD 例外，所以匯出器能用）。⇒ 目錄索引建在 **`TESModel::GetModel()`** 上，而**模型路徑其實是更好的鍵**：搜 `mountain` 直接命中 `Landscape\Mountains\*.nif`。要真 EDID 只能離線用 Mutagen 產 catalog（backlog）。
3. 🔴 **讓 ghost 穿得過去，唯一有效的是「不讓碰撞被建出來」**：spawn 當下（3D 還沒建、就是凍結必須延後的同一個空檔）呼叫 **`ref->SetCollision(false)`**——它只翻 `kCollisionsDisabled` record flag、不碰 havok，**但引擎正是在「幫 ref 建碰撞」時讀那顆旗標**。兩種「事後拿掉」都實機失敗：`NiAVObject::SetCollisionLayer()` 只碰得到根節點（剛體掛在子節點）；走遍碰撞 scenegraph 改每顆剛體的 `collisionFilterInfo`（po3/BOS 那招）**log 證明一次都沒碰到過剛體**。**教訓：不要事後拿掉你一開始就可以不產生的東西。**（附帶真相：**havok body 不跟著 `SetPosition` 走**——STAT 的剛體固定在 ref 第一次落位處，所以「畫面跟著準心跑、碰撞箱留在原地」。）

**ghost 絕不外洩到匯出**（實機 `1 preview ghosts excluded`，擺下去的 Ivy01 逐位元對上）：除了 live handle，ghost **自帶哨兵**（`ExtraTextDisplayData`，savegame 會連 created ref 一起序列化）⇒ 開著 ghost 快存、明天讀回來，registry 是空的但 `IsGhost()` 還是認得出來（並在 kPostLoadGame 掃掉）。**能從世界重建的狀態，勝過必須記住的狀態。**
