using UnityEngine;

namespace ConveyorTwin
{
    public class TwinMetricsHud : MonoBehaviour
    {
        public ConveyorBeltDigitalTwin belt;
        public Vector2 position = new Vector2(16f, 16f);
        public Vector2 size = new Vector2(430f, 230f);

        private GUIStyle labelStyle;

        private void OnGUI()
        {
            if (belt == null)
            {
                return;
            }

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.fontSize = 18;
                labelStyle.normal.textColor = Color.white;
            }

            GUI.Box(new Rect(position, size), "Conveyor Digital Twin");
            GUILayout.BeginArea(new Rect(position.x + 16f, position.y + 28f, size.x - 32f, size.y - 40f));
            DrawLine($"Mode: {belt.mode}");
            DrawLine($"Speed: {belt.CurrentTelemetry.beltSpeedMps:0.00} m/s | omega: {belt.AngularSpeedRadPerSec:0.00} rad/s");
            DrawLine($"Load: {belt.CurrentTelemetry.loadKg:0.0} kg | Pull force: {belt.PullForceN:0.0} N");
            DrawLine($"Power: model {belt.EstimatedPowerW:0.0} W | measured {belt.CurrentTelemetry.measuredPowerW:0.0} W");
            DrawLine($"Throughput: {belt.ThroughputPackagesPerHour:0} packages/hour");
            DrawLine($"Status: {StatusText()}");
            GUILayout.EndArea();
        }

        private void DrawLine(string text)
        {
            GUILayout.Label(text, labelStyle);
        }

        private string StatusText()
        {
            if (belt.EmergencyStop)
            {
                return "EMERGENCY STOP";
            }

            if (belt.IsOverloaded && belt.IsOverspeed)
            {
                return "OVERLOAD + OVERSPEED";
            }

            if (belt.IsOverloaded)
            {
                return "OVERLOAD";
            }

            return belt.IsOverspeed ? "OVERSPEED" : "NORMAL";
        }
    }
}
