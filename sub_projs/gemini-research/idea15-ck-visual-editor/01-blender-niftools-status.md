Attempt 1 failed: You have exhausted your capacity on this model. Your quota will reset after 1s.. Retrying after 5791ms...
As of **early 2025**, the Blender Niftools ecosystem for Skyrim Special Edition (SSE) is divided between the "stable legacy" official releases and newer community forks/alternatives. 

### **1. Can it import ESP/ESM placed references (REFR)?**
**No.** The standard Blender Niftools addon (official and forks) is strictly a **mesh/geometry tool** for `.nif` files. It has no native capability to read `.esp`, `.esm`, or `.esl` files to visualize or manipulate world-space placements (REFR records).

*   **Workaround:** To import object placements (REFRs) from a game cell into Blender, you must use **F4RefToBlender** (despite the name, it fully supports SSE). You export a list of references from xEdit (SSEEdit) or the Creation Kit, which the script then uses to "rebuild" the scene in Blender using the appropriate `.nif` files.
    *   **Source:** [F4RefToBlender GitHub](https://github.com/6ooflames/F4RefToBlender)

### **2. Can it export object placement back to ESP?**
**No.** The addon cannot generate or modify plugin files (`.esp`). 
*   **Workflow:** You must manually place your exported `.nif` files in the **Creation Kit (CK)** or define the `REFR` records using **SSEEdit**. 

### **3. Supported Skyrim Record Types (NIF Blocks)**
The addon focuses on NIF blocks, not ESP records. For Skyrim Special Edition, support is as follows:
*   **Reading (Import):**
    *   **`BSTriShape`**: Fully supported (the SSE equivalent of LE's `NiTriShape`).
    *   **`NiNode`**: Fully supported for hierarchy and transforms.
    *   **`BSLightingShaderProperty`**: Partially supported (reads texture paths and basic shader flags).
*   **Writing (Export):**
    *   **Static Meshes:** Fully supported (exports as `BSTriShape`).
    *   **Skinned/Rigged Meshes (Armor/NPCs):** **Incomplete/Experimental.** The official addon often fails to export SSE skinning data correctly. Most modders still use **PyNifly** or the "Export for LE and run through SSE NIF Optimizer" workflow for rigged assets.
    *   **Source:** [Blender Niftools v0.1.1 Release Notes](https://github.com/niftools/blender_niftools_addon/releases/tag/v0.1.1)

### **4. GitHub URLs & Latest Status (2025)**

| Tool | GitHub URL | Latest Release / Status |
| :--- | :--- | :--- |
| **Official Niftools** | [niftools/blender_niftools_addon](https://github.com/niftools/blender_niftools_addon) | **v0.1.1** (Nov 2023). Supports Blender 2.8 - 3.6. **Not compatible with Blender 4.0+.** |
| **DuncanWasHere Fork** | [DuncanWasHere/blender_niftools_addon](https://github.com/DuncanWasHere/blender_niftools_addon) | **Active (2025).** A specialized fork for **Blender 4.0, 4.1, 4.2, and 4.3**. Fixes API breaks. |
| **PyNifly (Top Alt)** | [BadDogSkyrim/PyNifly](https://github.com/BadDogSkyrim/PyNifly) | **Active (2025).** Preferred by many for SSE because it was built specifically for modern formats and handles skinning better. |

### **Summary of Supported vs. Not Supported**
*   **IS Supported:** Importing SSE meshes (`BSTriShape`), exporting static world objects, Blender 3.6 (Official), Blender 4.x (DuncanWasHere fork).
*   **IS NOT Supported:** Native `.esp/.esm` reading, exporting placement/REFR data, native SSE skinning export (in official addon), importing compressed BSA files directly (must extract NIFs first using **BAE**).
