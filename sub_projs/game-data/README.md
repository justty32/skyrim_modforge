# game-data — 已移出

**2026-08-02 起本資料夾只是導引。** 內容全在：

```
projects/game-data/          ← 本機：~/repo/moddings/skyrim/projects/game-data
```

→ [新位置的 README](../../../game-data/README.md)

## 是什麼

抽取出的全遊戲文本／清單（vanilla + DLC + CC + 已下載 mod）：對白、書、任務、NPC/物品/地點/魔法 tsv。給劇情與 mod 調查 agent **唯讀**取用；內容 gitignore，`extract.sh` 可重生。

## 留在 ModForge 的

`extract.sh` 呼叫的是 **ModForge CLI 的 `gamedata` 指令**（`dotnet ModForge.Cli.dll gamedata <plugin> <outDir>`），預設走同層 `../ModForge`，可用 `MODFORGE_REPO` 覆寫。

---

**未帶 commit 歷史**（使用者決定）——舊歷史查 `git log -- sub_projs/game-data`。
