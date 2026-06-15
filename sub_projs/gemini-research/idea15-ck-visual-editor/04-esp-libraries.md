Attempt 1 failed: You have exhausted your capacity on this model. Your quota will reset after 0s.. Retrying after 5180ms...
Attempt 1 failed: You have exhausted your capacity on this model. Your quota will reset after 0s.. Retrying after 5054ms...
Attempt 1 failed: You have exhausted your capacity on this model. Your quota will reset after 2s.. Retrying after 5796ms...
Attempt 1 failed: You have exhausted your capacity on this model. Your quota will reset after 2s.. Retrying after 5250ms...
As of 2024, the ecosystem for parsing Bethesda plugin files (ESP, ESM, ESL) in languages other than C# has matured significantly, driven largely by the needs of the **LOOT** team, the **Wrye Bash** maintainers, and modern web-based modding tools.

### 1. Python Libraries
Python remains a primary language for Skyrim modding tools, especially those that prioritize logic over performance.

| Library | Language | Support (Read/Write) | GitHub URL | Last Commit |
| :--- | :--- | :--- | :--- | :--- |
| **Wrye Bash (Core)** | Python | Full (Battle-tested R/W) | [wrye-bash/wrye-bash](https://github.com/wrye-bash/wrye-bash) | Active (Jan 2025) |
| **esplugin-python** | Python (C) | Read-only (via libloot) | [loot/libloot](https://github.com/loot/libloot) | June 2024 |
| **bethesda-structs** | Python | Read (Partial Write) | [stephen-bunn/bethesda-structs](https://github.com/stephen-bunn/bethesda-structs) | Late 2023 |
| **MothPriest** | Python | Read focus (Modern 3.10+) | [cameronchurchwell/MothPriest](https://github.com/cameronchurchwell/MothPriest) | 2024 |
| **SkyrimLib (pyesm)** | Python | Rudimentary R/W | [tstavrianos/SkyrimLib](https://github.com/tstavrianos/SkyrimLib) | 2023 |
| **GameStringer** | Python | Read/Write (Modern SE/AE) | [GameStringer/GameStringer](https://github.com/GameStringer) | Active 2024 |

*   **Note:** The legacy `pyesp` (samdeane) and `esplugin-python` (standalone versions) are largely considered abandoned; modern Python projects should look toward extracting the core modules from **Wrye Bash** for production-grade reliability or use the **libloot** bindings for simple read tasks.

### 2. Rust Crates
Rust is currently the preferred language for high-performance Bethesda tooling, often replacing older C++ implementations.

| Crate | Language | Support (Read/Write) | GitHub URL | Last Commit |
| :--- | :--- | :--- | :--- | :--- |
| **esplugin** | Rust | Read (Metadata/Headers) | [Ortham/esplugin](https://github.com/Ortham/esplugin) | June 2024 |
| **esp_extractor** | Rust | Read/Write (High-perf) | [Orcax-1399/esp-string-parser](https://github.com/Orcax-1399/esp-string-parser) | 2024 |
| **esl** | Rust | Full Read/Write | [A1-Triard/esl](https://github.com/A1-Triard/esl) | Late 2024 |
| **project-wormhole**| Rust | Read (Low-overhead/Zero-copy)| [project-wormhole/esm](https://github.com/project-wormhole/esm) | 2024 |

*   **Recommendation:** Use **`esplugin`** for stability and metadata (industry standard for LOOT). Use **`esl`** or **`esp_extractor`** if you need "Mutagen-style" modification and writing capabilities.

### 3. JavaScript/TypeScript Parsers
The rise of web-based modding tools and Electron-based installers has led to several modern TS implementations.

| Library | Language | Support (Read/Write) | GitHub URL | Last Commit |
| :--- | :--- | :--- | :--- | :--- |
| ~~**tes-data**~~ | TS/JS | — | ~~shmup/tes-data~~ | ❌ **GitHub 404, hallucinated** |
| **SkyrimLib (JS)** | TS/JS | Basic (Experimental Write) | [tstavrianos/SkyrimLib](https://github.com/tstavrianos/SkyrimLib) | 2023 |
| ~~**skyrim-cell-dump**~~ | JS | — | ~~hallada/skyrim-cell-dump~~ | ❌ **GitHub 404, hallucinated** |

*   **Note:** `tes-data` and `skyrim-cell-dump` confirmed 404 — hallucinated repos. JS/TS ESP tooling remains thin; `SkyrimLib` is the only verified option (lightweight).

### 4. Language-Agnostic Format Documentation
For developers building their own parsers, these are the definitive references:

*   **UESP Wiki (Skyrim Mod File Format):** The gold standard for field-by-field breakdowns of every record type.
*   **xEdit Source (wbDefinitions):** The most accurate "living" definition of the format. Look for the Pascal `.pas` files in the [SSEEdit GitHub](https://github.com/TES5Edit/TES5Edit) that define record structures.
*   **Header Version 1.71 Changes:** In 2024, ensure your parser supports the 1.71 header version (Anniversary Edition 1.6.1130+), which expanded the ESL record limit from 2,048 to **4,096** and updated the FormID mapping for the "Creations" menu.
