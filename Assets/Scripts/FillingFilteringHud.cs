using System.Collections.Generic;
using UnityEngine;

namespace ConveyorTwin
{
    /// <summary>
    /// In-game operator dashboard. It intentionally uses the immediate-mode UI already present in the
    /// project so the twin remains self-contained when the scene is rebuilt from the Tools menu.
    /// </summary>
    public class FillingFilteringHud : MonoBehaviour
    {
        private struct TrendSample
        {
            public float time;
            public float throughput;
            public float fillPercent;
            public float rejectPercent;
            public float vesselPercent;
        }

        public FillingFilteringDigitalTwin process;
        public Vector2 position = new Vector2(16f, 16f);
        public Vector2 size = new Vector2(700f, 650f);
        [Min(10f)] public float trendWindowSeconds = 60f;

        private readonly List<TrendSample> trend = new List<TrendSample>();
        private GUIStyle titleStyle;
        private GUIStyle metricStyle;
        private GUIStyle smallStyle;
        private Texture2D pixel;
        private TwinSetpoints draftSetpoints;
        private float nextSampleAt;

        private void Awake()
        {
            pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
        }

        private void Start()
        {
            if (process == null)
            {
                return;
            }

            // Existing scenes that were built before the web bridge was added still gain the browser dashboard.
            var webDashboard = GetComponent<TwinDashboardWebServer>();
            if (webDashboard == null)
            {
                webDashboard = gameObject.AddComponent<TwinDashboardWebServer>();
            }

            webDashboard.process = process;
        }

        private void OnDestroy()
        {
            if (pixel != null)
            {
                Destroy(pixel);
            }
        }

        private void Update()
        {
            if (process == null)
            {
                return;
            }

            if (draftSetpoints == null)
            {
                draftSetpoints = process.GetSetpoints();
            }

            if (Time.unscaledTime < nextSampleAt)
            {
                return;
            }

            nextSampleAt = Time.unscaledTime + 0.2f;
            var snapshot = process.CreateSnapshot();
            trend.Add(new TrendSample
            {
                time = snapshot.simulationSeconds,
                throughput = snapshot.throughputBottlesPerHour,
                fillPercent = snapshot.averageFillPercent,
                rejectPercent = snapshot.rejectRatePercent,
                vesselPercent = snapshot.vesselCapacityLiters > 0f
                    ? snapshot.vesselLevelLiters / snapshot.vesselCapacityLiters * 100f
                    : 0f
            });

            var earliest = snapshot.simulationSeconds - trendWindowSeconds;
            trend.RemoveAll(sample => sample.time < earliest);
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
            var panel = new Rect(position.x, position.y, Mathf.Max(700f, size.x), Mathf.Max(650f, size.y));
            DrawPanel(panel);
            var snapshot = process.CreateSnapshot();

            GUILayout.BeginArea(new Rect(panel.x + 14f, panel.y + 12f, panel.width - 28f, panel.height - 24f));
            GUILayout.Label("DIGITAL TWIN CONTROL ROOM", titleStyle);
            GUILayout.Label("What-if simulation only — no commands are sent to real equipment.", smallStyle);
            GUILayout.Space(4f);
            DrawAlert(snapshot.alert);
            GUILayout.Space(6f);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(330f));
            DrawControlPanel();
            GUILayout.EndVertical();
            GUILayout.Space(14f);
            GUILayout.BeginVertical(GUILayout.Width(320f));
            DrawKpis(snapshot);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            DrawTrendPanel();
            GUILayout.EndArea();
        }

