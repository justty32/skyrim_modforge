#include "Editor.h"

#include "Aim.h"
#include "Markers.h"
#include "Overrides.h"
#include "SceneExporter.h"
#include "log.h"

#include <cmath>

namespace {
    constexpr float kRadToDeg = 57.2957795f;
    constexpr float kDegToRad = 1.f / kRadToDeg;

    // Numpad DIK scancodes. NOT from the verified F-key block — if a key does
    // nothing in-game, edit mode logs the actual code of every unmapped press,
    // so one session of tapping reveals any wrong constant.
    constexpr std::uint32_t kSelect = 0x4C;     // numpad 5 — crosshair select
    constexpr std::uint32_t kSelectRay = 0x37;  // numpad * — EXPLICIT ray select (trees/statics)
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

    // Editing step sizes — player-adjustable in the Settings page, persisted
    // in the co-save (SETT v2). Defaults match the original constants.
    float g_moveStep = 5.f;      // units per tap
    float g_yawStep = 5.f;       // degrees per tap
    float g_scaleStep = 0.02f;   // scale factor per tap

    // Pure-rotation sub-mode (`sc ed ax` toggles it). When on, the numpad
    // directional keys drive rotation instead of movement:
    //   4/6 = yaw(Z) -/+,  1/3 = pitch(X) -/+,  7/9 = roll(Y) -/+,
    //   8/2 = reset rotation to the pre-edit angle (position/scale untouched).
    // Off (default): 8/2 fwd/back, 4/6 left/right, 1/3 down/up, 7/9 yaw.
    bool g_rotateMode = false;

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
        std::string authoredId;  // non-empty = authored ref -> commit registers an override
        bool isMarker = false;   // target is a marker gem -> commit updates its registry pose
        std::uint32_t markerSeq = 0;
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

    // byRay = the explicit physics-ray entry (panel button / numpad *) for
    // trees and non-activatable statics. NEVER an automatic fallback of the
    // crosshair: the ray always hits SOMETHING (walls and floors are refs), so
    // falling back silently would turn "numpad 5 on empty" into "grabbed the
    // wall behind" — the crosshair keeps its exact old feel.
    bool TrySelect(bool byRay) {
        RE::NiPointer<RE::TESObjectREFR> ref = byRay ? Aim::RayRef() : Aim::CrosshairRef();
        if (!ref) {
            SKSE::log::info("Editor: {} has no target",
                byRay ? "ray" : "crosshair");
            return false;
        }
        // A marker gem IS editable now (2026-07-11): moving it and committing
        // updates the marker's registry pose (not an override entry). Adopt an
        // orphan proxy on the spot so it never falls through to the authored
        // path (its base resolves to the tooling esp, which would be wrong).
        std::uint32_t markerSeq = 0;
        if (Markers::IsProxy(ref.get())) {
            markerSeq = Markers::SeqOf(ref.get());
            if (!markerSeq) markerSeq = Markers::AdoptOne(ref.get());
        }
        // An authored ref is editable too (contract decided 2026-07-11):
        // commit registers it in the Overrides registry -> `overrides[]`.
        // A marker never counts as authored (its own registry owns it).
        std::string authoredId;
        if (!markerSeq)
            if (auto id = SceneExporter::ResolveDurableId(ref.get()))
                authoredId = *id;
        g.active = true;
        g.isMarker = markerSeq != 0;
        g.markerSeq = markerSeq;
        g.authoredId = std::move(authoredId);
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
        SKSE::log::info("Editor: editing {} ref{} at ({:.1f}, {:.1f}, {:.1f}) — "
            "numpad 8/2 fwd/back, 4/6 left/right, 1/3 down/up, 7/9 rot, +/- scale, "
            "0 commit, . cancel",
            g.isMarker ? "MARKER" : g.authoredId.empty() ? "dynamic" : "AUTHORED",
            byRay ? " (ray)" : "",
            g.origPos.x, g.origPos.y, g.origPos.z);
        return true;
    }
}

namespace Editor {

    bool Active() { return g.active; }

