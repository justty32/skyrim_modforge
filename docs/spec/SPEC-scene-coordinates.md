# Scene coordinate conversion (pure library MVP)

`SceneCoordinates.ToSkyrim` converts a source position, quaternion, and 3-vector
scale into Skyrim position, Euler XYZ degrees, and the best uniform scale. It is
deliberately independent of JSON/spec/build code:

```csharp
var result = SceneCoordinates.ToSkyrim(
    new SceneTransformInput(position, rotation, scale3),
    SceneCoordinateProfile.UnityLeftHandedYUp());
```

Built-in profiles are explicitly named `unity-lh-y-up` (metres → 64 Skyrim
units) and `unreal-lh-z-up` (centimetres → 0.64 units). Both use a full basis
conjugation, `B * R * B^-1`, before Euler XYZ decomposition (implemented as
`B^-1 * R * B` because `System.Numerics` transforms row vectors). `Custom(...)` accepts
a calibrated basis, unit scale, and art-scale fudge for engines whose exact
handedness/sign convention is not yet established (including FromSoft).

Skyrim references have one uniform scale. A non-uniform source scale is therefore
returned as the arithmetic mean plus `HasNonUniformScale=true` and a diagnostic;
callers must decide whether to bake a mesh variant or accept the approximation.
The profiles are engineering conventions, not in-game calibration claims. Exact
engine signs and scale still require the documented cube/doorway fixture measured
in CK or the running game before mass conversion.
