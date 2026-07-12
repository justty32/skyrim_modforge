// UI.References — the References page (referrer registry, `sc ref` / `sc refc`).
// Split from UI.cpp per the 300-line convention, shaped like the Markers page:
// newest first, in-place rename, per-row delete.
//
// The one rule this page has to enforce: a LABEL IS A GLOBAL NAME in ModForge
// (build registers it as a resolvable id, so any ref field of the spec can point
// at it). Two rows with the same label would fail validate on the whole spec —
// so a duplicate rename is REFUSED here, in front of the user, not discovered
// four steps later in a build log.

#include "UI.h"

#include "Referrer.h"
#include "SceneExporter.h"

#include "SKSEMenuFramework.h"

#include <cstdio>
#include <string>
#include <unordered_map>

namespace {
    struct RowBufs {
        char label[64];
        char note[256];
        bool clash = false;  // last rename attempt hit a duplicate label
    };
    std::unordered_map<std::uint32_t, RowBufs> g_rows;
    bool g_thisCellOnly = false;

    void Apply(const Referrer::Entry& e, RowBufs& b) {
        b.clash = !::Referrer::Rename(e.seq, b.label);
        ::Referrer::SetNote(e.seq, b.note);
    }
}

void __stdcall UI::ReferencesPage::Render() {
    UI::ModeLine();
    constexpr ImGuiMCP::ImVec4 kWarn{1.f, 0.55f, 0.25f, 1.f};
    constexpr ImGuiMCP::ImVec4 kOurs{0.55f, 0.85f, 0.55f, 1.f};

    auto& all = ::Referrer::All();
    ImGuiMCP::TextWrapped(
        "%zu reference(s). Referrer mode (sc ref): the action key NAMES the ref you "
        "are aiming at — nothing in the world changes. `sc ref <Label>` names it and "
        "labels it in one go; `sc refc [Label]` uses the console selection instead. "
        "The label becomes a name ModForge can point at from any ref field of the "
        "spec (a package's sandbox location, an alias, a linked ref...).",
        all.size());
    ImGuiMCP::TextWrapped(
        "Labels must be UNIQUE — the label IS the name. A ref YOU placed (green) is "
        "exported as an in-file dependency: its placement gets a stable editorId and "
        "ModForge makes it persistent. A vanilla/mod ref is exported by its durable "
        "id, and ModForge warns if it is a temporary ref.");
    ImGuiMCP::SameLine();
    ImGuiMCP::Checkbox("this cell only", &g_thisCellOnly);
    ImGuiMCP::Separator();

    const std::string here = g_thisCellOnly ? SceneExporter::AnchorOf(nullptr).id : "";

    std::uint32_t removeSeq = 0;
    // Newest first — the one you just marked is the one you want to name.
    for (auto it = all.rbegin(); it != all.rend(); ++it) {
        auto& e = *it;
        if (g_thisCellOnly && e.cellOrWs != here) continue;
        ImGuiMCP::PushID(reinterpret_cast<const void*>(static_cast<std::uintptr_t>(e.seq)));

        auto [row, inserted] = g_rows.try_emplace(e.seq);
        if (inserted) {
            std::snprintf(row->second.label, sizeof(row->second.label), "%s", e.label.c_str());
            std::snprintf(row->second.note, sizeof(row->second.note), "%s", e.note.c_str());
        }
        auto& b = row->second;

        ImGuiMCP::Text("#%u", e.seq);
        ImGuiMCP::SameLine();
        ImGuiMCP::SetNextItemWidth(180.f);
        if (ImGuiMCP::InputText("##label", b.label, sizeof(b.label),
                ImGuiMCP::ImGuiInputTextFlags_EnterReturnsTrue)) {
            Apply(e, b);
        }
        ImGuiMCP::SameLine();
        ImGuiMCP::SetNextItemWidth(220.f);
        if (ImGuiMCP::InputText("##note", b.note, sizeof(b.note),
                ImGuiMCP::ImGuiInputTextFlags_EnterReturnsTrue)) {
            Apply(e, b);
        }
        ImGuiMCP::SameLine();
        if (ImGuiMCP::Button("apply")) Apply(e, b);
        ImGuiMCP::SameLine();
        if (ImGuiMCP::Button("del")) removeSeq = e.seq;  // registry row only; the world is untouched

        if (b.clash) {
            ImGuiMCP::TextColored(kWarn,
                "  label already used by another reference — labels are unique names; not renamed");
        }

        // What the row actually points at. In-file = one of our own placements (no
        // durable id exists), exported as `editorId` — show the very editorId the
        // exporter will write, so it can be matched against the json by eye.
        if (e.id.empty()) {
            const auto ed = ::Referrer::EditorIdOf(e);
            const bool lost = !e.handle.get();
            ImGuiMCP::TextColored(lost ? kWarn : kOurs,
                "  ours -> %s%s  %s  base %s  (%.0f, %.0f, %.0f)  %s", ed.c_str(),
                lost ? "  [TARGET LOST — not exportable; re-place it and re-mark]" : "",
                e.name.c_str(), e.base.empty() ? "?" : e.base.c_str(),
                e.position.x, e.position.y, e.position.z,
                e.cellOrWs.empty() ? "(unresolved)" : e.cellOrWs.c_str());
        } else {
            ImGuiMCP::Text("  %s  %s  base %s  (%.0f, %.0f, %.0f)  %s", e.id.c_str(),
                e.name.c_str(), e.base.empty() ? "?" : e.base.c_str(),
                e.position.x, e.position.y, e.position.z,
                e.cellOrWs.empty() ? "(unresolved)" : e.cellOrWs.c_str());
        }

        ImGuiMCP::PopID();
    }
    if (removeSeq != 0) {
        ::Referrer::Remove(removeSeq);
        g_rows.erase(removeSeq);
    }
}
