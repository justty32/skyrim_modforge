#include "Editor.h"

#include "Markers.h"
#include "SceneExporter.h"
#include "log.h"

#include <cmath>

namespace {
    constexpr float kRadToDeg = 57.2957795f;
    constexpr float kDegToRad = 1.f / kRadToDeg;

    // Numpad DIK scancodes. NOT from the verified F-key block — if a key does
    // nothing in-game, edit mode logs the actual code of every unmapped press,
    // so one session of tapping reveals any wrong constant.
    constexpr std::uint32_t kSelect = 0x4C;   // numpad 5
    constexpr std::uint32_t kCommit = 0x52;   // numpad 0
    constexpr std::uint32_t kCancel = 0x53;   // numpad . (Del)
    constexpr std::uint32_t kFwd = 0x48;      // numpad 8
    constexpr std::uint32_t kBack = 0x50;     // numpad 2
    constexpr std::uint32_t kLeft = 0x4B;     // numpad 4
    constexpr std::uint32_t kRight = 0x4D;    // numpad 6
    constexpr std::uint32_t kYawNeg = 0x47;   // numpad 7
    constexpr std::uint32_t kYawPos = 0x49;   // numpad 9
    constexpr std::uint32_t kDown = 0x4F;     // numpad 1  (deviation: height, not rotation)
    constexpr std::uint32_t kUp = 0x51;       // numpad 3
    constexpr std::uint32_t kScaleUp = 0x4E;  // numpad +
    constexpr std::uint32_t kScaleDn = 0x4A;  // numpad -

    constexpr float kMoveStep = 5.f;     // units per tap
    constexpr float kYawStep = 5.f;      // degrees per tap
    constexpr float kScaleStep = 0.02f;

    // Only these base types get the physics freeze: they are the naturally
    // havok-Dynamic clutter (cups, books, weapons on tables). Restoring a
    // STAT/FURN to kDynamic would knock walls loose, so anything not on this
    // list is left alone entirely.
    bool HavokMovable(RE::TESBoundObject* base) {
        switch (base ? base->GetFormType() : RE::FormType::None) {
        case RE::FormType::MovableStatic:
        case RE::FormType::Misc:
        case RE::FormType::Weapon:
        case RE::FormType::Ammo:
        case RE::FormType::Book:
        case RE::FormType::AlchemyItem:
        case RE::FormType::Ingredient:
        case RE::FormType::SoulGem:
            return true;
        default:
            return false;
        }
    }

    struct State {
        bool active = false;
        RE::ObjectRefHandle handle;
        bool isActor = false;
        bool frozen = false;   // we keyframed it on select; restore on release
        RE::NiPoint3 origPos;
        RE::NiPoint3 origAngle;
        float origScale = 1.f;
    };
    State g;

    RE::NiPointer<RE::TESObjectREFR> Target() { return g.handle.get(); }

    void ReleasePhysics() {
        if (!g.frozen) return;
        if (auto ref = Target()) {
            ref->SetMotionType(RE::hkpMotion::MotionType::kDynamic, true);
            SKSE::log::info("Editor: physics restored — the object will settle");
        }
        g.frozen = false;
    }

    void Apply(RE::TESObjectREFR* ref, const RE::NiPoint3& pos, const RE::NiPoint3& angle) {
        ref->SetPosition(pos);
        ref->SetAngle(angle);
        ref->Update3DPosition(true);  // SetPosition alone can leave the visual behind
    }