        private void DrawControlPanel()
        {
            GUILayout.Label("SETPOINTS", titleStyle);
            draftSetpoints.conveyorSpeedMps = DrawSlider("Conveyor", draftSetpoints.conveyorSpeedMps, 0.2f, 2.5f, "m/s");
            draftSetpoints.pumpFlowLitersPerMinute = DrawSlider("Pump flow", draftSetpoints.pumpFlowLitersPerMinute, 0f, 300f, "L/min");
            draftSetpoints.infeedMotorSpeedRpm = DrawSlider("Infeed turntable", draftSetpoints.infeedMotorSpeedRpm, 5f, 60f, "rpm");
            GUILayout.Label($"Release interval: {process.EffectiveReleaseIntervalSeconds:0.00} s (RPM-linked)", smallStyle);
            GUILayout.Label($"Filling dwell: {process.EffectiveFillingDwellSeconds:0.00} s (conveyor-linked)", smallStyle);

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply", GUILayout.Height(28f)))
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

            var webServer = GetComponent<TwinDashboardWebServer>();
            if (webServer != null)
            {
                GUILayout.Space(8f);
                GUILayout.Label(webServer.ServerRunning
                    ? $"Web dashboard: {webServer.DashboardUrl}"
                    : "Web dashboard is starting on 127.0.0.1:8088…", smallStyle);
                if (GUILayout.Button("Open web dashboard", GUILayout.Height(24f)))
                {
                    webServer.OpenDashboard();
                }
            }
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

        private void DrawKpis(TwinSnapshot snapshot)
        {
            GUILayout.Label("LIVE KPI", titleStyle);
            DrawMetric("Throughput", $"{snapshot.throughputBottlesPerHour:0} bottles/h");
            DrawMetric("Average fill", $"{snapshot.averageFillPercent:0.0}%");
            DrawMetric("Last batch", $"{snapshot.lastBatchFillPercent:0.0}%");
            DrawMetric("Reject rate", $"{snapshot.rejectRatePercent:0.0}%");
            DrawMetric("Vessel", $"{snapshot.vesselLevelLiters:0.0} / {snapshot.vesselCapacityLiters:0} L");
            DrawMetric("Turntable buffer", $"{snapshot.turntableBufferCount} | line {snapshot.bottlesOnConveyorCount}");
            DrawMetric("Result", $"Pass {snapshot.totalPassed} | Reject {snapshot.totalRejected}");
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

        private void DrawTrendPanel()
        {
            GUILayout.Label("LAST 60 SECONDS", titleStyle);
            var rect = GUILayoutUtility.GetRect(660f, 192f, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(rect, pixel, ScaleMode.StretchToFill, true, 0f, new Color(0.04f, 0.07f, 0.12f, 0.92f), 0f, 4f);
            DrawTrend(rect, sample => sample.fillPercent, 0f, 100f, new Color(0.2f, 0.85f, 1f), "Fill %");
            DrawTrend(rect, sample => sample.rejectPercent, 0f, 100f, new Color(1f, 0.36f, 0.25f), "Reject %");
            DrawTrend(rect, sample => sample.vesselPercent, 0f, 100f, new Color(0.35f, 1f, 0.48f), "Vessel %");
            DrawTrend(rect, sample => sample.throughput, 0f, Mathf.Max(100f, MaxThroughput()), new Color(1f, 0.8f, 0.2f), "Throughput");
            GUI.Label(new Rect(rect.x + 8f, rect.y + 5f, rect.width - 16f, 18f), "Blue fill %   Red reject %   Green vessel %   Yellow throughput", smallStyle);
        }

        private void DrawTrend(Rect rect, System.Func<TrendSample, float> selector, float min, float max, Color color, string label)
        {
            if (trend.Count < 2)
            {
                return;
            }

            var start = trend[0].time;
            var end = Mathf.Max(start + 0.01f, trend[trend.Count - 1].time);
            var previous = ToChartPoint(rect, trend[0].time, selector(trend[0]), min, max, start, end);
            for (var index = 1; index < trend.Count; index++)
            {
                var point = ToChartPoint(rect, trend[index].time, selector(trend[index]), min, max, start, end);
                DrawLine(previous, point, color, 2f);
                previous = point;
            }
        }

        private static Vector2 ToChartPoint(Rect rect, float time, float value, float min, float max, float start, float end)
        {
            var x = Mathf.Lerp(rect.x + 4f, rect.xMax - 4f, Mathf.InverseLerp(start, end, time));
            var y = Mathf.Lerp(rect.yMax - 6f, rect.y + 24f, Mathf.InverseLerp(min, max, value));
            return new Vector2(x, y);
        }

        private void DrawLine(Vector2 from, Vector2 to, Color color, float width)
        {
            var delta = to - from;
            var matrix = GUI.matrix;
            GUI.color = color;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, from);
            GUI.DrawTexture(new Rect(from.x, from.y - width * 0.5f, delta.magnitude, width), pixel);
            GUI.matrix = matrix;
            GUI.color = Color.white;
        }

        private float MaxThroughput()
        {
            var max = 0f;
            foreach (var sample in trend)
            {
                max = Mathf.Max(max, sample.throughput);
            }

            return max;
        }

        private float DrawSlider(string label, float value, float min, float max, string unit)
        {
            GUILayout.Label($"{label}: {value:0.00} {unit}", metricStyle);
            return GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(315f));
        }

        private void DrawAlert(string alert)
        {
            var previous = GUI.color;
            GUI.color = alert == "Normal" ? new Color(0.4f, 1f, 0.55f) : new Color(1f, 0.55f, 0.25f);
            GUILayout.Label($"STATUS: {alert}", metricStyle);
            GUI.color = previous;
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
