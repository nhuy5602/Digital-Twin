using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ConveyorTwin
{
    public class CsvTelemetryPlayer : MonoBehaviour
    {
        [Serializable]
        private struct TelemetryFrame
        {
            public float timeS;
            public ConveyorTelemetry telemetry;
        }

        public TextAsset csvFile;
        public bool loop = true;

        private readonly List<TelemetryFrame> frames = new List<TelemetryFrame>();
        private float startTime;

        private void Awake()
        {
            startTime = Time.time;
            LoadCsv();
        }

        public ConveyorTelemetry GetLatestTelemetry()
        {
            if (frames.Count == 0)
            {
                return new ConveyorTelemetry();
            }

            var elapsed = Time.time - startTime;
            var duration = frames[frames.Count - 1].timeS;
            if (loop && duration > 0f)
            {
                elapsed %= duration;
            }

            var selected = frames[0].telemetry;
            for (var i = 0; i < frames.Count; i++)
            {
                if (frames[i].timeS > elapsed)
                {
                    break;
                }

                selected = frames[i].telemetry;
            }

            return selected;
        }

        private void LoadCsv()
        {
            frames.Clear();
            if (csvFile == null)
            {
                return;
            }

            var lines = csvFile.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 1; i < lines.Length; i++)
            {
                var columns = lines[i].Split(',');
                if (columns.Length < 7)
                {
                    continue;
                }

                frames.Add(new TelemetryFrame
                {
                    timeS = ParseFloat(columns[0]),
                    telemetry = new ConveyorTelemetry
                    {
                        beltSpeedMps = ParseFloat(columns[1]),
                        loadKg = ParseFloat(columns[2]),
                        packageCountPerMinute = ParseFloat(columns[3]),
                        motorTorqueNm = ParseFloat(columns[4]),
                        measuredPowerW = ParseFloat(columns[5]),
                        emergencyStop = bool.TryParse(columns[6], out var stop) && stop
                    }
                });
            }
        }

        private static float ParseFloat(string value)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0f;
        }
    }
}
