# CODE_MAP.infra — SKSE 分發器（各分發器說明）

← [CODE_MAP.infra](../CODE_MAP.infra.md)

## SKSE 分發器 config 生成（續：各分發器說明）


`_DISTR.ini` 寫在 mod 資料夾**根目錄**（≠ SKSE/Plugins）；RecordID/EditorID 由玩家 load order 解析，ModForge 離線不驗。example：`examples/spid_distribution_spec.json`。
**MCM Helper**（Idea D-2，ini path **in-game confirmed 2026-06-20**）：ModSetting 值進 `MCM/Settings/<plugin-stem>.ini`；`global` bool toggle 另生 per-menu setter + VMAD GLOB property（offline 2026-08-11，runtime 待確認）。光丟 config 不出選單，必須有 registration QUST + player alias；資料夾名＝plugin stem。examples：`mcm_config_spec.json`、`mcm_global_perk.json`。
**FLM**（Idea D-4）：`_FLM.ini` 寫在 mod 根，runtime 把 form 追加進**任意既有 FLST**（vanilla/他 mod）零 override 零衝突；自建 FLST 仍走 esp-side `formLists[]`。MVP 涵蓋 FormList 操作行 + Filter/Alias/Group/Collection 定義；`ModEvent`（需 Papyrus 發送）+ 特化快捷（Plant/BToys/…）為範圍外。example：`examples/formlist_inject_spec.json`。
**KID**（D-5，**in-game 確認 2026-06-20**）：`_KID.ini`，把 Keyword 依 filter 掛到 record（unknown EditorID → KID 自建 KYWD）。syntax example：`examples/kid_distribution_spec.json`；known-good log-test：`examples/kid_keyword_test_spec.json`（新 KYWD→鐵武器，`po3_KeywordItemDistributor.log` 看 `added to N`）。
**BOS**（D-6，**in-game 確認 2026-06-20**）：`_SWAP.ini`，reference 載入時把 base object 換成另一個（可帶 location 條件/transform/chance），MVP 限 `[Forms]` section。syntax example：`examples/object_swap_spec.json`；known-good visible-test：`examples/bos_treeswap_visible_spec.json`（全松樹→白楊；遠景 LOD 不換、只看近處）。
**AOS**（D-7，**in-game 確認 2026-06-20**）：`_ANIO.ini`，換 idle 時手持的 ANIO 道具（隨機池 + NPC/faction/trait 條件）。syntax example：`examples/anim_object_swap_spec.json`；known-good visible-test：`examples/aos_bucket_test_spec.json`（酒館酒杯→水桶）。
**SkyPatcher**（D-3，**in-game 確認 2026-06-20**）：`SkyPatcher/<recordType>/<file>.ini`，runtime 依 filter 改 record（NPC 加 spell/perk、leveled list 注入）。MVP 不白名單欄位，verbatim emit。syntax example：`examples/skypatcher_spec.json`；known-good visible-test：`examples/skypatcher_scale_test_spec.json`（Nord `height=1.5`；尺寸 key 是 `height` 非 `setScale`）。
> **全 D-group 七個分發器（SPID/MCM/FLM/KID/BOS/AOS/SkyPatcher）皆 IN-GAME CONFIRMED 2026-06-20**。離線只驗結構、runtime 由 DLL 對玩家 load order 解析 ref；各框架的 known-good 測試 + SKSE log 路徑見 memory `dll-loose-ini-distributors-confirmed`。
