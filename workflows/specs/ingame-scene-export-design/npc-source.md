# 遊戲內場景匯出 — NPC 來源、ESL local-id、§D NPC 角色 macro

← [ingame-scene-export-design](../ingame-scene-export-design.md)（入口）

### NPC 來源：PROTEUS 是**可選**，預設走「大眾臉」（2026-07-10 使用者定調）

> 🔴 **路徑 A（PROTEUS）已被 2026-07-12 拍板取代**——見上節 `sc capp`（[captures](captures.md)）：直接吸玩家 base TESNPC，不需要 PROTEUS。以下保留為歷史脈絡。

原設計把 §A 拓印玩家（PROTEUS）當成 NPC 的唯一來源。改為兩條並列，**預設是後者**：

| 路徑 | 外貌 | ref 耐久性 | 產物自足 |
|---|---|---|---|
| **C. 大眾臉（預設）** — ModForge 直接生 `NpcSpec` | 種族預設（`NpcSpec` 有 `Race`，**無** headpart/tint/facegen 欄位 → 引擎用預設頭） | **耐久**（in-spec authored placement，`npcRoles[].actorRef` 指得到） | ✅ 玩家端不需裝任何東西 |
| **A. 拓印玩家（可選）** — 消費 PROTEUS clone | 玩家本人的臉（PROTEUS native facegen） | ⚠️ 見下 | ❌ 依賴玩家端裝 PROTEUS |

大眾臉路徑**今天就能跑**：`NpcSpec` 生出來的 NPC 已在實機出貨過（vendor / hireable follower / identity 系統）。要「一群沒名字的村民」時它才是對的工具——facegen GAP 根本不在關鍵路徑上。

**⚠️ 路徑 A 的未解風險**（降為可選後不再阻塞 MVP）：vanilla diff 用「ref 解不出耐久 id ⇒ 玩家擺的」。PROTEUS clone 的 actor **ref 必然是 dynamic**（`PlaceAtMe`），會被正確判為玩家擺的——但 `npcRoles[].actorRef` 需要**耐久** ref id 才指得到它。若 clone 的 **NPC_ base 本身也是 runtime 生成**，`ResolveDurableId(base)` 一併失敗，該 actor 直接落進 `skipped`。idea §A「crux 已拍板：PROTEUS clone 是穩定、可引用的」須釐清指的是 base 還是 ref——**實機待驗**。

### ESL local-id 寬度（2026-07-10 以 houseCARL 離線核對，已拆 TODO）

`file->IsLight() ? (rawId & 0xFFF) : (rawId & 0xFFFFFF)` **正確**：`ccBGSSSE037-Curios.esl` 的 local id 最大 `0x88E`（全 < 0x1000，12 位元）；`Skyrim.esm` 的 `01605E` 是 24 位元。兩者都能 round-trip 成 ModForge 要的 `<plugin>:0xLOCALID`（6 位 hex 補零）。

---

## §D NPC 角色 macro（已落地；以下保留設計脈絡）

scene.json 的 `npcRoles[]` 每筆 = `{ actorRef, role, backstory }`。ModForge build 時吃 role → **macro-expand 成既有生成型別**（對話 INFO + package + faction/service），底層零件全已實機——**macro 只是把它們串起來**：

```
npcRole: { actorRef: "SkyrimTown.esp:0x001234", role: "blacksmith",
           backstory: "曾是帝國軍鐵匠，戰後在此開鋪" }
   │  build-time macro-expand（1 個 role = 一包既有型別的組合）：
   ├─▶ 對話：conditioned Hello 問候 INFO（GetIsID actorRef）+ 服務 topic  ← [[conditioned-hello-one-topic-many-infos]]
   ├─▶ 行為：blacksmith sandbox package（綁鐵匠鋪 furniture/anvil）        ← [[radiant-alias-package-byte-truths]]
   ├─▶ 服務：vendor faction + merchant container（賣鐵匠貨）                ← 既有 Build.Vendor / SettlementVendorSpec
   └─▶ backstory → 對話文本（切片內手填；後續接 #17 AI 生成）
```

**⚠️ 用對既有機件（recon 2026-07-08 校正）**——兩個看似相關的既有型別都**不是**直接載體，別誤用：
- 既有 **`IdentitySpec`（[[identity-system-confirmed]]）是玩家向**：一個玩家加入的 FACT，`identity`/`primaryIdentity` tag 展開成**玩家**對話的 GetInFaction gate。**與 §D 無關**（§D 是給某個 NPC 一個職業角色，不是給玩家一個身份）。
- 既有 **`SettlementSpec.ResidentSpec` 最接近**，但它的 `Npc` 欄指向**in-spec** NpcSpec editorId、且以「住滿聚落」為框；§D 要的是**掛在外部 captured ActorRef 上**、且要**帶 conditioned-Hello 對話**。

→ **已落地**：`SceneNpcRoleSpec { Npc, Role, Backstory }` 在 `Spec.SceneExport.cs`，`ExpandNpcRoles` 在 `Generator.SceneNpcRoles.cs`；blacksmith 會展開 greeting、sandbox package，且有 companion placement 時再生成 merchant chest、vendor FACT、trade topic 與 faction membership。既有 `SceneNpcRolesTests` 涵蓋此 macro。

- **對話仍 build-time 由 ModForge 生**（使用者定調）——遊戲內只**貼 role tag**，不在遊戲內生對話。
- **切片只做 1 個 role（blacksmith）**證明管線；role 全集（守衛/商人/冒險者…）沿用 [#23 living-adventurers 的 archetype 框架](../../idea/living-adventurers.md)，一個 role = 一包資料（對話池/package/service），引擎不變。

---
