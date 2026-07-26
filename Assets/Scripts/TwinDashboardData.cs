using System;

namespace ConveyorTwin
{
    [Serializable]
    public class TwinSetpoints
    {
        public float conveyorSpeedMps;
        public float pumpFlowLitersPerMinute;
        public float infeedMotorSpeedRpm;
        public float starWheelIndexSpeedRpm;
        public float starWheelDwellSeconds;

        public TwinSetpoints Clone()
        {
            return new TwinSetpoints
            {
                conveyorSpeedMps = conveyorSpeedMps,
                pumpFlowLitersPerMinute = pumpFlowLitersPerMinute,
                infeedMotorSpeedRpm = infeedMotorSpeedRpm,
                starWheelIndexSpeedRpm = starWheelIndexSpeedRpm,
                starWheelDwellSeconds = starWheelDwellSeconds
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
        public float starWheelIndexSpeedRpm;
        public float starWheelDwellSeconds;
        public float starWheelIndexDurationSeconds;
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
        public int totalOverflowed;
        public int totalRejectEscapes;
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
        HighInfeedRpm,
        FastDiscIndex,
        SlowDiscIndex,
        ShortDiscDwell,
        LongDiscDwell,
        OverflowPumpTest
    }

    public static class TwinProcessMath
    {
        // Pure helpers keep the physical relations testable without a running scene.
        public static float CalculateDiscDwellSeconds(float dwellSeconds)
        {
            return UnityEngine.Mathf.Clamp(dwellSeconds, 0.10f, 5f);
        }

        public static float CalculateStarWheelIndexDurationSeconds(int pocketCount, int slotDelta, float indexSpeedRpm)
        {
            var safePocketCount = UnityEngine.Mathf.Max(1, pocketCount);
            var safeSlots = UnityEngine.Mathf.Max(1, slotDelta);
            var safeRpm = UnityEngine.Mathf.Max(0.01f, indexSpeedRpm);
            var revolutions = safeSlots / (float)safePocketCount;
            return UnityEngine.Mathf.Max(0.05f, revolutions * 60f / safeRpm);
        }

        public static bool IsBottleInsideRejectSweepBounds(UnityEngine.Vector3 bottleCenter, float bottleRadius, UnityEngine.Bounds sweepBounds)
        {
            var closest = sweepBounds.ClosestPoint(bottleCenter);
            var horizontalDelta = bottleCenter - closest;
            horizontalDelta.y = 0f;
            return horizontalDelta.sqrMagnitude <= UnityEngine.Mathf.Max(0f, bottleRadius) * UnityEngine.Mathf.Max(0f, bottleRadius);
        }

        public static bool HasBottlePassedRejectSweepZone(float bottleZ, float stationZ, float sweepHalfLengthM, float bottleRadius)
        {
            return bottleZ > stationZ + UnityEngine.Mathf.Max(0f, sweepHalfLengthM) + UnityEngine.Mathf.Max(0f, bottleRadius);
        }

        public static float CalculateAvailablePumpOutputLiters(float pumpFlowLitersPerMinute, float frameSeconds, float vesselLevelLiters)
        {
            var requestedLiters = UnityEngine.Mathf.Max(0f, pumpFlowLitersPerMinute) / 60f * UnityEngine.Mathf.Max(0f, frameSeconds);
            return UnityEngine.Mathf.Min(UnityEngine.Mathf.Max(0f, vesselLevelLiters), requestedLiters);
        }

        public static bool IsFillWithinSpecification(float fillRatio, float passThreshold)
        {
            return fillRatio >= UnityEngine.Mathf.Clamp01(passThreshold) && fillRatio <= 1f;
        }
    }
}
