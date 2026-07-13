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
        private enum SplitLane
        {
            A,
            B
        }

        private enum PackGatePhase
        {
            Loading,
            BlockingForPusher,
            ResetHold
        }

        private const float CapMagazineCapPitchM = 0.11f;
        private const float CapMagazineBottomLocalY = -0.63f;
        private const float CapMagazineGuideHalfLengthM = 0.55f;
        private const float CapMagazineGuideCurveDepthM = 0.025f;
        private static readonly Vector3 CapMagazineAssemblyLocalOffset = new Vector3(0f, 0.106515377f, -0.136654712f);
        private static readonly Vector3 CapMagazineOutletCapLocalEulerAngles = new Vector3(316.305176f, 183.988342f, 176.259995f);
        private const float CapGuideSlideSeconds = 0.14f;
        private const float CapCatchSlideSeconds = 0.06f;
        private const float CapMagazineRestackSeconds = 0.14f;

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
        public Transform splitSensorBeam;
        public Transform splitGuidePivot;
        public Transform packCarton;
        public Transform packPusher;
        public Transform packStopGateA;
        public Transform packStopGateB;
        public Transform packGateSensorA;
        public Transform packGateSensorB;

        [Header("Bottle line")]
        public BottleProcessState bottleTemplate;
        public List<BottleProcessState> bottles = new List<BottleProcessState>();
        public float conveyorSpeedMps = 0.85f;
        public float infeedStartZ = -4.2f;
        public float fillingZ = -1.65f;
        public float qcZ = 0.85f;
        public float pusherZ = 2.25f;
        public float cappingZ = 3.2f;
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
        public int capMagazineCapacity = 10;
        public int capDropPocketIndex = 5;
        public int cappingPocketStartIndex = 7;
        public float cappingSpeedMultiplier = 10f;

        [Header("Slat chain conveyor")]
        public float slatPitchM = 0.22f;
        [Range(0f, 0.25f)] public float conveyorSlipRatio = 0.02f;
        public float minimumBottleSpacingM = 0.46f;
        public float starWheelReleaseGapSeconds = 0.42f;
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

        [Header("Dual-lane splitter and six-pack station")]
        public float splitSensorZ = 3.58f;
        public float splitGuideZ = 4.47733736f;
        public float splitGuideExitZ = 5.05f;
        public float laneBCenterX = 0.62f;
        public int splitGroupSize = 3;
        public float splitGuideMoveSeconds = 0.08f;
        public float splitGuideAngleDegrees = 42f;
        public float splitterSafetyGapM = 0.03f;
        public float packFrontRowZ = 7.12f;
        public float packRowPitchM = 0.235f;
        public float packGateZ = 6.41f;
        public float packGateSensorZ = 6.57f;
        public float packGateOpenY = 0.38f;
        public float packGateClosedY = 0.82f;
        public float packGateMoveSeconds = 0.12f;
        public float packGateResetHoldSeconds = 0.15f;
        public float packPusherSeconds = 0.52f;
        public float packPusherReturnSeconds = 0.42f;
        public float packCartonWidthM = 0.96f;
        public float packPusherCartonClearanceM = 0.037f;
        public float packCartonExitSeconds = 0.55f;
        public Vector3 packCartonLoadPosition = new Vector3(1.56f, 0.58f, 6.6645f);
        public Vector3 packCartonExitPosition = new Vector3(2.71f, 0.58f, 6.6645f);

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
        public string StarWheelPhase { get; private set; } = "Waiting for infeed";
        public float StarWheelStepAngleDegrees => 360f / Mathf.Max(1, starWheelPocketCount);
        public float StarWheelPocketPitchM => Mathf.PI * 2f * starWheelPocketRadius / Mathf.Max(1, starWheelPocketCount);
        public float ConveyorBottleSpacingM => StarWheelPocketPitchM;
        public int FillingStationEndPocketIndex => fillingStationStartPocketIndex + ActiveFillingNozzleCount - 1;
        private int StarWheelIndexStepPockets => Mathf.Clamp(starWheelIndexStepPockets, 1, Mathf.Max(1, starWheelPocketCount));
        private int StarWheelFeedBatchSize => Mathf.Clamp(Mathf.Min(StarWheelIndexStepPockets, ActiveFillingNozzleCount), 1, Mathf.Max(1, starWheelPocketCount));
        private int FillingExitPocketIndex => Mathf.Max(0, starWheelPocketCount - 1);
        private float StarWheelAngularSpeedDegreesPerSecond => StarWheelStepAngleDegrees / Mathf.Max(0.05f, starWheelIndexDurationSeconds);
        private float CappingHeadAngularSpeedDegreesPerSecond => StarWheelAngularSpeedDegreesPerSecond * Mathf.Max(1f, cappingSpeedMultiplier);
        private float InfeedRailBottleSpacingM => Mathf.Max(0.18f, turntableBottleRadius * 1.7f);
        private float InfeedRailCaptureZoneM => Mathf.Max(0.45f, InfeedRailBottleSpacingM * StarWheelIndexStepPockets);
        public bool CappingActive { get; private set; }
        public int BottlesAtFillingStation { get; private set; }
        public int BottlesAtCappingStation { get; private set; }
        public bool ConveyorStoppedForCapping { get; private set; }
        public int SplitSensorCount { get; private set; }
        public int LaneAPackCount => packLaneABottles.Count;
        public int LaneBPackCount => packLaneBBottles.Count;
        public int PackBottleCount => LaneAPackCount + LaneBPackCount;
        public int CartonsFilled { get; private set; }
        public int PackGateSensorCountA { get; private set; }
        public int PackGateSensorCountB { get; private set; }
        public bool PackGateAClosed => IsPackGateClosed(SplitLane.A);
        public bool PackGateBClosed => IsPackGateClosed(SplitLane.B);
        public bool PackPusherActive => packLoadingOut;
        public string PackGateState => DeterminePackGateState();
        public bool SplitterSafetyInterlocked { get; private set; }
        public bool SplitConveyorPaused => splitterPaused;
        public string SplitGuideState => splitGuideLane == SplitLane.A ? "A / parallel" : "B / diagonal";
        public int ActiveFillingNozzleCount => Mathf.Max(1, Mathf.Min(fillingNozzleCount, fillingNozzles.Count > 0 ? fillingNozzles.Count : fillingNozzleCount));
        public int ActiveCappingHeadCount => Mathf.Max(1, Mathf.Min(cappingHeadCount, cappingHeads.Count > 0 ? cappingHeads.Count : cappingHeadCount));

        private readonly List<BottleProcessState> turntableBottles = new List<BottleProcessState>();
        private readonly HashSet<BottleProcessState> lineBottles = new HashSet<BottleProcessState>();
        private readonly HashSet<BottleProcessState> fillingBottles = new HashSet<BottleProcessState>();
        private readonly Queue<BottleProcessState> starWheelReleaseQueue = new Queue<BottleProcessState>();
        private readonly HashSet<BottleProcessState> queuedStarWheelReleaseBottles = new HashSet<BottleProcessState>();
        private readonly Dictionary<BottleProcessState, SplitLane> splitLaneAssignments = new Dictionary<BottleProcessState, SplitLane>();
        private readonly HashSet<BottleProcessState> splitGuidePassedBottles = new HashSet<BottleProcessState>();
        private readonly List<BottleProcessState> packLaneABottles = new List<BottleProcessState>();
        private readonly List<BottleProcessState> packLaneBBottles = new List<BottleProcessState>();
        private readonly HashSet<BottleProcessState> packingBottles = new HashSet<BottleProcessState>();
        private readonly HashSet<BottleProcessState> releasingBottles = new HashSet<BottleProcessState>();
        private readonly HashSet<BottleProcessState> cappingBottles = new HashSet<BottleProcessState>();
        private readonly HashSet<BottleProcessState> pushingBottles = new HashSet<BottleProcessState>();
        private readonly HashSet<BottleProcessState> droppingBottles = new HashSet<BottleProcessState>();
        private readonly HashSet<BottleProcessState> capDroppingBottles = new HashSet<BottleProcessState>();
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
        private bool starWheelReleaseQueueRunning;
        private SplitLane nextSplitLane = SplitLane.A;
        private SplitLane splitGuideLane = SplitLane.A;
        private int bottlesInSplitGroup;
        private bool splitGuideMoving;
        private bool splitterPaused;
        private bool packLoadingOut;
        private PackGatePhase packGatePhase = PackGatePhase.Loading;
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
            UpdateSplitterGuide();
            UpdatePackStopGateVisuals();
            // Filling stop gate is disabled so the conveyor and star wheel stay visually unobstructed.
            UpdateVesselVisual();
            RecoverStarWheelLocks();
            MoveBottles();
            TryStartSixPackDischarge();
            TryStartStarWheelFeedFromRail();
            ThroughputBottlesPerHour = completedCount / Mathf.Max(Time.time / 3600f, 0.0001f);
            TurntableBufferCount = turntableBottles.Count;
            BottlesOnConveyorCount = lineBottles.Count;
            BottlesAtFillingStation = CountUnfilledBottlesInFillingWindow();
            BottlesAtCappingStation = cappingBottles.Count;
            ConveyorStoppedForFilling = fillingStationBusy;
            ConveyorStoppedForCapping = cappingStationBusy;
            StarWheelLocked = fillingStationBusy || fillingCaptureBusy || StarWheelIndexing || cappingStationBusy;
            StarWheelPhase = DetermineStarWheelPhase();
            CappingActive = cappingStationBusy;
        }

        private void AnimateMachines()
        {
            if (infeedTurntable != null)
            {
                if (!TurntablePaused)
                {
                    infeedTurntable.Rotate(Vector3.up, -infeedMotorSpeedRpm * 6f * Time.deltaTime, Space.World);
                }
            }

            if (qcSensorBeam != null)
            {
                var scale = qcSensorBeam.localScale;
                scale.x = 1f + Mathf.Sin(Time.time * 10f) * 0.08f;
                qcSensorBeam.localScale = scale;
            }

            if (splitSensorBeam != null)
            {
                var scale = splitSensorBeam.localScale;
                scale.x = 1f + Mathf.Sin(Time.time * 12f) * 0.06f;
                splitSensorBeam.localScale = scale;
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
            splitLaneAssignments.Remove(bottle);
            splitGuidePassedBottles.Remove(bottle);
            packLaneABottles.Remove(bottle);
            packLaneBBottles.Remove(bottle);
            packingBottles.Remove(bottle);
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
                if (bottle == null ||
                    !lineBottles.Contains(bottle) ||
                    fillingBottles.Contains(bottle) ||
                    cappingBottles.Contains(bottle) ||
                    pushingBottles.Contains(bottle) ||
                    packingBottles.Contains(bottle))
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
                if (!onInfeedNeckRail && splitLaneAssignments.TryGetValue(bottle, out var assignedLane))
                {
                    position.x = ResolveLaneX(assignedLane, position.z);
                }
                else if (!onInfeedNeckRail)
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

                if (bottle.status == BottleQualityStatus.Capped)
                {
                    if (!splitLaneAssignments.ContainsKey(bottle) && position.z >= splitSensorZ)
                    {
                        AssignBottleToSplitLane(bottle);
                    }

                    if (splitterPaused && position.z >= splitSensorZ - turntableBottleRadius)
                    {
                        bottle.transform.position = position;
                        continue;
                    }
                }

                var candidateZ = position.z + ConveyorEffectiveSpeedMps * Time.deltaTime;
                if (splitLaneAssignments.TryGetValue(bottle, out assignedLane))
                {
                    if (!IsBottleInPack(bottle) && position.z >= packGateSensorZ && CanAcceptBottleIntoPack(assignedLane))
                    {
                        RegisterBottleInPack(bottle, assignedLane);
                    }

                    if (IsBottleInPack(bottle))
                    {
                        // Pack bottles retain conveyor motion; the backstop and bottle spacing, not an interpolation,
                        // establish their 3 x 2 grid.
                        candidateZ = Mathf.Min(candidateZ, packFrontRowZ);
                    }
                    else if (packLoadingOut || IsPackGateClosed(assignedLane))
                    {
                        candidateZ = Mathf.Min(candidateZ, PackGateHoldZ);
                    }
                }

                position.z = candidateZ;
                position.z = KeepBottleSpacing(bottle, position.z);
                if (splitLaneAssignments.TryGetValue(bottle, out assignedLane))
                {
                    position.x = ResolveLaneX(assignedLane, position.z);
                }
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
            var currentZ = currentBottle != null ? currentBottle.transform.position.z : candidateZ;
            var resolvedZ = Mathf.Max(candidateZ, currentZ);
            var currentLane = ResolveSpacingLane(currentBottle, currentZ);
            for (var guard = 0; guard < lineBottles.Count; guard++)
            {
                var nearestAheadZ = float.PositiveInfinity;
                BottleProcessState nearestAheadBottle = null;
                foreach (var otherBottle in lineBottles)
                {
                    if (otherBottle == null || otherBottle == currentBottle || fillingBottles.Contains(otherBottle))
                    {
                        continue;
                    }

                    var otherZ = otherBottle.transform.position.z;
                    if (ResolveSpacingLane(otherBottle, otherZ) != currentLane)
                    {
                        continue;
                    }

                    if (otherZ >= resolvedZ - 0.001f && otherZ < nearestAheadZ)
                    {
                        nearestAheadZ = otherZ;
                        nearestAheadBottle = otherBottle;
                    }
                }

                if (float.IsPositiveInfinity(nearestAheadZ))
                {
                    return resolvedZ;
                }

                var spacedZ = nearestAheadZ - RequiredBottleSpacing(currentBottle, nearestAheadBottle);
                if (resolvedZ <= spacedZ + 0.001f)
                {
                    return resolvedZ;
                }

                resolvedZ = Mathf.Max(currentZ, spacedZ);
            }

            return Mathf.Max(currentZ, resolvedZ);
        }

        private float RequiredBottleSpacing(BottleProcessState currentBottle, BottleProcessState otherBottle)
        {
            return IsBottleInPack(currentBottle) || IsBottleInPack(otherBottle)
                ? PackRowPitch
                : ConveyorBottleSpacingM;
        }

        private SplitLane ResolveSpacingLane(BottleProcessState bottle, float z)
        {
            if (bottle != null && z >= splitGuideExitZ && splitLaneAssignments.TryGetValue(bottle, out var lane))
            {
                return lane;
            }

            return SplitLane.A;
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

        private int CountBottlesWaitingOnInfeedRail()
        {
            var count = 0;
            foreach (var bottle in lineBottles)
            {
                if (bottle == null || bottle.fillingCompleted || fillingSlotAssignments.ContainsKey(bottle))
                {
                    continue;
                }

                if (Mathf.Abs(bottle.transform.position.z - neckRailZ) < 0.25f)
                {
                    count++;
                }
            }

            return count;
        }

        private bool CanCaptureBottleForFilling()
        {
            return !fillingStationBusy && !fillingCaptureBusy && !StarWheelIndexing && fillingSlotAssignments.Count < starWheelPocketCount;
        }

        private bool CanIndexStarWheel()
        {
            return !fillingStationBusy && !fillingCaptureBusy && !StarWheelIndexing && fillingSlotAssignments.Count > 0;
        }

        private int CountUnfilledBottlesInFillingWindow()
        {
            var count = 0;
            foreach (var entry in fillingSlotAssignments)
            {
                var bottle = entry.Key;
                if (bottle != null &&
                    !bottle.fillingCompleted &&
                    entry.Value >= fillingStationStartPocketIndex &&
                    entry.Value <= FillingStationEndPocketIndex)
                {
                    count++;
                }
            }

            return count;
        }

        private string DetermineStarWheelPhase()
        {
            if (fillingStationBusy)
            {
                return "STOPPED - filling bottles";
            }

            if (cappingStationBusy)
            {
                return "STOPPED - capping in star wheel";
            }

            if (StarWheelIndexing)
            {
                return "INDEXING pockets";
            }

            if (starWheelReleaseQueueRunning || starWheelReleaseQueue.Count > 0)
            {
                return "RELEASING one-by-one to QC conveyor";
            }

            if (GetReadyFillingBatch().Count >= ActiveFillingNozzleCount)
            {
                return "READY - starting fill dwell";
            }

            if (CountUnfilledBottlesInFillingWindow() > 0)
            {
                return "LOADING fill pockets";
            }

            foreach (var entry in fillingSlotAssignments)
            {
                if (entry.Key == null)
                {
                    continue;
                }

                if (entry.Key.cappingCompleted && entry.Value >= FillingExitPocketIndex)
                {
                    return "RELEASING to QC conveyor";
                }

                if (entry.Key.fillingCompleted)
                {
                    return "MOVING to capper/exit";
                }
            }

            return "Waiting for infeed";
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

            var fillingTimeout = Mathf.Max(
                starWheelLockRecoverySeconds,
                fillingTimeSeconds +
                fillingNozzleMoveSeconds * 2f +
                StarWheelIndexDurationForSlots(StarWheelIndexStepPockets) * 2f +
                StarWheelIndexDurationForSlots(1) * StarWheelIndexStepPockets +
                cappingTimeSeconds +
                capperMoveSeconds * 2f +
                8f);
            if (fillingStationBusy && fillingStationBusySince > 0f && Time.time - fillingStationBusySince > fillingTimeout)
            {
                fillingStationBusy = false;
                fillingStationBusySince = -1f;
            }
        }

        private void TryStartStarWheelFeedFromRail()
        {
            if (!fillingStationBusy && !fillingCaptureBusy && !StarWheelIndexing)
            {
                var readyBatch = GetReadyFillingBatch();
                if (readyBatch.Count >= ActiveFillingNozzleCount)
                {
                    StartCoroutine(FillBottleBatch(readyBatch));
                    return;
                }
            }

            var frontBottle = GetFrontBottleOnInfeedRail(true);
            var hasBottleWaitingInEntryPocket = !IsStarWheelPocketAvailable(0);
            var hasBottleOnStarWheel = fillingSlotAssignments.Count > 0;
            if (frontBottle == null && !hasBottleWaitingInEntryPocket && !hasBottleOnStarWheel)
            {
                return;
            }

            if (ShouldHoldStarWheelForIncompleteFillingBatch(frontBottle, hasBottleWaitingInEntryPocket))
            {
                return;
            }

            if ((CanIndexStarWheel() || CanCaptureBottleForFilling()) &&
                CountBottlesWaitingOnInfeedRail() >= StarWheelFeedBatchSize)
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

        private bool ShouldHoldStarWheelForIncompleteFillingBatch(BottleProcessState frontBottle, bool hasBottleWaitingInEntryPocket)
        {
            if (fillingStationBusy || fillingCaptureBusy || StarWheelIndexing)
            {
                return true;
            }

            var hasPartialBatch = false;
            foreach (var entry in fillingSlotAssignments)
            {
                var bottle = entry.Key;
                if (bottle == null || bottle.fillingCompleted)
                {
                    continue;
                }

                if (entry.Value >= fillingStationStartPocketIndex && entry.Value <= FillingStationEndPocketIndex)
                {
                    hasPartialBatch = true;
                    if (entry.Value >= FillingStationEndPocketIndex)
                    {
                        return GetReadyFillingBatch().Count < ActiveFillingNozzleCount;
                    }
                }
            }

            return hasPartialBatch && frontBottle == null && !hasBottleWaitingInEntryPocket;
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

            const int feedSlotDelta = 1;
            yield return IndexStarWheelOnePitchWithRailFeed(indexedBottles, feedSlotDelta, true);
            foreach (var entry in indexedBottles)
            {
                if (entry.Key != null)
                {
                    var newPocketIndex = Mathf.Min(entry.Value + feedSlotDelta, FillingExitPocketIndex);
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
            var operations = new List<KeyValuePair<BottleProcessState, int>>(fillingSlotAssignments);
            var cappingTargets = new List<KeyValuePair<BottleProcessState, int>>();
            operations.Sort((left, right) => left.Value.CompareTo(right.Value));
            foreach (var entry in operations)
            {
                var bottle = entry.Key;
                var pocketIndex = entry.Value;
                if (bottle == null || !bottle.fillingCompleted)
                {
                    continue;
                }

                if (pocketIndex == capDropPocketIndex && !bottle.capPlaced)
                {
                    BeginCapDrop(bottle);
                    while (capDroppingBottles.Contains(bottle))
                    {
                        yield return null;
                    }
                }

                if (pocketIndex >= cappingPocketStartIndex &&
                    pocketIndex <= FillingExitPocketIndex &&
                    !bottle.cappingCompleted)
                {
                    cappingTargets.Add(entry);
                }
            }

            if (cappingTargets.Count > 0)
            {
                cappingTargets.Sort((left, right) => left.Value.CompareTo(right.Value));
                var batch = new List<BottleProcessState>();
                for (var i = 0; i < cappingTargets.Count; i++)
                {
                    batch.Add(cappingTargets[i].Key);
                }

                yield return TightenCapsInStarWheel(batch);
            }
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

            while (CountBottlesWaitingOnInfeedRail() < StarWheelFeedBatchSize)
            {
                yield return null;
            }

            var indexedBottles = new Dictionary<BottleProcessState, int>();
            foreach (var entry in fillingSlotAssignments)
            {
                if (entry.Key != null)
                {
                    indexedBottles[entry.Key] = entry.Value;
                }
            }

            // One 108-degree index moves the filled group toward the next stations
            // while loading the next three bottles into the filling pockets.
            yield return IndexStarWheelOnePitchWithRailFeed(indexedBottles, StarWheelIndexStepPockets, true);
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

        private IEnumerator ConsumeCapMagazineCap()
        {
            if (capMagazineCaps == null || capMagazineCaps.Count == 0)
            {
                yield break;
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
                UpdateCapMagazineVisuals();
                yield break;
            }

            yield return SlideMagazineCapsToRestingPositions();
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
                    cap.localPosition = GetCapMagazineLocalPosition(i, capMagazineVisibleCount);
                    cap.localRotation = GetCapMagazineLocalRotation(i, capMagazineVisibleCount);
                }
            }
        }

        private Vector3 GetCapMagazineLocalPosition(int capIndex, int visibleCount)
        {
            var localY = GetCapMagazineLayoutLocalY(capIndex, visibleCount);
            var tubeExitPosition = GetCapMagazineTubeLocalPosition(-CapMagazineGuideHalfLengthM);
            if (localY >= -CapMagazineGuideHalfLengthM)
            {
                return GetCapMagazineTubeLocalPosition(localY);
            }

            var railRatio = Mathf.InverseLerp(-CapMagazineGuideHalfLengthM, CapMagazineBottomLocalY, localY);
            return Vector3.Lerp(tubeExitPosition, GetCapMagazineRailOutletLocalPosition(), railRatio);
        }

        private Quaternion GetCapMagazineLocalRotation(int capIndex, int visibleCount)
        {
            var localY = GetCapMagazineLayoutLocalY(capIndex, visibleCount);
            if (localY < -CapMagazineGuideHalfLengthM)
            {
                return Quaternion.Euler(CapMagazineOutletCapLocalEulerAngles);
            }

            Vector3 pathDirection;
            var normalizedY = Mathf.Clamp(localY / CapMagazineGuideHalfLengthM, -1f, 1f);
            var slope = 2f * CapMagazineGuideCurveDepthM * normalizedY / CapMagazineGuideHalfLengthM;
            pathDirection = new Vector3(0f, 1f, slope);

            return Quaternion.FromToRotation(Vector3.up, pathDirection.normalized) * Quaternion.Euler(90f, 0f, 0f);
        }

        private static float GetCapMagazineLayoutLocalY(int capIndex, int visibleCount)
        {
            return CapMagazineBottomLocalY + (visibleCount - 1 - capIndex) * CapMagazineCapPitchM;
        }

        private static Vector3 GetCapMagazineTubeLocalPosition(float localY)
        {
            var normalizedY = Mathf.Clamp(localY / CapMagazineGuideHalfLengthM, -1f, 1f);
            return CapMagazineAssemblyLocalOffset
                + new Vector3(0f, localY, CapMagazineGuideCurveDepthM * (normalizedY * normalizedY - 1f));
        }

        private Vector3 GetCapMagazineRailOutletLocalPosition()
        {
            var assembly = capMagazineCaps != null && capMagazineCaps.Count > 0 && capMagazineCaps[0] != null
                ? capMagazineCaps[0].parent
                : null;
            if (assembly == null || capDropper == null)
            {
                return GetCapMagazineTubeLocalPosition(-CapMagazineGuideHalfLengthM);
            }

            return assembly.InverseTransformPoint(capDropper.position);
        }

        private IEnumerator SlideMagazineCapsToRestingPositions()
        {
            var capsToSlide = new List<Transform>();
            var startPositions = new List<Vector3>();
            var targetPositions = new List<Vector3>();
            for (var i = 0; i < capMagazineVisibleCount; i++)
            {
                var cap = capMagazineCaps[i];
                if (cap == null)
                {
                    continue;
                }

                capsToSlide.Add(cap);
                startPositions.Add(cap.localPosition);
                targetPositions.Add(GetCapMagazineLocalPosition(i, capMagazineVisibleCount));
            }

            var elapsed = 0f;
            while (elapsed < CapMagazineRestackSeconds)
            {
                elapsed += Time.deltaTime;
                var ratio = Mathf.SmoothStep(0f, 1f, elapsed / CapMagazineRestackSeconds);
                for (var i = 0; i < capsToSlide.Count; i++)
                {
                    capsToSlide[i].localPosition = Vector3.Lerp(startPositions[i], targetPositions[i], ratio);
                    capsToSlide[i].localRotation = GetCapMagazineLocalRotation(i, capMagazineVisibleCount);
                }

                yield return null;
            }
        }

        private Transform GetBottomMagazineCap()
        {
            if (capMagazineCaps == null || capMagazineCaps.Count == 0)
            {
                return null;
            }

            var capacity = Mathf.Clamp(capMagazineCapacity, 1, capMagazineCaps.Count);
            if (capMagazineVisibleCount <= 0)
            {
                capMagazineVisibleCount = capacity;
                UpdateCapMagazineVisuals();
            }

            return capMagazineCaps[Mathf.Clamp(capMagazineVisibleCount - 1, 0, capacity - 1)];
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
            return StarWheelSlotPosition(cappingPocketStartIndex + slotIndex);
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
            var duration = StarWheelIndexDurationForSlots(slotDelta);

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

        private IEnumerator IndexStarWheelOnePitchWithRailFeed(Dictionary<BottleProcessState, int> indexedBottles, int slotDelta, bool allowRailCapture)
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
            var duration = StarWheelIndexDurationForSlots(slotDelta);

            if (allowRailCapture)
            {
                TryCaptureBottleFromRailIntoPassingPocket(indexedBottles, capturedSteps, 0, slotDelta);
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var ratio = Mathf.Clamp01(elapsed / duration);
                if (allowRailCapture)
                {
                    var passedStep = Mathf.Min(slotDelta - 1, Mathf.FloorToInt(ratio * slotDelta));
                    for (var step = 0; step <= passedStep; step++)
                    {
                        TryCaptureBottleFromRailIntoPassingPocket(indexedBottles, capturedSteps, step, slotDelta);
                    }
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
            var bottlesToRelease = new List<KeyValuePair<BottleProcessState, float>>();
            foreach (var entry in indexedBottles)
            {
                var bottle = entry.Key;
                if (bottle == null ||
                    releasedBottles.Contains(bottle) ||
                    releasingBottles.Contains(bottle) ||
                    queuedStarWheelReleaseBottles.Contains(bottle))
                {
                    continue;
                }

                var targetSlot = entry.Value + slotDelta;
                var currentSlot = Mathf.Lerp(entry.Value, targetSlot, ratio);
                ApplyStarWheelOperationAtSlot(bottle, currentSlot);
                if (targetSlot < FillingExitPocketIndex)
                {
                    continue;
                }

                if (currentSlot >= releaseThresholdSlot && bottle.cappingCompleted)
                {
                    bottlesToRelease.Add(new KeyValuePair<BottleProcessState, float>(bottle, currentSlot));
                }
            }

            bottlesToRelease.Sort((left, right) => right.Value.CompareTo(left.Value));
            foreach (var releaseCandidate in bottlesToRelease)
            {
                var bottle = releaseCandidate.Key;
                releasedBottles.Add(bottle);
                indexedBottles.Remove(bottle);
                EnqueueStarWheelRelease(bottle);
            }
        }

        private void ApplyStarWheelOperationAtSlot(BottleProcessState bottle, float slot)
        {
            if (bottle == null || !bottle.fillingCompleted)
            {
                return;
            }

            if (slot >= capDropPocketIndex && !bottle.capPlaced)
            {
                BeginCapDrop(bottle);
            }
        }

        private float StarWheelIndexDurationForSlots(int slotDelta)
        {
            return Mathf.Max(0.05f, starWheelIndexDurationSeconds) * Mathf.Max(1, slotDelta);
        }

        private float StarWheelReleaseConveyorZ()
        {
            var exitPoint = StarWheelSlotPosition(FillingExitPocketIndex);
            return exitPoint.z + ConveyorBottleSpacingM * 0.6f;
        }

        private bool IsStarWheelReleaseConveyorClear()
        {
            var releaseZ = StarWheelReleaseConveyorZ();
            var requiredSpacing = ConveyorBottleSpacingM;
            foreach (var bottle in lineBottles)
            {
                if (bottle == null)
                {
                    continue;
                }

                var position = bottle.transform.position;
                if (Mathf.Abs(position.x - lineX) < 0.25f &&
                    Mathf.Abs(position.z - releaseZ) < requiredSpacing)
                {
                    return false;
                }
            }

            return true;
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
                if (entry.Key != null &&
                    entry.Key.cappingCompleted &&
                    entry.Value >= FillingExitPocketIndex &&
                    !releasingBottles.Contains(entry.Key) &&
                    !queuedStarWheelReleaseBottles.Contains(entry.Key))
                {
                    readyToExit.Add(entry);
                }
            }

            if (readyToExit.Count == 0)
            {
                yield break;
            }

            readyToExit.Sort((left, right) => right.Value.CompareTo(left.Value));
            foreach (var entry in readyToExit)
            {
                var bottle = entry.Key;
                if (bottle == null || !fillingSlotAssignments.ContainsKey(bottle))
                {
                    continue;
                }

                EnqueueStarWheelRelease(bottle);
            }

            yield return null;
        }

        private void EnqueueStarWheelRelease(BottleProcessState bottle)
        {
            if (bottle == null ||
                queuedStarWheelReleaseBottles.Contains(bottle) ||
                releasingBottles.Contains(bottle))
            {
                return;
            }

            queuedStarWheelReleaseBottles.Add(bottle);
            starWheelReleaseQueue.Enqueue(bottle);
            fillingSlotAssignments.Remove(bottle);
            fillingBottles.Remove(bottle);
            bottle.transform.position = StarWheelSlotPosition(FillingExitPocketIndex);

            if (!starWheelReleaseQueueRunning)
            {
                StartCoroutine(ProcessStarWheelReleaseQueue());
            }
        }

        private IEnumerator ProcessStarWheelReleaseQueue()
        {
            starWheelReleaseQueueRunning = true;
            while (starWheelReleaseQueue.Count > 0)
            {
                var bottle = starWheelReleaseQueue.Dequeue();
                queuedStarWheelReleaseBottles.Remove(bottle);
                if (bottle != null)
                {
                    while (!IsStarWheelReleaseConveyorClear())
                    {
                        yield return null;
                    }

                    yield return ReleaseOneFilledBottleToConveyor(bottle, StarWheelReleaseConveyorZ());
                    var speedBasedGap = ConveyorBottleSpacingM / Mathf.Max(0.1f, ConveyorEffectiveSpeedMps) * 0.65f;
                    yield return new WaitForSeconds(Mathf.Max(0.12f, starWheelReleaseGapSeconds, speedBasedGap));
                }
            }

            starWheelReleaseQueueRunning = false;
        }

        private IEnumerator ReleaseOneFilledBottleToConveyor(BottleProcessState bottle, float finalZ)
        {
            if (bottle == null)
            {
                yield break;
            }

            if (!releasingBottles.Add(bottle))
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
            releasingBottles.Remove(bottle);
        }

        private void BeginCapDrop(BottleProcessState bottle)
        {
            if (bottle == null || bottle.capPlaced || !capDroppingBottles.Add(bottle))
            {
                return;
            }

            StartCoroutine(DropCapOnBottle(bottle));
        }

        private IEnumerator DropCapOnBottle(BottleProcessState bottle)
        {
            if (bottle == null)
            {
                yield break;
            }

            var sourceCap = GetBottomMagazineCap();
            var capTargetPosition = bottle.capVisual != null
                ? bottle.capVisual.position
                : bottle.transform.position + Vector3.up * 0.66f;
            var outletPosition = capDropper != null
                ? capDropper.position + Vector3.down * 0.04f
                : capTargetPosition + Vector3.up * 0.10f;

            if (sourceCap != null)
            {
                sourceCap.gameObject.SetActive(true);
                yield return MoveCapVisual(sourceCap, sourceCap.position, outletPosition, CapGuideSlideSeconds);
            }

            if (sourceCap != null)
            {
                yield return MoveCapVisual(sourceCap, outletPosition, capTargetPosition, CapCatchSlideSeconds);
            }

            if (sourceCap != null)
            {
                sourceCap.gameObject.SetActive(false);
            }

            bottle.capPlaced = true;
            bottle.RefreshVisuals();
            yield return ConsumeCapMagazineCap();

            if (capSensorBeam != null)
            {
                var baseScale = capSensorBeam.localScale;
                capSensorBeam.localScale = new Vector3(baseScale.x, baseScale.y * 2.2f, baseScale.z);
                yield return null;
                capSensorBeam.localScale = baseScale;
            }

            capDroppingBottles.Remove(bottle);
        }

        private IEnumerator MoveCapVisual(Transform cap, Vector3 from, Vector3 to, float duration)
        {
            if (cap == null)
            {
                yield break;
            }

            var elapsed = 0f;
            var moveDuration = Mathf.Max(0.01f, duration);
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                cap.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / moveDuration));
                yield return null;
            }

            cap.position = to;
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

        private IEnumerator TightenCapsInStarWheel(List<BottleProcessState> batch)
        {
            if (batch == null || batch.Count == 0)
            {
                yield break;
            }

            cappingStationBusy = true;
            CappingActive = true;
            var activeHeads = GetActiveCappingHeads();
            if (activeHeads.Count == 0)
            {
                cappingStationBusy = false;
                CappingActive = false;
                yield break;
            }

            AlignCappingHeadsToStarWheelPockets(activeHeads);
            foreach (var bottle in batch)
            {
                if (bottle != null && fillingSlotAssignments.ContainsKey(bottle))
                {
                    cappingBottles.Add(bottle);
                    SnapBottleToFillingSlot(bottle);
                }
            }

            var basePositions = GetTransformPositions(activeHeads);
            var downPositions = OffsetPositions(basePositions, Vector3.down * (capperStrokeM * 0.65f));
            yield return MoveAndSpinCappingHeads(activeHeads, basePositions, downPositions, capperMoveSeconds, 360f);

            var spinTime = Mathf.Max(0.05f, cappingTimeSeconds);
            var elapsed = 0f;
            while (elapsed < spinTime)
            {
                elapsed += Time.deltaTime;
                foreach (var bottle in batch)
                {
                    SnapBottleToFillingSlot(bottle);
                }

                SpinCappingHeadTools(activeHeads, CappingHeadAngularSpeedDegreesPerSecond);
                yield return null;
            }

            foreach (var bottle in batch)
            {
                if (bottle != null && fillingSlotAssignments.ContainsKey(bottle))
                {
                    bottle.capPlaced = true;
                    bottle.cappingCompleted = true;
                    bottle.status = BottleQualityStatus.Capped;
                    bottle.RefreshVisuals();
                }
            }

            yield return MoveCappingHeads(activeHeads, downPositions, basePositions, capperMoveSeconds);

            foreach (var bottle in batch)
            {
                if (bottle != null)
                {
                    cappingBottles.Remove(bottle);
                }
            }

            cappingStationBusy = false;
            CappingActive = false;
        }

        private void AlignCappingHeadsToStarWheelPockets(List<Transform> activeHeads)
        {
            if (activeHeads == null)
            {
                return;
            }

            for (var i = 0; i < activeHeads.Count; i++)
            {
                var head = activeHeads[i];
                if (head == null)
                {
                    continue;
                }

                var pocketPosition = StarWheelSlotPosition(cappingPocketStartIndex + i);
                var position = head.position;
                position.x = pocketPosition.x;
                position.z = pocketPosition.z;
                head.position = position;
            }
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
                if (bottle.cappingCompleted)
                {
                    bottle.status = BottleQualityStatus.Capped;
                    bottle.RefreshVisuals();
                }
                else
                {
                    bottle.MarkPassed();
                }
            }
            else
            {
                InspectionStatus = InspectionStatus.AnomalyDetected;
                bottle.MarkRejected();
            }
        }

        private float PackRowPitch => Mathf.Max(packRowPitchM, turntableBottleRadius * 2f + 0.015f);
        private float PackGateHoldZ => packGateZ - turntableBottleRadius - 0.01f;

        private void AssignBottleToSplitLane(BottleProcessState bottle)
        {
            if (bottle == null || splitLaneAssignments.ContainsKey(bottle))
            {
                return;
            }

            splitLaneAssignments[bottle] = nextSplitLane;
            SplitSensorCount++;
            bottlesInSplitGroup++;
            if (bottlesInSplitGroup >= Mathf.Max(1, splitGroupSize))
            {
                bottlesInSplitGroup = 0;
                nextSplitLane = nextSplitLane == SplitLane.A ? SplitLane.B : SplitLane.A;
            }
        }

        private void UpdateSplitterGuide()
        {
            foreach (var assignment in splitLaneAssignments)
            {
                if (assignment.Key != null && assignment.Key.transform.position.z >= splitGuideZ + turntableBottleRadius)
                {
                    splitGuidePassedBottles.Add(assignment.Key);
                }
            }

            if (splitGuideMoving)
            {
                return;
            }

            BottleProcessState nextBottle = null;
            SplitLane desiredLane = SplitLane.A;
            var nearestZ = float.NegativeInfinity;
            foreach (var assignment in splitLaneAssignments)
            {
                var bottle = assignment.Key;
                if (bottle == null || splitGuidePassedBottles.Contains(bottle) || !lineBottles.Contains(bottle))
                {
                    continue;
                }

                var z = bottle.transform.position.z;
                if (z > nearestZ)
                {
                    nearestZ = z;
                    nextBottle = bottle;
                    desiredLane = assignment.Value;
                }
            }

            if (nextBottle == null || desiredLane == splitGuideLane)
            {
                return;
            }

            var speed = Mathf.Max(0.01f, ConveyorEffectiveSpeedMps);
            var leadTime = Mathf.Max(0f, splitGuideZ - nextBottle.transform.position.z) / speed;
            var availableSwitchTime = Mathf.Max(0f, ConveyorBottleSpacingM - turntableBottleRadius * 2f - splitterSafetyGapM) / speed;
            if (leadTime < splitGuideMoveSeconds + 0.02f || availableSwitchTime < splitGuideMoveSeconds)
            {
                SplitterSafetyInterlocked = true;
                splitterPaused = true;
            }

            StartCoroutine(MoveSplitGuide(desiredLane));
        }

        private IEnumerator MoveSplitGuide(SplitLane targetLane)
        {
            splitGuideMoving = true;
            var from = splitGuidePivot != null ? splitGuidePivot.rotation : Quaternion.identity;
            var to = Quaternion.Euler(0f, targetLane == SplitLane.B ? splitGuideAngleDegrees : 0f, 0f);
            var elapsed = 0f;
            var duration = Mathf.Max(0.01f, splitGuideMoveSeconds);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (splitGuidePivot != null)
                {
                    splitGuidePivot.rotation = Quaternion.Slerp(from, to, elapsed / duration);
                }

                yield return null;
            }

            if (splitGuidePivot != null)
            {
                splitGuidePivot.rotation = to;
            }

            splitGuideLane = targetLane;
            splitGuideMoving = false;
            splitterPaused = false;
            SplitterSafetyInterlocked = false;
        }

        private float ResolveLaneX(SplitLane lane, float z)
        {
            if (lane == SplitLane.A || z <= splitGuideZ)
            {
                return lineX;
            }

            return Mathf.Lerp(lineX, laneBCenterX, Mathf.InverseLerp(splitGuideZ, splitGuideExitZ, z));
        }

        private bool CanAcceptBottleIntoPack(SplitLane lane)
        {
            return !packLoadingOut && PackLaneBottles(lane).Count < 3;
        }

        private List<BottleProcessState> PackLaneBottles(SplitLane lane)
        {
            return lane == SplitLane.A ? packLaneABottles : packLaneBBottles;
        }

        private bool IsBottleInPack(BottleProcessState bottle)
        {
            return bottle != null && (packLaneABottles.Contains(bottle) || packLaneBBottles.Contains(bottle));
        }

        private bool IsPackGateClosed(SplitLane lane)
        {
            if (packGatePhase == PackGatePhase.BlockingForPusher)
            {
                return true;
            }

            if (packGatePhase == PackGatePhase.ResetHold)
            {
                return true;
            }

            return PackLaneBottles(lane).Count >= 3;
        }

        private void RegisterBottleInPack(BottleProcessState bottle, SplitLane lane)
        {
            if (bottle == null || IsBottleInPack(bottle) || !CanAcceptBottleIntoPack(lane))
            {
                return;
            }

            PackLaneBottles(lane).Add(bottle);
            if (lane == SplitLane.A)
            {
                PackGateSensorCountA++;
            }
            else
            {
                PackGateSensorCountB++;
            }

        }

        private void TryStartSixPackDischarge()
        {
            if (packLoadingOut || packLaneABottles.Count != 3 || packLaneBBottles.Count != 3)
            {
                return;
            }

            if (!IsPackLaneReadyForPush(packLaneABottles) || !IsPackLaneReadyForPush(packLaneBBottles))
            {
                return;
            }

            StartCoroutine(DischargeFullSixPack());
        }

        private bool IsPackLaneReadyForPush(List<BottleProcessState> laneBottles)
        {
            for (var row = 0; row < laneBottles.Count; row++)
            {
                var bottle = laneBottles[row];
                if (bottle == null || bottle.transform.position.z < PackBottleRestZ(row) - 0.01f)
                {
                    return false;
                }
            }

            return true;
        }

        private float PackBottleRestZ(int row)
        {
            return packFrontRowZ - row * PackRowPitch;
        }

        private void UpdatePackStopGateVisuals()
        {
            SetPackStopGateVisual(packStopGateA, IsPackGateClosed(SplitLane.A));
            SetPackStopGateVisual(packStopGateB, IsPackGateClosed(SplitLane.B));
        }

        private void SetPackStopGateVisual(Transform gate, bool closed)
        {
            if (gate == null)
            {
                return;
            }

            if (gate == packStopGateA || gate == packStopGateB)
            {
                var blockedAngle = gate == packStopGateA ? -90f : 90f;
                var targetRotation = Quaternion.Euler(0f, 0f, closed ? blockedAngle : 0f);
                var turnSpeed = 90f / Mathf.Max(0.01f, packGateMoveSeconds);
                gate.localRotation = Quaternion.RotateTowards(gate.localRotation, targetRotation, turnSpeed * Time.deltaTime);
                return;
            }

            var targetY = closed ? packGateClosedY : packGateOpenY;
            var travelSpeed = Mathf.Abs(packGateClosedY - packGateOpenY) / Mathf.Max(0.01f, packGateMoveSeconds);
            var position = gate.position;
            position.y = Mathf.MoveTowards(position.y, targetY, travelSpeed * Time.deltaTime);
            gate.position = position;
        }

        private string DeterminePackGateState()
        {
            if (packGatePhase == PackGatePhase.BlockingForPusher)
            {
                return "Blocking during push";
            }

            if (packGatePhase == PackGatePhase.ResetHold)
            {
                return "Reset hold";
            }

            return PackGateAClosed || PackGateBClosed ? "Blocking" : "Loading";
        }

        private IEnumerator WaitForPackGateTravel()
        {
            var elapsed = 0f;
            while (elapsed < packGateMoveSeconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private Vector3 PackCartonSlot(SplitLane lane, int row)
        {
            var laneOffset = lane == SplitLane.A ? -laneBCenterX * 0.5f : laneBCenterX * 0.5f;
            return packCartonLoadPosition + new Vector3(laneOffset, 0.18f, PackRowPitch - row * PackRowPitch);
        }

        private IEnumerator DischargeFullSixPack()
        {
            packLoadingOut = true;
            packGatePhase = PackGatePhase.BlockingForPusher;
            var batch = new List<BottleProcessState>();
            batch.AddRange(packLaneABottles);
            batch.AddRange(packLaneBBottles);
            foreach (var bottle in batch)
            {
                if (bottle == null)
                {
                    continue;
                }

                packingBottles.Add(bottle);
                lineBottles.Remove(bottle);
            }

            // Ensure both swing-gates are fully across their lanes before the pusher stroke.
            yield return WaitForPackGateTravel();

            var bottleStarts = new List<Vector3>();
            var bottleTargets = new List<Vector3>();
            for (var i = 0; i < batch.Count; i++)
            {
                var lane = i < 3 ? SplitLane.A : SplitLane.B;
                var row = i % 3;
                bottleStarts.Add(batch[i].transform.position);
                bottleTargets.Add(PackCartonSlot(lane, row));
            }

            var pusherStart = packPusher != null ? packPusher.position : Vector3.zero;
            var pusherHalfWidth = packPusher != null ? packPusher.lossyScale.x * 0.5f : 0f;
            var pusherEnd = new Vector3(
                packCartonLoadPosition.x - packCartonWidthM * 0.5f - pusherHalfWidth - packPusherCartonClearanceM,
                pusherStart.y,
                pusherStart.z);
            var elapsed = 0f;
            while (elapsed < packPusherSeconds)
            {
                elapsed += Time.deltaTime;
                var ratio = Mathf.SmoothStep(0f, 1f, elapsed / Mathf.Max(0.01f, packPusherSeconds));
                if (packPusher != null)
                {
                    packPusher.position = Vector3.Lerp(pusherStart, pusherEnd, ratio);
                }

                for (var i = 0; i < batch.Count; i++)
                {
                    batch[i].transform.position = Vector3.Lerp(bottleStarts[i], bottleTargets[i], ratio);
                }

                yield return null;
            }

            var cartonStart = packCartonLoadPosition;
            var cartonOffset = packCartonExitPosition - cartonStart;
            elapsed = 0f;
            while (elapsed < packCartonExitSeconds)
            {
                elapsed += Time.deltaTime;
                var ratio = Mathf.SmoothStep(0f, 1f, elapsed / Mathf.Max(0.01f, packCartonExitSeconds));
                if (packCarton != null)
                {
                    packCarton.position = Vector3.Lerp(cartonStart, packCartonExitPosition, ratio);
                }

                for (var i = 0; i < batch.Count; i++)
                {
                    batch[i].transform.position = Vector3.Lerp(bottleTargets[i], bottleTargets[i] + cartonOffset, ratio);
                }

                yield return null;
            }

            elapsed = 0f;
            while (elapsed < packPusherReturnSeconds)
            {
                elapsed += Time.deltaTime;
                var ratio = Mathf.SmoothStep(0f, 1f, elapsed / Mathf.Max(0.01f, packPusherReturnSeconds));
                if (packPusher != null)
                {
                    packPusher.position = Vector3.Lerp(pusherEnd, pusherStart, ratio);
                }

                yield return null;
            }

            foreach (var bottle in batch)
            {
                if (bottle == null)
                {
                    continue;
                }

                bottle.status = BottleQualityStatus.AcceptedBin;
                bottle.RefreshVisuals();
                CountBottle(bottle, true);
                packingBottles.Remove(bottle);
                bottle.gameObject.SetActive(false);
            }

            if (packCarton != null)
            {
                packCarton.position = cartonStart;
            }

            if (packPusher != null)
            {
                packPusher.position = pusherStart;
            }

            packLaneABottles.Clear();
            packLaneBBottles.Clear();
            CartonsFilled++;

            // Hold the swing-gates closed briefly after the piston has returned.
            packGatePhase = PackGatePhase.ResetHold;
            yield return new WaitForSeconds(Mathf.Max(0f, packGateResetHoldSeconds));

            packGatePhase = PackGatePhase.Loading;
            yield return WaitForPackGateTravel();
            packLoadingOut = false;
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
            if (bottle == null || !pushingBottles.Add(bottle))
            {
                yield break;
            }

            bottle.transform.position = new Vector3(lineX, bottle.transform.position.y, pusherZ);

            var basePosition = pneumaticPusher != null ? pneumaticPusher.localPosition : Vector3.zero;
            var extendedPosition = basePosition + new Vector3(-0.65f, 0f, 0f);

            yield return MovePusher(basePosition, extendedPosition, 0.18f);
            bottle.status = BottleQualityStatus.RejectedBin;
            bottle.RefreshVisuals();
            CountBottle(bottle, false);
            bottle.gameObject.SetActive(false);
            yield return MovePusher(extendedPosition, basePosition, 0.22f);

            pushingBottles.Remove(bottle);
        }

        private IEnumerator MoveBottleToChute(BottleProcessState bottle, Vector3 from, Vector3 to, float duration, float lift)
        {
            if (bottle == null)
            {
                yield break;
            }

            var elapsed = 0f;
            var moveDuration = Mathf.Max(0.05f, duration);
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                var ratio = Mathf.SmoothStep(0f, 1f, elapsed / moveDuration);
                var arc = Mathf.Sin(ratio * Mathf.PI) * lift;
                bottle.transform.position = Vector3.Lerp(from, to, ratio) + Vector3.up * arc;
                yield return null;
            }

            bottle.transform.position = to;
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
            splitLaneAssignments.Remove(bottle);
            splitGuidePassedBottles.Remove(bottle);
            packLaneABottles.Remove(bottle);
            packLaneBBottles.Remove(bottle);
            packingBottles.Remove(bottle);
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
