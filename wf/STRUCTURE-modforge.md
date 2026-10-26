**ModForge 補充**（2026-10-26 由原根目錄 `DEV-GUIDE.md` 併入；本檔是 ModForge 結構整理原則的唯一出處，性質類似 zh-tw / html：需要時才取用，不是 always-on 憲法；always-on 鐵律在根 [CLAUDE.md](../CLAUDE.md)，碰原始碼的程式碼慣例與 CODE_MAP 維護鏈在 [common/conventions](../workflows/common/conventions.md)）：

- **觸發 A 的門檻在 ModForge 對應的路徑**：① 根 `workflows/` 開發流程文檔 > 8192 bytes（操作筆記，bytes 較穩定、逼它精簡）；② `docs/` 使用手冊 > 300 行（產品文檔，連貫敘事＋程式碼區塊多，沿用行數較寬）；③ 程式碼 `src/`、`examples/` > 300 行。可超標保留的單體也包括**一篇連貫解碼筆記**；已完成的 spec/plan 移 `*/archive/` 凍結，不留現役夾、不拆。
- **不套門檻**：凍結的 `*/archive/`，以及鏡像程式碼領域的 `workflows/common/code-map/`。
- **觸發 C：平鋪太多即包夾**——**判準不是檔案數量，是「檔名前綴已經在扮演資料夾」**：當你看到 `Generator.Build.*`、`Generator.Validate.*`、`Spec.*`、`Diagnostics.*` 這種一群檔案靠共同前綴分群，那個前綴就是一個還沒被建出來的資料夾——把它建出來，前綴留著當檔名（**不改 namespace**，C# 的資料夾與 namespace 無關，純 `git mv` 零風險）。數量只是次要煙霧：一個資料夾直屬幾十個檔、而你得先認得前綴才知道要開哪個，就是它。下方「方法」三種觸發都照做。
  - **這條會跟觸發 A 打架**：A 說「單檔超過 300 行就拆」，照做久了就會生出一大群同前綴的小檔——**A 解決的是單檔過大，代價是平鋪爆炸**，C 是收拾這個代價的。
  - **行為不變怎麼保證**：純搬檔本身零風險，但同一批要順手改的東西（CODE_MAP 路徑、文檔連結）不是。做法是先蓋護欄再動——見 [testing.md](../workflows/testing.md) 的 `golden-hash.sh` 與 `cli-dispatch-snapshot.sh`。
- **適用場景**：單檔工作流（根 `workflows/` 下的 `testing.md` 等）膨脹到撐得起獨立資料夾（`tooling/`、`roadmap/` 就是膨脹後升 L2 的實例）、code 大檔超過 300 行要拆 partial、docs/workflows 單檔過大要分檔、一個資料夾變成什麼都丟的雜物抽屜、或一群檔案已經在用共同前綴假裝資料夾（觸發 C）。先分結構、補/更新 index，再讓上層只剩導航。
- **成長軌跡第 3 級在 ModForge**：拆出的獨立檔還包括 guide（細則，或併入本檔）；待使用者做的拆去根 [WAIT_USER](../WAIT_USER.md)。
