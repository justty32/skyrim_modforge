#include "Aim.h"

#include "log.h"

#include <cmath>

namespace Aim {

    // Havok ray from eye level along the player's facing. Sign convention on
    // pitch is UNVERIFIED in-game — if markers land behind/above, flip it.
    bool LookHit(RE::NiPoint3& out) {
        auto* player = RE::PlayerCharacter::GetSingleton();
        if (!player) return false;
        auto* cell = player->GetParentCell();
        auto* world = cell ? cell->GetbhkWorld() : nullptr;
        if (!world) return false;

        RE::NiPoint3 from = player->GetPosition();
        from.z += 120.f;  // eye-ish; good enough for a ground pick
        const float pitch = player->data.angle.x;  // radians; positive = down
        const float yaw = player->data.angle.z;
        const RE::NiPoint3 dir{
            std::sin(yaw) * std::cos(pitch),
            std::cos(yaw) * std::cos(pitch),
            -std::sin(pitch),
        };
        constexpr float kRange = 4096.f;
        const RE::NiPoint3 to = from + dir * kRange;

        const float scale = RE::bhkWorld::GetWorldScale();
        RE::bhkPickData pick;
        pick.rayInput.from = RE::hkVector4(from * scale);
        pick.rayInput.to = RE::hkVector4(to * scale);
        bool hit = false;
        {
            RE::BSReadLockGuard lock(world->worldLock);
            hit = world->PickObject(pick) && pick.rayOutput.HasHit();
        }
        if (!hit) return false;
        out = from + dir * (kRange * pick.rayOutput.hitFraction);
        return true;
    }


}  // namespace Aim
