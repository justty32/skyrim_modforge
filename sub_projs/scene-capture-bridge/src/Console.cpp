#include "Console.h"

#include "Captures.h"
#include "Editor.h"
#include "Eraser.h"
#include "Markers.h"
#include "Modes.h"
#include "log.h"

#include <algorithm>
#include <cctype>
#include <string>

namespace {
    // Inert-in-retail debug commands, tried in order. ClearAchievement is the
    // community's usual donor; the others are dev-tracking commands with no
    // retail behaviour. Wrong guesses are harmless: LocateConsoleCommand just
    // returns null and the next candidate is tried.
    constexpr const char* kDonors[] = {
        "ClearAchievement",
        "StartTrackPlayerDoors",
        "CheckMemory",
    };

    std::string Lower(const char* s) {
        std::string out = s ? s : "";
        std::transform(out.begin(), out.end(), out.begin(),
            [](unsigned char c) { return static_cast<char>(std::tolower(c)); });
        return out;
    }

    void Print(const char* fmt, auto&&... args) {
        if (auto* log = RE::ConsoleLog::GetSingleton())
            log->Print(fmt, std::forward<decltype(args)>(args)...);
    }

    void PrintUsage() {
        Print("SCB mode: %s", Modes::Name(Modes::Current()));
        Print("  sc mk | del | pk | pl | ed | cap | off  switch mode");
        Print("  sc mk dp0 / dp1                    hide / show marker gems");
        Print("  sc del|pk|ed|cap er0 / er1         aim by crosshair / ray");
        Print("  sc ed ax / sc ed                  enter rotate sub-mode / back to move");
        Print("  sc delc                           erase the console-selected ref");
        Print("  sc capc [Label]                   capture the console-selected ref (item or NPC)");
        Print("  sc capp [Label]                   capture the PLAYER (face/stats/perks/gear)");
        Print("  sc cap  -> capture mode: aim at item/NPC, press the action key");
    }

    // The `sc` parser lower-cases its args (mode words are case-insensitive), but an identity
    // LABEL must keep the case the player typed — it becomes the record's editorId. So the
    // label always comes from the RAW param, never from `a2`.
    std::string Trim(const char* s) {
        std::string out = s ? s : "";
        const auto b = out.find_first_not_of(" \t\"");
        if (b == std::string::npos) return {};
        const auto e = out.find_last_not_of(" \t\"");
        return out.substr(b, e - b + 1);
    }

    // Map a tool word ("del"/"pk"/"ed"/...) to its mode, or kTotal if none.
    Modes::Mode ModeOf(const std::string& word) {
        for (auto m : {Modes::Mode::kOff, Modes::Mode::kMarker, Modes::Mode::kDelete,
                 Modes::Mode::kPick, Modes::Mode::kPlace, Modes::Mode::kEdit,
                 Modes::Mode::kCapture})
            if (word == Modes::Cmd(m)) return m;
        return Modes::Mode::kTotal;
    }

