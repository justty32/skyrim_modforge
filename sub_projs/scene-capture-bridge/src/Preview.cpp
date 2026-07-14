#include "Preview.h"

#include "Aim.h"
#include "Palette.h"
#include "Physics.h"
#include "SceneExporter.h"
#include "log.h"

#include <algorithm>
#include <cmath>

namespace {
    constexpr float kDegToRad = 1.f / 57.2957795f;

    // The sentinel that makes a ghost a ghost, forever — see Preview.h. It is a
    // display name, so the one place it can ever surface is a console selection,
    // which is exactly where you would want to be told.
    constexpr const char* kSentinel = "[SCB preview ghost]";

    RE::ObjectRefHandle g_handle;
    RE::TESBoundObject* g_base = nullptr;
    std::string g_label;
    bool g_follow = true;
    float g_yaw = 0.f;      // degrees
    float g_scale = 1.f;

    bool HasSentinel(RE::TESObjectREFR* ref) {
        if (!ref) return false;
        auto* x = ref->extraList.GetByType<RE::ExtraTextDisplayData>();
        return x && x->displayName == kSentinel;
    }

    // Strip a ghost of ALL collision.
    //
    // 🔴 IN-GAME 2026-07-14: `Get3D()->SetCollisionLayer()` IS NOT ENOUGH — the
    // player reported "the ghost follows my aim but a collision box stays behind
    // at the spawn point". Two facts, and you need both to see why:
    //
    //   1. The rigid bodies hang off CHILD nodes of the 3D, not the root. The
    //      NiAVObject call only touches the node's own collision object, so the
    //      nif's actual bhkRigidBodies were never reached — the ghost stayed
    //      solid.
    //   2. A havok body DOES NOT FOLLOW SetPosition/Update3DPosition. A STAT's
    //      body is fixed in the havok world where the ref was first placed, so
    //      the visual walks off with your aim and the collision stays behind.
    //      That is the box he walked into.
    //
    // So we do what po3's Papyrus Extender / Base Object Swapper do: walk the
    // whole collision scenegraph and rewrite the LAYER BITS of every body's
    // collisionFilterInfo. Once every body is kNonCollidable, (2) stops mattering
    // — a body that collides with nothing can be left wherever it likes.
    void StripCollision(RE::NiAVObject* obj) {
        if (!obj) return;
        RE::BSVisit::TraverseScenegraphCollision(obj,
            [](RE::bhkNiCollisionObject* col) {
                auto* body = col ? col->body.get() : nullptr;
                auto* hkBody = body
                    ? static_cast<RE::hkpWorldObject*>(body->referencedObject.get())
                    : nullptr;
                if (hkBody) {
                    auto& info = hkBody->collidable.broadPhaseHandle.collisionFilterInfo;
                    info &= ~0x7Fu;  // the low 7 bits ARE the COL_LAYER
                    info |= static_cast<std::uint32_t>(RE::COL_LAYER::kNonCollidable);
                }
                return RE::BSVisit::BSVisitControl::kContinue;
            });
    }

    // Right after PlaceObjectAtMe the 3D is not loaded, so there is nothing to
    // strip yet (the same one-frame problem Physics::FreezeDeferred solves —
    // this is that pattern, for the other property).
    //
    // Intangibility is not cosmetic. The aim ray is a physics ray: a solid ghost
    // at the aim point is the thing the ray hits, and the aim point would creep
    // toward the player every frame. And a preview you can walk into, shoot, or
    // trip over is not a preview — it is an object you did not agree to place.
    void MakeGhostlyDeferred(RE::ObjectRefHandle handle, int retries = 60) {
        auto* task = SKSE::GetTaskInterface();
        if (!task) return;
        task->AddTask([handle, retries]() {
            auto ref = handle.get();
            if (!ref) return;
            if (auto* obj = ref->Get3D()) {
                StripCollision(obj);
                return;
            }
            if (retries > 0) MakeGhostlyDeferred(handle, retries - 1);
        });
    }

    void Destroy(RE::TESObjectREFR* ref) {
        if (!ref) return;
        ref->Disable();
        ref->SetDelete(true);  // no trace: it was never content (`markfordelete` semantics)
    }
}

namespace Preview {

    bool Active() { return static_cast<bool>(g_handle.get()); }
    RE::TESBoundObject* Base() { return g_base; }
    const std::string& Label() { return g_label; }

    bool IsGhost(RE::TESObjectREFR* ref) {
        if (!ref) return false;
        if (auto live = g_handle.get(); live && live.get() == ref) return true;
        return HasSentinel(ref);  // an orphan from a reloaded save — still not content
    }

    void SetFollow(bool on) { g_follow = on; }
    bool Follow() { return g_follow; }
    float Yaw() { return g_yaw; }
    float Scale() { return g_scale; }

    void SetYaw(float degrees) {
        g_yaw = std::fmod(degrees, 360.f);
        if (auto ref = g_handle.get()) {
            RE::NiPoint3 angle = ref->data.angle;
            angle.z = g_yaw * kDegToRad;
            ref->SetAngle(angle);
            ref->Update3DPosition(true);
        }
    }

