using UnityEngine;

namespace ConveyorTwin
{
    public class FillingFilteringHud : MonoBehaviour
    {
        public FillingFilteringDigitalTwin process;
        public Vector2 position = new Vector2(16f, 16f);
        public Vector2 size = new Vector2(520f, 285f);

        private GUIStyle labelStyle;

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
            }

            GUI.Box(new Rect(position, size), "Filling & Filtering Digital Twin");
            GUILayout.BeginArea(new Rect(position.x + 16f, position.y + 28f, size.x - 32f, size.y - 40f));
            DrawLine($"1. Infeed throughput: {process.ThroughputBottlesPerHour:0} bottles/hour");
            DrawLine($"   Infeed motor speed: {process.InfeedMotorSpeedRpm:0.0} rpm");
            DrawLine($"   Turntable buffer: {process.TurntableBufferCount} bottles | Conveyor: {process.BottlesOnConveyorCount}");
            DrawLine($"   omega: {process.TurntableAngularSpeedRadPerSec:0.00} rad/s | a_c rim: {process.CentrifugalAccelerationAtRimMps2:0.00} m/s2");
            DrawLine($"   Slat pitch: {process.slatPitchM:0.00} m | slip ratio: {process.conveyorSlipRatio:P0}");
            DrawLine($"2. Filling indexed: {process.BottlesAtFillingStation}/{process.ActiveFillingNozzleCount} | Conveyor stopped: {process.ConveyorStoppedForFilling}");
            DrawLine($"   Turntable paused: {process.TurntablePaused} | Star wheel locked: {process.StarWheelLocked}");
            DrawLine($"   Vessel liquid level: {process.LiquidLevelLiters:0.0} L");
            DrawLine($"   Filling time: {process.LastFillingTimeSeconds:0.00} s");
            DrawLine($"3. Inspection status: {process.InspectionStatus}");
            DrawLine($"   Capping indexed: {process.BottlesAtCappingStation}/{process.ActiveCappingHeadCount} | Conveyor stopped: {process.ConveyorStoppedForCapping}");
            DrawLine($"4. Total passed: {process.TotalPassed}");
            DrawLine($"   Total rejected: {process.TotalRejected}");
            DrawLine($"Rule: volume >= 95% => PASSED, otherwise REJECTED");
            GUILayout.EndArea();
        }

        private void DrawLine(string text)
        {
            GUILayout.Label(text, labelStyle);
        }
    }
}
