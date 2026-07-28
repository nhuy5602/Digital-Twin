using UnityEngine;

namespace ConveyorTwin
{
    /// <summary>
    /// In-game operator dashboard. It intentionally uses the immediate-mode UI already present in the
    /// project so the twin remains self-contained when the scene is rebuilt from the Tools menu.
    /// </summary>
    public class FillingFilteringHud : MonoBehaviour
    {
        public FillingFilteringDigitalTwin process;
        public Vector2 position = new Vector2(16f, 16f);
        public Vector2 size = new Vector2(700f, 640f);

        private GUIStyle titleStyle;
        private GUIStyle metricStyle;
        private GUIStyle smallStyle;
        private Texture2D pixel;
        private TwinSetpoints draftSetpoints;

        private void Awake()
        {
            pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
        }

        private void OnDestroy()
        {
            if (pixel != null)
            {
                Destroy(pixel);
            }
        }

        private void OnGUI()
        {
            if (process == null)
            {
                return;
            }

            EnsureStyles();
            if (draftSetpoints == null)
            {
                draftSetpoints = process.GetSetpoints();
            }
            var panel = new Rect(position.x, position.y, Mathf.Max(700f, size.x), Mathf.Max(640f, size.y));
            DrawPanel(panel);
            var snapshot = process.CreateSnapshot();

            GUILayout.BeginArea(new Rect(panel.x + 14f, panel.y + 12f, panel.width - 28f, panel.height - 24f));
            GUILayout.Label("DIGITAL TWIN CONTROL ROOM", titleStyle);
            GUILayout.Label("What-if simulation only — no commands are sent to real equipment.", smallStyle);
            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(330f));
            DrawControlPanel();
            GUILayout.EndVertical();
            GUILayout.Space(14f);
            GUILayout.BeginVertical(GUILayout.Width(320f));
            DrawKpis(snapshot);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawControlPanel()
        {
            GUILayout.Label("SETPOINTS", titleStyle);
            draftSetpoints.conveyorSpeedMps = DrawSlider("Conveyor", draftSetpoints.conveyorSpeedMps, 0.2f, 2.5f, "m/s");
            draftSetpoints.pumpFlowLitersPerMinute = DrawSlider("Pump flow", draftSetpoints.pumpFlowLitersPerMinute, 0f, 300f, "L/min");
            draftSetpoints.infeedMotorSpeedRpm = DrawSlider("Infeed turntable", draftSetpoints.infeedMotorSpeedRpm, 5f, 60f, "rpm");
            draftSetpoints.starWheelIndexSpeedRpm = DrawSlider("Disc index speed", draftSetpoints.starWheelIndexSpeedRpm, 1f, 30f, "rpm");
            draftSetpoints.starWheelDwellSeconds = DrawSlider("Disc dwell", draftSetpoints.starWheelDwellSeconds, 0.10f, 5f, "s");
            ApplyDraftSetpointsIfChanged();
            GUILayout.Label("Changes apply immediately while dragging.", smallStyle);

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply now", GUILayout.Height(28f)))
            {
                process.ApplySetpoints(draftSetpoints);
            }

            if (GUILayout.Button(process.SimulationPaused ? "Resume" : "Pause", GUILayout.Height(28f)))
            {
                process.SetSimulationPaused(!process.SimulationPaused);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset", GUILayout.Height(25f)))
            {
                process.ResetSimulation(false);
            }

            if (GUILayout.Button("New seed + reset", GUILayout.Height(25f)))
            {
                process.ResetSimulation(true);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.Label("EXPERIMENT PRESETS", titleStyle);
            GUILayout.BeginHorizontal();
            DrawPresetButton("Nominal", TwinScenarioPreset.Nominal);
            DrawPresetButton("High conveyor", TwinScenarioPreset.HighConveyor);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            DrawPresetButton("Low pump flow", TwinScenarioPreset.LowPumpFlow);
            DrawPresetButton("High infeed RPM", TwinScenarioPreset.HighInfeedRpm);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            DrawPresetButton("Fast Disc index", TwinScenarioPreset.FastDiscIndex);
            DrawPresetButton("Slow Disc index", TwinScenarioPreset.SlowDiscIndex);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            DrawPresetButton("Short Disc dwell", TwinScenarioPreset.ShortDiscDwell);
            DrawPresetButton("Long Disc dwell", TwinScenarioPreset.LongDiscDwell);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            DrawPresetButton("Overflow pump test", TwinScenarioPreset.OverflowPumpTest);
            GUILayout.EndHorizontal();

        }

        private void DrawPresetButton(string label, TwinScenarioPreset preset)
        {
            if (!GUILayout.Button(label, GUILayout.Height(25f)))
            {
                return;
            }

            process.ApplyPreset(preset);
            draftSetpoints = process.GetSetpoints();
        }

        private void ApplyDraftSetpointsIfChanged()
        {
            var active = process.GetSetpoints();
            if (Mathf.Abs(draftSetpoints.conveyorSpeedMps - active.conveyorSpeedMps) > 0.0001f ||
                Mathf.Abs(draftSetpoints.pumpFlowLitersPerMinute - active.pumpFlowLitersPerMinute) > 0.0001f ||
                Mathf.Abs(draftSetpoints.infeedMotorSpeedRpm - active.infeedMotorSpeedRpm) > 0.0001f ||
                Mathf.Abs(draftSetpoints.starWheelIndexSpeedRpm - active.starWheelIndexSpeedRpm) > 0.0001f ||
                Mathf.Abs(draftSetpoints.starWheelDwellSeconds - active.starWheelDwellSeconds) > 0.0001f)
            {
                process.ApplySetpoints(draftSetpoints);
            }
        }

        private void DrawKpis(TwinSnapshot snapshot)
        {
            GUILayout.Label("LIVE KPI", titleStyle);
            DrawMetric("Throughput", $"{snapshot.throughputBottlesPerHour:0} bottles/h");
            DrawMetric("Last batch", $"{snapshot.lastBatchFillPercent:0.0}%");
            DrawMetric("Reject rate", $"{snapshot.rejectRatePercent:0.0}%");
            DrawMetric("Turntable buffer", $"{snapshot.turntableBufferCount} | line {snapshot.bottlesOnConveyorCount}");
            DrawMetric("Result", $"Pass {snapshot.totalPassed} | Reject {snapshot.totalRejected}");
            DrawMetric("Overflow", snapshot.totalOverflowed.ToString());
            DrawMetric("Reject escapes", snapshot.totalRejectEscapes.ToString());
            DrawMetric("Disc", $"{snapshot.starWheelIndexSpeedRpm:0.00} rpm | dwell {snapshot.starWheelDwellSeconds:0.00} s");
            DrawMetric("Turntable omega", $"{snapshot.angularSpeedRadPerSec:0.00} rad/s");
            DrawMetric("Centrifugal a", $"{snapshot.centrifugalAccelerationMps2:0.00} m/s²");
            DrawMetric("Star wheel", snapshot.starWheelPhase);
        }

        private void DrawMetric(string name, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name, smallStyle, GUILayout.Width(132f));
            GUILayout.Label(value, metricStyle);
            GUILayout.EndHorizontal();
        }

        private float DrawSlider(string label, float value, float min, float max, string unit)
        {
            GUILayout.Label($"{label}: {value:0.00} {unit}", metricStyle);
            return GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(315f));
        }

        private void DrawPanel(Rect rect)
        {
            GUI.DrawTexture(rect, pixel, ScaleMode.StretchToFill, true, 0f, new Color(0.02f, 0.035f, 0.07f, 0.94f), 0f, 6f);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.45f, 0.84f, 1f) }
            };
            metricStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = Color.white }
            };
            smallStyle = new GUIStyle(metricStyle)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.72f, 0.8f, 0.88f) }
            };
        }
    }
}
