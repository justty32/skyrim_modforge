using System.Numerics;

namespace ModForge;

/// <summary>Named source coordinate convention. The basis maps source vectors to Skyrim axes.</summary>
public sealed record SceneCoordinateProfile(
    string Name,
    Matrix4x4 Basis,
    float SourceUnitsToSkyrim,
    float UnitScaleFudge = 1f)
{
    public static SceneCoordinateProfile UnityLeftHandedYUp(float unitScaleFudge = 1f) =>
        new("unity-lh-y-up", new Matrix4x4(
            1, 0, 0, 0,  0, 0, 1, 0,  0, 1, 0, 0,  0, 0, 0, 1), 64f, unitScaleFudge);

    public static SceneCoordinateProfile UnrealLeftHandedZUp(float unitScaleFudge = 1f) =>
        new("unreal-lh-z-up", new Matrix4x4(
            1, 0, 0, 0,  0, -1, 0, 0,  0, 0, 1, 0,  0, 0, 0, 1), 0.64f, unitScaleFudge);

    /// <summary>Custom basis for calibrated engines (e.g. FromSoft; signs are intentionally caller-owned).</summary>
    public static SceneCoordinateProfile Custom(string name, Matrix4x4 basis,
        float sourceUnitsToSkyrim, float unitScaleFudge = 1f)
    {
        if (string.IsNullOrWhiteSpace(name) || sourceUnitsToSkyrim <= 0 || unitScaleFudge <= 0)
            throw new ArgumentOutOfRangeException(nameof(sourceUnitsToSkyrim));
        if (!Matrix4x4.Invert(basis, out _)) throw new ArgumentException("Basis must be invertible.", nameof(basis));
        return new(name, basis, sourceUnitsToSkyrim, unitScaleFudge);
    }
}

public readonly record struct SceneTransformInput(Vector3 Position, Quaternion Rotation, Vector3 Scale);

public readonly record struct SceneTransformResult(
    Vec3 Position,
    Vec3 EulerXyzDegrees,
    float UniformScale,
    bool HasNonUniformScale,
    string? Diagnostic);

/// <summary>Pure source-scene transform conversion. Rotation uses B * R * B^-1, never Euler axis swapping.</summary>
public static class SceneCoordinates
{
    public static SceneTransformResult ToSkyrim(SceneTransformInput source, SceneCoordinateProfile profile,
        float nonUniformTolerance = 0.0001f)
    {
        if (profile is null) throw new ArgumentNullException(nameof(profile));
        if (nonUniformTolerance < 0) throw new ArgumentOutOfRangeException(nameof(nonUniformTolerance));

        var basis = profile.Basis;
        var p = Vector3.Transform(source.Position * (profile.SourceUnitsToSkyrim * profile.UnitScaleFudge), basis);
        var rotation = Matrix4x4.CreateFromQuaternion(Quaternion.Normalize(source.Rotation));
        if (!Matrix4x4.Invert(basis, out var inverse)) throw new ArgumentException("Basis must be invertible.", nameof(profile));
        // Mathematical column-vector notation is B * R * B^-1. System.Numerics transforms row
        // vectors (v' = v * B), so the code-order equivalent is B^-1 * R * B.
        var converted = inverse * rotation * basis;
        var euler = ToEulerXyz(converted);

        var nonUniform = MathF.Abs(source.Scale.X - source.Scale.Y) > nonUniformTolerance ||
                         MathF.Abs(source.Scale.X - source.Scale.Z) > nonUniformTolerance;
        var uniform = (source.Scale.X + source.Scale.Y + source.Scale.Z) / 3f * profile.UnitScaleFudge;
        return new(new Vec3 { X = p.X, Y = p.Y, Z = p.Z }, euler, uniform, nonUniform,
            nonUniform ? "Non-uniform source scale cannot be represented by a Skyrim reference." : null);
    }

    // Decompose the System.Numerics row-vector matrix R = Rx(x) * Ry(y) * Rz(z), in degrees.
    // This is deliberately explicit so the public contract does not depend on an opaque Euler API.
    private static Vec3 ToEulerXyz(Matrix4x4 m)
    {
        var y = MathF.Asin(Math.Clamp(-m.M13, -1f, 1f));
        float x, z;
        if (MathF.Abs(MathF.Cos(y)) > 1e-5f)
        {
            x = MathF.Atan2(m.M23, m.M33);
            z = MathF.Atan2(m.M12, m.M11);
        }
        else
        {
            x = y >= 0 ? MathF.Atan2(m.M21, m.M22) : MathF.Atan2(-m.M21, m.M22);
            z = 0;
        }
        const float r2d = 180f / MathF.PI;
        return new Vec3 { X = x * r2d, Y = y * r2d, Z = z * r2d };
    }
}
