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
        Print("  sc mk | del | pk | pl | ed | off   switch mode");
        Print("  sc mk dp0 / dp1                    hide / show marker gems");
        Print("  sc del|pk|ed er0 / er1             aim by crosshair / ray");
        Print("  sc ed ax / sc ed                  enter rotate sub-mode / back to move");
        Print("  sc delc                           erase the console-selected ref");
        Print("  sc cap / sc cap r                 capture item enchant/effects (crosshair / ray)");
    }

    // Map a tool word ("del"/"pk"/"ed"/...) to its mode, or kTotal if none.
    Modes::Mode ModeOf(const std::string& word) {
        for (auto m : {Modes::Mode::kOff, Modes::Mode::kMarker, Modes::Mode::kDelete,
                 Modes::Mode::kPick, Modes::Mode::kPlace, Modes::Mode::kEdit})
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

        // Capture the aimed item's enchantment/effects into capturedItems[].
        // `sc cap` = crosshair, `sc cap r` = look-ray (statics/trees).
        if (a1 == "cap") {
            const bool ray = (a2 == "r");
            const auto r = ray ? Captures::CaptureByRay() : Captures::CaptureCrosshair();
            switch (r) {
            case Captures::Result::kCaptured:   Print("SCB: captured (item enchant/effects or NPC snapshot)"); break;
            case Captures::Result::kNotItem:    Print("SCB: no enchant/effects to capture there"); break;
            case Captures::Result::kMarkerProxy:Print("SCB: that's a marker gem"); break;
            default: Print("SCB: nothing under the %s", ray ? "ray" : "crosshair"); break;
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
            if (m == Modes::Mode::kDelete || m == Modes::Mode::kPick || m == Modes::Mode::kEdit) {
                if (a2 == "er0" || a2 == "er1") {  // aim source
                    Modes::SetUseRay(m, a2 == "er1");
                    Print("SCB: %s aim -> %s", Modes::Name(m), a2 == "er1" ? "ray" : "crosshair");
                    return true;
                }
                if (m == Modes::Mode::kEdit && a2 == "ax") {  // enter rotate sub-mode
                    Editor::SetRotateMode(true);
                    Print("SCB: edit ROTATE mode (4/6 yaw, 1/3 pitch, 7/9 roll, "
                        "8/2 reset) — `sc ed` to go back to move mode");
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
            cmd->helpString = "SceneCaptureBridge: sc mk|del|pk|pl|ed|off, sc mk dp0|dp1";
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
