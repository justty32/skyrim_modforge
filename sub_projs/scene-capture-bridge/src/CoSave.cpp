#include "CoSave.h"

#include "Editor.h"
#include "Eraser.h"
#include "Markers.h"
#include "Modes.h"
#include "Overrides.h"
#include "log.h"

#include <algorithm>

namespace {
    constexpr std::uint32_t kUID = 'SCBR';
    constexpr std::uint32_t kSett = 'SETT';
    constexpr std::uint32_t kMkrs = 'MKRS';
    constexpr std::uint32_t kErsr = 'ERSR';
    constexpr std::uint32_t kOvrd = 'OVRD';

    // Per-record versions (an older save's record is read with its own layout).
    constexpr std::uint32_t kVerSett = 3;  // v2 adds editor step sizes; v3 adds aim/axis
    constexpr std::uint32_t kVerMkrs = 1;
    constexpr std::uint32_t kVerErsr = 2;  // v2 adds name + position for panel rows
    constexpr std::uint32_t kVerOvrd = 1;

    // ---- primitives -------------------------------------------------------

    void WriteStr(const SKSE::SerializationInterface* si, const std::string& s) {
        const auto len = static_cast<std::uint16_t>(std::min<std::size_t>(s.size(), 0xFFFF));
        si->WriteRecordData(len);
        if (len) si->WriteRecordData(s.data(), len);
    }

    std::string ReadStr(const SKSE::SerializationInterface* si) {
        std::uint16_t len = 0;
        if (!si->ReadRecordData(len) || !len) return {};
        std::string s(len, '\0');
        si->ReadRecordData(s.data(), len);
        return s;
    }

    std::uint32_t FormIdOf(const RE::ObjectRefHandle& h) {
        auto ref = h.get();
        return ref ? ref->GetFormID() : 0;
    }

    RE::ObjectRefHandle ResolveHandle(const SKSE::SerializationInterface* si,
                                      std::uint32_t oldId) {
        RE::FormID newId = 0;
        if (!oldId || !si->ResolveFormID(oldId, newId)) return {};
        auto* ref = RE::TESForm::LookupByID<RE::TESObjectREFR>(newId);
        return ref ? ref->GetHandle() : RE::ObjectRefHandle{};
    }

    void WriteVec3(const SKSE::SerializationInterface* si, const RE::NiPoint3& v) {
        si->WriteRecordData(v.x); si->WriteRecordData(v.y); si->WriteRecordData(v.z);
    }
    void ReadVec3(const SKSE::SerializationInterface* si, RE::NiPoint3& v) {
        si->ReadRecordData(v.x); si->ReadRecordData(v.y); si->ReadRecordData(v.z);
    }

    // ---- per-record save/load ---------------------------------------------

    void SaveSettings(const SKSE::SerializationInterface* si) {
        si->WriteRecordData(static_cast<std::uint8_t>(Modes::Current()));
        si->WriteRecordData(static_cast<std::uint8_t>(Markers::ProxiesVisible() ? 1 : 0));
        for (auto m : {Modes::Mode::kMarker, Modes::Mode::kDelete, Modes::Mode::kPick,
                 Modes::Mode::kPlace, Modes::Mode::kEdit})
            si->WriteRecordData(Modes::Bind(m));
        si->WriteRecordData(Editor::MoveStep());   // v2
        si->WriteRecordData(Editor::YawStep());    // v2
        si->WriteRecordData(Editor::ScaleStep());  // v2
        for (auto m : {Modes::Mode::kDelete, Modes::Mode::kPick, Modes::Mode::kEdit})
            si->WriteRecordData(static_cast<std::uint8_t>(Modes::UseRay(m) ? 1 : 0));  // v3
        si->WriteRecordData(static_cast<std::uint8_t>(Editor::RotAxis()));  // v3
    }

