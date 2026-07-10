#pragma once

// Aim — the shared look-ray: where is the player pointing, in world units.
// Used by Markers (place marker there) and Palette (place object there).

namespace Aim {

    // Havok ray from eye level along the player's facing (range 4096).
    // Pitch sign is UNVERIFIED in-game — if hits land behind/above, flip it.
    bool LookHit(RE::NiPoint3& out);

}  // namespace Aim
