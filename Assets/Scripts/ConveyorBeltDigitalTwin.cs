using System;
using UnityEngine;

namespace ConveyorTwin
{
    public enum TwinMode
    {
        DigitalModel,
        DigitalShadow
    }

    [Serializable]
    public class ConveyorTelemetry
    {
        public float beltSpeedMps;
        public float motorTorqueNm;
        public float loadKg;
        public float packageCountPerMinute;
        public float measuredPowerW;
        public bool emergencyStop;
    }

    public class ConveyorBeltDigitalTwin : MonoBehaviour
    {
        [Header("Twin mode")]
        public TwinMode mode = TwinMode.DigitalShadow;
        public TwinTelemetrySimulator telemetrySource;
        public CsvTelemetryPlayer csvTelemetrySource;

        [Header("Belt physical parameters")]
        [Min(0.01f)] public float beltLengthM = 6f;
        [Min(0.01f)] public float beltWidthM = 0.8f;
        [Min(0.01f)] public float pulleyRadiusM = 0.18f;
        [Min(0.01f)] public float beltMassKg = 38f;
        [Range(0f, 1f)] public float rollingFrictionCoefficient = 0.035f;
        [Range(0.01f, 1f)] public float motorEfficiency = 0.86f;
        [Min(0f)] public float maxSafeLoadKg = 120f;
        [Min(0f)] public float maxSafeSpeedMps = 2.5f;

        [Header("Digital model input")]
        [Min(0f)] public float commandedSpeedMps = 1.2f;
        [Min(0f)] public float commandedLoadKg = 20f;

        [Header("Visuals")]
        public Transform beltSurface;
        public Renderer beltRenderer;
        public string textureProperty = "_MainTex";
        public float textureScrollScale = 0.65f;

        public ConveyorTelemetry CurrentTelemetry { get; private set; } = new ConveyorTelemetry();
        public float AngularSpeedRadPerSec { get; private set; }
        public float PullForceN { get; private set; }
        public float EstimatedPowerW { get; private set; }
        public float ThroughputPackagesPerHour { get; private set; }
        public bool IsOverloaded { get; private set; }
        public bool IsOverspeed { get; private set; }
        public bool EmergencyStop { get; private set; }

        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private Vector2 textureOffset;

        private void Reset()
        {
            beltSurface = transform;
            beltRenderer = GetComponentInChildren<Renderer>();
        }

        private void Update()
        {
            ReadTwinInput();
            CalculatePhysics();
            AnimateBelt();
        }

        private void ReadTwinInput()
        {
            if (mode == TwinMode.DigitalShadow && telemetrySource != null)
            {
                CurrentTelemetry = telemetrySource.GetLatestTelemetry();
            }
            else if (mode == TwinMode.DigitalShadow && csvTelemetrySource != null)
            {
                CurrentTelemetry = csvTelemetrySource.GetLatestTelemetry();
            }
            else
            {
                CurrentTelemetry.beltSpeedMps = commandedSpeedMps;
                CurrentTelemetry.loadKg = commandedLoadKg;
                CurrentTelemetry.packageCountPerMinute = EstimatePackagesPerMinute(commandedSpeedMps);
                CurrentTelemetry.motorTorqueNm = 0f;
                CurrentTelemetry.measuredPowerW = 0f;
                CurrentTelemetry.emergencyStop = false;
            }
        }

        private void CalculatePhysics()
        {
            EmergencyStop = CurrentTelemetry.emergencyStop;
            var speed = EmergencyStop ? 0f : Mathf.Max(0f, CurrentTelemetry.beltSpeedMps);
            var totalMovingMassKg = beltMassKg + Mathf.Max(0f, CurrentTelemetry.loadKg);

            // omega = v / r
            AngularSpeedRadPerSec = speed / pulleyRadiusM;

            // F = mu * m * g. This approximates rolling/frictional resistance on a horizontal conveyor.
            PullForceN = rollingFrictionCoefficient * totalMovingMassKg * Physics.gravity.magnitude;

            // P = F * v / eta. Torque estimate: tau = F * r.
            EstimatedPowerW = motorEfficiency > 0f ? PullForceN * speed / motorEfficiency : 0f;
            if (CurrentTelemetry.motorTorqueNm <= 0f)
            {
                CurrentTelemetry.motorTorqueNm = PullForceN * pulleyRadiusM;
            }

            if (CurrentTelemetry.measuredPowerW <= 0f)
            {
                CurrentTelemetry.measuredPowerW = EstimatedPowerW;
            }

            ThroughputPackagesPerHour = CurrentTelemetry.packageCountPerMinute * 60f;
            IsOverloaded = CurrentTelemetry.loadKg > maxSafeLoadKg;
            IsOverspeed = speed > maxSafeSpeedMps;
        }

        private void AnimateBelt()
        {
            var speed = EmergencyStop ? 0f : CurrentTelemetry.beltSpeedMps;

            if (beltSurface != null)
            {
                var localScale = beltSurface.localScale;
                localScale.x = beltWidthM;
                localScale.z = beltLengthM;
                beltSurface.localScale = localScale;
            }

            if (beltRenderer == null)
            {
                return;
            }

            textureOffset.y += speed * textureScrollScale * Time.deltaTime;
            var propertyId = textureProperty == "_MainTex" ? MainTex : Shader.PropertyToID(textureProperty);
            beltRenderer.material.SetTextureOffset(propertyId, textureOffset);
        }

        private float EstimatePackagesPerMinute(float speedMps)
        {
            const float averagePackageSpacingM = 0.85f;
            return averagePackageSpacingM > 0f ? speedMps / averagePackageSpacingM * 60f : 0f;
        }
    }
}