    void SetScale(float scale) {
        g_scale = std::clamp(scale, 0.05f, 10.f);
        if (auto ref = g_handle.get()) {
            ref->SetScale(g_scale);
            ref->Update3DPosition(true);
        }
    }

    bool Show(RE::TESBoundObject* base, const std::string& label) {
        if (!base) return false;
        auto* player = RE::PlayerCharacter::GetSingleton();
        if (!player) return false;

        Clear();  // one ghost at a time — switching entries swaps what you see

        RE::NiPointer<RE::TESObjectREFR> ghost = player->PlaceObjectAtMe(base, false);
        if (!ghost) {
            SKSE::log::error("Preview: PlaceObjectAtMe failed for '{}'", label);
            return false;
        }
        // The sentinel goes on FIRST: from this line on, the ref is recognisable
        // as a ghost by anything that looks at it, including a future session.
        ghost->extraList.Add(new RE::ExtraTextDisplayData(kSentinel));

        RE::NiPoint3 pos;
        if (!Aim::LookHit(pos)) pos = player->GetPosition();
        ghost->SetPosition(pos);
        RE::NiPoint3 angle{0.f, 0.f, g_yaw * kDegToRad};
        ghost->SetAngle(angle);
        if (g_scale != 1.f) ghost->SetScale(g_scale);
        ghost->Update3DPosition(true);

        // A ghost never falls, topples or settles: it is a picture of a decision,
        // not an object in the world. (Type-gated like every other runtime freeze
        // — keyframing a STAT is meaningless, and it has no rigid body to fall.)
        if (Physics::HavokMovable(base)) Physics::FreezeDeferred(ghost->GetHandle());
        MakeGhostlyDeferred(ghost->GetHandle());

        g_handle = ghost->GetHandle();
        g_base = base;
        g_label = label;
        SKSE::log::info("Preview: ghost '{}' at ({:.1f}, {:.1f}, {:.1f}) yaw={:.0f} scale={:.2f}",
            label, pos.x, pos.y, pos.z, g_yaw, g_scale);
        return true;
    }

    void Clear() {
        if (auto ref = g_handle.get()) {
            Destroy(ref.get());
            SKSE::log::info("Preview: ghost cleared");
        }
        g_handle = {};
        g_base = nullptr;
        g_label.clear();
    }

    void Update() {
        auto ref = g_handle.get();
        if (!ref) {
            if (g_base) DropState();  // the ghost died with its cell — forget it
            return;
        }
        // You walked out of the cell you were previewing in. Destroy the ghost
        // NOW, while the handle is still good: a ghost left behind in a cell we
        // no longer watch is an orphan — export-safe (the sentinel sees to that)
        // but still a mountain standing in someone's inn until the next load.
        // Update() runs every frame, so this fires on the first frame after the
        // transition, long before the old cell unloads.
        auto* player = RE::PlayerCharacter::GetSingleton();
        if (player && ref->GetParentCell() != player->GetParentCell()) {
            SKSE::log::info("Preview: left the cell — ghost cleared");
            Clear();
            return;
        }
        if (!g_follow) return;
        RE::NiPoint3 pos;
        if (!Aim::LookHit(pos)) return;  // looking at the sky: leave it where it is
        ref->SetPosition(pos);
        ref->Update3DPosition(true);
    }

    bool Commit() {
        auto ref = g_handle.get();
        if (!ref || !g_base) {
            SKSE::log::info("Preview: nothing to commit — no ghost up");
            return false;
        }
        auto id = SceneExporter::ResolveDurableId(g_base);
        if (!id) {  // Catalog never admits one of these, but the ghost outlives the page
            SKSE::log::warn("Preview: ghost's base is runtime-only — cannot place");
            return false;
        }

        // Commit through the ONE place path (`sc pl`'s), at the ghost's exact
        // pose. Not re-aimed: what you see standing there is what gets placed —
        // and `sc pl py0`/`ed1` behave exactly as they do for a palette slot.
        Palette::Slot s;
        s.name = g_label;
        s.baseId = *id;
        s.base = g_base;
        s.angle = ref->data.angle;
        s.scale = ref->GetScale();
        const RE::NiPoint3 pos = ref->GetPosition();

        if (!Palette::PlaceSlot(s, &pos)) return false;
        // The ghost STAYS up — placing a row of trees is the same key, five times.
        SKSE::log::info("Preview: committed '{}' ({})", g_label, s.baseId);
        return true;
    }

    std::size_t SweepOrphans() {
        auto* player = RE::PlayerCharacter::GetSingleton();
        RE::TESObjectCELL* cell = player ? player->GetParentCell() : nullptr;
        if (!cell) return 0;
        std::vector<RE::TESObjectREFR*> doomed;
        cell->ForEachReference([&](RE::TESObjectREFR* ref) -> RE::BSContainer::ForEachResult {
            if (ref && !ref->IsDeleted() && HasSentinel(ref)) doomed.push_back(ref);
            return RE::BSContainer::ForEachResult::kContinue;
        });
        for (auto* ref : doomed) Destroy(ref);
        if (!doomed.empty())
            SKSE::log::info("Preview: removed {} orphan ghost(s) from the loaded save",
                doomed.size());
        return doomed.size();
    }

    void DropState() {
        g_handle = {};
        g_base = nullptr;
        g_label.clear();
    }

}  // namespace Preview
