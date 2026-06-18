# 對 ModForge 的參考價值

← [animobject-swapper](animobject-swapper.md)

## 五、對 ModForge 的參考價值

### 純參考（目前無直接生成需求）

AOS 的對應 record 類型是 ANIO，它是 animation 管線的一部分，不是 ESP 的主力生成項。ModForge 目前的工作重心在 NPC、Dialogue、Quest、Scene 等 record；ANIO 替換屬於更精細的角色化視覺演出層。

純參考的原因：
- 生成 `_ANIO.ini` 需要知道 base ANIO 的 formID/editorID，這依賴對 vanilla idle animation 與 ANIO 配對的深度調查。
- 使用場景是「特定 follower 拿特定道具」，通常是手工設計決策，不是程序化生成的強項。

### 有潛力的支援點（推斷，需 code 驗證）

若 ModForge 的 follower spec 包含「道具/手持物品風格化」欄位，可以後期加入 `_ANIO.ini` 生成器：

- 輸入：`character.anio_profile` → `{ base_anio: "DrinkingCupANIO", swaps: ["SofiaWineBottleANIO"] }`
- 輸出：`<character>_ANIO.ini` 的對應行

Filter 條件中 `NPC base form` 是最精確的角色化方式（指定 Sofia 的 base formID），這需要 ModForge 在生成時知道對應 NPC 的 formID（通常是 spec 已知的）。

### 搭配使用模式（OAR + AOS）

最有力的演出技法：
- OAR（Open Animation Replacer）：負責換動作（idle animation hkx）
- AOS（AnimObject Swapper）：負責換動作中拿的物件（ANIO）

兩者都不需要 ESP patch，都是 config 驅動，可以各自獨立疊加。ModForge 若要支援「角色化演出包」輸出，OAR config 生成（已在 roadmap）搭配 AOS ini 生成是自然的配對。

### 小結

AOS 是「讓不同角色在相同 idle 裡拿不同道具」的最輕量工具。它比複製整個 idle animation record 更乾淨，兼容性更好。對 ModForge 目前的短期 roadmap 是**純參考**；中期若要做「follower 角色化道具演出包」，AOS ini 生成器是值得考慮的低成本輸出。

