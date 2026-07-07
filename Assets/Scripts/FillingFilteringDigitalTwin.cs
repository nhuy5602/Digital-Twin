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
        public List<Transform> fillingNozzleSprings = new List<Transform>();
        public Transform fillingStopGate;
        public Transform fillingStarWheel;
        public Transform liquidVessel;
        public Transform vesselLiquidVisual;
        public Transform qcSensorBeam;
        public Transform cappingHead;
        public List<Transform> cappingHeads = new List<Transform>();
        public Transform capDropper;
        public Transform capSensorBeam;
        public List<Transform> capMagazineCaps = new List<Transform>();
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
        public int fillingNozzleCount = 3;
        public float fillingFirstZ = -1.2f;
        public float fillingQueueStopZ = -2.45f;
        public float fillingSlotToleranceM = 0.03f;
        public int starWheelPocketCount = 8;
        public Vector3 starWheelCenter = new Vector3(0.78f, 0.82f, -0.68f);
        public float starWheelPocketRadius = 0.78f;
        public float starWheelEntryAngleDegrees = 220f;
        public int fillingStationStartPocketIndex = 2;
        public int starWheelIndexStepPockets = 3;
        public float starWheelIndexDurationSeconds = 0.32f;
        public float starWheelContinuousSpeedRpm = 7.5f;
        public float starWheelExitReleaseLeadDegrees = 12f;
        public float starWheelLockRecoverySeconds = 4f;
        public float fillingNozzleStrokeM = 0.26f;
        public float fillingNozzleMoveSeconds = 0.18f;

        [Header("Capping indexing")]
        public int cappingHeadCount = 4;
        public float cappingFirstZ = 1.65f;
        public float cappingPitchM = 0.42f;
        public float cappingQueueStopZ = 2.75f;
        public float cappingSlotToleranceM = 0.03f;
        public float cappingTimeSeconds = 0.75f;
        public float capDropZ = 1.36f;
        public float capTightenZ = 1.78f;
        public float capDropSeconds = 0.08f;
        public float capperMoveSeconds = 0.08f;
        public float capperStrokeM = 0.38f;
        public int capMagazineCapacity = 5;
        public int capDropPocketIndex = 5;
        public int cappingPocketStartIndex = 7;

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
        public float turntableBottleRadius = 0.11f;
        [Range(0, 6)] public int turntableBottleSeparationIterations = 2;
        public float outletCaptureRadius = 0.78f;
        public float outletAngleToleranceDegrees = 28f;

        [Header("Neck rail gravity feed")]
        public float neckRailStartX = 2.5f;
        public float neckRailEndX = 1.44f;
        public float neckRailZ = -0.68f;
        public float neckRailStartZ = -4.2f;
        public float neckRailEndZ = -1.1f;
        public float neckRailStartBottleY = 1.05f;
        public float neckRailEndBottleY = 0.82f;
        public float neckRailMinSlideSpeedMps = 0.12f;
        public float neckRailMaxSlideSpeedMps = 0.95f;
        public float neckRailGravityAccelerationMps2 = 0.72f;
        public float airBlowerWindSpeedMps = 0.8f;
        public float airBlowerAccelerationGain = 0.8f;
        public float neckRailWheelCaptureDistanceM = 0.055f;

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
        public int FillingStationEndPocketIndex => fillingStationStartPocketIndex + ActiveFillingNozzleCount - 1;
        private int StarWheelIndexStepPockets => Mathf.Clamp(starWheelIndexStepPockets, 1, Mathf.Max(1, starWheelPocketCount));
        private int FillingExitPocketIndex => Mathf.Max(0, starWheelPocketCount - 1);
        private float InfeedRailBottleSpacingM => Mathf.Max(0.18f, turntableBottleRadius * 1.7f);
        private float InfeedRailCaptureZoneM => Mathf.Max(0.45f, InfeedRailBottleSpacingM * StarWheelIndexStepPockets);
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
        private readonly Dictionary<BottleProcessState, float> neckRailSlideSpeeds = new Dictionary<BottleProcessState, float>();
        private int completedCount;
        private float spawnTimer;
        private float releaseTimer;
        private int spawnedCount;
        private bool fillingStationBusy;
        private bool fillingCaptureBusy;
        private bool cappingStationBusy;
        private float fillingCaptureBusySince = -1f;
        private float fillingStationBusySince = -1f;
        private float starWheelIndexingSince = -1f;
        private bool outletOccupied;
        private bool initializedTurntable;
        private int starWheelIndex;
        private int capMagazineVisibleCount;

        private float MaxTurntableBottleCenterRadius => Mathf.Max(0.05f, turntableRadius - turntableBottleRadius);

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

        private void Start()
        {
            SetFillingFlowVisuals(GetActiveFillingNozzles(), false);
            capMagazineVisibleCount = Mathf.Clamp(capMagazineCapacity, 1, Mathf.Max(1, capMagazineCaps.Count));
            UpdateCapMagazineVisuals();
        }

        private void Update()
        {
            InitializeTurntableIfNeeded();
            AnimateMachines();
            UpdateTurntableBuffer();
            // Filling stop gate is disabled so the conveyor and star wheel stay visually unobstructed.
            UpdateVesselVisual();
            RecoverStarWheelLocks();
            MoveBottles();
            TryStartStarWheelFeedFromRail();
            ThroughputBottlesPerHour = completedCount / Mathf.Max(Time.time / 3600f, 0.0001f);
            TurntableBufferCount = turntableBottles.Count;
            BottlesOnConveyorCount = lineBottles.Count;
            BottlesAtFillingStation = fillingSlotAssignments.Count;
            BottlesAtCappingStation = cappingBottles.Count;
            ConveyorStoppedForFilling = false;
            ConveyorStoppedForCapping = false;
            StarWheelLocked = false;
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

            // The star wheel visual is indexed by coroutine so bottles stay aligned with pockets.
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
            bottle.capPlaced = false;
            bottle.cappingCompleted = false;
            bottle.counted = false;
            bottle.turntableVelocity = Vector2.zero;
            neckRailSlideSpeeds.Remove(bottle);
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

                var maxCenterRadius = MaxTurntableBottleCenterRadius;
                if (radius > maxCenterRadius)
                {
                    radial = radial.normalized * maxCenterRadius;
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

            ResolveTurntableBottleSeparation();
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

            var captureRadius = Mathf.Min(outletCaptureRadius, MaxTurntableBottleCenterRadius);
            if (bottleVector.magnitude < captureRadius)
            {
                return false;
            }

            return Vector3.Angle(bottleVector, outletVector) <= outletAngleToleranceDegrees;
        }

        private void ResolveTurntableBottleSeparation()
        {
            if (turntableBottleSeparationIterations <= 0 || turntableBottles.Count < 2)
            {
                return;
            }

            var minDistance = turntableBottleRadius * 2f;
            var minDistanceSqr = minDistance * minDistance;

            for (var iteration = 0; iteration < turntableBottleSeparationIterations; iteration++)
            {
                for (var i = 0; i < turntableBottles.Count - 1; i++)
                {
                    var first = turntableBottles[i];
                    if (first == null || outletBottles.Contains(first))
                    {
                        continue;
                    }

                    for (var j = i + 1; j < turntableBottles.Count; j++)
                    {
                        var second = turntableBottles[j];
                        if (second == null || outletBottles.Contains(second))
                        {
                            continue;
                        }

                        var firstRadial = TurntableRadial(first.transform.position);
                        var secondRadial = TurntableRadial(second.transform.position);
                        var delta = firstRadial - secondRadial;
                        var distanceSqr = delta.sqrMagnitude;
                        if (distanceSqr >= minDistanceSqr)
                        {
                            continue;
                        }

                        Vector2 direction;
                        float distance;
                        if (distanceSqr < 0.0001f)
                        {
                            var angle = (i * 73f + j * 137f) * Mathf.Deg2Rad;
                            direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                            distance = 0f;
                        }
                        else
                        {
                            distance = Mathf.Sqrt(distanceSqr);
                            direction = delta / distance;
                        }

                        var correction = direction * ((minDistance - distance) * 0.5f);
                        SetTurntableRadialPosition(first, firstRadial + correction);
                        SetTurntableRadialPosition(second, secondRadial - correction);

                        first.turntableVelocity *= 0.9f;
                        second.turntableVelocity *= 0.9f;
                    }
                }
            }
        }

        private Vector2 TurntableRadial(Vector3 position)
        {
            return new Vector2(position.x - turntableCenter.x, position.z - turntableCenter.z);
        }

        private void SetTurntableRadialPosition(BottleProcessState bottle, Vector2 radial)
        {
            if (bottle == null)
            {
                return;
            }

            var maxCenterRadius = MaxTurntableBottleCenterRadius;
            if (radial.sqrMagnitude > maxCenterRadius * maxCenterRadius)
            {
                radial = radial.normalized * maxCenterRadius;
            }

            bottle.transform.position = new Vector3(turntableCenter.x + radial.x, turntableCenter.y, turntableCenter.z + radial.y);
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

            bottle.transform.position = new Vector3(neckRailStartX, neckRailStartBottleY, neckRailZ);
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

                var onInfeedNeckRail = !bottle.fillingCompleted &&
                    !fillingSlotAssignments.ContainsKey(bottle) &&
                    Mathf.Abs(position.z - neckRailZ) < 0.25f;
                if (!onInfeedNeckRail)
                {
                    position.x = lineX;
                }

                var canUseNeckRail = onInfeedNeckRail && IsBeforeFillingEntry(position.x);
                if (!canUseNeckRail && IsConveyorStopped())
                {
                    bottle.transform.position = position;
                    continue;
                }

                if (canUseNeckRail)
                {
                    position = MoveBottleAlongInfeedNeckRail(bottle, position);
                    if (IsBeforeFillingEntry(position.x) && !IsInInfeedCaptureZone(position.x))
                    {
                        bottle.transform.position = position;
                        continue;
                    }
                }

                if (!bottle.fillingCompleted)
                {
                    if (!fillingSlotAssignments.ContainsKey(bottle) && IsInInfeedCaptureZone(position.x) && Mathf.Abs(position.z - neckRailZ) < 0.2f)
                    {
                        position.x = IsFrontBottleOnInfeedRail(bottle) && IsReadyForWheelCapture(position.x)
                            ? FillingEntryX
                            : ResolveInfeedRailSpacing(bottle, position.x);
                        position.y = NeckRailBottleYAtPosition(position.x);
                        position.z = neckRailZ;
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
                    if (!bottle.capPlaced && position.z >= capDropZ)
                    {
                        StartCoroutine(DropCapOnBottle(bottle));
                    }

                    if (bottle.capPlaced && position.z >= capTightenZ)
                    {
                        if (!cappingStationBusy && !cappingBottles.Contains(bottle))
                        {
                            StartCoroutine(TightenCapForBottle(bottle));
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

        private Vector3 MoveBottleAlongInfeedNeckRail(BottleProcessState bottle, Vector3 position)
        {
            if (!neckRailSlideSpeeds.TryGetValue(bottle, out var slideSpeed))
            {
                slideSpeed = neckRailMinSlideSpeedMps;
            }

            var windSpeed = Mathf.Max(0f, airBlowerWindSpeedMps);
            var targetSpeed = Mathf.Clamp(neckRailMinSlideSpeedMps + windSpeed, neckRailMinSlideSpeedMps, neckRailMaxSlideSpeedMps);
            var acceleration = neckRailGravityAccelerationMps2 + windSpeed * airBlowerAccelerationGain;
            slideSpeed = Mathf.MoveTowards(slideSpeed, targetSpeed, acceleration * Time.deltaTime);
            neckRailSlideSpeeds[bottle] = slideSpeed;

            var nextX = position.x + NeckRailDirection * slideSpeed * Time.deltaTime;
            position.x = HasReachedFillingEntry(nextX) ? FillingEntryX : nextX;
            position.x = ResolveInfeedRailSpacing(bottle, position.x);
            position.y = NeckRailBottleYAtPosition(position.x);
            position.z = neckRailZ;
            return position;
        }

        private float ResolveInfeedRailSpacing(BottleProcessState currentBottle, float desiredX)
        {
            var desiredProgress = InfeedRailProgress(desiredX);
            var nearestAheadProgress = float.PositiveInfinity;
            foreach (var otherBottle in lineBottles)
            {
                if (otherBottle == null || otherBottle == currentBottle || otherBottle.fillingCompleted || fillingSlotAssignments.ContainsKey(otherBottle))
                {
                    continue;
                }

                var otherPosition = otherBottle.transform.position;
                if (Mathf.Abs(otherPosition.z - neckRailZ) > 0.25f)
                {
                    continue;
                }

                var otherProgress = InfeedRailProgress(otherPosition.x);
                if (otherProgress > desiredProgress + 0.001f && otherProgress < nearestAheadProgress)
                {
                    nearestAheadProgress = otherProgress;
                }
            }

            var entryProgress = InfeedRailProgress(FillingEntryX);
            var maxProgress = entryProgress;
            if (!float.IsPositiveInfinity(nearestAheadProgress))
            {
                maxProgress = Mathf.Min(maxProgress, nearestAheadProgress - InfeedRailBottleSpacingM);
            }

            desiredProgress = Mathf.Min(desiredProgress, maxProgress);
            desiredProgress = Mathf.Max(0f, desiredProgress);
            return neckRailStartX + desiredProgress * NeckRailDirection;
        }

        private float NeckRailBottleYAtPosition(float x)
        {
            var railRatio = Mathf.InverseLerp(neckRailStartX, neckRailEndX, x);
            return Mathf.Lerp(neckRailStartBottleY, neckRailEndBottleY, railRatio);
        }

        private float InfeedRailProgress(float x)
        {
            return (x - neckRailStartX) * NeckRailDirection;
        }

        private BottleProcessState GetFrontBottleOnInfeedRail(bool requireCaptureZone)
        {
            BottleProcessState frontBottle = null;
            var frontProgress = float.NegativeInfinity;
            foreach (var bottle in lineBottles)
            {
                if (bottle == null || bottle.fillingCompleted || fillingSlotAssignments.ContainsKey(bottle))
                {
                    continue;
                }

                var position = bottle.transform.position;
                if (Mathf.Abs(position.z - neckRailZ) > 0.25f)
                {
                    continue;
                }

                if (requireCaptureZone && !IsReadyForWheelCapture(position.x))
                {
                    continue;
                }

                var progress = InfeedRailProgress(position.x);
                if (progress > frontProgress)
                {
                    frontProgress = progress;
                    frontBottle = bottle;
                }
            }

            return frontBottle;
        }

        private float KeepBottleSpacing(BottleProcessState currentBottle, float candidateZ)
        {
            var resolvedZ = candidateZ;
            for (var guard = 0; guard < lineBottles.Count; guard++)
            {
                var nearestAheadZ = float.PositiveInfinity;
                foreach (var otherBottle in lineBottles)
                {
                    if (otherBottle == null || otherBottle == currentBottle || fillingBottles.Contains(otherBottle))
                    {
                        continue;
                    }

                    var otherZ = otherBottle.transform.position.z;
                    if (otherZ >= resolvedZ - 0.001f && otherZ < nearestAheadZ)
                    {
                        nearestAheadZ = otherZ;
                    }
                }

                if (float.IsPositiveInfinity(nearestAheadZ))
                {
                    return resolvedZ;
                }

                var spacedZ = nearestAheadZ - minimumBottleSpacingM;
                if (resolvedZ <= spacedZ + 0.001f)
                {
                    return resolvedZ;
                }

                resolvedZ = spacedZ;
            }

            return resolvedZ;
        }

        private bool IsConveyorStopped()
        {
            return false;
        }

        private bool IsLineStartBlocked()
        {
            foreach (var bottle in lineBottles)
            {
                if (bottle == null)
                {
                    continue;
                }

                var position = bottle.transform.position;
                var distanceFromStart = (position.x - neckRailStartX) * NeckRailDirection;
                if (!bottle.fillingCompleted && Mathf.Abs(position.z - neckRailZ) < 0.25f && distanceFromStart > -InfeedRailBottleSpacingM && distanceFromStart < InfeedRailBottleSpacingM)
                {
                    return true;
                }
            }

            return false;
        }

        private float FillingEntryZ => StarWheelSlotPosition(0).z;
        private float FillingEntryX => StarWheelSlotPosition(0).x;

        private float NeckRailDirection
        {
            get
            {
                var direction = Mathf.Sign(FillingEntryX - neckRailStartX);
                return Mathf.Approximately(direction, 0f) ? 1f : direction;
            }
        }

        private bool IsBeforeFillingEntry(float x)
        {
            return (FillingEntryX - x) * NeckRailDirection > 0.001f;
        }

        private bool HasReachedFillingEntry(float x)
        {
            return (x - FillingEntryX) * NeckRailDirection >= -0.001f;
        }

        private bool IsInInfeedCaptureZone(float x)
        {
            var distanceToEntry = (FillingEntryX - x) * NeckRailDirection;
            return distanceToEntry <= InfeedRailCaptureZoneM;
        }

        private bool IsReadyForWheelCapture(float x)
        {
            var distanceToEntry = (FillingEntryX - x) * NeckRailDirection;
            return distanceToEntry >= -0.01f && distanceToEntry <= neckRailWheelCaptureDistanceM;
        }

        private bool IsFrontBottleOnInfeedRail(BottleProcessState currentBottle)
        {
            if (currentBottle == null)
            {
                return false;
            }

            var currentPosition = currentBottle.transform.position;
            var currentProgress = InfeedRailProgress(currentPosition.x);
            foreach (var otherBottle in lineBottles)
            {
                if (otherBottle == null || otherBottle == currentBottle || otherBottle.fillingCompleted || fillingSlotAssignments.ContainsKey(otherBottle))
                {
                    continue;
                }

                var otherPosition = otherBottle.transform.position;
                if (Mathf.Abs(otherPosition.z - neckRailZ) > 0.25f)
                {
                    continue;
                }

                if (InfeedRailProgress(otherPosition.x) > currentProgress + 0.001f)
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanCaptureBottleForFilling()
        {
            return !fillingStationBusy && !fillingCaptureBusy && !StarWheelIndexing && fillingSlotAssignments.Count < starWheelPocketCount;
        }

        private bool CanIndexStarWheel()
        {
            return !fillingStationBusy && !fillingCaptureBusy && !StarWheelIndexing && fillingSlotAssignments.Count > 0;
        }

        private void RecoverStarWheelLocks()
        {
            var captureTimeout = Mathf.Max(starWheelLockRecoverySeconds, starWheelIndexDurationSeconds * 3f + 1f);
            if (fillingCaptureBusy && fillingCaptureBusySince > 0f && Time.time - fillingCaptureBusySince > captureTimeout)
            {
                fillingCaptureBusy = false;
                fillingCaptureBusySince = -1f;
            }

            var indexTimeout = Mathf.Max(starWheelLockRecoverySeconds, starWheelIndexDurationSeconds * 2f + 1f);
            if (StarWheelIndexing && starWheelIndexingSince > 0f && Time.time - starWheelIndexingSince > indexTimeout)
            {
                StarWheelIndexing = false;
                starWheelIndexingSince = -1f;
            }

            var fillingTimeout = Mathf.Max(starWheelLockRecoverySeconds, fillingTimeSeconds + fillingNozzleMoveSeconds * 2f + starWheelIndexDurationSeconds + 2f);
            if (fillingStationBusy && fillingStationBusySince > 0f && Time.time - fillingStationBusySince > fillingTimeout)
            {
                fillingStationBusy = false;
                fillingStationBusySince = -1f;
            }
        }

        private void TryStartStarWheelFeedFromRail()
        {
            var frontBottle = GetFrontBottleOnInfeedRail(true);
            var hasBottleWaitingInEntryPocket = !IsStarWheelPocketAvailable(0);
            var hasBottleOnStarWheel = fillingSlotAssignments.Count > 0;
            if (frontBottle == null && !hasBottleWaitingInEntryPocket && !hasBottleOnStarWheel)
            {
                return;
            }

            if (CanIndexStarWheel() || CanCaptureBottleForFilling())
            {
                StartCoroutine(CaptureBottleIntoStarWheel(frontBottle));
                return;
            }

            if (!fillingCaptureBusy && !StarWheelIndexing && IsStarWheelPocketAvailable(0))
            {
                CaptureBottleIntoEntryPocket(frontBottle);
            }
        }

        private bool IsStarWheelPocketAvailable(int pocketIndex)
        {
            foreach (var entry in fillingSlotAssignments)
            {
                if (entry.Key != null && entry.Value == pocketIndex)
                {
                    return false;
                }
            }

            return true;
        }

        private void CaptureBottleIntoEntryPocket(BottleProcessState bottle)
        {
            if (bottle == null || !lineBottles.Contains(bottle) || !IsStarWheelPocketAvailable(0))
            {
                return;
            }

            lineBottles.Remove(bottle);
            neckRailSlideSpeeds.Remove(bottle);
            fillingBottles.Add(bottle);
            fillingSlotAssignments[bottle] = 0;
            bottle.transform.position = StarWheelSlotPosition(0);
        }

        private IEnumerator CaptureBottleIntoStarWheel(BottleProcessState bottle)
        {
            if (!CanIndexStarWheel() && !CanCaptureBottleForFilling())
            {
                yield break;
            }

            var hasBottleWaitingInEntryPocket = !IsStarWheelPocketAvailable(0);
            if (GetFrontBottleOnInfeedRail(true) == null && !hasBottleWaitingInEntryPocket && fillingSlotAssignments.Count == 0)
            {
                yield break;
            }

            fillingCaptureBusy = true;
            fillingCaptureBusySince = Time.time;
            var indexedBottles = new Dictionary<BottleProcessState, int>();
            foreach (var entry in fillingSlotAssignments)
            {
                if (entry.Key != null)
                {
                    indexedBottles[entry.Key] = entry.Value;
                }
            }

            yield return IndexStarWheelOnePitchWithRailFeed(indexedBottles, StarWheelIndexStepPockets);
            foreach (var entry in indexedBottles)
            {
                if (entry.Key != null)
                {
                    var newPocketIndex = Mathf.Min(entry.Value + StarWheelIndexStepPockets, FillingExitPocketIndex);
                    fillingSlotAssignments[entry.Key] = newPocketIndex;
                    entry.Key.transform.position = StarWheelSlotPosition(newPocketIndex);
                }
            }

            yield return ApplyStarWheelPocketOperations();
            yield return ReleaseFilledBottlesAtExit();
            fillingCaptureBusy = false;
            fillingCaptureBusySince = -1f;
            TryStartFillingBatch();
        }

        private IEnumerator ApplyStarWheelPocketOperations()
        {
            // Bottles leave the star wheel after filling; cap drop and tightening happen after QC on the straight conveyor.
            yield break;
        }

        private void TryStartFillingBatch()
        {
            if (fillingStationBusy || fillingCaptureBusy || StarWheelIndexing)
            {
                return;
            }

            var batch = GetReadyFillingBatch();
            if (batch.Count < ActiveFillingNozzleCount)
            {
                return;
            }

            StartCoroutine(FillBottleBatch(batch));
        }

        private List<BottleProcessState> GetReadyFillingBatch()
        {
            var batch = new List<BottleProcessState>();
            for (var pocketIndex = fillingStationStartPocketIndex; pocketIndex <= FillingStationEndPocketIndex; pocketIndex++)
            {
                BottleProcessState bottleInPocket = null;
                foreach (var entry in fillingSlotAssignments)
                {
                    if (entry.Value == pocketIndex)
                    {
                        bottleInPocket = entry.Key;
                        break;
                    }
                }

                if (bottleInPocket == null ||
                    bottleInPocket.fillingCompleted ||
                    Vector3.Distance(bottleInPocket.transform.position, StarWheelSlotPosition(pocketIndex)) > fillingSlotToleranceM)
                {
                    batch.Clear();
                    return batch;
                }

                batch.Add(bottleInPocket);
            }

            return batch;
        }

        private IEnumerator FillBottleBatch(List<BottleProcessState> batch)
        {
            fillingStationBusy = true;
            fillingStationBusySince = Time.time;
            var targets = new Dictionary<BottleProcessState, float>();
            var activeNozzles = GetActiveFillingNozzles();
            var activeSprings = GetActiveFillingNozzleSprings(activeNozzles);
            var springBasePositions = GetTransformPositions(activeSprings);
            var springDownPositions = OffsetPositions(springBasePositions, Vector3.down * fillingNozzleStrokeM);
            SetFillingFlowVisuals(activeNozzles, false);

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

            yield return MoveFillingNozzles(activeSprings, springBasePositions, springDownPositions, fillingNozzleMoveSeconds, batch);
            SetFillingFlowVisuals(activeNozzles, true);

            var elapsed = 0f;
            var previousRatio = 0f;
            while (elapsed < fillingTimeSeconds)
            {
                elapsed += Time.deltaTime;
                var ratio = Mathf.Clamp01(elapsed / fillingTimeSeconds);
                var frameFilledVolume = 0f;
                foreach (var bottle in batch)
                {
                    if (bottle != null && targets.TryGetValue(bottle, out var targetVolume))
                    {
                        SnapBottleToFillingSlot(bottle);
                        bottle.SetVolume(Mathf.Lerp(0f, targetVolume, ratio));
                        frameFilledVolume += Mathf.Max(0f, ratio - previousRatio) * targetVolume;
                    }
                }

                LiquidLevelLiters = Mathf.Max(0f, LiquidLevelLiters - frameFilledVolume * bottleCapacityLiters);
                previousRatio = ratio;
                yield return null;
            }

            foreach (var bottle in batch)
            {
                if (bottle == null || !targets.TryGetValue(bottle, out var targetVolume))
                {
                    continue;
                }

                bottle.SetVolume(targetVolume);
                bottle.status = BottleQualityStatus.Filled;
                bottle.fillingCompleted = true;
            }

            LastFillingTimeSeconds = fillingTimeSeconds;
            SetFillingFlowVisuals(activeNozzles, false);
            yield return MoveFillingNozzles(activeSprings, springDownPositions, springBasePositions, fillingNozzleMoveSeconds, batch);
            yield return AdvanceStarWheelAfterFilling();
            fillingStationBusy = false;
            fillingStationBusySince = -1f;
            TryStartFillingBatch();
        }

        private IEnumerator AdvanceStarWheelAfterFilling()
        {
            if (fillingSlotAssignments.Count == 0)
            {
                yield break;
            }

            var indexedBottles = new Dictionary<BottleProcessState, int>();
            foreach (var entry in fillingSlotAssignments)
            {
                if (entry.Key != null)
                {
                    indexedBottles[entry.Key] = entry.Value;
                }
            }

            yield return IndexStarWheelOnePitchWithRailFeed(indexedBottles, StarWheelIndexStepPockets);
            foreach (var entry in indexedBottles)
            {
                if (entry.Key == null)
                {
                    continue;
                }

                var newPocketIndex = Mathf.Min(entry.Value + StarWheelIndexStepPockets, FillingExitPocketIndex);
                fillingSlotAssignments[entry.Key] = newPocketIndex;
                entry.Key.transform.position = StarWheelSlotPosition(newPocketIndex);
            }

            yield return ApplyStarWheelPocketOperations();
            yield return ReleaseFilledBottlesAtExit();
        }

        private void SnapBottleToFillingSlot(BottleProcessState bottle)
        {
            if (bottle == null || !fillingSlotAssignments.TryGetValue(bottle, out var slotIndex))
            {
                return;
            }

            bottle.transform.position = StarWheelSlotPosition(slotIndex);
        }

        private List<Transform> GetActiveFillingNozzles()
        {
            var activeNozzles = new List<Transform>();
            var nozzleLimit = ActiveFillingNozzleCount;
            foreach (var nozzle in fillingNozzles)
            {
                if (nozzle != null)
                {
                    activeNozzles.Add(nozzle);
                    if (activeNozzles.Count >= nozzleLimit)
                    {
                        break;
                    }
                }
            }

            if (activeNozzles.Count == 0 && fillingNozzle != null)
            {
                activeNozzles.Add(fillingNozzle);
            }

            return activeNozzles;
        }

        private List<Transform> GetActiveFillingNozzleSprings(List<Transform> activeNozzles)
        {
            var activeSprings = new List<Transform>();
            var springLimit = ActiveFillingNozzleCount;
            foreach (var spring in fillingNozzleSprings)
            {
                if (spring != null)
                {
                    activeSprings.Add(spring);
                    if (activeSprings.Count >= springLimit)
                    {
                        return activeSprings;
                    }
                }
            }

            foreach (var nozzle in activeNozzles)
            {
                if (nozzle == null)
                {
                    continue;
                }

                var searchRoot = nozzle.parent != null ? nozzle.parent : nozzle;
                foreach (var child in searchRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (child != null && child.name.StartsWith("Nozzle Spring"))
                    {
                        activeSprings.Add(child);
                        break;
                    }
                }

                if (activeSprings.Count >= springLimit)
                {
                    break;
                }
            }

            return activeSprings;
        }

        private Vector3[] GetTransformPositions(List<Transform> transforms)
        {
            var positions = new Vector3[transforms.Count];
            for (var i = 0; i < transforms.Count; i++)
            {
                positions[i] = transforms[i].position;
            }

            return positions;
        }

        private Vector3[] OffsetPositions(Vector3[] positions, Vector3 offset)
        {
            var offsetPositions = new Vector3[positions.Length];
            for (var i = 0; i < positions.Length; i++)
            {
                offsetPositions[i] = positions[i] + offset;
            }

            return offsetPositions;
        }

        private IEnumerator MoveFillingNozzles(List<Transform> activeNozzles, Vector3[] from, Vector3[] to, float duration, List<BottleProcessState> batch)
        {
            if (activeNozzles == null || activeNozzles.Count == 0)
            {
                yield break;
            }

            var elapsed = 0f;
            var moveDuration = Mathf.Max(0.05f, duration);
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                var ratio = Mathf.SmoothStep(0f, 1f, elapsed / moveDuration);
                for (var i = 0; i < activeNozzles.Count; i++)
                {
                    if (activeNozzles[i] != null)
                    {
                        activeNozzles[i].position = Vector3.Lerp(from[i], to[i], ratio);
                    }
                }

                SnapFillingBatch(batch);
                yield return null;
            }

            for (var i = 0; i < activeNozzles.Count; i++)
            {
                if (activeNozzles[i] != null)
                {
                    activeNozzles[i].position = to[i];
                }
            }
        }

        private void SnapFillingBatch(List<BottleProcessState> batch)
        {
            foreach (var bottle in batch)
            {
                SnapBottleToFillingSlot(bottle);
            }
        }

        private void SetFillingFlowVisuals(List<Transform> activeNozzles, bool active)
        {
            foreach (var nozzle in activeNozzles)
            {
                if (nozzle == null)
                {
                    continue;
                }

                var children = nozzle.GetComponentsInChildren<Transform>(true);
                foreach (var child in children)
                {
                    if (child != null && child.name.StartsWith("Liquid Flow Visual"))
                    {
                        child.gameObject.SetActive(active);
                    }
                }
            }
        }

        private void ConsumeCapMagazineCap()
        {
            if (capMagazineCaps == null || capMagazineCaps.Count == 0)
            {
                return;
            }

            var capacity = Mathf.Clamp(capMagazineCapacity, 1, capMagazineCaps.Count);
            if (capMagazineVisibleCount <= 0)
            {
                capMagazineVisibleCount = capacity;
            }

            capMagazineVisibleCount--;
            if (capMagazineVisibleCount <= 0)
            {
                capMagazineVisibleCount = capacity;
            }

            UpdateCapMagazineVisuals();
        }

        private void UpdateCapMagazineVisuals()
        {
            if (capMagazineCaps == null || capMagazineCaps.Count == 0)
            {
                return;
            }

            var capacity = Mathf.Clamp(capMagazineCapacity, 1, capMagazineCaps.Count);
            capMagazineVisibleCount = Mathf.Clamp(capMagazineVisibleCount <= 0 ? capacity : capMagazineVisibleCount, 1, capacity);
            for (var i = 0; i < capMagazineCaps.Count; i++)
            {
                var cap = capMagazineCaps[i];
                if (cap == null)
                {
                    continue;
                }

                var visible = i < capMagazineVisibleCount;
                cap.gameObject.SetActive(visible);
                if (visible)
                {
                    cap.localPosition = new Vector3(cap.localPosition.x, 1.57f + i * 0.13f, cap.localPosition.z);
                }
            }
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
            return StarWheelSlotPosition(fillingStationStartPocketIndex + slotIndex);
        }

        private Vector3 CappingSlotPosition(int slotIndex)
        {
            return new Vector3(lineX, starWheelCenter.y, cappingFirstZ + slotIndex * cappingPitchM);
        }

        private float FillingSlotAngleDegrees(int slotIndex)
        {
            return StarWheelSlotAngleDegrees(fillingStationStartPocketIndex + slotIndex);
        }

        private IEnumerator IndexStarWheelOnePitch(Dictionary<BottleProcessState, int> indexedBottles, int slotDelta)
        {
            if (fillingStarWheel == null || indexedBottles.Count == 0)
            {
                yield break;
            }

            StarWheelIndexing = true;
            starWheelIndexingSince = Time.time;
            var startRotation = fillingStarWheel.localRotation;
            var targetRotation = startRotation * Quaternion.Euler(0f, -slotDelta * StarWheelStepAngleDegrees, 0f);
            starWheelIndex = (starWheelIndex + slotDelta) % Mathf.Max(1, starWheelPocketCount);
            var elapsed = 0f;
            var duration = Mathf.Max(0.05f, starWheelIndexDurationSeconds);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var ratio = Mathf.Clamp01(elapsed / duration);
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
            starWheelIndexingSince = -1f;
        }

        private IEnumerator IndexStarWheelOnePitchWithRailFeed(Dictionary<BottleProcessState, int> indexedBottles, int slotDelta)
        {
            if (fillingStarWheel == null)
            {
                yield break;
            }

            slotDelta = Mathf.Clamp(slotDelta, 1, Mathf.Max(1, starWheelPocketCount));
            StarWheelIndexing = true;
            starWheelIndexingSince = Time.time;
            var capturedSteps = new HashSet<int>();
            var releasedBottles = new HashSet<BottleProcessState>();
            var startRotation = fillingStarWheel.localRotation;
            var targetRotation = startRotation * Quaternion.Euler(0f, -slotDelta * StarWheelStepAngleDegrees, 0f);
            starWheelIndex = (starWheelIndex + slotDelta) % Mathf.Max(1, starWheelPocketCount);
            var elapsed = 0f;
            var duration = Mathf.Max(0.05f, starWheelIndexDurationSeconds);

            TryCaptureBottleFromRailIntoPassingPocket(indexedBottles, capturedSteps, 0, slotDelta);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var ratio = Mathf.Clamp01(elapsed / duration);
                var passedStep = Mathf.Min(slotDelta - 1, Mathf.FloorToInt(ratio * slotDelta));
                for (var step = 0; step <= passedStep; step++)
                {
                    TryCaptureBottleFromRailIntoPassingPocket(indexedBottles, capturedSteps, step, slotDelta);
                }

                TryReleaseBottlesCrossingExit(indexedBottles, releasedBottles, ratio, slotDelta);
                fillingStarWheel.localRotation = Quaternion.Slerp(startRotation, targetRotation, ratio);
                foreach (var entry in new List<KeyValuePair<BottleProcessState, int>>(indexedBottles))
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
            TryReleaseBottlesCrossingExit(indexedBottles, releasedBottles, 1f, slotDelta);
            foreach (var entry in indexedBottles)
            {
                var bottle = entry.Key;
                if (bottle != null)
                {
                    bottle.transform.position = StarWheelSlotPosition(entry.Value + slotDelta);
                }
            }

            StarWheelIndexing = false;
            starWheelIndexingSince = -1f;
        }

        private void TryCaptureBottleFromRailIntoPassingPocket(Dictionary<BottleProcessState, int> indexedBottles, HashSet<int> capturedSteps, int captureStep, int slotDelta)
        {
            if (capturedSteps.Contains(captureStep))
            {
                return;
            }

            var finalPocketIndex = slotDelta - captureStep;
            if (!IsProjectedPocketAvailable(indexedBottles, finalPocketIndex, slotDelta))
            {
                return;
            }

            var bottle = GetFrontBottleOnInfeedRail(true);
            if (bottle == null)
            {
                return;
            }

            lineBottles.Remove(bottle);
            neckRailSlideSpeeds.Remove(bottle);
            fillingBottles.Add(bottle);
            bottle.transform.position = StarWheelSlotPosition(0);
            indexedBottles[bottle] = -captureStep;
            capturedSteps.Add(captureStep);
        }

        private bool IsProjectedPocketAvailable(Dictionary<BottleProcessState, int> indexedBottles, int pocketIndex, int slotDelta)
        {
            foreach (var entry in indexedBottles)
            {
                if (entry.Key != null && Mathf.RoundToInt(entry.Value + slotDelta) == pocketIndex)
                {
                    return false;
                }
            }

            return true;
        }

        private void TryReleaseBottlesCrossingExit(Dictionary<BottleProcessState, int> indexedBottles, HashSet<BottleProcessState> releasedBottles, float ratio, int slotDelta)
        {
            var releaseLeadSlots = Mathf.Clamp(starWheelExitReleaseLeadDegrees / Mathf.Max(1f, StarWheelStepAngleDegrees), 0f, 0.85f);
            var releaseThresholdSlot = FillingExitPocketIndex - releaseLeadSlots;
            var bottlesToRelease = new List<BottleProcessState>();
            foreach (var entry in indexedBottles)
            {
                var bottle = entry.Key;
                if (bottle == null || releasedBottles.Contains(bottle))
                {
                    continue;
                }

                var targetSlot = entry.Value + slotDelta;
                if (targetSlot < FillingExitPocketIndex)
                {
                    continue;
                }

                var currentSlot = Mathf.Lerp(entry.Value, targetSlot, ratio);
                ApplyStarWheelOperationAtSlot(bottle, currentSlot);
                if (currentSlot >= releaseThresholdSlot && bottle.fillingCompleted)
                {
                    bottlesToRelease.Add(bottle);
                }
            }

            var finalZ = NextStarWheelReleaseConveyorZ();
            foreach (var bottle in bottlesToRelease)
            {
                releasedBottles.Add(bottle);
                indexedBottles.Remove(bottle);
                StartCoroutine(ReleaseOneFilledBottleToConveyor(bottle, finalZ));
                finalZ += minimumBottleSpacingM;
            }
        }

        private void ApplyStarWheelOperationAtSlot(BottleProcessState bottle, float slot)
        {
            if (bottle == null || !bottle.fillingCompleted)
            {
                return;
            }

            // Capping is handled after QC on the straight conveyor, so the star wheel only fills and releases bottles.
        }

        private float NextStarWheelReleaseConveyorZ()
        {
            var exitPoint = StarWheelSlotPosition(FillingExitPocketIndex);
            var finalZ = exitPoint.z + minimumBottleSpacingM * 0.6f;
            foreach (var bottle in lineBottles)
            {
                if (bottle == null)
                {
                    continue;
                }

                var position = bottle.transform.position;
                if (Mathf.Abs(position.x - lineX) < 0.25f && position.z >= finalZ - minimumBottleSpacingM)
                {
                    finalZ = Mathf.Max(finalZ, position.z + minimumBottleSpacingM);
                }
            }

            return finalZ;
        }

        private Vector3 StarWheelSlotPosition(float slotIndex)
        {
            var angle = StarWheelSlotAngleDegrees(slotIndex) * Mathf.Deg2Rad;
            return new Vector3(
                starWheelCenter.x + Mathf.Cos(angle) * starWheelPocketRadius,
                starWheelCenter.y,
                starWheelCenter.z + Mathf.Sin(angle) * starWheelPocketRadius);
        }

        private float StarWheelSlotAngleDegrees(float slotIndex)
        {
            return starWheelEntryAngleDegrees + slotIndex * StarWheelStepAngleDegrees;
        }

        private IEnumerator ReleaseFilledBottlesAtExit()
        {
            var readyToExit = new List<KeyValuePair<BottleProcessState, int>>();
            foreach (var entry in fillingSlotAssignments)
            {
                if (entry.Key != null && entry.Key.fillingCompleted && entry.Value >= FillingExitPocketIndex)
                {
                    readyToExit.Add(entry);
                }
            }

            if (readyToExit.Count == 0)
            {
                yield break;
            }

            readyToExit.Sort((left, right) => right.Value.CompareTo(left.Value));
            var exitPoint = StarWheelSlotPosition(FillingExitPocketIndex);
            var baseExitZ = exitPoint.z + minimumBottleSpacingM * 0.6f;

            for (var i = 0; i < readyToExit.Count; i++)
            {
                var bottle = readyToExit[i].Key;
                if (bottle == null || !fillingSlotAssignments.ContainsKey(bottle))
                {
                    continue;
                }

                var finalZ = baseExitZ + (readyToExit.Count - 1 - i) * minimumBottleSpacingM;
                yield return ReleaseOneFilledBottleToConveyor(bottle, finalZ);
            }
        }

        private IEnumerator ReleaseOneFilledBottleToConveyor(BottleProcessState bottle, float finalZ)
        {
            if (bottle == null)
            {
                yield break;
            }
            var tangentStart = bottle.transform.position;
            var tangentEnd = new Vector3(lineX, starWheelCenter.y, finalZ);
            fillingBottles.Remove(bottle);
            fillingSlotAssignments.Remove(bottle);

            var elapsed = 0f;
            var tangentDuration = Mathf.Max(0.08f, starWheelIndexDurationSeconds * 0.35f);
            while (elapsed < tangentDuration)
            {
                elapsed += Time.deltaTime;
                var ratio = Mathf.SmoothStep(0f, 1f, elapsed / tangentDuration);
                bottle.transform.position = Vector3.Lerp(tangentStart, tangentEnd, ratio);
                yield return null;
            }

            bottle.transform.position = tangentEnd;
            lineBottles.Add(bottle);
        }

        private IEnumerator DropCapOnBottle(BottleProcessState bottle)
        {
            if (bottle == null || bottle.capPlaced)
            {
                yield break;
            }

            bottle.capPlaced = true;
            bottle.RefreshVisuals();
            ConsumeCapMagazineCap();

            if (capSensorBeam != null)
            {
                var baseScale = capSensorBeam.localScale;
                capSensorBeam.localScale = new Vector3(baseScale.x, baseScale.y * 2.2f, baseScale.z);
                yield return null;
                capSensorBeam.localScale = baseScale;
            }

            if (capDropper == null)
            {
                yield break;
            }

            var basePosition = capDropper.position;
            var dropPosition = basePosition + Vector3.down * 0.16f;
            yield return MoveSingleTransform(capDropper, basePosition, dropPosition, capDropSeconds * 0.5f);
            yield return MoveSingleTransform(capDropper, dropPosition, basePosition, capDropSeconds * 0.5f);
        }

        private IEnumerator TightenCapForBottle(BottleProcessState bottle)
        {
            if (bottle == null || bottle.cappingCompleted)
            {
                yield break;
            }

            cappingStationBusy = true;
            CappingActive = true;
            cappingBottles.Add(bottle);

            bottle.transform.position = new Vector3(lineX, starWheelCenter.y, capTightenZ);
            var activeHeads = GetActiveCappingHeads();
            var basePositions = new Vector3[activeHeads.Count];
            var downPositions = new Vector3[activeHeads.Count];
            for (var i = 0; i < activeHeads.Count; i++)
            {
                basePositions[i] = activeHeads[i].position;
                downPositions[i] = basePositions[i] + Vector3.down * capperStrokeM;
            }

            yield return MoveAndSpinCappingHeads(activeHeads, basePositions, downPositions, capperMoveSeconds, 720f);

            var spinTime = Mathf.Max(0.04f, cappingTimeSeconds * 0.22f);
            var elapsed = 0f;
            while (elapsed < spinTime)
            {
                elapsed += Time.deltaTime;
                bottle.transform.position = new Vector3(lineX, starWheelCenter.y, capTightenZ);
                SpinCappingHeadTools(activeHeads, 2160f);
                yield return null;
            }

            bottle.capPlaced = true;
            bottle.cappingCompleted = true;
            bottle.status = BottleQualityStatus.Capped;
            bottle.RefreshVisuals();

            yield return MoveCappingHeads(activeHeads, downPositions, basePositions, capperMoveSeconds);

            cappingBottles.Remove(bottle);
            cappingStationBusy = false;
            CappingActive = false;
        }

        private IEnumerator MoveAndSpinCappingHeads(List<Transform> activeHeads, Vector3[] from, Vector3[] to, float duration, float totalSpinDegrees)
        {
            if (activeHeads == null || activeHeads.Count == 0)
            {
                yield break;
            }

            var elapsed = 0f;
            var moveDuration = Mathf.Max(0.02f, duration);
            var previousRatio = 0f;
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                var ratio = Mathf.SmoothStep(0f, 1f, elapsed / moveDuration);
                var spinDelta = (ratio - previousRatio) * totalSpinDegrees;
                for (var i = 0; i < activeHeads.Count; i++)
                {
                    if (activeHeads[i] != null)
                    {
                        activeHeads[i].position = Vector3.Lerp(from[i], to[i], ratio);
                        SpinCappingHeadTool(activeHeads[i], spinDelta);
                    }
                }

                previousRatio = ratio;
                yield return null;
            }

            for (var i = 0; i < activeHeads.Count; i++)
            {
                if (activeHeads[i] != null)
                {
                    activeHeads[i].position = to[i];
                }
            }
        }

        private void SpinCappingHeadTools(List<Transform> activeHeads, float degreesPerSecond)
        {
            foreach (var headRoot in activeHeads)
            {
                SpinCappingHeadTool(headRoot, degreesPerSecond * Time.deltaTime);
            }
        }

        private void SpinCappingHeadTool(Transform headRoot, float degrees)
        {
            if (headRoot == null)
            {
                return;
            }

            headRoot.Rotate(Vector3.up, degrees, Space.Self);
        }

        private IEnumerator MoveSingleTransform(Transform target, Vector3 from, Vector3 to, float duration)
        {
            if (target == null)
            {
                yield break;
            }

            var elapsed = 0f;
            var moveDuration = Mathf.Max(0.02f, duration);
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                var ratio = Mathf.SmoothStep(0f, 1f, elapsed / moveDuration);
                target.position = Vector3.Lerp(from, to, ratio);
                yield return null;
            }

            target.position = to;
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
                downPositions[i] = basePositions[i] + Vector3.down * 0.34f;
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