    bool TrySelect() {
        auto* pick = RE::CrosshairPickData::GetSingleton();
        RE::NiPointer<RE::TESObjectREFR> ref = pick ? pick->target[0].get() : nullptr;
        if (!ref) {
            SKSE::log::info("Editor: crosshair has no target");
            return false;
        }
        if (Markers::IsProxy(ref.get())) {
            SKSE::log::info("Editor: marker proxies are anchors — not editable");
            return false;
        }
        if (auto id = SceneExporter::ResolveDurableId(ref.get())) {
            // The honest MVP boundary: moving an authored ref needs the
            // overrides[] contract shape, which is not decided yet.
            SKSE::log::info(
                "Editor: {} is an authored ref — editing existing refs awaits "
                "the overrides[] contract (see plan)", *id);
            return false;
        }
        g.active = true;
        g.handle = ref->GetHandle();
        g.isActor = ref->GetFormType() == RE::FormType::ActorCharacter;
        g.origPos = ref->GetPosition();
        g.origAngle = ref->data.angle;
        g.origScale = ref->GetScale();
        // Freeze havok while editing, or physics fights every nudge (細摳③).
        if (HavokMovable(ref->GetBaseObject())) {
            g.frozen = ref->SetMotionType(RE::hkpMotion::MotionType::kKeyframed, false);
            if (g.frozen) SKSE::log::info("Editor: physics frozen while editing");
        }
        SKSE::log::info("Editor: editing dynamic ref at ({:.1f}, {:.1f}, {:.1f}) — "
            "numpad 8/2 fwd/back, 4/6 left/right, 1/3 down/up, 7/9 yaw, +/- scale, "
            "0 commit, . cancel", g.origPos.x, g.origPos.y, g.origPos.z);
        return true;
    }
}

namespace Editor {

    bool Active() { return g.active; }

    bool HandleKey(std::uint32_t code) {
        if (!g.active) {
            if (code != kSelect) return false;
            TrySelect();
            return true;  // consume numpad-5 either way
        }

        auto ref = Target();
        if (!ref) {  // target unloaded under us — bail out cleanly
            SKSE::log::info("Editor: target vanished — edit mode off");
            g = {};
            return true;
        }

        RE::NiPoint3 pos = ref->GetPosition();
        RE::NiPoint3 angle = ref->data.angle;
        // Player-relative horizontal axes: 8 pushes away from the player.
        const float yaw = RE::PlayerCharacter::GetSingleton()->data.angle.z;
        const RE::NiPoint3 fwd{std::sin(yaw), std::cos(yaw), 0.f};
        const RE::NiPoint3 right{std::cos(yaw), -std::sin(yaw), 0.f};

        switch (code) {
        case kSelect:   // re-select something else: commit current first
        case kCommit:
            SKSE::log::info("Editor: committed at ({:.1f}, {:.1f}, {:.1f})",
                pos.x, pos.y, pos.z);
            ReleasePhysics();
            g = {};
            if (code == kSelect) TrySelect();
            return true;
        case kCancel:
            Cancel();
            return true;
        case kFwd:     Apply(ref.get(), pos + fwd * kMoveStep, angle); return true;
        case kBack:    Apply(ref.get(), pos - fwd * kMoveStep, angle); return true;
        case kLeft:    Apply(ref.get(), pos - right * kMoveStep, angle); return true;
        case kRight:   Apply(ref.get(), pos + right * kMoveStep, angle); return true;
        case kUp:      pos.z += kMoveStep; Apply(ref.get(), pos, angle); return true;
        case kDown:    pos.z -= kMoveStep; Apply(ref.get(), pos, angle); return true;
        case kYawPos:  angle.z += kYawStep * kDegToRad; Apply(ref.get(), pos, angle); return true;
        case kYawNeg:  angle.z -= kYawStep * kDegToRad; Apply(ref.get(), pos, angle); return true;
        case kScaleUp:
            if (!g.isActor) { ref->SetScale(ref->GetScale() + kScaleStep); ref->Update3DPosition(true); }
            return true;
        case kScaleDn:
            if (!g.isActor) { ref->SetScale(ref->GetScale() - kScaleStep); ref->Update3DPosition(true); }
            return true;
        default:
            // Self-diagnosis for the unverified numpad DIK constants.
            SKSE::log::info("Editor: unmapped scancode 0x{:X} in edit mode", code);
            return true;  // swallow everything while editing
        }
    }

    void Cancel() {
        if (!g.active) return;
        if (auto ref = Target()) {
            Apply(ref.get(), g.origPos, g.origAngle);
            if (!g.isActor) ref->SetScale(g.origScale);
            SKSE::log::info("Editor: cancelled — transform restored");
        }
        ReleasePhysics();
        g = {};
    }

    Status Current() {
        Status s;
        s.active = g.active;
        if (auto ref = Target(); g.active && ref) {
            const char* dn = ref->GetDisplayFullName();
            s.name = (dn && *dn) ? dn : "(unnamed)";
            s.pos = ref->GetPosition();
            s.yawDeg = ref->data.angle.z * kRadToDeg;
            s.scale = ref->GetScale();
        }
        return s;
    }

}  // namespace Editor
