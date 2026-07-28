using ConveyorTwin;
using System.Collections.Generic;
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
    public void InfiniteWaterSupplyDoesNotLimitPumpOutputByVesselLevel()
    {
        var output = TwinProcessMath.CalculateAvailablePumpOutputLiters(120f, 1f, 0.75f, true);

        Assert.That(output, Is.EqualTo(2f).Within(0.0001f));
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
    public void FillSpecificationPassesFromNinetyFiveToOneHundredAndFivePercent()
    {
        Assert.That(TwinProcessMath.IsFillWithinSpecification(0.95f, 0.95f), Is.True);
        Assert.That(TwinProcessMath.IsFillWithinSpecification(1f, 0.95f), Is.True);
        Assert.That(TwinProcessMath.IsFillWithinSpecification(1.05f, 0.95f), Is.True);
        Assert.That(TwinProcessMath.IsFillWithinSpecification(1.051f, 0.95f), Is.False);
        Assert.That(TwinProcessMath.IsFillWithinSpecification(0.94f, 0.95f), Is.False);
    }

    [Test]
    public void HighPumpOutputCanOverflowBottlesDuringTheSameDwell()
    {
        var dwell = TwinProcessMath.CalculateDiscDwellSeconds(1.35f);
        var totalDispensed = TwinProcessMath.CalculateAvailablePumpOutputLiters(300f, dwell, 120f);
        var perBottle = totalDispensed / 3f;

        Assert.That(perBottle, Is.GreaterThan(1f));
        Assert.That(perBottle, Is.EqualTo(2.25f).Within(0.001f));
    }

    [Test]
    public void OverflowBeginsOnlyAboveOneHundredAndFivePercent()
    {
        Assert.That(TwinProcessMath.HasBottleOverflowed(1.05f), Is.False);
        Assert.That(TwinProcessMath.HasBottleOverflowed(1.051f), Is.True);
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

    [Test]
    public void RecentGoodOutputKeepsOnlyTheLatestSixtySecondsAndDoesNotAdvanceWhilePaused()
    {
        var outputTimes = new Queue<float>(new[] { 0f, 10f, 61f });

        var countAtSixtyOneSeconds = TwinProcessMath.PruneAndCountRecentEvents(outputTimes, 61f, 60f);
        var countWhilePaused = TwinProcessMath.PruneAndCountRecentEvents(outputTimes, 61f, 60f);

        Assert.That(countAtSixtyOneSeconds, Is.EqualTo(2));
        Assert.That(countWhilePaused, Is.EqualTo(2));
        Assert.That(TwinProcessMath.CalculateHourlyRate(countWhilePaused, 60f), Is.EqualTo(120f).Within(0.0001f));
    }

    [Test]
    public void KpiPercentagesUseQcAndDefectCountsWithSafeEmptyDenominators()
    {
        Assert.That(TwinProcessMath.CalculateQcPassRatePercent(10, 2), Is.EqualTo(80f).Within(0.0001f));
        Assert.That(TwinProcessMath.CalculatePercentage(2, 10), Is.EqualTo(20f).Within(0.0001f));
        Assert.That(TwinProcessMath.CalculatePercentage(1, 2), Is.EqualTo(50f).Within(0.0001f));
        Assert.That(TwinProcessMath.CalculateQcPassRatePercent(0, 0), Is.EqualTo(0f));
        Assert.That(TwinProcessMath.CalculatePercentage(1, 0), Is.EqualTo(0f));
    }
}
