#include "UI.Fields.h"

#include "SKSEMenuFramework.h"

#include <cstdio>
#include <unordered_map>
#include <vector>

namespace {
    // Cross-frame buffers, keyed by (slot, row). Nothing is ever erased from
    // here: a stale buffer costs a few dozen bytes and re-seeds itself the
    // moment its row reappears (RULE 1), whereas an erase-on-delete protocol is
    // precisely what the six pages kept getting wrong.
    std::unordered_map<std::uint64_t, std::vector<char>> g_bufs;

    // The one field being typed in, or 0. ImGui has a single active item, so
    // this is an id and not a set — everything else mirrors the registry.
    std::uint64_t g_active = 0;

    std::uint64_t Fnv1a(const char* p, std::size_t n) {
        std::uint64_t h = 1469598103934665603ull;
        for (std::size_t i = 0; i < n; ++i) {
            h ^= static_cast<std::uint8_t>(p[i]);
            h *= 1099511628211ull;
        }
        return h;
    }

    std::uint64_t KeyOf(const char* slot, std::uint64_t row) {
        std::uint64_t h = Fnv1a(slot, std::char_traits<char>::length(slot));
        h ^= row + 0x9E3779B97F4A7C15ull + (h << 6) + (h >> 2);
        return h ? h : 1;  // 0 is the "nothing is active" sentinel
    }

    // RULE 1 — mirror the registry unless this is the field under the cursor.
    char* Buffer(std::uint64_t key, const std::string& value, std::size_t cap) {
        auto& buf = g_bufs[key];
        if (buf.size() != cap) buf.assign(cap, '\0');
        if (key != g_active) std::snprintf(buf.data(), cap, "%s", value.c_str());
        return buf.data();
    }

    // RULE 2 — Enter, or leaving a field you changed. Called straight after the
    // widget, so the IsItem* queries still refer to it. Also refreshes which
    // field is active, which is what licenses the re-seed above.
    bool Committed(std::uint64_t key, bool enterPressed) {
        if (ImGuiMCP::IsItemActive()) g_active = key;
        else if (g_active == key) g_active = 0;
        return enterPressed || ImGuiMCP::IsItemDeactivatedAfterEdit();
    }
}

namespace UI {

    bool BoundText(const char* slot, std::uint64_t row, const std::string& value,
                   std::size_t cap, float width, std::string& out) {
        const auto key = KeyOf(slot, row);
        char* buf = Buffer(key, value, cap);
        ImGuiMCP::SetNextItemWidth(width);
        const bool enter = ImGuiMCP::InputText(slot, buf, cap,
            ImGuiMCP::ImGuiInputTextFlags_EnterReturnsTrue);
        if (!Committed(key, enter)) return false;
        out = buf;
        return true;
    }

    std::uint64_t RowKey(const std::string& id) {
        return Fnv1a(id.data(), id.size());
    }

    std::string Shown(const char* slot, std::uint64_t row) {
        const auto it = g_bufs.find(KeyOf(slot, row));
        return it == g_bufs.end() ? std::string{} : std::string(it->second.data());
    }

    void ForgetEdits() {
        g_bufs.clear();
        // Clearing the latch is the load-bearing half: it is what makes the
        // still-focused field re-seed from its (now different) entry before its
        // pending deactivate-commit fires, so that commit writes the row's own
        // value back to itself instead of the typing meant for the old row.
        g_active = 0;
    }

}  // namespace UI
