using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ConveyorTwin
{
    public enum InspectionStatus
    {
        Normal,
        AnomalyDetected
    }

    public class FillingFilteringDigitalTwin : MonoBehaviour
    {
        [Header("Stations")]
        public Transform infeedTurntable;
        public Transform fillingNozzle;
        public Transform liquidVessel;
        public Transform vesselLiquidVisual;
        public Transform qcSensorBeam;
        public Transform pneumaticPusher;
        public Transform acceptChute;
        public Transform rejectChute;

        [Header("Bottle line")]
        public List<BottleProcessState> bottles = new List<BottleProcessState>();
        public float conveyorSpeedMps = 0.85f;
        public float infeedStartZ = -4.2f;
        public float fillingZ = -1.65f;
        public float qcZ = 0.85f;
        public float pusherZ = 2.25f;
        public float acceptEndZ = 4.1f;
        public float lineX = 0f;

        [Header("Process settings")]
        [Range(0f, 1f)] public float properFillProbability = 0.9f;
        [Range(0f, 1f)] public float passThreshold = 0.95f;
        public float fillingTimeSeconds = 1.35f;
        public float bottleCapacityLiters = 1f;
        public float initialVesselLevelLiters = 120f;
        public float vesselCapacityLiters = 150f;
        public float infeedMotorSpeedRpm = 18f;

        public float ThroughputBottlesPerHour { get; private set; }
        public float InfeedMotorSpeedRpm => infeedMotorSpeedRpm;
        public float LiquidLevelLiters { get; private set; }
        public float LastFillingTimeSeconds { get; private set; }
        public InspectionStatus InspectionStatus { get; private set; } = InspectionStatus.Normal;
        public int TotalPassed { get; private set; }
        public int TotalRejected { get; private set; }

        private readonly HashSet<BottleProcessState> fillingBottles = new HashSet<BottleProcessState>();
        private readonly HashSet<BottleProcessState> pushingBottles = new HashSet<BottleProcessState>();
        private int completedCount;

        private void Awake()
        {
            LiquidLevelLiters = initialVesselLevelLiters;
            foreach (var bottle in bottles)
            {
                if (bottle != null)
                {
                    bottle.RefreshVisuals();
                }
            }
        }

        private void Update()
        {
            AnimateMachines();
            UpdateVesselVisual();
            MoveBottles();
            ThroughputBottlesPerHour = completedCount / Mathf.Max(Time.time / 3600f, 0.0001f);
        }

        private void AnimateMachines()
        {
            if (infeedTurntable != null)
            {
                infeedTurntable.Rotate(Vector3.up, infeedMotorSpeedRpm * 6f * Time.deltaTime, Space.World);
            }

            if (qcSensorBeam != null)
            {
                var scale = qcSensorBeam.localScale;
                scale.x = 1f + Mathf.Sin(Time.time * 10f) * 0.08f;
                qcSensorBeam.localScale = scale;
            }
        }

        private void MoveBottles()
        {
            foreach (var bottle in bottles)
            {
                if (bottle == null || fillingBottles.Contains(bottle) || pushingBottles.Contains(bottle))
                {
                    continue;
                }

                var position = bottle.transform.position;
                position.x = lineX;

                if (bottle.status == BottleQualityStatus.AcceptedBin)
                {
                    position += new Vector3(0.55f, -0.28f, 0.55f) * Time.deltaTime;
                    bottle.transform.position = position;
                    continue;
                }

                if (bottle.status == BottleQualityStatus.RejectedBin)
                {
                    position += new Vector3(-0.65f, -0.25f, 0.2f) * Time.deltaTime;
                    bottle.transform.position = position;
                    continue;
                }

                if (!bottle.fillingCompleted && position.z >= fillingZ)
                {
                    StartCoroutine(FillBottle(bottle));
                    continue;
                }

                if (bottle.fillingCompleted && !bottle.inspectionCompleted && position.z >= qcZ)
                {
                    InspectBottle(bottle);
                }

                if (bottle.status == BottleQualityStatus.Rejected && position.z >= pusherZ)
                {
                    StartCoroutine(TriggerPusher(bottle));
                    continue;
                }

                if (bottle.status == BottleQualityStatus.Passed && position.z >= acceptEndZ)
                {
                    bottle.status = BottleQualityStatus.AcceptedBin;
                    CountBottle(bottle, true);
                    bottle.RefreshVisuals();
                    continue;
                }

                position.z += conveyorSpeedMps * Time.deltaTime;
                bottle.transform.position = position;
            }
        }

        private IEnumerator FillBottle(BottleProcessState bottle)
        {
            fillingBottles.Add(bottle);
            bottle.status = BottleQualityStatus.Filling;
            bottle.transform.position = new Vector3(lineX, bottle.transform.position.y, fillingZ);

            var targetVolume = Random.value <= properFillProbability ? 1f : Random.Range(0.5f, 0.6f);
            bottle.isDefective = targetVolume < passThreshold;

            var elapsed = 0f;
            while (elapsed < fillingTimeSeconds)
            {
                elapsed += Time.deltaTime;
                var volume = Mathf.Lerp(0f, targetVolume, elapsed / fillingTimeSeconds);
                bottle.SetVolume(volume);
                yield return null;
            }

            bottle.SetVolume(targetVolume);
            bottle.status = BottleQualityStatus.Filled;
            bottle.fillingCompleted = true;
            LastFillingTimeSeconds = fillingTimeSeconds;
            LiquidLevelLiters = Mathf.Max(0f, LiquidLevelLiters - targetVolume * bottleCapacityLiters);
            fillingBottles.Remove(bottle);
        }

        private void InspectBottle(BottleProcessState bottle)
        {
            bottle.inspectionCompleted = true;
            if (bottle.liquidVolume01 >= passThreshold)
            {
                InspectionStatus = InspectionStatus.Normal;
                bottle.MarkPassed();
            }
            else
            {
                InspectionStatus = InspectionStatus.AnomalyDetected;
                bottle.MarkRejected();
            }
        }

        private IEnumerator TriggerPusher(BottleProcessState bottle)
        {
            pushingBottles.Add(bottle);
            bottle.transform.position = new Vector3(lineX, bottle.transform.position.y, pusherZ);

            var basePosition = pneumaticPusher != null ? pneumaticPusher.localPosition : Vector3.zero;
            var extendedPosition = basePosition + new Vector3(-0.65f, 0f, 0f);

            yield return MovePusher(basePosition, extendedPosition, 0.18f);
            bottle.transform.position += new Vector3(-1.1f, -0.1f, 0f);
            bottle.status = BottleQualityStatus.RejectedBin;
            bottle.RefreshVisuals();
            CountBottle(bottle, false);
            yield return MovePusher(extendedPosition, basePosition, 0.22f);

            pushingBottles.Remove(bottle);
        }

        private IEnumerator MovePusher(Vector3 from, Vector3 to, float duration)
        {
            if (pneumaticPusher == null)
            {
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                pneumaticPusher.localPosition = Vector3.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            pneumaticPusher.localPosition = to;
        }

        private void CountBottle(BottleProcessState bottle, bool passed)
        {
            if (bottle.counted)
            {
                return;
            }

            bottle.counted = true;
            completedCount++;

            if (passed)
            {
                TotalPassed++;
            }
            else
            {
                TotalRejected++;
            }
        }

        private void UpdateVesselVisual()
        {
            if (vesselLiquidVisual == null)
            {
                return;
            }

            var fillRatio = vesselCapacityLiters > 0f ? LiquidLevelLiters / vesselCapacityLiters : 0f;
            var scale = vesselLiquidVisual.localScale;
            scale.y = Mathf.Clamp(fillRatio, 0.05f, 1f);
            vesselLiquidVisual.localScale = scale;
        }
    }
}