    void LoadSettings(const SKSE::SerializationInterface* si, std::uint32_t version) {
        std::uint8_t mode = 0, display = 1;
        si->ReadRecordData(mode);
        si->ReadRecordData(display);
        for (auto m : {Modes::Mode::kMarker, Modes::Mode::kDelete, Modes::Mode::kPick,
                 Modes::Mode::kPlace, Modes::Mode::kEdit}) {
            std::uint32_t bind = 0;
            si->ReadRecordData(bind);
            // Keybind rebinding is hidden pending a fix — read the byte to keep
            // the stream aligned, but ignore it so binds stay at the F11
            // default (a stored bad bind from the buggy UI can't stick).
        }
        if (version >= 2) {
            float mv = 0.f, yaw = 0.f, sc = 0.f;
            si->ReadRecordData(mv);
            si->ReadRecordData(yaw);
            si->ReadRecordData(sc);
            Editor::SetMoveStep(mv);
            Editor::SetYawStep(yaw);
            Editor::SetScaleStep(sc);
        }
        if (version >= 3) {
            for (auto m : {Modes::Mode::kDelete, Modes::Mode::kPick, Modes::Mode::kEdit}) {
                std::uint8_t ray = 0;
                si->ReadRecordData(ray);
                Modes::SetUseRay(m, ray != 0);
            }
            std::uint8_t axis = 0;
            si->ReadRecordData(axis);
            Editor::SetRotAxis(axis);
        }
        if (mode < static_cast<std::uint8_t>(Modes::Mode::kTotal))
            Modes::Set(static_cast<Modes::Mode>(mode));
        // Registry is still empty here (MKRS is read after SETT — write
        // order): this just records the flag; OnRegistryRestored applies it.
        Markers::SetProxiesVisible(display != 0);
    }

    void SaveMarkers(const SKSE::SerializationInterface* si) {
        const auto& all = Markers::All();
        si->WriteRecordData(static_cast<std::uint32_t>(all.size()));
        for (const auto& e : all) {
            si->WriteRecordData(e.seq);
            WriteStr(si, e.label);
            WriteStr(si, e.kind);
            WriteStr(si, e.note);
            WriteVec3(si, e.position);
            si->WriteRecordData(e.angleZDeg);
            WriteStr(si, e.cellOrWs);
            si->WriteRecordData(static_cast<std::uint8_t>(e.isInterior ? 1 : 0));
            si->WriteRecordData(FormIdOf(e.proxy));
        }
    }

    void LoadMarkers(const SKSE::SerializationInterface* si) {
        std::uint32_t count = 0;
        si->ReadRecordData(count);
        auto& all = Markers::All();
        std::size_t dropped = 0;
        for (std::uint32_t i = 0; i < count; ++i) {
            Markers::Entry e;
            std::uint8_t interior = 0;
            std::uint32_t proxyId = 0;
            si->ReadRecordData(e.seq);
            e.label = ReadStr(si);
            e.kind = ReadStr(si);
            e.note = ReadStr(si);
            ReadVec3(si, e.position);
            si->ReadRecordData(e.angleZDeg);
            e.cellOrWs = ReadStr(si);
            si->ReadRecordData(interior);
            si->ReadRecordData(proxyId);
            e.isInterior = interior != 0;
            e.proxy = ResolveHandle(si, proxyId);
            if (!e.proxy.get()) {
                // Proxy FormID didn't resolve (dynamic refs aren't reliably
                // remapped across a full restart). The gem still exists in the
                // save — hand the note/kind to Markers so the load-time adopt
                // scan can merge them back by position, instead of losing them.
                ++dropped;
                Markers::AddPendingOrphan(e.position, e.label, e.kind, e.note, e.angleZDeg);
                continue;
            }
            all.push_back(std::move(e));
        }
        if (dropped)
            SKSE::log::info("CoSave: dropped {} marker(s) with unresolvable proxies", dropped);
    }

    void SaveEraser(const SKSE::SerializationInterface* si) {
        const auto& all = Eraser::All();
        si->WriteRecordData(static_cast<std::uint32_t>(all.size()));
        for (const auto& e : all) {
            WriteStr(si, e.id);
            WriteStr(si, e.plugin);
            si->WriteRecordData(static_cast<std::uint8_t>(e.addsMaster ? 1 : 0));
            WriteStr(si, e.cellOrWs);
            WriteStr(si, e.name);       // v2
            WriteVec3(si, e.position);  // v2
            si->WriteRecordData(FormIdOf(e.handle));
        }
    }

    void LoadEraser(const SKSE::SerializationInterface* si, std::uint32_t version) {
        std::uint32_t count = 0;
        si->ReadRecordData(count);
        auto& all = Eraser::All();
        for (std::uint32_t i = 0; i < count; ++i) {
            Eraser::Entry e;
            std::uint8_t adds = 0;
            std::uint32_t formId = 0;
            e.id = ReadStr(si);
            e.plugin = ReadStr(si);
            si->ReadRecordData(adds);
            e.cellOrWs = ReadStr(si);
            if (version >= 2) {
                e.name = ReadStr(si);
                ReadVec3(si, e.position);
            }
            si->ReadRecordData(formId);
            e.addsMaster = adds != 0;
            // A dead handle is fine: the durable id is what exports; undo on a
            // not-loaded ref just unmarks (Eraser already words it that way).
            e.handle = ResolveHandle(si, formId);
            all.push_back(std::move(e));
        }
    }

