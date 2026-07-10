#include "UI.h"

#include "SceneExporter.h"
#include "log.h"

#include "SKSEMenuFramework.h"

namespace {
    // Resolve the cell the player is standing in, for display. Cheap enough to
    // do per frame: it is a couple of pointer hops plus a formatted string.
    std::string CurrentCellLabel() {
        auto* player = RE::PlayerCharacter::GetSingleton();
        RE::TESObjectCELL* cell = player ? player->GetParentCell() : nullptr;
        if (!cell) {
            return "(no cell)";
        }
        if (cell->IsInteriorCell()) {
            if (auto id = SceneExporter::ResolveDurableId(cell)) {
                return "interior " + *id;
            }
            return "interior (unresolvable)";
        }
        if (auto* ws = cell->GetRuntimeData().worldSpace) {
            if (auto id = SceneExporter::ResolveDurableId(ws)) {
                return "exterior " + *id;
            }
        }
        return "exterior (unresolvable)";
    }
}

void UI::Register() {
    if (!SKSEMenuFramework::IsInstalled()) {
        SKSE::log::info("SKSE Menu Framework not present — hotkey only, no panel");
        return;
    }
    SKSEMenuFramework::SetSection("Scene Capture Bridge");
    SKSEMenuFramework::AddSectionItem("Export", Export::Render);
    SKSE::log::info("SKSE Menu Framework panel registered");
}

void __stdcall UI::Export::Render() {
    const auto label = CurrentCellLabel();
    ImGuiMCP::Text("Cell: %s", label.c_str());
    ImGuiMCP::Separator();

    if (ImGuiMCP::Button("Export player cell")) {
        SceneExporter::ExportPlayerCellToFile();
    }
    ImGuiMCP::SameLine();
    ImGuiMCP::Text("(or press F10)");

    ImGuiMCP::Separator();

    const auto& s = SceneExporter::LastExport();
    if (!s.valid) {
        ImGuiMCP::TextWrapped("No export yet this session.");
        return;
    }

    ImGuiMCP::Text("Last export — %s", s.cell.c_str());
    ImGuiMCP::BulletText("%zu placements (%zu actors)", s.placements, s.actors);
    // The number that proves the vanilla diff: authored refs are recognised and
    // skipped, so `build` does not re-place the whole room on top of itself.
    ImGuiMCP::BulletText("%zu pre-existing (skipped)", s.preexisting);
    if (s.skipped) {
        // A dynamic base cannot be named in an esp, so its ref cannot be
        // exported at all — worth surfacing rather than silently dropping.
        ImGuiMCP::BulletText("%zu skipped (dynamic base, not exportable)", s.skipped);
    }
    if (!s.path.empty()) {
        ImGuiMCP::TextWrapped("Wrote %s", s.path.c_str());
    }
}
