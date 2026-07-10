#pragma once

// SceneExporter — the "collection bridge" core (Idea #24, component ③).
//
// Walks a target cell's placed references, reads each ref's base + world
// transform + enable state, resolves every runtime FormID back to a durable
// "<plugin>:0xLOCALID" string, and serialises the result into scene.json —
// the contract consumed by ModForge (`dotnet run -- build scene.json`).
//
// Contract authority: workflows/specs/ingame-scene-export-design.md §契約.
// This module owns only the OUTPUT shape; ModForge owns the generation.

#include <filesystem>
#include <optional>
#include <string>

namespace SceneExporter {

    // Resolve a runtime form to a durable, load-order-independent id of the
    // form "<plugin>:0xLOCALID" (e.g. "Skyrim.esm:0x0001A26F").
    //
    // Runtime FormIDs embed the load-order index in the high byte(s), which
    // shifts between launches — so it MUST be stripped before export. The
    // owning file comes from the form's defining plugin; the local id is the
    // plugin-relative portion (masked per full/light plugin width).
    //
    // Returns std::nullopt for forms with no owning file (e.g. dynamically
    // created runtime-only forms), which cannot be referenced from an esp.
    [[nodiscard]] std::optional<std::string> ResolveDurableId(const RE::TESForm* form);

    // What the last ExportCell saw. The log line is not enough once a UI wants
    // to show the same numbers — and the pre-existing count is the one that
    // tells you the vanilla diff is working.
    struct Stats {
        bool valid = false;           // false until the first export
        std::size_t placements = 0;   // player-placed refs emitted
        std::size_t actors = 0;       // subset of `placements` that are ACHR
        std::size_t preexisting = 0;  // authored refs skipped (the vanilla diff)
        std::size_t skipped = 0;      // dynamic bases, not esp-referenceable
        std::string cell;             // durable id of the exported cell/worldspace
        std::string path;             // where the last WriteSceneFile went
    };
    [[nodiscard]] const Stats& LastExport();

    // Build a scene.json object for one cell: iterates placed refs, emits the
    // `placements[]` segment plus each placement's `cell`/`worldspace`
    // attribution. Semantic-marker / role / removal segments (§B/§D/§E) are
    // layered in by the in-game editor UI, not by a raw cell sweep — this is
    // the M4 "spike" surface (walk cell → placements → scene.json → ModForge).
    [[nodiscard]] nlohmann::json ExportCell(RE::TESObjectCELL* cell);

    // Convenience: export the cell the player is currently in.
    [[nodiscard]] nlohmann::json ExportPlayerCell();

    // Serialise a scene.json object to disk (pretty-printed, 2-space indent).
    // Returns true on success. Default target: SKSE/Plugins/SceneCaptureBridge/.
    bool WriteSceneFile(const nlohmann::json& scene, const std::filesystem::path& path);

    // Full one-shot: export the player's cell and write it next to the log
    // dir as scene-export.json. Wired to a hotkey / console once UI lands.
    void ExportPlayerCellToFile();

}  // namespace SceneExporter
