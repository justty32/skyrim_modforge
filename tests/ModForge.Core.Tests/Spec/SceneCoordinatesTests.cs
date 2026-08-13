using System.Numerics;
using ModForge;
using Xunit;

namespace ModForge.Tests;

public class SceneCoordinatesTests
{
    [Fact]
    public void Identity_UsesProfileScaleAndKeepsAxes()
    {
        var r = SceneCoordinates.ToSkyrim(new(Vector3.UnitX, Quaternion.Identity, Vector3.One),
            SceneCoordinateProfile.UnrealLeftHandedZUp());
        Assert.Equal(0.64f, r.Position.X, 3); Assert.Equal(0f, r.Position.Y, 3); Assert.Equal(0f, r.Position.Z, 3);
        Assert.Equal(0f, r.EulerXyzDegrees.X, 3); Assert.Equal(1f, r.UniformScale, 3);
    }

    [Fact]
    public void UnityYUpBasis_MapsUpToSkyrimZAndForwardToSkyrimY()
    {
        var r = SceneCoordinates.ToSkyrim(new(new Vector3(0, 1, 2), Quaternion.Identity, Vector3.One),
            SceneCoordinateProfile.UnityLeftHandedYUp());
        Assert.Equal(0f, r.Position.X, 3); Assert.Equal(128f, r.Position.Y, 3); Assert.Equal(64f, r.Position.Z, 3);
    }

    [Fact]
    public void RotationUsesBasisConjugation_NotEulerAxisSwap()
    {
        var q = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);
        var r = SceneCoordinates.ToSkyrim(new(Vector3.Zero, q, Vector3.One), SceneCoordinateProfile.UnityLeftHandedYUp());
        // The Unity->Skyrim basis includes a handedness reflection, so the axial rotation sign flips.
        Assert.Equal(-90f, r.EulerXyzDegrees.Z, 3);
    }

    [Fact]
    public void IdentityBasis_PreservesPositiveRotationSigns()
    {
        var identity = SceneCoordinateProfile.Custom("identity", Matrix4x4.Identity, 1f);
        var x = SceneCoordinates.ToSkyrim(new(Vector3.Zero,
            Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 4), Vector3.One), identity);
        var y = SceneCoordinates.ToSkyrim(new(Vector3.Zero,
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 6), Vector3.One), identity);
        var z = SceneCoordinates.ToSkyrim(new(Vector3.Zero,
            Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 3), Vector3.One), identity);
        Assert.Equal(45f, x.EulerXyzDegrees.X, 3);
        Assert.Equal(30f, y.EulerXyzDegrees.Y, 3);
        Assert.Equal(60f, z.EulerXyzDegrees.Z, 3);
    }

    [Fact]
    public void NonSymmetricCustomBasis_UsesRowVectorEquivalentOfBasisConjugation()
    {
        // Source axes cycle into target axes: X->Y, Y->Z, Z->X. A source +X rotation must
        // therefore become a target +Y rotation. This catches B*R*B^-1 written in the wrong
        // order for System.Numerics' row-vector Transform convention.
        var cycle = new Matrix4x4(0, 1, 0, 0,  0, 0, 1, 0,  1, 0, 0, 0,  0, 0,0,1);
        var profile = SceneCoordinateProfile.Custom("axis-cycle", cycle, 1f);
        var r = SceneCoordinates.ToSkyrim(new(Vector3.Zero,
            Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2), Vector3.One), profile);
        Assert.Equal(90f, r.EulerXyzDegrees.Y, 3);
    }

    [Fact]
    public void CustomProfile_AllowsUnitScaleFudge()
    {
        var p = SceneCoordinateProfile.Custom("calibrated", Matrix4x4.Identity, 2f, 1.5f);
        var r = SceneCoordinates.ToSkyrim(new(Vector3.One, Quaternion.Identity, Vector3.One), p);
        Assert.Equal(3f, r.Position.X, 3); Assert.Equal(1.5f, r.UniformScale, 3);
    }

    [Fact]
    public void NonUniformScale_IsFlaggedAndAveragesForOutput()
    {
        var r = SceneCoordinates.ToSkyrim(new(Vector3.Zero, Quaternion.Identity, new(1, 2, 3)),
            SceneCoordinateProfile.UnrealLeftHandedZUp());
        Assert.True(r.HasNonUniformScale);
        Assert.Contains("Non-uniform", r.Diagnostic);
        Assert.Equal(2f, r.UniformScale, 3);
    }
}
