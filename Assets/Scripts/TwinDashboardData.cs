using System;

namespace ConveyorTwin
{
    [Serializable]
    public class TwinSetpoints
    {
        public float conveyorSpeedMps;
        public float pumpFlowLitersPerMinute;
        public float infeedMotorSpeedRpm;

        public TwinSetpoints Clone()
        {
            return new TwinSetpoints
            {
                conveyorSpeedMps = conveyorSpeedMps,
                pumpFlowLitersPerMinute = pumpFlowLitersPerMinute,
                infeedMotorSpeedRpm = infeedMotorSpeedRpm
            };
        }
    }

    [Serializable]
    public class TwinSnapshot
    {
        public float simulationSeconds;
        public bool paused;
        public float conveyorSpeedMps;
        public float pumpFlowLitersPerMinute;
        public float infeedMotorSpeedRpm;
        public float effectiveReleaseIntervalSeconds;
        public float effectiveFillingDwellSeconds;
        public float throughputBottlesPerHour;
        public float averageFillPercent;
        public float lastBatchFillPercent;
        public float rejectRatePercent;
        public float vesselLevelLiters;
        public float vesselCapacityLiters;
        public int turntableBufferCount;
        public int bottlesOnConveyorCount;
        public int totalPassed;
        public int totalRejected;
        public float angularSpeedRadPerSec;
        public float centrifugalAccelerationMps2;
        public string starWheelPhase;
        public string alert;
    }

    public enum TwinScenarioPreset
    {
        Nominal,
        HighConveyor,
        LowPumpFlow,
        HighInfeedRpm
    }

    public static class TwinProcessMath
    {
        // Pure helpers keep the physical relations testable without a running scene.
        public static float CalculateFillingDwellSeconds(float baseDwellSeconds, float referenceConveyorSpeedMps, float conveyorSpeedMps)
        {
            return UnityEngine.Mathf.Max(
                0.05f,
                baseDwellSeconds * UnityEngine.Mathf.Max(0.05f, referenceConveyorSpeedMps) / UnityEngine.Mathf.Max(0.2f, conveyorSpeedMps));
        }

        public static float CalculateAvailablePumpOutputLiters(float pumpFlowLitersPerMinute, float frameSeconds, float vesselLevelLiters)
        {
            var requestedLiters = UnityEngine.Mathf.Max(0f, pumpFlowLitersPerMinute) / 60f * UnityEngine.Mathf.Max(0f, frameSeconds);
            return UnityEngine.Mathf.Min(UnityEngine.Mathf.Max(0f, vesselLevelLiters), requestedLiters);
        }
    }
}
