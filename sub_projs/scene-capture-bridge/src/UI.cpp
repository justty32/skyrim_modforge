#include "UI.h"

#include "Editor.h"
#include "Eraser.h"
#include "Markers.h"
#include "Overrides.h"
#include "Palette.h"
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
    SKSEMenuFramework::AddSectionItem("Editor", EditorPage::Render);
    SKSEMenuFramework::AddSectionItem("Settings", SettingsPage::Render);
    MarkerEditor::Init();  // the standalone E-interaction window
    SKSE::log::info("SKSE Menu Framework panel registered");
}

void __stdcall UI::Export::Render() {
    UI::ModeLine();
    const auto label = CurrentCellLabel();
    ImGuiMCP::Text("Cell: %s", label.c_str());

    // Human-readable location + the player's world coords, so the exported
    // anchor can be sanity-checked against where you're actually standing.
    if (auto* player = RE::PlayerCharacter::GetSingleton()) {
        RE::TESObjectCELL* cell = player->GetParentCell();
        if (cell && !cell->IsInteriorCell()) {
            if (auto* ws = cell->GetRuntimeData().worldSpace) {
                const char* wn = ws->GetFullName();
                if (!wn || !*wn) wn = ws->GetFormEditorID();
                ImGuiMCP::Text("World: %s", (wn && *wn) ? wn : "(unnamed)");
            }
        } else if (cell) {
            const char* cn = cell->GetFullName();
            if (cn && *cn) ImGuiMCP::Text("Cell name: %s", cn);
        }
        const auto p = player->GetPosition();
        ImGuiMCP::Text("Player XYZ: (%.1f, %.1f, %.1f)", p.x, p.y, p.z);
    }
    ImGuiMCP::Separator();

    // Export deliberately has no hotkey (user-decided): this button is it.
    if (ImGuiMCP::Button("Export player cell")) {
        SceneExporter::ExportPlayerCellToFile();
    }
    ImGuiMCP::SameLine();
    // Every loaded cell + all registries. Placements in unloaded cells can't be
    // captured (registries — markers/erasures/overrides — are always global).
    if (ImGuiMCP::Button("Export all (loaded cells)")) {
        SceneExporter::ExportAllToFile();
    }

    ImGuiMCP::Separator();

    const auto& s = SceneExporter::LastExport();
    if (!s.valid) {
        ImGuiMCP::TextWrapped("No export yet this session.");
        return;
    }

    ImGuiMCP::Text("Last export — %s", s.cell.c_str());
    // Added / modified / removed each count independently (user-requested).
    ImGuiMCP::BulletText("%zu added (placements, %zu actors)", s.placements, s.actors);
    ImGuiMCP::BulletText("%zu modified (overrides[])", s.overrides);
    ImGuiMCP::BulletText("%zu removed (removals[])", s.removals);
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

// MarkersPage + MarkerEditor live in UI.Markers.cpp (300-line convention).

void __stdcall UI::EraserPage::Render() {
    UI::ModeLine();
    constexpr ImGuiMCP::ImVec4 kWarn{1.f, 0.55f, 0.25f, 1.f};
    static bool thisCellOnly = false;

    auto& marked = ::Eraser::All();
    const std::string here = SceneExporter::AnchorOf(nullptr).id;

    ImGuiMCP::Text("%zu marked for removal. In delete mode (sc del) the action "
                   "key erases the crosshair target.", marked.size());
    // With "this cell only" on, undo pops the last mark made in THIS cell.
    if (ImGuiMCP::Button("undo")) {
        if (thisCellOnly) ::Eraser::UndoInCell(here); else ::Eraser::Undo();
    }
    ImGuiMCP::SameLine();
    if (ImGuiMCP::Button("clear (re-enable all)")) { ::Eraser::Clear(); }
    ImGuiMCP::SameLine();
    // Trees/architecture the crosshair never sees — explicit entry, see Aim.h.
    if (ImGuiMCP::Button("erase by ray")) { ::Eraser::MarkByRay(); }
    ImGuiMCP::SameLine();
    ImGuiMCP::Checkbox("this cell only", &thisCellOnly);
    ImGuiMCP::Separator();

    // Each row: [undo] id  name  (x, y, z). Newest first, matching undo order.
    std::string undoId;
    for (auto it = marked.rbegin(); it != marked.rend(); ++it) {
        const auto& e = *it;
        if (thisCellOnly && e.cellOrWs != here) continue;
        ImGuiMCP::PushID(e.id.c_str());
        if (ImGuiMCP::Button("undo")) undoId = e.id;
        ImGuiMCP::SameLine();
        const auto& col = e.addsMaster ? kWarn : ImGuiMCP::ImVec4{1.f, 1.f, 1.f, 1.f};
        ImGuiMCP::TextColored(col, "%s  %s  (%.0f, %.0f, %.0f)%s",
            e.id.c_str(), e.name.empty() ? "" : e.name.c_str(),
            e.position.x, e.position.y, e.position.z,
            e.addsMaster ? "  -- adds a master" : "");
        ImGuiMCP::PopID();
    }
    if (!undoId.empty()) ::Eraser::UndoEntry(undoId);
}

namespace {
    std::unordered_map<std::size_t, std::array<char, 64>> g_slotBufs;
}

void __stdcall UI::PalettePage::Render() {
    UI::ModeLine();
    constexpr ImGuiMCP::ImVec4 kWarn{1.f, 0.55f, 0.25f, 1.f};

    auto& slots = ::Palette::All();
    ImGuiMCP::Text("%zu slot(s). Pick mode (sc pk) eyedrops the crosshair "
                   "target; place mode (sc pl) spawns the selected slot where "
                   "you aim. Slots persist across saves.", slots.size());
    // Trees/architecture the crosshair never sees — explicit entry, see Aim.h.
    if (ImGuiMCP::Button("pick by ray")) { ::Palette::PickByRay(); }

    // Named palette file (in the SKSE folder): load appends its slots, save
    // writes the current set — share/reuse curated palettes across playthroughs.
    static char fileName[128] = "";
    ImGuiMCP::SetNextItemWidth(260.f);
    ImGuiMCP::InputText("##palfile", fileName, sizeof(fileName));
    ImGuiMCP::SameLine();
    if (ImGuiMCP::Button("load from file")) {
        if (fileName[0]) { ::Palette::LoadFromFile(fileName); g_slotBufs.clear(); }
    }
    ImGuiMCP::SameLine();
    if (ImGuiMCP::Button("save to file")) {
        if (fileName[0]) ::Palette::SaveToFile(fileName);
    }
    ImGuiMCP::SameLine();
    ImGuiMCP::TextWrapped("(SKSE folder; e.g. my-palette.json)");
    ImGuiMCP::Separator();

    std::size_t removeIdx = SIZE_MAX;
    // Newest first — the slot you just eyedropped is the one you want to name.
    for (std::size_t n = 0; n < slots.size(); ++n) {
        const std::size_t i = slots.size() - 1 - n;
        auto& s = slots[i];
        ImGuiMCP::PushID(reinterpret_cast<const void*>(static_cast<std::uintptr_t>(i + 1)));

        const bool selected = (i == ::Palette::SelectedIndex());
        if (ImGuiMCP::Button(selected ? "[use]" : " use ")) ::Palette::Select(i);
        ImGuiMCP::SameLine();

        // The name is freely editable (Bed -> GoodBed); Enter commits + saves.
        auto [it, inserted] = g_slotBufs.try_emplace(i);
        if (inserted) std::snprintf(it->second.data(), it->second.size(), "%s", s.name.c_str());
        ImGuiMCP::SetNextItemWidth(260.f);
        if (ImGuiMCP::InputText("##slotname", it->second.data(), it->second.size(),
                ImGuiMCP::ImGuiInputTextFlags_EnterReturnsTrue)) {
            ::Palette::Rename(i, it->second.data());
        }
        ImGuiMCP::SameLine();
        if (!s.base) {
            // The slot's plugin left the load order — kept, but F7 refuses it.
            ImGuiMCP::TextColored(kWarn, "%s (unavailable)", s.baseId.c_str());
        } else if (s.addsMaster) {
            ImGuiMCP::TextColored(kWarn, "%s", s.baseId.c_str());
        } else {
            ImGuiMCP::Text("%s", s.baseId.c_str());
        }
        ImGuiMCP::SameLine();
        if (ImGuiMCP::Button("del")) removeIdx = i;

        ImGuiMCP::PopID();
    }
    if (removeIdx != SIZE_MAX) {
        ::Palette::Remove(removeIdx);
        g_slotBufs.clear();  // indices shifted — rebuild lazily next frame
    }
}

void __stdcall UI::EditorPage::Render() {
    UI::ModeLine();
    constexpr ImGuiMCP::ImVec4 kWarn{1.f, 0.55f, 0.25f, 1.f};

    const auto st = ::Editor::Current();
    if (!st.active) {
        ImGuiMCP::TextWrapped(
            "Edit mode (sc ed): aim and press the action key to edit the "
            "target (numpad * ray-selects trees/statics the crosshair "
            "misses). Your own refs export their live pose; an authored "
            "(vanilla/mod) ref becomes an overrides[] entry when you commit.");
        if (ImGuiMCP::Button("select by ray")) { ::Editor::SelectByRay(); }
    } else {
        ImGuiMCP::Text("Editing: %s", st.name);
        ImGuiMCP::BulletText("pos (%.1f, %.1f, %.1f)", st.pos.x, st.pos.y, st.pos.z);
        ImGuiMCP::BulletText("yaw %.1f deg   scale %.2f", st.yawDeg, st.scale);
        ImGuiMCP::Separator();
        if (::Editor::RotateMode()) {
            ImGuiMCP::TextWrapped(
                "ROTATE mode (sc ed ax): 4/6 yaw - 1/3 pitch - 7/9 roll - "
                "8/2 reset angle - +/- scale - 5 reset all - 0 commit - . cancel");
        } else {
            ImGuiMCP::TextWrapped(
                "numpad: 8/2 fwd/back - 4/6 left/right - 1/3 down/up - 7/9 yaw - "
                "+/- scale - 5 reset - 0 commit - . cancel  (sc ed ax = rotate mode)");
        }
        if (ImGuiMCP::Button("cancel (restore)")) { ::Editor::Cancel(); }
    }

    // Committed edits of AUTHORED refs — these export as overrides[]. Revert
    // moves the ref back to its pre-edit baseline and unregisters it.
    ImGuiMCP::Separator();
    auto& moved = ::Overrides::All();
    ImGuiMCP::Text("%zu authored ref override(s)", moved.size());
    if (!moved.empty()) {
        ImGuiMCP::SameLine();
        if (ImGuiMCP::Button("revert all")) { ::Overrides::Clear(); }
    }
    std::size_t revert = SIZE_MAX;
    for (std::size_t i = 0; i < moved.size(); ++i) {
        const auto& e = moved[i];
        ImGuiMCP::PushID(reinterpret_cast<const void*>(static_cast<std::uintptr_t>(i + 1)));
        if (ImGuiMCP::Button("revert")) revert = i;
        ImGuiMCP::SameLine();
        const auto& col = e.addsMaster ? kWarn : ImGuiMCP::ImVec4{1.f, 1.f, 1.f, 1.f};
        ImGuiMCP::TextColored(col, "%s  %s  (%.0f, %.0f, %.0f)%s",
            e.id.c_str(), e.name.empty() ? "" : e.name.c_str(),
            e.pos.x, e.pos.y, e.pos.z,
            e.addsMaster ? "  -- adds a master" : "");
        ImGuiMCP::PopID();
    }
    if (revert != SIZE_MAX) ::Overrides::Revert(revert);
}
