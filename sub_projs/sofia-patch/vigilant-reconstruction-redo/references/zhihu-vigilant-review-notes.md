# Zhihu VIGILANT review notes - secondary reference

Source supplied by user in chat:
- Title: `上古卷轴5Mod推荐：Vigilant 斯坦达尔的警戒者`
- Author: `上单波比`
- Platform: Zhihu
- Also includes selected comments.

Use status:
- Secondary/player interpretation reference.
- Not canonical source.
- Do not copy its plot claims into reconstruction files without checking against `Vigilant.esm` and `game-data/mods/Vigilant/*`.
- Useful for prioritizing what to verify and which lore/ending claims matter to Chinese community interpretation.

## High-Level Evaluation

The article strongly recommends VIGILANT and frames it as one of the peak Skyrim quest mods.

Evaluation points:
- Compatibility: high; mostly standalone quest/worldspace content, with many patches available.
- Art/scene design: strong atmosphere, but mixed asset quality; Coldharbour is praised more than some armor/creature designs.
- Level design: high; especially boss fights and indoor layouts.
- Story design: very high; described as romantic, logical, lore-based but not confined by lore.
- Player experience caveat: first playthrough can be hard to understand; external analysis helps clarify the story.

## Structural Claims To Verify

Article claims:
- VIGILANT is divided into 4 chapters.
- First 3 chapters happen in Skyrim / near-world spaces.
- Chapter 4 happens entirely in Molag Bal's Oblivion realm, Coldharbour.
- Chapter 3 Chorrol-area mansion is notably frightening and often skipped by players.
- Coldharbour in VIGILANT is an Imperial City mirror, revisiting recognizable Oblivion-era sites:
  - Arena
  - Sandman Inn
  - prison
  - sewers
  - Lake Rumare / Imperial City island area
  - Malada
  - library
  - courtyards

Verification targets:
- worldspaces/cells in `locations.tsv`;
- relevant CELL/WRLD FormIDs from `Vigilant.esm`;
- Act/chapter quest chains in `quests.md` and `questdiag`.

## Coldharbour Visual / Staging Notes

Article interpretation:
- Official ESO Coldharbour baseline: bleak, barren, twisted, lifeless.
- VIGILANT's Coldharbour is not color-accurate to ESO in the author's view, but it conveys barrenness, corruption, death.
- Environmental details mentioned:
  - gargoyles / Daedric style on walls;
  - bone piles;
  - crosses;
  - ruined infrastructure;
  - carefully designed Malada, library, courtyard interiors.
- Jyggalag/order invasion restricts playable Coldharbour area.

Comment supplement:
- One commenter says in-game dialogue explains Coldharbour is bright because of the Order army.
- Author replies current version turns Coldharbour fully gray after the Greymarch begins; the battlefield light is from the orb above the Sancremathi/Sancremor tower, not simply Coldharbour itself.
- Another commenter says coastal NPC dialogue mentions Coldharbour used to be cold/dark, then changed after events around 200 years ago.

Verification targets:
- search `Coldharbour`, `Greymarch`, `Jyggalag`, `Sancre`, `Sancremathi/Sancremor`, `Order`, `orb`, `coast`, `200 years` in dialogue/books;
- inspect weather/lighting/worldspace data if needed;
- inspect relevant scene/cell records for the orb/staging.

## Story Interpretation: Core Romance

Article's core interpretation:
- VIGILANT is fundamentally a love story.
- Main emotional axis: the nameless Ayleid bard and Lamae Beolfag / Lamae Bal.
- Suggested in-game readings:
  - `The Wild Elves`
  - `Opusculus Lamae Bal`

Verification targets:
- find/booktext these titles in `books.md` / ESM;
- map bard/Lamae records and memory quests;
- verify how bard/Lamae relation is expressed in dialogue, books, and endings.

## Ada Bal / Chim-el Adabal Interpretation

Article claims:
- `Molag Bal` is interpreted as Ayleidoon: `Molag` = fire, `Bal` = stone.
- The repeated "red stone" in Chapter 4 is `Ada Bal`:
  - divine stone;
  - Amulet of Kings / Chim-el Adabal / Red Diamond analogue;
  - "Chain of Kings" in the article wording.
- In VIGILANT, this Ada Bal is a Molag Bal-created illusion/fake, not the true lore object.
- In this interpretation, the souls of Alessia and former kings are not merged with Akatosh but imprisoned by Molag Bal.
- Imprisoned souls in Coldharbour include:
  - Belharza;
  - Alessia;
  - Pelinal;
  - Morihaus;
  - Knights of the Nine;
  - Marukh;
  - others.
- The huge stored soul energy can open a portal to Aetherius.
- In the worst ending, Molag Bal uses the portal to possess Stendarr.
- Suggested in-game reading:
  - `The Chim-el Adabal`

Verification targets:
- books containing `Ada Bal`, `Chim-el Adabal`, `Red Stone`, `Amulet`, `Kings`, `Aetherius`, `Stendarr`;
- final quest/endings via `questdiag` and `infodiag`;
- records for imprisoned historical souls / bosses.

## Lorkhan / Fox Symbol

Article claim:
- One ending shows a fox symbolizing Lorkhan.
- Nordic pantheon associations cited:
  - Lorkhan = fox;
  - Stuhn, Nordic aspect of Stendarr = whale;
  - Tsun, possibly Zenithar aspect = bear.
- Suggested in-game reading:
  - `Varieties of Faith in the Empire`

Verification targets:
- ending scene/dialogue with fox;
- book/text references to fox/Lorkhan/Stuhn/Tsun;
- determine if this is VIGILANT explicit content or external TES lore used for interpretation.

