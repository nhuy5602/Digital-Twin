using ConveyorTwin;
using NUnit.Framework;

public class TwinProcessMathTests
{
    [Test]
    public void HigherConveyorSpeedShortensFillingDwell()
    {
        var nominal = TwinProcessMath.CalculateFillingDwellSeconds(1.35f, 0.85f, 0.85f);
        var highConveyor = TwinProcessMath.CalculateFillingDwellSeconds(1.35f, 0.85f, 1.70f);

        Assert.That(nominal, Is.EqualTo(1.35f).Within(0.0001f));
        Assert.That(highConveyor, Is.EqualTo(0.675f).Within(0.0001f));
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
        var dwell = TwinProcessMath.CalculateFillingDwellSeconds(1.35f, 0.85f, 0.85f);
        var totalDispensed = TwinProcessMath.CalculateAvailablePumpOutputLiters(66.6667f, dwell, 120f);
        var perBottle = totalDispensed / 3f;

        Assert.That(perBottle, Is.LessThan(1f));
        Assert.That(perBottle, Is.EqualTo(0.5f).Within(0.001f));
    }
}
