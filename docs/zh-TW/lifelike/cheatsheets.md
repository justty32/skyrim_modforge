# 速查表 — 診斷、console、工作流

← 回到 [lifelike hub](README.md)

## 診斷指令

```bash
cd ~/repo/ModForge
# run (no rebuild): dotnet run --project src/ModForge.Cli --no-build -- <cmd> ...

# Find vanilla forms by editorID substring (use [Type] to narrow — ~0.9s typed vs ~3.3s full-ESM scan)
dotnet run --project src/ModForge.Cli --no-build -- find <Skyrim.esm> "Ysolda" Npc
dotnet run --project src/ModForge.Cli --no-build -- find <Skyrim.esm> "CrimeFaction" Faction

# Inspect specific record types — used to diff vanilla vs our generated records
dotnet run ... -- packagediag    <plugin> <0xFORMID>   # PACK: template, flags, schedule, Data slots
dotnet run ... -- pkgsbytemplate <plugin> <0xFORMID>   # every package USING a given procedure template
dotnet run ... -- npcdiag        <plugin> <0xFORMID>   # NPC: race/class/voice/factions/CrimeFaction/AIData/packages/spells
dotnet run ... -- cstydiag       <plugin> <0xFORMID>   # CSTY: offensive/defensive/equip mults/flags
dotnet run ... -- eczndiag       <plugin> <0xFORMID>   # ECZN: level range (max 0 = uncapped)/rank/flags/owner/location
dotnet run ... -- mgefdiag       <plugin> <0xFORMID>   # MGEF: archetype/AV/flags/projectile/casting art
dotnet run ... -- lightdiag      <plugin> [0xFORMID]   # LIGH (no ID lists room-fill candidates)
dotnet run ... -- refpos         <plugin> <0xFORMID>   # REFR/ACHR: position+rotation+base (anchor placements on known navmesh)
dotnet run ... -- cellblk        <plugin> [0xFORMID]   # Cell block/sub-block by FormID
dotnet run ... -- infodiag       <plugin> <0xFORMID> [substr]  # INFO: responses + FULL CTDA conditions + OnEnd VMAD fragment, for a topic OR every topic a quest owns
dotnet run ... -- factdiag       <plugin> <0xFORMID>   # FACT: flags / ranks / inter-faction relations
dotnet run ... -- reladiag       <plugin> <0xFORMID>   # RELA: one record, or every RELA referencing the FormID as parent/child

# Build / inspect round-trip
dotnet run ... -- validate <spec.json>              # ALWAYS run first
dotnet run ... -- build    <spec.json> <out.esp>
dotnet run ... -- dump     <out.esp>                # see what we actually wrote
dotnet run ... -- extract  <out.esp> <strings.json> # read a plugin back to JSON (round-trip verify; distinct from dump)
dotnet run ... -- package  <spec.json> <outDir>     # esp + .pex
```

提示：
- **`pkgsbytemplate` 是你為某個 template 採集原版 package 的方法** — `find` 只比對
  EditorID，所以那些 ID 裡不帶 template 名稱、基於 template 的原版 package（例如 `WhiterunTempleCastHealingSpellSoldier`）
  對 `find` 是隱形的。傳入一個 template FormID
  （例如 UseMagic `0x0504F5`）就能列出每個使用它的具體 package，再對其中一個跑 `packagediag` 來
  複製它的 slot 模式。
- **對 `Skyrim.esm` 跑 `cellblk`** 可交叉驗證室內 block/sub-block 公式
  （block = id%10、sub = (id/10)%10）— 用它來確認原版 cell override 落在
  正確的 GRUP，不必跑一輪遊戲內。
- **`infodiag` 是重用任何原版對話路徑前 THE probe（首要探針）。** dump 該 topic 的 INFO CTDA
  堆疊，看一個生成的 NPC 必須滿足什麼 — It.27 follower bug 就是這樣破解的
  （每個 paid-recruit INFO 都是 `GetIsID==<a specific vanilla mercenary>`，所以自訂 NPC 永遠無法
  通過；`infodiag Skyrim.esm 0x0BCC84`）。它也會印出每個 INFO 的 OnEnd VMAD fragment，讓你
  看出某條原版 line 是否會跑你需要複製的 result script。
- **`MODFORGE_DEBUG=1`** 在出錯時印出完整 stack trace（否則只有 `ERROR: Type: msg`）。

## 遊戲內 console（用於測試生成的 NPC）

```
help "ModForge X" 0                # find an NPC's runtime FormID (FExx0XXX form for ESL)
prid <FormID>                       # select an NPC by FormID
player.moveto <FormID>              # teleport player to NPC
moveto player                       # teleport selected NPC to player
getCurrentPackage                   # what package is the engine running on this NPC?
evp                                 # force re-evaluate packages (alias for evaluatePackage)
placeatme <baseFormID> <count>      # spawn an enemy (e.g. placeatme 0x10F2A3 1 → wolf)
getav health|magicka|stamina        # read selected actor's stats
coc <cellEditorID>                  # teleport to a cell (no load screen → LOD may break briefly)
tcl                                 # toggle clip / no-clip
```

## Papyrus 前置需求（compile / package-with-scripts）

- 若 CK 不在預設的 Steam 路徑，設定 `MODFORGE_PAPYRUS_COMPILER`（指向 `PapyrusCompiler.exe` 的路徑）與 `MODFORGE_PAPYRUS_BASE`
  （含 base `.psc` + `TESV_Papyrus_Flags.flg` 的目錄）。
- 一次性：`unzip <CK>/Data/Scripts.zip "Source/Scripts/*" -d ~/.cache/modforge/papyrus/`
  （≈14,301 個 `.psc`）。若某腳本使用 SKSE 函式，把 SKSE 的 `.psc` 加進該目錄。
- 編譯器**即使失敗也回傳 exit code 0** — 本工具會掃 stdout 找 `Failed on`
  並確認 `.pex` 存在；若你曾直接呼叫 `wine PapyrusCompiler.exe`，也照做。

## CJK 在地化（簡體中文）

簡體中文 SSE 讀取 Localized 的 `<plugin>_chinese.STRINGS`，採 **UTF-8**（NOT GBK），語言後綴為
**lowercase**（Mutagen 寫的是 `_Chinese`；`applyloc` 會將其轉小寫 — 在
Linux/Proton 上大小寫有差）。CJK 文字請用 `applyloc`，絕不用 `apply`/`build`（inline strings 在
引擎的 cp1252 下會變成 `?`）。