    bool Execute(const RE::SCRIPT_PARAMETER* a_paramInfo,
        RE::SCRIPT_FUNCTION::ScriptData* a_scriptData, RE::TESObjectREFR* a_thisObj,
        RE::TESObjectREFR* a_containingObj, RE::Script* a_scriptObj,
        RE::ScriptLocals* a_locals, double& a_result, std::uint32_t& a_opcodeOffsetPtr)
    {
        char raw1[128]{};
        char raw2[128]{};
        RE::Script::ParseParameters(a_paramInfo, a_scriptData, a_opcodeOffsetPtr,
            a_thisObj, a_containingObj, a_scriptObj, a_locals, raw1, raw2);
        const std::string a1 = Lower(raw1);
        const std::string a2 = Lower(raw2);
        const std::string label = Trim(raw2);  // identity label — case PRESERVED (see Trim)
        a_result = 1.0;

        if (a1.empty()) {
            PrintUsage();
            return true;
        }

        // Single-word tool command: erase the console's currently selected ref
        // (click an object in the console, then `sc delc`). Objects only.
        if (a1 == "delc") {
            switch (Eraser::MarkConsoleRef()) {
            case Eraser::MarkResult::kMarked:      Print("SCB: erased console ref"); break;
            case Eraser::MarkResult::kOwnDeleted:  Print("SCB: deleted your ref (no trace)"); break;
            case Eraser::MarkResult::kDuplicate:   Print("SCB: already marked"); break;
            case Eraser::MarkResult::kMarkerProxy: Print("SCB: that's a marker gem"); break;
            default: Print("SCB: no console ref selected (or it's an actor)"); break;
            }
            return true;
        }

        // Capture the console-selected ref — the aim-free eyedropper (items AND
        // actors; the capture-mode crosshair/ray path can't reach an NPC that's
        // easier to click than to aim at). Optional label → the entry's editorId.
        if (a1 == "capc") {
            switch (Captures::CaptureConsoleRef(label)) {
            case Captures::Result::kCaptured:
                Print("SCB: captured console ref%s", label.empty() ? "" : (" as '" + label + "'").c_str());
                break;
            case Captures::Result::kNotItem:     Print("SCB: not capturable (weapon/armour/potion/ingredient/NPC)"); break;
            case Captures::Result::kMarkerProxy: Print("SCB: that's a marker gem"); break;
            default: Print("SCB: no console ref selected (or nothing to capture)"); break;
            }
            return true;
        }

        // Capture THE PLAYER. The engine keeps the player's chargen on its base TESNPC
        // (0x7), so the DLL reads the same record it reads for any NPC — no PROTEUS clone
        // in the middle (which reported level 1 / 50-50-50 and wrote no tints). Perks come
        // from PlayerCharacter's runtime addedPerks; H/M/S + the 18 skills from the PERMANENT
        // actor values (GetPermanentActorValue — base+permanent, excludes buffs; see Captures.cpp),
        // exported explicitly so ModForge writes DNAM instead of autocalc.
        if (a1 == "capp") {
            switch (Captures::CapturePlayer(label)) {
            case Captures::Result::kCaptured:
                Print("SCB: captured the player%s", label.empty() ? "" : (" as '" + label + "'").c_str());
                break;
            default: Print("SCB: could not capture the player (see the log)"); break;
            }
            return true;
        }

        // Second layer: `sc <tool> <arg>`.
        if (!a2.empty()) {
            if (a1 == "mk") {  // sc mk dp0/dp1
                if (a2 == "dp0" || a2 == "dp1") {
                    const bool show = (a2 == "dp1");
                    Markers::SetProxiesVisible(show);
                    Print("SCB: marker gems %s", show ? "shown" : "hidden");
                } else {
                    Print("SCB: unknown mk arg '%s' (dp0 | dp1)", a2.c_str());
                }
                return true;
            }
            const Modes::Mode m = ModeOf(a1);
            if (m == Modes::Mode::kDelete || m == Modes::Mode::kPick ||
                m == Modes::Mode::kEdit || m == Modes::Mode::kCapture) {
                if (a2 == "er0" || a2 == "er1") {  // aim source
                    Modes::SetUseRay(m, a2 == "er1");
                    Print("SCB: %s aim -> %s", Modes::Name(m), a2 == "er1" ? "ray" : "crosshair");
                    return true;
                }
                if (m == Modes::Mode::kEdit && a2 == "ax") {  // enter rotate sub-mode
                    Editor::SetRotateMode(true);
                    Print("SCB: edit ROTATE mode (4/6 yaw, 1/3 pitch, 7/9 roll; "
                        "5/2/8 revert that axis) — `sc ed` to go back to move mode");
                    return true;
                }
            }
            Print("SCB: unknown arg '%s' for '%s'", a2.c_str(), a1.c_str());
            return true;
        }

        // Bare mode switch. Entering edit mode also drops the rotate sub-mode,
        // so `sc ed` is the way back from `sc ed ax`.
        const Modes::Mode m = ModeOf(a1);
        if (m != Modes::Mode::kTotal) {
            Modes::Set(m);
            if (m == Modes::Mode::kEdit) Editor::SetRotateMode(false);
            Print("SCB mode: %s", Modes::Name(m));
            return true;
        }
        Print("SCB: unknown command '%s'", a1.c_str());
        PrintUsage();
        return true;
    }
}

namespace Console {

    void Install() {
        for (const auto* donor : kDonors) {
            auto* cmd = RE::SCRIPT_FUNCTION::LocateConsoleCommand(donor);
            if (!cmd) continue;

            // Two optional string params: "sc" alone prints usage; "sc mk";
            // "sc mk dp0". The array must outlive the table entry -> static.
            static RE::SCRIPT_PARAMETER params[] = {
                {"String", RE::SCRIPT_PARAM_TYPE::kChar, true},
                {"String", RE::SCRIPT_PARAM_TYPE::kChar, true},
            };
            cmd->functionName = "sc";
            cmd->shortName = "sc";
            cmd->helpString = "SceneCaptureBridge: sc mk|del|pk|pl|ed|cap|off, sc delc|capc [Label]|capp [Label], sc mk dp0|dp1";
            cmd->referenceFunction = false;
            cmd->SetParameters(params);
            cmd->executeFunction = &Execute;
            cmd->conditionFunction = nullptr;
            SKSE::log::info("Console: 'sc' installed (donor command '{}')", donor);
            return;
        }
        SKSE::log::error(
            "Console: no donor console command found — 'sc' NOT installed; "
            "use the panel's Settings page to switch modes");
    }

}  // namespace Console
