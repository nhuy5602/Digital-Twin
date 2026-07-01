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
        public List<Transform> fillingNozzles = new List<Transform>();
        public Transform fillingStopGate;
        public Transform fillingStarWheel;
        public Transform liquidVessel;
        public Transform vesselLiquidVisual;
        public Transform qcSensorBeam;
        public Transform cappingHead;
        public List<Transform> cappingHeads = new List<Transform>();
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
        public float cappingZ = 3.2f;
        public float acceptEndZ = 4.1f;
        public float lineX = 0f;

        [Header("Filling indexing")]
        public int fillingNozzleCount = 4;
        public float fillingFirstZ = -1.2f;
        public float fillingQueueStopZ = -2.45f;
        public float fillingSlotToleranceM = 0.03f;
        public int starWheelPocketCount = 8;
        public Vector3 starWheelCenter = new Vector3(0.78f, 0.82f, -0.68f);
        public float starWheelPocketRadius = 0.78f;
        public float starWheelEntryAngleDegrees = 220f;
        public float starWheelIndexDurationSeconds = 0.32f;

        [Header("Capping indexing")]
        public int cappingHeadCount = 4;
        public float cappingFirstZ = 1.65f;
        public float cappingPitchM = 0.42f;
        public float cappingQueueStopZ = 2.75f;
        public float cappingSlotToleranceM = 0.03f;
        public float cappingTimeSeconds = 0.75f;

        [Header("Slat chain conveyor")]
        public float slatPitchM = 0.22f;
        [Range(0f, 0.25f)] public float conveyorSlipRatio = 0.02f;
        public float minimumBottleSpacingM = 0.46f;
        public float ConveyorEffectiveSpeedMps => conveyorSpeedMps * (1f - conveyorSlipRatio);

        [Header("Turntable buffer")]
        public Vector3 turntableCenter = new Vector3(0f, 0.82f, -4.7f);
        public float turntableRadius = 0.95f;
        public float bottleDropHeight = 2.6f;
        public float bottleDropTimeSeconds = 0.45f;
        public float spawnIntervalSeconds = 0.45f;
        public int initialTurntableBottleCount = 12;
        public int maxTurntableBuffer = 16;
        public int releaseThreshold = 7;
        public float releaseIntervalSeconds = 0.65f;
        public float outletMoveTimeSeconds = 0.42f;
        public float turntableSurfaceGrip = 3.5f;
        public float turntableVelocityDamping = 0.96f;
        public float outletCaptureRadius = 0.88f;
        public float outletAngleToleranceDegrees = 28f;

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
        public float TurntableAngularSpeedRadPerSec { get; private set; }
        public float CentrifugalAccelerationAtRimMps2 { get; private set; }
        public bool ConveyorStoppedForFilling { get; private set; }
        public bool TurntablePaused { get; private set; }
        public bool StarWheelLocked { get; private set; }
        public bool StarWheelIndexing { get; private set; }
        public float StarWheelStepAngleDegrees => 360f / Mathf.Max(1, starWheelPocketCount);
        public float StarWheelPocketPitchM => Mathf.PI * 2f * starWheelPocketRadius / Mathf.Max(1, starWheelPocketCount);
        public bool CappingActive { get; private set; }
        public int BottlesAtFillingStation { get; private set; }
        public int BottlesAtCappingStation { get; private set; }
        public bool ConveyorStoppedForCapping { get; private set; }
        public int ActiveFillingNozzleCount => Mathf.Max(1, Mathf.Min(fillingNozzleCount, fillingNozzles.Count > 0 ? fillingNozzles.Count : fillingNozzleCount));
        public int ActiveCappingHeadCount => Mathf.Max(1, Mathf.Min(cappingHeadCount, cappingHeads.Count > 0 ? cappingHeads.Count : cappingHeadCount));

        private readonly List<BottleProcessState> turntableBottles = new List<BottleProcessState>();
        private readonly HashSet<BottleProcessState> lineBottles = new HashSet<BottleProcessState>();
        private readonly HashSet<BottleProcessState> fillingBottles = new HashSet<BottleProcessState>();
        private readonly HashSet<BottleProcessState> cappingBottles = new HashSet<BottleProcessState>();
        private readonly HashSet<BottleProcessState> pushingBottles = new HashSet<BottleProcessState>();
        private readonly HashSet<BottleProcessState> droppingBottles = new HashSet<BottleProcessState>();
        private readonly HashSet<BottleProcessState> outletBottles = new HashSet<BottleProcessState>();
        private readonly Dictionary<BottleProcessState, int> fillingSlotAssignments = new Dictionary<BottleProcessState, int>();
        private readonly Dictionary<BottleProcessState, int> cappingSlotAssignments = new Dictionary<BottleProcessState, int>();
        private int completedCount;
        private float spawnTimer;
        private float releaseTimer;
        private int spawnedCount;
        private bool fillingStationBusy;
        private bool cappingStationBusy;
        private bool outletOccupied;
        private bool initializedTurntable;
        private int starWheelIndex;

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
            InitializeTurntableIfNeeded();
            AnimateMachines();
            UpdateTurntableBuffer();
            UpdateFillingGateVisual();
            UpdateVesselVisual();
            MoveBottles();
            ThroughputBottlesPerHour = completedCount / Mathf.Max(Time.time / 3600f, 0.0001f);
            TurntableBufferCount = turntableBottles.Count;
            BottlesOnConveyorCount = lineBottles.Count;
            BottlesAtFillingStation = fillingSlotAssignments.Count;
            BottlesAtCappingStation = cappingSlotAssignments.Count;
            ConveyorStoppedForFilling = fillingStationBusy;
            ConveyorStoppedForCapping = cappingStationBusy;
            StarWheelLocked = fillingStationBusy || StarWheelIndexing;
            CappingActive = cappingStationBusy;
        }

        private void AnimateMachines()
        {
            if (infeedTurntable != null)
            {
                if (!TurntablePaused)
                {
                    infeedTurntable.Rotate(Vector3.up, infeedMotorSpeedRpm * 6f * Time.deltaTime, Space.World);
                }
            }

            if (qcSensorBeam != null)
            {
                var scale = qcSensorBeam.localScale;
                scale.x = 1f + Mathf.Sin(Time.time * 10f) * 0.08f;
                qcSensorBeam.localScale = scale;
            }

            // The processing star wheel is indexed by coroutine, one tooth pitch at a time.
        }

        private void InitializeTurntableIfNeeded()
        {
            if (initializedTurntable || bottleTemplate == null)
            {
                return;
            }

            initializedTurntable = true;
            var count = Mathf.Min(initialTurntableBottleCount, maxTurntableBuffer);
            for (var i = 0; i < count; i++)
            {
                var angle = i * 360f / Mathf.Max(1, count) + (i % 3) * 18f;
                var ring = i % 3;
                var radius = turntableRadius * (0.28f + ring * 0.22f);
                var position = TurntablePosition(angle, radius);
                var bottle = CreateBottleInstance(position);
                bottle.status = BottleQualityStatus.InTurntableBuffer;
                bottle.turntableVelocity = Random.insideUnitCircle * 0.04f;
                turntableBottles.Add(bottle);
            }

            TurntableBufferCount = turntableBottles.Count;
            spawnTimer = 0f;
            releaseTimer = releaseIntervalSeconds;
        }

        private void UpdateTurntableBuffer()
        {
            spawnTimer += Time.deltaTime;
            releaseTimer += Time.deltaTime;
            TurntableAngularSpeedRadPerSec = infeedMotorSpeedRpm * Mathf.PI * 2f / 60f;
            CentrifugalAccelerationAtRimMps2 = TurntableAngularSpeedRadPerSec * TurntableAngularSpeedRadPerSec * turntableRadius;
            TurntablePaused = IsConveyorStopped() && turntableBottles.Count >= releaseThreshold;

            if (TurntablePaused)
            {
                return;
            }

            if (spawnTimer >= spawnIntervalSeconds && turntableBottles.Count + droppingBottles.Count < maxTurntableBuffer)
            {
                spawnTimer = 0f;
                SpawnBottleIntoTurntable();
            }

            UpdateTurntablePhysics();
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
            ResetBottle(bottle);
            bottle.status = BottleQualityStatus.DroppingToTurntable;

            var angle = spawnedCount * 137.5f * Mathf.Deg2Rad;
            var radius = Random.Range(0.05f, turntableRadius * 0.25f);
            var target = turntableCenter + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
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
            bottle.turntableVelocity = Random.insideUnitCircle * 0.05f;
            droppingBottles.Remove(bottle);
            turntableBottles.Add(bottle);
        }

        private BottleProcessState CreateBottleInstance(Vector3 position)
        {
            spawnedCount++;
            var bottle = Instantiate(bottleTemplate, position, Quaternion.identity, transform);
            ResetBottle(bottle);
            return bottle;
        }

        private void ResetBottle(BottleProcessState bottle)
        {
            bottle.name = $"Bottle {spawnedCount:00}";
            bottle.gameObject.SetActive(true);
            bottle.SetVolume(0f);
            bottle.isDefective = false;
            bottle.fillingCompleted = false;
            bottle.inspectionCompleted = false;
            bottle.cappingCompleted = false;
            bottle.counted = false;
            bottle.turntableVelocity = Vector2.zero;
            bottles.Add(bottle);
        }

        private Vector3 TurntablePosition(float angleDegrees, float radius)
        {
            var angle = angleDegrees * Mathf.Deg2Rad;
            return turntableCenter + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        private void UpdateTurntablePhysics()
        {
            if (turntableBottles.Count == 0)
            {
                return;
            }

            for (var i = turntableBottles.Count - 1; i >= 0; i--)
            {
                var bottle = turntableBottles[i];
                if (bottle == null)
                {
                    continue;
                }

                if (outletBottles.Contains(bottle))
                {
                    continue;
                }

                var position = bottle.transform.position;
                var radial = new Vector2(position.x - turntableCenter.x, position.z - turntableCenter.z);
                if (radial.sqrMagnitude < 0.0001f)
                {
                    radial = Random.insideUnitCircle.normalized * 0.02f;
                }

                var radius = radial.magnitude;
                var radialDirection = radial / Mathf.Max(radius, 0.0001f);
                var tangentDirection = new Vector2(-radialDirection.y, radialDirection.x);
                var tableSurfaceVelocity = tangentDirection * TurntableAngularSpeedRadPerSec * radius;

                // Centrifugal term: a = omega^2 * r, plus surface grip that drags bottles with the rotating table.
                var centrifugalAcceleration = radialDirection * TurntableAngularSpeedRadPerSec * TurntableAngularSpeedRadPerSec * radius;
                var gripAcceleration = (tableSurfaceVelocity - bottle.turntableVelocity) * turntableSurfaceGrip;
                bottle.turntableVelocity += (centrifugalAcceleration + gripAcceleration) * Time.deltaTime;
                bottle.turntableVelocity *= Mathf.Pow(turntableVelocityDamping, Time.deltaTime * 60f);

                radial += bottle.turntableVelocity * Time.deltaTime;
                radius = radial.magnitude;

                if (radius > turntableRadius)
                {
                    radial = radial.normalized * turntableRadius;
                    var outwardSpeed = Vector2.Dot(bottle.turntableVelocity, radial.normalized);
                    if (outwardSpeed > 0f)
                    {
                        bottle.turntableVelocity -= radial.normalized * outwardSpeed;
                    }
                }

                bottle.transform.position = new Vector3(turntableCenter.x + radial.x, turntableCenter.y, turntableCenter.z + radial.y);

                if (CanReleaseThroughOutlet(bottle))
                {
                    turntableBottles.RemoveAt(i);
                    releaseTimer = 0f;
                    StartCoroutine(MoveBottleToOutlet(bottle));
                }
            }
        }

        private bool CanReleaseThroughOutlet(BottleProcessState bottle)
        {
            if (outletOccupied || IsConveyorStopped() || IsLineStartBlocked() || releaseTimer < releaseIntervalSeconds || turntableBottles.Count < releaseThreshold)
            {
                return false;
            }

            var outletPosition = turntableOutlet != null ? turntableOutlet.position : new Vector3(lineX, turntableCenter.y, infeedStartZ);
            var bottleVector = bottle.transform.position - turntableCenter;
            var outletVector = outletPosition - turntableCenter;
            bottleVector.y = 0f;
            outletVector.y = 0f;

            if (bottleVector.magnitude < outletCaptureRadius)
            {
                return false;
            }

            return Vector3.Angle(bottleVector, outletVector) <= outletAngleToleranceDegrees;
        }

        private IEnumerator MoveBottleToOutlet(BottleProcessState bottle)
        {
            outletOccupied = true;
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
            outletOccupied = false;
        }

        private void MoveBottles()
        {
            var orderedBottles = new List<BottleProcessState>(bottles);
            orderedBottles.Sort((left, right) =>
            {
                if (left == null && right == null)
                {
                    return 0;
                }

                if (left == null)
                {
                    return 1;
                }

                if (right == null)
                {
                    return -1;
                }

                return right.transform.position.z.CompareTo(left.transform.position.z);
            });

            foreach (var bottle in orderedBottles)
            {
                if (bottle == null || !lineBottles.Contains(bottle) || fillingBottles.Contains(bottle) || cappingBottles.Contains(bottle) || pushingBottles.Contains(bottle))
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

                if (IsConveyorStopped())
                {
                    bottle.transform.position = position;
                    continue;
                }

                position.x = lineX;

                if (!bottle.fillingCompleted)
                {
                    if (!fillingSlotAssignments.ContainsKey(bottle) && position.z >= FillingEntryZ)
                    {
                        if (CanCaptureBottleForFilling())
                        {
                            StartCoroutine(CaptureBottleIntoStarWheel(bottle));
                            continue;
                        }

                        position.z = Mathf.Min(position.z, fillingQueueStopZ);
                        bottle.transform.position = position;
                        continue;
                    }
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

                if (bottle.status == BottleQualityStatus.Passed && !bottle.cappingCompleted)
                {
                    if (!cappingSlotAssignments.ContainsKey(bottle) && position.z >= cappingQueueStopZ)
                    {
                        if (cappingSlotAssignments.Count < ActiveCappingHeadCount)
                        {
                            AssignCappingSlot(bottle);
                        }
                        else
                        {
                            position.z = Mathf.Min(position.z, cappingQueueStopZ);
                            bottle.transform.position = position;
                            continue;
                        }
                    }

                    if (cappingSlotAssignments.TryGetValue(bottle, out var cappingSlotIndex))
                    {
                        var targetZ = CappingSlotZ(cappingSlotIndex);
                        if (position.z >= targetZ)
                        {
                            SnapBottleToCappingSlot(bottle);
                            TryStartCappingBatch();
                            continue;
                        }
                    }
                }

                if (bottle.status == BottleQualityStatus.Capped && position.z >= acceptEndZ)
                {
                    bottle.status = BottleQualityStatus.AcceptedBin;
                    CountBottle(bottle, true);
                    bottle.RefreshVisuals();
                    continue;
                }

                position.z += ConveyorEffectiveSpeedMps * Time.deltaTime;
                position.z = KeepBottleSpacing(bottle, position.z);
                bottle.transform.position = position;
            }
        }

        private float KeepBottleSpacing(BottleProcessState currentBottle, float candidateZ)
        {
            var nearestAheadZ = float.PositiveInfinity;
            foreach (var otherBottle in lineBottles)
            {
                if (otherBottle == null || otherBottle == currentBottle)
                {
                    continue;
                }

                var otherZ = otherBottle.transform.position.z;
                if (otherZ > candidateZ && otherZ < nearestAheadZ)
                {
                    nearestAheadZ = otherZ;
                }
            }

            if (float.IsPositiveInfinity(nearestAheadZ))
            {
                return candidateZ;
            }

            return Mathf.Min(candidateZ, nearestAheadZ - minimumBottleSpacingM);
        }

        private bool IsConveyorStopped()
        {
            return fillingStationBusy || cappingStationBusy || StarWheelIndexing;
        }

        private bool IsLineStartBlocked()
        {
            foreach (var bottle in lineBottles)
            {
                if (bottle == null)
                {
                    continue;
                }

                var bottleZ = bottle.transform.position.z;
                if (bottleZ < infeedStartZ + minimumBottleSpacingM)
                {
                    return true;
                }
            }

            return false;
        }

        private float FillingEntryZ => FillingSlotPosition(0).z;

        private bool CanCaptureBottleForFilling()
        {
            return !fillingStationBusy && !StarWheelIndexing && fillingSlotAssignments.Count < ActiveFillingNozzleCount;
        }

        private IEnumerator CaptureBottleIntoStarWheel(BottleProcessState bottle)
        {
            if (bottle == null || !CanCaptureBottleForFilling())
            {
                yield break;
            }

            fillingBottles.Add(bottle);
            bottle.transform.position = new Vector3(lineX, starWheelCenter.y, FillingEntryZ);

            var indexedBottles = new Dictionary<BottleProcessState, int>();
            foreach (var entry in fillingSlotAssignments)
            {
                if (entry.Key != null)
                {
                    indexedBottles[entry.Key] = entry.Value;
                }
            }

            if (indexedBottles.Count > 0)
            {
                yield return IndexStarWheelOnePitch(indexedBottles, 1);
                foreach (var entry in indexedBottles)
                {
                    fillingSlotAssignments[entry.Key] = Mathf.Clamp(entry.Value + 1, 0, ActiveFillingNozzleCount - 1);
                }
            }

            fillingSlotAssignments[bottle] = 0;
            SnapBottleToFillingSlot(bottle);
            TryStartFillingBatch();
        }

        private void TryStartFillingBatch()
        {
            if (fillingStationBusy || fillingSlotAssignments.Count < ActiveFillingNozzleCount)
            {
                return;
            }

            foreach (var entry in fillingSlotAssignments)
            {
                if (entry.Key == null || Vector3.Distance(entry.Key.transform.position, FillingSlotPosition(entry.Value)) > fillingSlotToleranceM)
                {
                    return;
                }
            }

            StartCoroutine(FillBottleBatch());
        }

        private IEnumerator FillBottleBatch()
        {
            fillingStationBusy = true;
            var batch = new List<BottleProcessState>(fillingSlotAssignments.Keys);
            var targets = new Dictionary<BottleProcessState, float>();

            foreach (var bottle in batch)
            {
                if (bottle == null)
                {
                    continue;
                }

                fillingBottles.Add(bottle);
                bottle.status = BottleQualityStatus.Filling;
                SnapBottleToFillingSlot(bottle);

                var targetVolume = Random.value <= properFillProbability ? 1f : Random.Range(0.5f, 0.6f);
                bottle.isDefective = targetVolume < passThreshold;
                targets[bottle] = targetVolume;
            }

            var elapsed = 0f;
            while (elapsed < fillingTimeSeconds)
            {
                elapsed += Time.deltaTime;
                var ratio = elapsed / fillingTimeSeconds;
                foreach (var bottle in batch)
                {
                    if (bottle != null && targets.TryGetValue(bottle, out var targetVolume))
                    {
                        SnapBottleToFillingSlot(bottle);
                        bottle.SetVolume(Mathf.Lerp(0f, targetVolume, ratio));
                    }
                }

                yield return null;
            }

            var totalFilledVolume = 0f;
            foreach (var bottle in batch)
            {
                if (bottle == null || !targets.TryGetValue(bottle, out var targetVolume))
                {
                    continue;
                }

                bottle.SetVolume(targetVolume);
                bottle.status = BottleQualityStatus.Filled;
                bottle.fillingCompleted = true;
                totalFilledVolume += targetVolume;
            }

            LastFillingTimeSeconds = fillingTimeSeconds;
            LiquidLevelLiters = Mathf.Max(0f, LiquidLevelLiters - totalFilledVolume * bottleCapacityLiters);
            yield return ReleaseFilledBatchToConveyor(batch);

            foreach (var bottle in batch)
            {
                if (bottle != null)
                {
                    fillingBottles.Remove(bottle);
                }
            }

            fillingSlotAssignments.Clear();
            fillingStationBusy = false;
        }

        private void SnapBottleToFillingSlot(BottleProcessState bottle)
        {
            if (bottle == null || !fillingSlotAssignments.TryGetValue(bottle, out var slotIndex))
            {
                return;
            }

            bottle.transform.position = FillingSlotPosition(slotIndex);
        }

        private void AssignCappingSlot(BottleProcessState bottle)
        {
            var slotIndex = ActiveCappingHeadCount - 1 - cappingSlotAssignments.Count;
            cappingSlotAssignments[bottle] = Mathf.Clamp(slotIndex, 0, ActiveCappingHeadCount - 1);
        }

        private float CappingSlotZ(int slotIndex)
        {
            return CappingSlotPosition(slotIndex).z;
        }

        private Vector3 FillingSlotPosition(int slotIndex)
        {
            var angle = FillingSlotAngleDegrees(slotIndex) * Mathf.Deg2Rad;
            return new Vector3(
                starWheelCenter.x + Mathf.Cos(angle) * starWheelPocketRadius,
                starWheelCenter.y,
                starWheelCenter.z + Mathf.Sin(angle) * starWheelPocketRadius);
        }

        private Vector3 CappingSlotPosition(int slotIndex)
        {
            return new Vector3(lineX, starWheelCenter.y, cappingFirstZ + slotIndex * cappingPitchM);
        }

        private float FillingSlotAngleDegrees(int slotIndex)
        {
            return starWheelEntryAngleDegrees - slotIndex * StarWheelStepAngleDegrees;
        }

        private IEnumerator IndexStarWheelOnePitch(Dictionary<BottleProcessState, int> indexedBottles, int slotDelta)
        {
            if (fillingStarWheel == null || indexedBottles.Count == 0)
            {
                yield break;
            }

            StarWheelIndexing = true;
            var startRotation = fillingStarWheel.localRotation;
            var targetRotation = startRotation * Quaternion.Euler(0f, -slotDelta * StarWheelStepAngleDegrees, 0f);
            starWheelIndex = (starWheelIndex + slotDelta) % Mathf.Max(1, starWheelPocketCount);
            var elapsed = 0f;
            var duration = Mathf.Max(0.05f, starWheelIndexDurationSeconds);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var ratio = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                fillingStarWheel.localRotation = Quaternion.Slerp(startRotation, targetRotation, ratio);

                foreach (var entry in indexedBottles)
                {
                    var bottle = entry.Key;
                    if (bottle != null)
                    {
                        bottle.transform.position = StarWheelSlotPosition(Mathf.Lerp(entry.Value, entry.Value + slotDelta, ratio));
                    }
                }

                yield return null;
            }

            fillingStarWheel.localRotation = targetRotation;
            foreach (var entry in indexedBottles)
            {
                var bottle = entry.Key;
                if (bottle != null)
                {
                    bottle.transform.position = StarWheelSlotPosition(entry.Value + slotDelta);
                }
            }

            StarWheelIndexing = false;
        }

        private Vector3 StarWheelSlotPosition(float slotIndex)
        {
            var angle = (starWheelEntryAngleDegrees - slotIndex * StarWheelStepAngleDegrees) * Mathf.Deg2Rad;
            return new Vector3(
                starWheelCenter.x + Mathf.Cos(angle) * starWheelPocketRadius,
                starWheelCenter.y,
                starWheelCenter.z + Mathf.Sin(angle) * starWheelPocketRadius);
        }

        private IEnumerator ReleaseFilledBatchToConveyor(List<BottleProcessState> batch)
        {
            var startPositions = new Dictionary<BottleProcessState, Vector3>();
            var endPositions = new Dictionary<BottleProcessState, Vector3>();
            var exitZ = FillingSlotPosition(ActiveFillingNozzleCount - 1).z + minimumBottleSpacingM * 0.4f;

            foreach (var bottle in batch)
            {
                if (bottle == null || !fillingSlotAssignments.TryGetValue(bottle, out var slotIndex))
                {
                    continue;
                }

                startPositions[bottle] = bottle.transform.position;
                var queueOffset = ActiveFillingNozzleCount - 1 - slotIndex;
                endPositions[bottle] = new Vector3(lineX, starWheelCenter.y, exitZ - queueOffset * minimumBottleSpacingM);
            }

            var elapsed = 0f;
            var duration = Mathf.Max(0.12f, starWheelIndexDurationSeconds);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var ratio = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                foreach (var entry in startPositions)
                {
                    if (entry.Key != null && endPositions.TryGetValue(entry.Key, out var endPosition))
                    {
                        entry.Key.transform.position = Vector3.Lerp(entry.Value, endPosition, ratio);
                    }
                }

                yield return null;
            }

            foreach (var entry in endPositions)
            {
                if (entry.Key != null)
                {
                    entry.Key.transform.position = entry.Value;
                }
            }
        }

        private void TryStartCappingBatch()
        {
            if (cappingStationBusy || cappingSlotAssignments.Count < ActiveCappingHeadCount)
            {
                return;
            }

            foreach (var entry in cappingSlotAssignments)
            {
                if (entry.Key == null || Vector3.Distance(entry.Key.transform.position, CappingSlotPosition(entry.Value)) > cappingSlotToleranceM)
                {
                    return;
                }
            }

            StartCoroutine(CapBottleBatch());
        }

        private IEnumerator CapBottleBatch()
        {
            cappingStationBusy = true;
            CappingActive = true;
            var batch = new List<BottleProcessState>(cappingSlotAssignments.Keys);

            foreach (var bottle in batch)
            {
                if (bottle == null)
                {
                    continue;
                }

                cappingBottles.Add(bottle);
                SnapBottleToCappingSlot(bottle);
            }

            var activeHeads = GetActiveCappingHeads();
            var basePositions = new Vector3[activeHeads.Count];
            var downPositions = new Vector3[activeHeads.Count];
            for (var i = 0; i < activeHeads.Count; i++)
            {
                basePositions[i] = activeHeads[i].position;
                downPositions[i] = basePositions[i] + Vector3.down * 0.22f;
            }

            yield return MoveCappingHeads(activeHeads, basePositions, downPositions, 0.18f);

            var dwellTime = Mathf.Max(0.05f, cappingTimeSeconds - 0.4f);
            var elapsed = 0f;
            while (elapsed < dwellTime)
            {
                elapsed += Time.deltaTime;
                foreach (var bottle in batch)
                {
                    SnapBottleToCappingSlot(bottle);
                }

                yield return null;
            }

            foreach (var bottle in batch)
            {
                if (bottle == null)
                {
                    continue;
                }

                bottle.cappingCompleted = true;
                bottle.status = BottleQualityStatus.Capped;
                bottle.RefreshVisuals();
            }

            yield return MoveCappingHeads(activeHeads, downPositions, basePositions, 0.22f);

            foreach (var bottle in batch)
            {
                if (bottle != null)
                {
                    cappingBottles.Remove(bottle);
                }
            }

            cappingSlotAssignments.Clear();
            cappingStationBusy = false;
            CappingActive = false;
        }

        private void SnapBottleToCappingSlot(BottleProcessState bottle)
        {
            if (bottle == null || !cappingSlotAssignments.TryGetValue(bottle, out var slotIndex))
            {
                return;
            }

            bottle.transform.position = CappingSlotPosition(slotIndex);
        }

        private List<Transform> GetActiveCappingHeads()
        {
            var activeHeads = new List<Transform>();
            foreach (var head in cappingHeads)
            {
                if (head != null)
                {
                    activeHeads.Add(head);
                }
            }

            if (activeHeads.Count == 0 && cappingHead != null)
            {
                activeHeads.Add(cappingHead);
            }

            return activeHeads;
        }

        private void UpdateFillingGateVisual()
        {
            if (fillingStopGate == null)
            {
                return;
            }

            var blocked = fillingStationBusy || fillingSlotAssignments.Count >= ActiveFillingNozzleCount;
            var scale = fillingStopGate.localScale;
            scale.y = blocked ? 0.5f : 0.16f;
            fillingStopGate.localScale = scale;

            var position = fillingStopGate.position;
            position.y = blocked ? 0.92f : 1.1f;
            fillingStopGate.position = position;
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

        private IEnumerator MoveCappingHeads(List<Transform> activeHeads, Vector3[] from, Vector3[] to, float duration)
        {
            if (activeHeads == null || activeHeads.Count == 0)
            {
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var ratio = elapsed / duration;
                for (var i = 0; i < activeHeads.Count; i++)
                {
                    activeHeads[i].position = Vector3.Lerp(from[i], to[i], ratio);
                }

                yield return null;
            }

            for (var i = 0; i < activeHeads.Count; i++)
            {
                activeHeads[i].position = to[i];
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
            lineBottles.Remove(bottle);
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
