#include "UI.h"

#include "Markers.h"
#include "SceneExporter.h"
#include "log.h"

#include "SKSEMenuFramework.h"

#include <cstdio>
#include <unordered_map>

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
    SKSEMenuFramework::AddSectionItem("Markers", MarkersPage::Render);
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

namespace {
    // Per-row edit buffers, keyed by marker seq. Initialised from the entry
    // once; afterwards the buffer is the user's in-progress edit.
    struct RowBufs {
        char label[64];
        char kind[24];
    };
    std::unordered_map<std::uint32_t, RowBufs> g_rows;
}

void __stdcall UI::MarkersPage::Render() {
    auto& all = ::Markers::All();
    ImGuiMCP::Text("%zu marker(s). F11 drops one at your feet.", all.size());
    ImGuiMCP::SameLine();
    if (ImGuiMCP::Button("place marker here")) {
        ::Markers::PlaceAtPlayer();   // hotkey-free path — immune to key conflicts
    }
    ImGuiMCP::Separator();

    std::uint32_t removeSeq = 0;
    // Newest first — the one you just placed is the one you want to rename.
    for (auto it = all.rbegin(); it != all.rend(); ++it) {
        auto& e = *it;
        ImGuiMCP::PushID(reinterpret_cast<const void*>(static_cast<std::uintptr_t>(e.seq)));

        auto [row, inserted] = g_rows.try_emplace(e.seq);
        if (inserted) {
            std::snprintf(row->second.label, sizeof(row->second.label), "%s", e.label.c_str());
            std::snprintf(row->second.kind, sizeof(row->second.kind), "%s", e.kind.c_str());
        }
        auto& b = row->second;

        ImGuiMCP::Text("#%u", e.seq);
        ImGuiMCP::SameLine();
        ImGuiMCP::SetNextItemWidth(180.f);
        if (ImGuiMCP::InputText("##label", b.label, sizeof(b.label),
                ImGuiMCP::ImGuiInputTextFlags_EnterReturnsTrue)) {
            ::Markers::Rename(e.seq, b.label);
        }
        ImGuiMCP::SameLine();
        ImGuiMCP::SetNextItemWidth(90.f);
        if (ImGuiMCP::InputText("##kind", b.kind, sizeof(b.kind),
                ImGuiMCP::ImGuiInputTextFlags_EnterReturnsTrue)) {
            ::Markers::SetKind(e.seq, b.kind);
        }
        ImGuiMCP::SameLine();
        if (ImGuiMCP::Button("apply")) {
            ::Markers::Rename(e.seq, b.label);
            ::Markers::SetKind(e.seq, b.kind);
        }
        ImGuiMCP::SameLine();
        if (ImGuiMCP::Button("del")) {
            removeSeq = e.seq;
        }
        ImGuiMCP::SameLine();
        ImGuiMCP::Text("%s", e.cellOrWs.empty() ? "(unresolved)" : e.cellOrWs.c_str());

        ImGuiMCP::PopID();
    }
    if (removeSeq != 0) {
        ::Markers::Remove(removeSeq);
        g_rows.erase(removeSeq);
    }
}