## Ending / Loop Interpretation

Article's major ending theory:
- In all endings, what the player sees is not simply "the player character", but the result of the player's actions seen through the perspective of Altano / Bal / the nameless bard.
- Best and worst endings both end the nameless bard's eternal cycle.
- The other three endings restart another turn of the bard's cycle.
- The loop and dream structure are central to the author's praise.
- The player's Coldharbour actions do not literally change the past; they provide release/opportunity to souls imprisoned by Molag Bal.
- The final victory is not undoing history, but ending a Dragon Break-like repeating timeline so Lamae and the bard can rest.

Verification targets:
- ending quest records and scenes;
- ending dialogue for Altano, Bal, bard;
- Karma variables/stages;
- final quest `GoodEnd`/bad ending branches;
- direct text for "loop", "dream", "Dragon Break", "cycle", "release/rest".

## Memory / Dream Counterfactual List

The article lists key counterfactual choices the player gives trapped souls the chance to make differently. Treat as a roadmap for verification, not fact yet.

Claims to verify:
- Varan / "hunter" did not kill the innocent Ayleid girl.
- Khajiit evil king did not massacre `Gil-der-Vale`.
- Pelinal did not kill Mary, thereby releasing Pelinal, Morihaus, and Alessia.
- Dulsa did not let Marukh kill her and become the first soul imprisoned in the red stone.
- Marukh did not submit to Molag Bal in the desert.
- Mary did not interrupt inquisitor Pepe's interrogation of her conscience, and therefore was not hanged by the Order.
- Johan did not accept Molag Bal's mace, preventing the Bravil tragedy and the fall of four siblings.
- The bard did not sacrifice Lamae to Molag Bal, allowing both souls to rest.

Verification targets:
- map each claim to memory quest IDs:
  - Marukh/Dulsa: likely `zzzCHMemoryQuest07` and related `The Illusion of Death`.
  - Mary/Pepe: likely `zzzCHMemoryQuest01` and `zzzCHMemoryQuest06` context.
  - Pelinal/Mary: likely memory 10 plus Mary records.
  - Johan: memory 04 or related Bravil records.
  - Bard/Lamae: memory 08 and Lamae records.
  - Khajiit king / Gil-der-Vale and Varan/Ayleid girl need ID discovery.
- verify exact names in English extraction, because the article uses Chinese transliteration.

## Boss / Level Design Notes

Article praises:
- numerous boss fights;
- Chapter 4 dungeons usually have challenging end bosses;
- platform to fight Oblivion-era/lore figures:
  - Pelinal;
  - Morihaus;
  - Umaril;
  - Mannimarco;
  - others.

Article criticism:
- final Molag Bal duel is not very challenging;
- meteor-rain style skill feels weak.

Comment supplement:
- A commenter asks why they did not encounter Mannimarco.
- Author replies Mannimarco is on the small island to the east, imprisoned.
- Another reply asks if it is near the Marukh order; author confirms with location clue.

Verification targets:
- NPC/boss records for Mannimarco;
- eastern island cells/worldspace;
- prison records;
- boss quest records for Pelinal/Morihaus/Umaril/Mannimarco.

## Practical / Version / Route Comments

Useful comments:
- VIGILANT is praised for stability; fewer bugs than base Skyrim/DLC by one commenter.
- If accidentally teleported to Coldharbour via a needle quest at low level, player may become stuck against high-level bosses; console teleport can escape.
- Third chapter skip:
  - after getting off carriage, run opposite direction;
  - an old man sitting on a barrel near the gate warns the butler is suspicious and gives a sewing needle to stab him;
  - skip is also possible in the Vigilant HQ storage room;
  - old versions may differ, and updating old versions may require a new save.
- Altano invisible/bodyless and quest not progressing was reportedly solved by reinstalling SKSE and SkyUI in one comment.

Verification/use:
- skip route should be checked against current v181 records before documenting;
- old man may be a major Vicnverse character with limited role in VIGILANT, per author comment.

## Mod Recommendations Mentioned In Comments

Same author recommends:
- Vicn follow-ups:
  - `Unslaad`
  - `Glenmoril`
- Long quest mods:
  - `Beyond Reach`
  - `Project AHO`
- Shorter quest mods:
  - `Moon and Star`
  - `Tools of Kagrenac`
  - `Teldryn Sero`
  - `The Forgotten City`
  - `Undeath`
  - `Identity Crisis`
  - `Tale of Tsatampra Xiros`
- Other mention:
  - `Dragonborn Gallery / Legacy of the Dragonborn` as gameplay-changing quest/mod framework with VIGILANT integration.

## Redo Work Items Derived From This Reference

High priority:
- Build an ending map: ending quests, stages, Karma gates, final perspectives, loop/reset claims.
- Build a memory quest index mapping each dream/counterfactual to record IDs and source lines.
- Extract and translate key books:
  - `The Wild Elves`
  - `Opusculus Lamae Bal`
  - `The Chim-el Adabal`
  - `Varieties of Faith in the Empire`
  - `The Illusion of Death`
- Verify `Ada Bal` / red stone claims through books, dialogue, item records, and final quest records.
- Verify Coldharbour brightness / Greymarch / Order orb through dialogue and world/cell/weather records.
- Locate Mannimarco and other optional historical boss encounters.

Do not use without verification:
- "Molag = fire, Bal = stone" etymology.
- "all endings are through Altano/Bal/bard perspective."
- exact list of souls imprisoned by Molag Bal.
- exact counterfactual list.
- "Dragon Break" language unless VIGILANT text explicitly supports it.