    void SaveOverrides(const SKSE::SerializationInterface* si) {
        const auto& all = Overrides::All();
        si->WriteRecordData(static_cast<std::uint32_t>(all.size()));
        for (const auto& e : all) {
            WriteStr(si, e.id);
            WriteStr(si, e.name);
            WriteStr(si, e.plugin);
            si->WriteRecordData(static_cast<std::uint8_t>(e.addsMaster ? 1 : 0));
            si->WriteRecordData(static_cast<std::uint8_t>(e.isActor ? 1 : 0));
            si->WriteRecordData(FormIdOf(e.handle));
            WriteVec3(si, e.origPos); WriteVec3(si, e.origAngle);
            si->WriteRecordData(e.origScale);
            WriteVec3(si, e.pos); WriteVec3(si, e.angle);
            si->WriteRecordData(e.scale);
        }
    }

    void LoadOverrides(const SKSE::SerializationInterface* si) {
        std::uint32_t count = 0;
        si->ReadRecordData(count);
        auto& all = Overrides::All();
        for (std::uint32_t i = 0; i < count; ++i) {
            Overrides::Entry e;
            std::uint8_t adds = 0, actor = 0;
            std::uint32_t formId = 0;
            e.id = ReadStr(si);
            e.name = ReadStr(si);
            e.plugin = ReadStr(si);
            si->ReadRecordData(adds);
            si->ReadRecordData(actor);
            si->ReadRecordData(formId);
            ReadVec3(si, e.origPos); ReadVec3(si, e.origAngle);
            si->ReadRecordData(e.origScale);
            ReadVec3(si, e.pos); ReadVec3(si, e.angle);
            si->ReadRecordData(e.scale);
            e.addsMaster = adds != 0;
            e.isActor = actor != 0;
            e.handle = ResolveHandle(si, formId);  // dead handle kept — id is the payload
            all.push_back(std::move(e));
        }
    }

    // ---- SKSE callbacks ----------------------------------------------------

    void OnSave(SKSE::SerializationInterface* si) {
        if (si->OpenRecord(kSett, kVerSett)) SaveSettings(si);
        if (si->OpenRecord(kMkrs, kVerMkrs)) SaveMarkers(si);
        if (si->OpenRecord(kErsr, kVerErsr)) SaveEraser(si);
        if (si->OpenRecord(kOvrd, kVerOvrd)) SaveOverrides(si);
        SKSE::log::info("CoSave: saved {} marker(s), {} erasure(s), {} override(s)",
            Markers::All().size(), Eraser::All().size(), Overrides::All().size());
    }

    void OnLoad(SKSE::SerializationInterface* si) {
        std::uint32_t type = 0, version = 0, length = 0;
        while (si->GetNextRecordInfo(type, version, length)) {
            switch (type) {
            case kSett: LoadSettings(si, version); break;
            case kMkrs: LoadMarkers(si); break;
            case kErsr: LoadEraser(si, version); break;
            case kOvrd: LoadOverrides(si); break;
            default:
                SKSE::log::warn("CoSave: unknown record 0x{:X} — skipped", type);
                break;
            }
        }
        Markers::OnRegistryRestored();   // seq counter + freeze + display state
        Eraser::OnRegistryRestored();    // rebuild the marked-id set
        SKSE::log::info("CoSave: loaded {} marker(s), {} erasure(s), {} override(s)",
            Markers::All().size(), Eraser::All().size(), Overrides::All().size());
    }

    // Runs before every load AND on new game: wipe registries (no world
    // touches — the incoming save owns the world state) and reset settings so
    // a save without our records starts from defaults.
    void OnRevert(SKSE::SerializationInterface*) {
        Markers::All().clear();
        Markers::ClearPending();  // stale orphan notes from the previous load
        Eraser::DropAll();
        Overrides::DropAll();
        Modes::ResetDefaults();
        Markers::SetProxiesVisible(true);  // registry is empty: flag only
    }
}

namespace CoSave {

    void Register() {
        auto* si = SKSE::GetSerializationInterface();
        if (!si) {
            SKSE::log::error("CoSave: no serialization interface — state will not persist");
            return;
        }
        si->SetUniqueID(kUID);
        si->SetSaveCallback(OnSave);
        si->SetLoadCallback(OnLoad);
        si->SetRevertCallback(OnRevert);
        SKSE::log::info("CoSave: registered (UID 'SCBR')");
    }

}  // namespace CoSave
