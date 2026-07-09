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

    // Build a scene.json object for one cell: iterates placed refs, emits the
    // `placements[]` / `npcRefs[]` segments plus the `cell` / `worldspace`
    // header. Semantic-marker / role / removal segments (§B/§D/§E) are layered
    // in by the in-game editor UI, not by a raw cell sweep — this is the M4
    // "spike" surface (walk cell → placements → scene.json → feed ModForge).
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
