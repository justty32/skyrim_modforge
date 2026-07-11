#include "Console.h"

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
        if (a1 == "mk" && !a2.empty()) {  // tool subcommand layer: sc mk dp0/dp1
            if (a2 == "dp0" || a2 == "dp1") {
                const bool show = (a2 == "dp1");
                Markers::SetProxiesVisible(show);
                Print("SCB: marker gems %s", show ? "shown" : "hidden");
            } else {
                Print("SCB: unknown mk arg '%s' (dp0 | dp1)", a2.c_str());
            }
            return true;
        }
        for (auto m : {Modes::Mode::kOff, Modes::Mode::kMarker, Modes::Mode::kDelete,
                 Modes::Mode::kPick, Modes::Mode::kPlace, Modes::Mode::kEdit}) {
            if (a1 == Modes::Cmd(m)) {
                Modes::Set(m);
                Print("SCB mode: %s", Modes::Name(m));
                return true;
            }
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
