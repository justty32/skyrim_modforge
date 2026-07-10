#include "UI.h"

#include "Eraser.h"
#include "Palette.h"
#include "Markers.h"
#include "SceneExporter.h"
#include "log.h"

#include "SKSEMenuFramework.h"

#include <array>
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
    SKSEMenuFramework::AddSectionItem("Eraser", EraserPage::Render);
    SKSEMenuFramework::AddSectionItem("Palette", PalettePage::Render);
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
    ImGuiMCP::SameLine();
    if (ImGuiMCP::Button("adopt this cell")) {
        // Recover markers from a previous session: their proxies + display
        // names live in the savegame, only this registry was lost.
        ::Markers::AdoptOrphans();
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

void __stdcall UI::EraserPage::Render() {
    constexpr ImGuiMCP::ImVec4 kWarn{1.f, 0.55f, 0.25f, 1.f};

    auto& marked = ::Eraser::All();
    ImGuiMCP::Text("%zu marked for removal. F8 erases the crosshair target.", marked.size());
    ImGuiMCP::SameLine();
    if (ImGuiMCP::Button("undo")) { ::Eraser::Undo(); }
    ImGuiMCP::SameLine();
    if (ImGuiMCP::Button("clear (re-enable all)")) { ::Eraser::Clear(); }
    ImGuiMCP::Separator();

    for (const auto& e : marked) {
        if (e.addsMaster) {
            ImGuiMCP::TextColored(kWarn, "%s", e.id.c_str());
            ImGuiMCP::SameLine();
            ImGuiMCP::TextColored(kWarn, "-- patch will depend on %s", e.plugin.c_str());
        } else {
            ImGuiMCP::Text("%s", e.id.c_str());
        }
    }

    ImGuiMCP::Separator();
    // Explicit adoption, never inference: the scan only PROPOSES; each row is
    // confirmed by hand, so quest-disabled clutter can't sneak in.
    if (ImGuiMCP::Button("scan disabled refs in this cell")) {
        ::Eraser::ScanDisabled();
    }
    auto& cands = ::Eraser::Candidates();
    if (!cands.empty()) {
        ImGuiMCP::SameLine();
        if (ImGuiMCP::Button("dismiss")) { ::Eraser::DismissCandidates(); }
        ImGuiMCP::TextWrapped(
            "%zu disabled candidate(s) — runtime-disabled, record not "
            "InitiallyDisabled. Adopt only what YOU erased; quest-hidden "
            "clutter looks identical.", cands.size());
        std::size_t adopt = SIZE_MAX;
        for (std::size_t i = 0; i < cands.size(); ++i) {
            const auto& c = cands[i];
            ImGuiMCP::PushID(reinterpret_cast<const void*>(static_cast<std::uintptr_t>(i + 1)));
            if (ImGuiMCP::Button("adopt")) adopt = i;
            ImGuiMCP::SameLine();
            if (c.addsMaster) ImGuiMCP::TextColored(kWarn, "%s  %s", c.id.c_str(), c.name.c_str());
            else              ImGuiMCP::Text("%s  %s", c.id.c_str(), c.name.c_str());
            ImGuiMCP::PopID();
        }
        if (adopt != SIZE_MAX) ::Eraser::AdoptCandidate(adopt);
    }
}

namespace {
    std::unordered_map<std::size_t, std::array<char, 64>> g_slotBufs;
}

void __stdcall UI::PalettePage::Render() {
    constexpr ImGuiMCP::ImVec4 kWarn{1.f, 0.55f, 0.25f, 1.f};

    auto& slots = ::Palette::All();
    ImGuiMCP::Text("%zu slot(s). F6 picks the crosshair target; F7 places the "
                   "selected slot where you aim.", slots.size());
    ImGuiMCP::Separator();

    std::size_t removeIdx = SIZE_MAX;
    for (std::size_t i = 0; i < slots.size(); ++i) {
        auto& s = slots[i];
        ImGuiMCP::PushID(reinterpret_cast<const void*>(static_cast<std::uintptr_t>(i + 1)));

        const bool selected = (i == ::Palette::SelectedIndex());
        if (ImGuiMCP::Button(selected ? "[use]" : " use ")) ::Palette::Select(i);
        ImGuiMCP::SameLine();

        auto [it, inserted] = g_slotBufs.try_emplace(i);
        if (inserted) std::snprintf(it->second.data(), it->second.size(), "%s", s.name.c_str());
        ImGuiMCP::SetNextItemWidth(160.f);
        if (ImGuiMCP::InputText("##slotname", it->second.data(), it->second.size(),
                ImGuiMCP::ImGuiInputTextFlags_EnterReturnsTrue)) {
            ::Palette::Rename(i, it->second.data());
        }
        ImGuiMCP::SameLine();
        if (s.addsMaster) ImGuiMCP::TextColored(kWarn, "%s", s.baseId.c_str());
        else              ImGuiMCP::Text("%s", s.baseId.c_str());
        ImGuiMCP::SameLine();
        if (ImGuiMCP::Button("del")) removeIdx = i;

        ImGuiMCP::PopID();
    }
    if (removeIdx != SIZE_MAX) {
        ::Palette::Remove(removeIdx);
        g_slotBufs.clear();  // indices shifted — rebuild lazily next frame
    }
}
