using UnityEngine;

namespace ConveyorTwin
{
    public class TwinTelemetrySimulator : MonoBehaviour
    {
        [Header("Signal baseline")]
        public float baseSpeedMps = 1.25f;
        public float baseLoadKg = 34f;
        public float basePackagesPerMinute = 70f;

        [Header("Signal variation")]
        public float speedNoiseMps = 0.08f;
        public float loadOscillationKg = 14f;
        public float torqueNoiseNm = 0.3f;
        public float overloadStartSeconds = 45f;
        public float overloadDurationSeconds = 10f;

        private readonly ConveyorTelemetry latest = new ConveyorTelemetry();

        public ConveyorTelemetry GetLatestTelemetry()
        {
            var t = Time.time;
            var overloadPhase = t > overloadStartSeconds && t < overloadStartSeconds + overloadDurationSeconds;

            latest.beltSpeedMps = Mathf.Max(0f, baseSpeedMps + Mathf.Sin(t * 0.7f) * speedNoiseMps);
            latest.loadKg = baseLoadKg + Mathf.Sin(t * 0.33f) * loadOscillationKg + (overloadPhase ? 95f : 0f);
            latest.packageCountPerMinute = Mathf.Max(0f, basePackagesPerMinute + Mathf.Sin(t * 0.5f) * 8f);
            latest.motorTorqueNm = 5.2f + latest.loadKg * 0.035f + Random.Range(-torqueNoiseNm, torqueNoiseNm);
            latest.measuredPowerW = latest.motorTorqueNm * (latest.beltSpeedMps / 0.18f);
            latest.emergencyStop = false;

            return latest;
        }
    }
}
