# skyrim-voicegen — 已移出

**2026-08-02 起本資料夾只是導引。** 內容全在：

```
projects/skyrim-voicegen/          ← 本機：~/repo/moddings/skyrim/projects/skyrim-voicegen
```

→ [新位置的 README](../../../skyrim-voicegen/README.md)

## 是什麼

語音合成基石工具：收「臺詞 + 情緒 + 參考嗓音」吐 `.wav`。用哪個 TTS 後端是它的內政。

## 留在 ModForge 的

**黑盒 exec，不整合**——掛勾＝環境變數 `MODFORGE_TTS_BIN`，契約見該 repo 的 `PROTOCOL.md`。ModForge 這邊只認 args 與產出的 wav。

---

**未帶 commit 歷史**（使用者決定）——舊歷史查 `git log -- sub_projs/skyrim-voicegen`。
