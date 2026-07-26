using ConveyorTwin;
using NUnit.Framework;
using UnityEngine;

public class TwinProcessMathTests
{
    [Test]
    public void DiscDwellIsIndependentFromConveyorSpeed()
    {
        var configuredDwell = TwinProcessMath.CalculateDiscDwellSeconds(1.35f);

        Assert.That(configuredDwell, Is.EqualTo(1.35f).Within(0.0001f));
    }

    [Test]
    public void StarWheelRpmDeterminesIndexDuration()
    {
        var onePocketAtNominal = TwinProcessMath.CalculateStarWheelIndexDurationSeconds(10, 1, 6.6666667f);
        var threePocketsAtDoubleSpeed = TwinProcessMath.CalculateStarWheelIndexDurationSeconds(10, 3, 13.333333f);

        Assert.That(onePocketAtNominal, Is.EqualTo(0.9f).Within(0.001f));
        Assert.That(threePocketsAtDoubleSpeed, Is.EqualTo(1.35f).Within(0.001f));
    }

    [Test]
    public void PumpOutputIsLimitedByVesselLevel()
    {
        var output = TwinProcessMath.CalculateAvailablePumpOutputLiters(120f, 1f, 0.75f);

        Assert.That(output, Is.EqualTo(0.75f).Within(0.0001f));
    }

    [Test]
    public void LowerPumpOutputCannotReachNominalBottleTargetDuringDwell()
    {
        var dwell = TwinProcessMath.CalculateDiscDwellSeconds(1.35f);
        var totalDispensed = TwinProcessMath.CalculateAvailablePumpOutputLiters(66.6667f, dwell, 120f);
        var perBottle = totalDispensed / 3f;

        Assert.That(perBottle, Is.LessThan(1f));
        Assert.That(perBottle, Is.EqualTo(0.5f).Within(0.001f));
    }

    [Test]
    public void SweepBoundsCaptureBottleDuringEitherBarStroke()
    {
        var barBounds = new Bounds(new Vector3(0f, 0.78f, 1.15f), new Vector3(0.07f, 0.30f, 0.42f));

        Assert.That(TwinProcessMath.IsBottleInsideRejectSweepBounds(new Vector3(0.09f, 0.82f, 1.15f), 0.11f, barBounds), Is.True);
        Assert.That(TwinProcessMath.IsBottleInsideRejectSweepBounds(new Vector3(0.35f, 0.82f, 1.15f), 0.11f, barBounds), Is.False);
    }

    [Test]
    public void BottlePastSweepZoneIsARejectEscape()
    {
        Assert.That(TwinProcessMath.HasBottlePassedRejectSweepZone(1.48f, 1.15f, 0.21f, 0.11f), Is.True);
        Assert.That(TwinProcessMath.HasBottlePassedRejectSweepZone(1.46f, 1.15f, 0.21f, 0.11f), Is.False);
    }
}
