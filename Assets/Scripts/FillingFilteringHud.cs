using UnityEngine;

namespace ConveyorTwin
{
    public class FillingFilteringHud : MonoBehaviour
    {
        public FillingFilteringDigitalTwin process;
        public Vector2 position = new Vector2(16f, 16f);
        public Vector2 size = new Vector2(820f, 360f);

        private GUIStyle labelStyle;
        private GUIStyle sectionStyle;
        private FillingFilteringDigitalTwin defaultProcess;
        private float defaultConveyorSpeedMps;
        private float defaultInfeedMotorSpeedRpm;
        private float defaultStarWheelIndexDurationSeconds;
        private float defaultFillingTimeSeconds;
        private float defaultCappingTimeSeconds;
        private float defaultReleaseIntervalSeconds;

        private void OnGUI()
        {
            if (process == null)
            {
                return;
            }

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.fontSize = 18;
                labelStyle.normal.textColor = Color.white;

                sectionStyle = new GUIStyle(labelStyle);
                sectionStyle.fontStyle = FontStyle.Bold;
            }

            CaptureDefaultsIfNeeded();
            var panelSize = new Vector2(Mathf.Max(size.x, 820f), Mathf.Max(size.y, 360f));

            GUI.Box(new Rect(position, panelSize), "Filling & Filtering Digital Twin");
            GUILayout.BeginArea(new Rect(position.x + 16f, position.y + 28f, panelSize.x - 32f, panelSize.y - 40f));
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(380f));
            DrawLine($"1. Infeed throughput: {process.ThroughputBottlesPerHour:0} bottles/hour");
            DrawLine($"   Infeed motor speed: {process.InfeedMotorSpeedRpm:0.0} rpm");
            DrawLine($"   Turntable buffer: {process.TurntableBufferCount} bottles | Conveyor: {process.BottlesOnConveyorCount}");
            DrawLine($"   omega: {process.TurntableAngularSpeedRadPerSec:0.00} rad/s | a_c rim: {process.CentrifugalAccelerationAtRimMps2:0.00} m/s2");
            DrawLine($"   Slat pitch: {process.slatPitchM:0.00} m | slip ratio: {process.conveyorSlipRatio:P0}");
            DrawLine($"   Star wheel step: {process.StarWheelStepAngleDegrees:0.0} deg | pitch: {process.StarWheelPocketPitchM:0.00} m");
            DrawLine($"2. Filling indexed: {process.BottlesAtFillingStation}/{process.ActiveFillingNozzleCount} | Conveyor stopped: {process.ConveyorStoppedForFilling}");
            DrawLine($"   Turntable paused: {process.TurntablePaused} | Star wheel locked: {process.StarWheelLocked}");
            DrawLine($"   Vessel liquid level: {process.LiquidLevelLiters:0.0} L");
            DrawLine($"   Filling time: {process.LastFillingTimeSeconds:0.00} s");
            DrawLine($"3. Inspection status: {process.InspectionStatus}");
            DrawLine($"   Capping machine busy: {process.CappingActive} | Conveyor stopped: {process.ConveyorStoppedForCapping}");
            DrawLine($"4. Total passed: {process.TotalPassed}");
            DrawLine($"   Total rejected: {process.TotalRejected}");
            DrawLine($"Rule: volume >= 95% => PASSED, otherwise REJECTED");
            GUILayout.EndVertical();
            GUILayout.Space(16f);
            GUILayout.BeginVertical(GUILayout.Width(360f));
            DrawSection("Runtime Controls");
            process.conveyorSpeedMps = DrawSlider("Conveyor speed", process.conveyorSpeedMps, 0.2f, 2.0f, "m/s");
            process.infeedMotorSpeedRpm = DrawSlider("Turntable speed", process.infeedMotorSpeedRpm, 5f, 45f, "rpm");

            var starWheelStepsPerSecond = 1f / Mathf.Max(0.05f, process.starWheelIndexDurationSeconds);
            starWheelStepsPerSecond = DrawSlider("Star wheel speed", starWheelStepsPerSecond, 1f, 8f, "pockets/s");
            process.starWheelIndexDurationSeconds = 1f / Mathf.Max(0.1f, starWheelStepsPerSecond);

            var fillingSpeed = defaultFillingTimeSeconds / Mathf.Max(0.1f, process.fillingTimeSeconds);
            fillingSpeed = DrawSlider("Filling speed", fillingSpeed, 0.4f, 3f, "x");
            process.fillingTimeSeconds = defaultFillingTimeSeconds / Mathf.Max(0.1f, fillingSpeed);

            var cappingSpeed = defaultCappingTimeSeconds / Mathf.Max(0.1f, process.cappingTimeSeconds);
            cappingSpeed = DrawSlider("Capping speed", cappingSpeed, 0.4f, 3f, "x");
            process.cappingTimeSeconds = defaultCappingTimeSeconds / Mathf.Max(0.1f, cappingSpeed);

            process.releaseIntervalSeconds = DrawSlider("Turntable release gap", process.releaseIntervalSeconds, 0.18f, 1.5f, "s");

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset controls", GUILayout.Width(140f), GUILayout.Height(28f)))
            {
                ResetControls();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawLine(string text)
        {
            GUILayout.Label(text, labelStyle);
        }

        private void DrawSection(string text)
        {
            GUILayout.Label(text, sectionStyle);
        }

        private float DrawSlider(string label, float value, float min, float max, string unit)
        {
            GUILayout.Label($"{label}: {value:0.00} {unit}", labelStyle);
            var updatedValue = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(330f));
            return updatedValue;
        }

        private void CaptureDefaultsIfNeeded()
        {
            if (defaultProcess == process)
            {
                return;
            }

            defaultProcess = process;
            defaultConveyorSpeedMps = process.conveyorSpeedMps;
            defaultInfeedMotorSpeedRpm = process.infeedMotorSpeedRpm;
            defaultStarWheelIndexDurationSeconds = process.starWheelIndexDurationSeconds;
            defaultFillingTimeSeconds = process.fillingTimeSeconds;
            defaultCappingTimeSeconds = process.cappingTimeSeconds;
            defaultReleaseIntervalSeconds = process.releaseIntervalSeconds;
        }

        private void ResetControls()
        {
            process.conveyorSpeedMps = defaultConveyorSpeedMps;
            process.infeedMotorSpeedRpm = defaultInfeedMotorSpeedRpm;
            process.starWheelIndexDurationSeconds = defaultStarWheelIndexDurationSeconds;
            process.fillingTimeSeconds = defaultFillingTimeSeconds;
            process.cappingTimeSeconds = defaultCappingTimeSeconds;
            process.releaseIntervalSeconds = defaultReleaseIntervalSeconds;
        }
    }
}