    bool HandleKey(std::uint32_t code) {
        if (!g.active) {
            // P5: edit mode is ENTERED via the edit mode's action key
            // (Modes.cpp -> EnterSelect). Numpad * stays as the explicit
            // ray-select entry; numpad 5 is no longer an entry key.
            if (code != kSelectRay) return false;
            TrySelect(true);
            return true;
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
        case kSelect:  // numpad 5 — mode-scoped reset, KEEP editing
            if (g_rotateMode) {
                // Rotate mode: 5 (like 8/2) zeroes only the angle.
                Apply(ref.get(), ref->GetPosition(), g.origAngle);
                RE::DebugNotification("SCB: rotation reset");
            } else {
                Apply(ref.get(), g.origPos, g.origAngle);
                if (!g.isActor) { ref->SetScale(g.origScale); ref->Update3DPosition(true); }
                RE::DebugNotification("SCB: reset to pre-edit pose");
            }
            return true;
        case kSelectRay:  // numpad * — commit, then ray-select the next target
        case kCommit:     // numpad 0 — commit and exit
            SKSE::log::info("Editor: committed at ({:.1f}, {:.1f}, {:.1f})",
                pos.x, pos.y, pos.z);
            // Three commit targets: a marker gem updates its own registry pose;
            // an authored ref becomes an overrides[] entry; our own dynamic ref
            // needs nothing (its live pose exports as-is, so it correctly does
            // NOT show up in the Editor page's override list).
            if (g.isMarker) {
                Markers::SetTransform(g.markerSeq, ref->GetPosition(),
                    ref->data.angle * kRadToDeg, ref->GetScale());
                RE::DebugNotification("SCB: marker moved");
            } else if (!g.authoredId.empty()) {
                Overrides::Register(g.authoredId, ref.get(),
                    g.origPos, g.origAngle, g.origScale);
                RE::DebugNotification("SCB: edit committed (overrides[])");
            } else {
                RE::DebugNotification("SCB: edit committed (your ref exports as-is)");
            }
            ReleasePhysics();
            g = {};
            if (code != kCommit) TrySelect(code == kSelectRay);
            return true;
        case kCancel:
            RE::DebugNotification("SCB: edit cancelled");
            Cancel();
            return true;
        // 7/9 rotate roll(Y) in rotate mode, yaw(Z) in move mode.
        case kYawPos:
        case kYawNeg: {
            const float d = (code == kYawPos ? 1.f : -1.f) * g_yawStep * kDegToRad;
            (g_rotateMode ? angle.y : angle.z) += d;
            Apply(ref.get(), pos, angle);
            return true;
        }
        // 4/6 and 1/3: rotate (yaw / pitch) in rotate mode, move in move mode.
        case kLeft:
            if (g_rotateMode) angle.z -= g_yawStep * kDegToRad;
            else pos = pos - right * g_moveStep;
            Apply(ref.get(), pos, angle); return true;
        case kRight:
            if (g_rotateMode) angle.z += g_yawStep * kDegToRad;
            else pos = pos + right * g_moveStep;
            Apply(ref.get(), pos, angle); return true;
        case kDown:  // numpad 1
            if (g_rotateMode) angle.x -= g_yawStep * kDegToRad;
            else pos.z -= g_moveStep;
            Apply(ref.get(), pos, angle); return true;
        case kUp:    // numpad 3
            if (g_rotateMode) angle.x += g_yawStep * kDegToRad;
            else pos.z += g_moveStep;
            Apply(ref.get(), pos, angle); return true;
        // 8/2: move fwd/back in move mode; reset rotation in rotate mode.
        case kFwd:
            if (g_rotateMode) { Apply(ref.get(), pos, g.origAngle); return true; }
            Apply(ref.get(), pos + fwd * g_moveStep, angle); return true;
        case kBack:
            if (g_rotateMode) { Apply(ref.get(), pos, g.origAngle); return true; }
            Apply(ref.get(), pos - fwd * g_moveStep, angle); return true;
        case kScaleUp:
            if (!g.isActor) { ref->SetScale(ref->GetScale() + g_scaleStep); ref->Update3DPosition(true); }
            return true;
        case kScaleDn:
            if (!g.isActor) { ref->SetScale(ref->GetScale() - g_scaleStep); ref->Update3DPosition(true); }
            return true;
        default:
            // Self-diagnosis for numpad DIK constants only. Movement keys
            // (WASD/Alt) land here too while editing and flooded the log
            // (in-game 2026-07-11: 0x11/0x1F/0x20/0x38 noise).
            // 0x47..0x53 = numpad block; 0x37/0xB5/0x9C = num * / Enter.
            if ((code >= 0x47 && code <= 0x53) || code == 0x37 || code == 0xB5 || code == 0x9C) {
                SKSE::log::info("Editor: unmapped scancode 0x{:X} in edit mode", code);
            }
            return true;  // swallow everything while editing
        }
    }

    bool SelectByRay() {
        if (g.active) return false;  // finish (0) or cancel (.) first
        return TrySelect(true);
    }

    bool EnterSelect() {
        if (g.active) return false;  // finish (0) or cancel (.) first
        return TrySelect(false);     // the crosshair, classic feel
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

    float MoveStep() { return g_moveStep; }
    float YawStep() { return g_yawStep; }
    float ScaleStep() { return g_scaleStep; }
    void SetMoveStep(float v) { if (v > 0.f) g_moveStep = v; }
    void SetYawStep(float v) { if (v > 0.f) g_yawStep = v; }
    void SetScaleStep(float v) { if (v > 0.f) g_scaleStep = v; }

    bool RotateMode() { return g_rotateMode; }
    void SetRotateMode(bool on) { g_rotateMode = on; }
    bool ToggleRotateMode() { g_rotateMode = !g_rotateMode; return g_rotateMode; }

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
