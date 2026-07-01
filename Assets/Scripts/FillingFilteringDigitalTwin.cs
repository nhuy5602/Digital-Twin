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
        public Transform bottleSpawnPoint;
        public Transform turntableOutlet;
        public Transform fillingNozzle;
        public Transform liquidVessel;
        public Transform vesselLiquidVisual;
        public Transform qcSensorBeam;
        public Transform pneumaticPusher;
        public Transform acceptChute;
        public Transform rejectChute;

        [Header("Bottle line")]
        public BottleProcessState bottleTemplate;
        public List<BottleProcessState> bottles = new List<BottleProcessState>();
        public float conveyorSpeedMps = 0.85f;
        public float infeedStartZ = -4.2f;
        public float fillingZ = -1.65f;
        public float qcZ = 0.85f;
        public float pusherZ = 2.25f;
        public float acceptEndZ = 4.1f;
        public float lineX = 0f;

        [Header("Turntable buffer")]
        public Vector3 turntableCenter = new Vector3(0f, 0.82f, -4.7f);
        public float turntableRadius = 0.95f;
        public float bottleDropHeight = 2.6f;
        public float bottleDropTimeSeconds = 0.45f;
        public float spawnIntervalSeconds = 0.45f;
        public int maxTurntableBuffer = 16;
        public int releaseThreshold = 7;
        public float releaseIntervalSeconds = 0.65f;
        public float outletMoveTimeSeconds = 0.42f;

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
        public int TurntableBufferCount { get; private set; }
        public int BottlesOnConveyorCount { get; private set; }

        private readonly List<BottleProcessState> turntableBottles = new List<BottleProcessState>();
        private readonly HashSet<BottleProcessState> lineBottles = new HashSet<BottleProcessState>();
        private readonly HashSet<BottleProcessState> fillingBottles = new HashSet<BottleProcessState>();
        private readonly HashSet<BottleProcessState> pushingBottles = new HashSet<BottleProcessState>();
        private readonly HashSet<BottleProcessState> droppingBottles = new HashSet<BottleProcessState>();
        private readonly HashSet<BottleProcessState> outletBottles = new HashSet<BottleProcessState>();
        private int completedCount;
        private float spawnTimer;
        private float releaseTimer;
        private int spawnedCount;
        private bool fillingStationBusy;

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
            UpdateTurntableBuffer();
            UpdateVesselVisual();
            MoveBottles();
            ThroughputBottlesPerHour = completedCount / Mathf.Max(Time.time / 3600f, 0.0001f);
            TurntableBufferCount = turntableBottles.Count;
            BottlesOnConveyorCount = lineBottles.Count;
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

        private void UpdateTurntableBuffer()
        {
            spawnTimer += Time.deltaTime;
            releaseTimer += Time.deltaTime;

            if (spawnTimer >= spawnIntervalSeconds && turntableBottles.Count + droppingBottles.Count < maxTurntableBuffer)
            {
                spawnTimer = 0f;
                SpawnBottleIntoTurntable();
            }

            OrbitBufferedBottles();

            if (releaseTimer >= releaseIntervalSeconds && turntableBottles.Count >= releaseThreshold)
            {
                releaseTimer = 0f;
                ReleaseBottleToOutlet();
            }
        }

        private void SpawnBottleIntoTurntable()
        {
            if (bottleTemplate == null)
            {
                return;
            }

            spawnedCount++;
            var spawnPosition = bottleSpawnPoint != null
                ? bottleSpawnPoint.position
                : turntableCenter + Vector3.up * bottleDropHeight;

            var bottle = Instantiate(bottleTemplate, spawnPosition, Quaternion.identity, transform);
            bottle.name = $"Bottle {spawnedCount:00}";
            bottle.gameObject.SetActive(true);
            bottle.status = BottleQualityStatus.DroppingToTurntable;
            bottle.SetVolume(0f);
            bottle.isDefective = false;
            bottle.fillingCompleted = false;
            bottle.inspectionCompleted = false;
            bottle.counted = false;
            bottles.Add(bottle);

            var slotAngle = spawnedCount * 137.5f;
            var target = TurntableSlotPosition(slotAngle, turntableBottles.Count + droppingBottles.Count);
            StartCoroutine(DropBottleToTurntable(bottle, target));
        }

        private IEnumerator DropBottleToTurntable(BottleProcessState bottle, Vector3 target)
        {
            droppingBottles.Add(bottle);
            var start = bottle.transform.position;
            var elapsed = 0f;

            while (elapsed < bottleDropTimeSeconds)
            {
                elapsed += Time.deltaTime;
                bottle.transform.position = Vector3.Lerp(start, target, elapsed / bottleDropTimeSeconds);
                yield return null;
            }

            bottle.transform.position = target;
            bottle.status = BottleQualityStatus.InTurntableBuffer;
            droppingBottles.Remove(bottle);
            turntableBottles.Add(bottle);
        }

        private void OrbitBufferedBottles()
        {
            if (turntableBottles.Count == 0)
            {
                return;
            }

            var rotationDegrees = infeedMotorSpeedRpm * 6f * Time.time;
            for (var i = 0; i < turntableBottles.Count; i++)
            {
                var bottle = turntableBottles[i];
                if (bottle == null)
                {
                    continue;
                }

                bottle.transform.position = TurntableSlotPosition(rotationDegrees + i * 360f / turntableBottles.Count, i);
            }
        }

        private Vector3 TurntableSlotPosition(float angleDegrees, int index)
        {
            var ring = index % 3;
            var radius = turntableRadius * (0.42f + ring * 0.24f);
            var angle = angleDegrees * Mathf.Deg2Rad;
            return turntableCenter + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        private void ReleaseBottleToOutlet()
        {
            for (var i = 0; i < turntableBottles.Count; i++)
            {
                var bottle = turntableBottles[i];
                if (bottle == null || outletBottles.Contains(bottle))
                {
                    continue;
                }

                turntableBottles.RemoveAt(i);
                StartCoroutine(MoveBottleToOutlet(bottle));
                return;
            }
        }

        private IEnumerator MoveBottleToOutlet(BottleProcessState bottle)
        {
            outletBottles.Add(bottle);
            bottle.status = BottleQualityStatus.MovingToOutlet;

            var start = bottle.transform.position;
            var outlet = turntableOutlet != null
                ? turntableOutlet.position
                : new Vector3(lineX, turntableCenter.y, infeedStartZ);
            var elapsed = 0f;

            while (elapsed < outletMoveTimeSeconds)
            {
                elapsed += Time.deltaTime;
                bottle.transform.position = Vector3.Lerp(start, outlet, elapsed / outletMoveTimeSeconds);
                yield return null;
            }

            bottle.transform.position = new Vector3(lineX, outlet.y, infeedStartZ);
            bottle.status = BottleQualityStatus.Empty;
            outletBottles.Remove(bottle);
            lineBottles.Add(bottle);
        }

        private void MoveBottles()
        {
            foreach (var bottle in bottles)
            {
                if (bottle == null || !lineBottles.Contains(bottle) || fillingBottles.Contains(bottle) || pushingBottles.Contains(bottle))
                {
                    continue;
                }

                var position = bottle.transform.position;

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

                position.x = lineX;

                if (!bottle.fillingCompleted && position.z >= fillingZ)
                {
                    if (fillingStationBusy)
                    {
                        position.z = fillingZ - 0.45f;
                        bottle.transform.position = position;
                    }
                    else
                    {
                        StartCoroutine(FillBottle(bottle));
                    }

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
            fillingStationBusy = true;
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
            fillingStationBusy = false;
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
