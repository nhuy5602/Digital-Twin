using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ConveyorTwin
{
    public enum InspectionStatus
    {
        Normal,
        AnomalyDetected
    }

    public class FillingFilteringDigitalTwin : MonoBehaviour
    {
        private struct InfeedGuideTransition
        {
            public Vector3 startPosition;
            public Vector3 targetPosition;
            public float elapsedSeconds;
        }

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
        public Transform rejectSweepBar;
        public Transform rejectedBottleTray;
        public Transform acceptChute;
        public Transform splitSensorBeam;
        public Transform splitGuidePivot;
        public Transform packCarton;
        public Transform packPusher;
        public Transform packStopGateA;
        public Transform packStopGateB;
        public Transform packGateSensorA;
        public Transform packGateSensorB;

        [Header("Infeed mechanical guides")]
        public Collider infeedTurntableTransferPlate;
        public Collider infeedTurntableDiagonalDeflector;
        public List<Transform> infeedGuidePathPoints = new List<Transform>();

        [Header("Bottle line")]
        public BottleProcessState bottleTemplate;
        public List<BottleProcessState> bottles = new List<BottleProcessState>();
        public float conveyorSpeedMps = 0.85f;
        public float infeedStartZ = -4.2f;
        public float fillingZ = -1.65f;
        public float qcZ = 0.85f;
        public float rejectStationZ = 2.25f;
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
        [Min(0.1f)] public float starWheelIndexSpeedRpm = 6.67f;
        [Min(0.10f)] public float starWheelDwellSeconds = 1.35f;
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
        public int cappingPocketStartIndex = 6;
        public float cappingSpeedMultiplier = 10f;

        [Header("Rejected bottle tray")]
        [Min(1)] public int rejectedTrayCapacity = 4;
        [Min(0f)] public float rejectedTrayDischargeDelaySeconds = 0.08f;
        [Min(0.05f)] public float rejectedTrayDischargeSeconds = 0.10f;
        [Min(0.05f)] public float rejectedTrayReturnSeconds = 0.10f;
        public Vector3 rejectedTrayDischargeOffset = new Vector3(-1.15f, 0f, 0f);

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
        public float bottleDropOutletBiasM = 0.18f;
        public float spawnIntervalSeconds = 0.45f;
        public int initialTurntableBottleCount = 12;
        public int maxTurntableBuffer = 16;
        public int releaseThreshold = 7;
        public float releaseIntervalSeconds = 0.65f;
        [Tooltip("When enabled, the infeed RPM changes the bottle release interval while respecting conveyor spacing.")]
        public bool linkInfeedRpmToRelease = true;
        public float referenceInfeedMotorSpeedRpm = 18f;
        public float referenceReleaseIntervalSeconds = 0.62f;
        public float turntableSurfaceGrip = 3.5f;
        public float turntableVelocityDamping = 0.96f;
        public float turntableBottleRadius = 0.11f;
        [Range(0, 6)] public int turntableBottleSeparationIterations = 2;

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

        [Header("Turntable-to-conveyor infeed guide")]
        public float infeedGuideWheelCaptureDistanceM = 0.08f;
        [Min(0.01f)] public float infeedGuideCaptureTransitionSeconds = 0.14f;

        [Header("Process settings")]
        [Range(0f, 1f)] public float passThreshold = 0.95f;
        [Min(0f)] public float pumpFlowLitersPerMinute;
        public float bottleCapacityLiters = 1f;
        public float initialVesselLevelLiters = 120f;
        public float vesselCapacityLiters = 150f;
        public float infeedMotorSpeedRpm = 18f;
        [Header("Repeatable experiments")]
        public int randomSeed = 12345;

        public float ThroughputBottlesPerHour { get; private set; }
        public float InfeedMotorSpeedRpm => infeedMotorSpeedRpm;
        public float LiquidLevelLiters { get; private set; }
        public float LastFillingTimeSeconds { get; private set; }
        public InspectionStatus InspectionStatus { get; private set; } = InspectionStatus.Normal;
        public int TotalPassed { get; private set; }
        public int TotalRejected { get; private set; }
        public int TotalRejectEscapes { get; private set; }
        public int RejectedTrayBottleCount => rejectedTrayBottles.Count;
        public bool RejectTrayDischargeActive => rejectedTrayDischargeActive;
        public int TurntableBufferCount { get; private set; }
        public int BottlesOnConveyorCount { get; private set; }
        public float TurntableAngularSpeedRadPerSec { get; private set; }
        public float CentrifugalAccelerationAtRimMps2 { get; private set; }
        public float AverageFillPercent => completedFillSamples > 0 ? completedFillRatioTotal / completedFillSamples * 100f : 0f;
        public float LastBatchFillPercent { get; private set; }
        public float RejectRatePercent => TotalPassed + TotalRejected > 0
            ? TotalRejected * 100f / (TotalPassed + TotalRejected)
            : 0f;
        public bool SimulationPaused { get; private set; }
        public float EffectiveReleaseIntervalSeconds
        {
            get
            {
                var safeInterval = minimumBottleSpacingM / Mathf.Max(0.05f, ConveyorEffectiveSpeedMps);
                if (!linkInfeedRpmToRelease)
                {
                    return Mathf.Max(safeInterval, releaseIntervalSeconds);
                }

                var referenceRpm = Mathf.Max(0.1f, referenceInfeedMotorSpeedRpm);
                var rpmScaledInterval = referenceReleaseIntervalSeconds * referenceRpm / Mathf.Max(0.1f, infeedMotorSpeedRpm);
                return Mathf.Max(safeInterval, rpmScaledInterval);
            }
        }
        public float EffectiveFillingDwellSeconds => TwinProcessMath.CalculateDiscDwellSeconds(starWheelDwellSeconds);
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
        private float StarWheelAngularSpeedDegreesPerSecond => Mathf.Max(0.1f, starWheelIndexSpeedRpm) * 6f;
        private float CappingHeadAngularSpeedDegreesPerSecond => StarWheelAngularSpeedDegreesPerSecond * Mathf.Max(1f, cappingSpeedMultiplier);
        private float InfeedGuideBottleSpacingM => Mathf.Max(0.18f, turntableBottleRadius * 1.7f);
        private float InfeedGuideCaptureZoneM => Mathf.Max(0.45f, InfeedGuideBottleSpacingM * StarWheelIndexStepPockets);
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
        private readonly HashSet<BottleProcessState> rejectingBottles = new HashSet<BottleProcessState>();
        private readonly HashSet<BottleProcessState> rejectSweepRequestedBottles = new HashSet<BottleProcessState>();
        private readonly HashSet<BottleProcessState> escapedRejectBottles = new HashSet<BottleProcessState>();
        private readonly List<BottleProcessState> sweepCapturedBottles = new List<BottleProcessState>();
        private readonly List<BottleProcessState> rejectedTrayBottles = new List<BottleProcessState>();
        private readonly HashSet<BottleProcessState> droppingBottles = new HashSet<BottleProcessState>();
        private readonly HashSet<BottleProcessState> capDroppingBottles = new HashSet<BottleProcessState>();
        private readonly Dictionary<BottleProcessState, int> fillingSlotAssignments = new Dictionary<BottleProcessState, int>();
        private readonly Dictionary<BottleProcessState, int> cappingSlotAssignments = new Dictionary<BottleProcessState, int>();
        private readonly Dictionary<BottleProcessState, float> infeedGuideProgresses = new Dictionary<BottleProcessState, float>();
        private readonly Dictionary<BottleProcessState, InfeedGuideTransition> infeedGuideTransitions = new Dictionary<BottleProcessState, InfeedGuideTransition>();
        private int completedCount;
        private float spawnTimer;
        private float releaseTimer;
        private int spawnedCount;
        private bool fillingStationBusy;
        private bool fillingCaptureBusy;
        private bool cappingStationBusy;
        private bool rejectSweepActive;
        private bool rejectedTrayDischargeActive;
        private float fillingCaptureBusySince = -1f;
        private float fillingStationBusySince = -1f;
        private float starWheelIndexingSince = -1f;
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
        private float completedFillRatioTotal;
        private int completedFillSamples;
        private TwinSetpoints defaultSetpoints;
        private bool defaultsCaptured;

        private static TwinSetpoints pendingResetSetpoints;
        private static int? pendingResetSeed;

        private float MaxTurntableBottleCenterRadius => Mathf.Max(0.05f, turntableRadius - turntableBottleRadius);
        private float QcSensorTriggerZ => qcSensorBeam != null ? qcSensorBeam.position.z : qcZ;
        private float RejectSweepStationZ => rejectSweepBar != null ? rejectSweepBar.position.z : rejectStationZ;
        private float RejectSweepHalfLengthM => GetRejectSweepBounds().extents.z;
        private float RejectEscapeOutfeedZ => Mathf.Max(packFrontRowZ + 0.60f, RejectSweepStationZ + RejectSweepHalfLengthM + turntableBottleRadius + 0.60f);

        private void Awake()
        {
            Time.timeScale = 1f;
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
            InitializeDigitalTwinDefaults();
            SetFillingFlowVisuals(GetActiveFillingNozzles(), false);
            capMagazineVisibleCount = Mathf.Clamp(capMagazineCapacity, 1, Mathf.Max(1, capMagazineCaps.Count));
            UpdateCapMagazineVisuals();
        }

        private void Update()
        {
            InitializeTurntableIfNeeded();
            AnimateMachines();
            UpdateInfeedGuideTransitions();
            UpdateTurntableBuffer();
            UpdateSplitterGuide();
            UpdatePackStopGateVisuals();
            // Filling stop gate is disabled so the conveyor and star wheel stay visually unobstructed.
            UpdateVesselVisual();
            RecoverStarWheelLocks();
            MoveBottles();
            TryStartSixPackDischarge();
            TryStartStarWheelFeedFromInfeedGuide();
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

        public TwinSetpoints GetSetpoints()
        {
            return new TwinSetpoints
            {
                conveyorSpeedMps = conveyorSpeedMps,
                pumpFlowLitersPerMinute = pumpFlowLitersPerMinute,
                infeedMotorSpeedRpm = infeedMotorSpeedRpm,
                starWheelIndexSpeedRpm = starWheelIndexSpeedRpm,
                starWheelDwellSeconds = starWheelDwellSeconds
            };
        }

        public void ApplySetpoints(TwinSetpoints setpoints)
        {
            if (setpoints == null)
            {
                return;
            }

            conveyorSpeedMps = Mathf.Clamp(setpoints.conveyorSpeedMps, 0.2f, 2.5f);
            pumpFlowLitersPerMinute = Mathf.Clamp(setpoints.pumpFlowLitersPerMinute, 0f, 300f);
            infeedMotorSpeedRpm = Mathf.Clamp(setpoints.infeedMotorSpeedRpm, 5f, 60f);
            starWheelIndexSpeedRpm = Mathf.Clamp(setpoints.starWheelIndexSpeedRpm, 1f, 30f);
            starWheelDwellSeconds = Mathf.Clamp(setpoints.starWheelDwellSeconds, 0.10f, 5f);
        }

        public void ApplyPreset(TwinScenarioPreset preset)
        {
            if (!defaultsCaptured)
            {
                return;
            }

            var settings = defaultSetpoints.Clone();
            switch (preset)
            {
                case TwinScenarioPreset.HighConveyor:
                    settings.conveyorSpeedMps = Mathf.Min(2.5f, settings.conveyorSpeedMps * 1.65f);
                    break;
                case TwinScenarioPreset.LowPumpFlow:
                    settings.pumpFlowLitersPerMinute *= 0.55f;
                    break;
                case TwinScenarioPreset.HighInfeedRpm:
                    settings.infeedMotorSpeedRpm = Mathf.Min(60f, settings.infeedMotorSpeedRpm * 1.65f);
                    break;
                case TwinScenarioPreset.FastDiscIndex:
                    settings.starWheelIndexSpeedRpm = 30f;
                    break;
                case TwinScenarioPreset.SlowDiscIndex:
                    settings.starWheelIndexSpeedRpm = 2f;
                    break;
                case TwinScenarioPreset.ShortDiscDwell:
                    settings.starWheelDwellSeconds = 0.35f;
                    break;
                case TwinScenarioPreset.LongDiscDwell:
                    settings.starWheelDwellSeconds = 3.50f;
                    break;
            }

            ApplySetpoints(settings);
        }

        public void SetSimulationPaused(bool paused)
        {
            SimulationPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
        }

        public void ResetSimulation(bool useNewSeed)
        {
            pendingResetSetpoints = GetSetpoints();
            pendingResetSeed = useNewSeed ? randomSeed + 1 : randomSeed;
            Time.timeScale = 1f;
            var activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
        }

        public TwinSnapshot CreateSnapshot()
        {
            var alert = "Normal";
            if (LiquidLevelLiters <= bottleCapacityLiters * ActiveFillingNozzleCount)
            {
                alert = "Low vessel level";
            }
            else if (TotalRejectEscapes > 0)
            {
                alert = "Reject escape detected";
            }
            else if (RejectRatePercent >= 10f)
            {
                alert = "High reject rate";
            }
            else if (TurntableBufferCount >= Mathf.Max(1, maxTurntableBuffer - 1))
            {
                alert = "Turntable buffer near full";
            }
            else if (LastBatchFillPercent > 0f && LastBatchFillPercent < passThreshold * 100f)
            {
                alert = "Underfill detected";
            }

            return new TwinSnapshot
            {
                simulationSeconds = Time.time,
                paused = SimulationPaused,
                conveyorSpeedMps = conveyorSpeedMps,
                pumpFlowLitersPerMinute = pumpFlowLitersPerMinute,
                infeedMotorSpeedRpm = infeedMotorSpeedRpm,
                starWheelIndexSpeedRpm = starWheelIndexSpeedRpm,
                starWheelDwellSeconds = EffectiveFillingDwellSeconds,
                starWheelIndexDurationSeconds = StarWheelIndexDurationForSlots(1),
                effectiveReleaseIntervalSeconds = EffectiveReleaseIntervalSeconds,
                effectiveFillingDwellSeconds = EffectiveFillingDwellSeconds,
                throughputBottlesPerHour = ThroughputBottlesPerHour,
                averageFillPercent = AverageFillPercent,
                lastBatchFillPercent = LastBatchFillPercent,
                rejectRatePercent = RejectRatePercent,
                vesselLevelLiters = LiquidLevelLiters,
                vesselCapacityLiters = vesselCapacityLiters,
                turntableBufferCount = TurntableBufferCount,
                bottlesOnConveyorCount = BottlesOnConveyorCount,
                totalPassed = TotalPassed,
                totalRejected = TotalRejected,
                totalRejectEscapes = TotalRejectEscapes,
                angularSpeedRadPerSec = TurntableAngularSpeedRadPerSec,
                centrifugalAccelerationMps2 = CentrifugalAccelerationAtRimMps2,
                starWheelPhase = StarWheelPhase,
                alert = alert
            };
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

        private void UpdateInfeedGuideTransitions()
        {
            if (infeedGuideTransitions.Count == 0)
            {
                return;
            }

            var transitions = new List<KeyValuePair<BottleProcessState, InfeedGuideTransition>>(infeedGuideTransitions);
            foreach (var entry in transitions)
            {
                var bottle = entry.Key;
                if (bottle == null || bottle.infeedState != InfeedBottleState.TransitioningToInfeedGuide)
                {
                    infeedGuideTransitions.Remove(bottle);
                    continue;
                }

                var transition = entry.Value;
                transition.elapsedSeconds += Time.deltaTime;
                var duration = Mathf.Max(0.01f, infeedGuideCaptureTransitionSeconds);
                var ratio = Mathf.Clamp01(transition.elapsedSeconds / duration);
                var terminalTangent = GetInfeedGuideTangentAtProgress(0f) * ConveyorEffectiveSpeedMps * duration;
                bottle.transform.position = EvaluateCubicHermite(
                    transition.startPosition,
                    transition.targetPosition,
                    Vector3.zero,
                    terminalTangent,
                    ratio);

                if (ratio >= 1f)
                {
                    bottle.transform.position = transition.targetPosition;
                    bottle.infeedState = InfeedBottleState.OnInfeedGuide;
                    infeedGuideProgresses[bottle] = 0f;
                    infeedGuideTransitions.Remove(bottle);
                }
                else
                {
                    infeedGuideTransitions[bottle] = transition;
                }
            }
        }

        private static Vector3 EvaluateCubicHermite(Vector3 start, Vector3 end, Vector3 startTangent, Vector3 endTangent, float t)
        {
            var t2 = t * t;
            var t3 = t2 * t;
            return
                (2f * t3 - 3f * t2 + 1f) * start +
                (t3 - 2f * t2 + t) * startTangent +
                (-2f * t3 + 3f * t2) * end +
                (t3 - t2) * endTangent;
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
                bottle.infeedState = InfeedBottleState.OnTurntable;
                turntableBottles.Add(bottle);
            }

            TurntableBufferCount = turntableBottles.Count;
            spawnTimer = 0f;
            releaseTimer = EffectiveReleaseIntervalSeconds;
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
            bottle.infeedState = InfeedBottleState.DroppingToTurntable;

            var angle = spawnedCount * 137.5f * Mathf.Deg2Rad;
            var radius = Random.Range(0.05f, turntableRadius * 0.25f);
            var landingRadial = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            var outletBias = Mathf.Max(0f, bottleDropOutletBiasM);
            landingRadial.x = Mathf.Max(landingRadial.x + outletBias, outletBias);
            landingRadial = Vector2.ClampMagnitude(landingRadial, MaxTurntableBottleCenterRadius);
            var target = turntableCenter + new Vector3(landingRadial.x, 0f, landingRadial.y);
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
            bottle.infeedState = InfeedBottleState.OnTurntable;
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
            bottle.infeedState = InfeedBottleState.None;
            infeedGuideProgresses.Remove(bottle);
            infeedGuideTransitions.Remove(bottle);
            splitLaneAssignments.Remove(bottle);
            splitGuidePassedBottles.Remove(bottle);
            packLaneABottles.Remove(bottle);
            packLaneBBottles.Remove(bottle);
            packingBottles.Remove(bottle);
            rejectingBottles.Remove(bottle);
            rejectedTrayBottles.Remove(bottle);
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
                if (bottle == null || bottle.infeedState != InfeedBottleState.OnTurntable)
                {
                    turntableBottles.RemoveAt(i);
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
                ConstrainTurntableBottleAgainstDiagonalDeflector(bottle);

                if (TryCaptureBottleAtInfeedGuide(bottle))
                {
                    turntableBottles.RemoveAt(i);
                }
            }

            ResolveTurntableBottleSeparation();
        }

        private bool TryCaptureBottleAtInfeedGuide(BottleProcessState bottle)
        {
            if (!ConstrainTurntableBottleAgainstInfeedGuide(bottle))
            {
                return false;
            }

            if (IsConveyorStopped() || releaseTimer < EffectiveReleaseIntervalSeconds || turntableBottles.Count < releaseThreshold || !HasInfeedGuidePath)
            {
                return false;
            }

            if (!TryGetAvailableInfeedGuideCapture(bottle))
            {
                return false;
            }

            var startPosition = bottle.transform.position;
            bottle.turntableVelocity = Vector2.zero;
            bottle.status = BottleQualityStatus.Empty;
            bottle.infeedState = InfeedBottleState.TransitioningToInfeedGuide;
            infeedGuideProgresses[bottle] = 0f;
            infeedGuideTransitions[bottle] = new InfeedGuideTransition
            {
                startPosition = startPosition,
                targetPosition = InfeedGuidePositionAtProgress(0f),
                elapsedSeconds = 0f
            };
            lineBottles.Add(bottle);
            releaseTimer = 0f;
            return true;
        }

        private void ResolveTurntableBottleSeparation()
        {
            if (turntableBottleSeparationIterations <= 0 || turntableBottles.Count == 0)
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
                    if (first == null || first.infeedState != InfeedBottleState.OnTurntable)
                    {
                        continue;
                    }

                    for (var j = i + 1; j < turntableBottles.Count; j++)
                    {
                        var second = turntableBottles[j];
                        if (second == null || second.infeedState != InfeedBottleState.OnTurntable)
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

                ResolveTurntableToInfeedGuideSeparation(minDistance, minDistanceSqr);
                foreach (var bottle in turntableBottles)
                {
                    if (bottle != null && bottle.infeedState == InfeedBottleState.OnTurntable)
                    {
                        ConstrainTurntableBottleAgainstDiagonalDeflector(bottle);
                        ConstrainTurntableBottleAgainstInfeedGuide(bottle);
                    }
                }
            }
        }

        private void ResolveTurntableToInfeedGuideSeparation(float minDistance, float minDistanceSqr)
        {
            foreach (var turntableBottle in turntableBottles)
            {
                if (turntableBottle == null || turntableBottle.infeedState != InfeedBottleState.OnTurntable)
                {
                    continue;
                }

                var turntableRadial = TurntableRadial(turntableBottle.transform.position);
                foreach (var guideBottle in lineBottles)
                {
                    if (!IsInfeedGuideOccupant(guideBottle))
                    {
                        continue;
                    }

                    var guideRadial = TurntableRadial(guideBottle.transform.position);
                    var delta = turntableRadial - guideRadial;
                    var distanceSqr = delta.sqrMagnitude;
                    if (distanceSqr >= minDistanceSqr)
                    {
                        continue;
                    }

                    Vector2 direction;
                    float distance;
                    if (distanceSqr < 0.0001f)
                    {
                        direction = turntableRadial.sqrMagnitude > 0.0001f
                            ? -turntableRadial.normalized
                            : Vector2.down;
                        distance = 0f;
                    }
                    else
                    {
                        distance = Mathf.Sqrt(distanceSqr);
                        direction = delta / distance;
                    }

                    SetTurntableRadialPosition(turntableBottle, turntableRadial + direction * (minDistance - distance));
                    turntableBottle.turntableVelocity *= 0.75f;
                    turntableRadial = TurntableRadial(turntableBottle.transform.position);
                }
            }
        }

        private bool TryGetInfeedTurntableGuideContact(BottleProcessState bottle, Collider guide, out Vector2 guidePoint, out Vector2 guideNormal, out float distance)
        {
            guidePoint = Vector2.zero;
            guideNormal = Vector2.zero;
            distance = float.PositiveInfinity;
            if (bottle == null || guide == null)
            {
                return false;
            }

            var position = bottle.transform.position;
            var probe = new Vector3(position.x, guide.bounds.center.y, position.z);
            var closest = guide.ClosestPoint(probe);
            guidePoint = new Vector2(closest.x, closest.z);
            var delta = new Vector2(position.x - closest.x, position.z - closest.z);
            distance = delta.magnitude;
            if (distance > 0.0001f)
            {
                guideNormal = delta / distance;
            }
            else
            {
                guideNormal = Vector2.left;
            }

            return true;
        }

        private bool IsInfeedGuideOccupant(BottleProcessState bottle)
        {
            return bottle != null &&
                (bottle.infeedState == InfeedBottleState.TransitioningToInfeedGuide ||
                 bottle.infeedState == InfeedBottleState.OnInfeedGuide);
        }

        private bool ConstrainTurntableBottleAgainstInfeedGuide(BottleProcessState bottle)
        {
            if (!TryGetInfeedTurntableGuideContact(bottle, infeedTurntableTransferPlate, out var guidePoint, out var guideNormal, out var distance))
            {
                return false;
            }

            return ApplyTurntableGuideConstraint(bottle, guidePoint, guideNormal, distance);
        }

        private bool ConstrainTurntableBottleAgainstDiagonalDeflector(BottleProcessState bottle)
        {
            if (!TryGetInfeedTurntableGuideContact(bottle, infeedTurntableDiagonalDeflector, out var guidePoint, out _, out var distance))
            {
                return false;
            }

            var deflectorNormal = GetDiagonalDeflectorOutletNormal();
            return ApplyTurntableGuideConstraint(bottle, guidePoint, deflectorNormal, distance);
        }

        private Vector2 GetDiagonalDeflectorOutletNormal()
        {
            if (infeedTurntableDiagonalDeflector == null)
            {
                return Vector2.right;
            }

            var forward = infeedTurntableDiagonalDeflector.transform.forward;
            var normal = new Vector2(forward.z, -forward.x).normalized;
            var outletCenter = infeedTurntableTransferPlate != null
                ? infeedTurntableTransferPlate.bounds.center
                : turntableCenter;
            var outletDirection = new Vector2(
                outletCenter.x - infeedTurntableDiagonalDeflector.bounds.center.x,
                outletCenter.z - infeedTurntableDiagonalDeflector.bounds.center.z);
            return Vector2.Dot(normal, outletDirection) >= 0f ? normal : -normal;
        }

        private bool ApplyTurntableGuideConstraint(BottleProcessState bottle, Vector2 guidePoint, Vector2 guideNormal, float distance)
        {
            if (guideNormal.sqrMagnitude < 0.0001f)
            {
                guideNormal = Vector2.right;
            }

            var contactRadius = turntableBottleRadius;
            if (distance > contactRadius + 0.002f)
            {
                return false;
            }

            var constrainedRadial = guidePoint + guideNormal * contactRadius;
            SetTurntableRadialPosition(
                bottle,
                new Vector2(constrainedRadial.x - turntableCenter.x, constrainedRadial.y - turntableCenter.z));

            var velocityIntoGuide = Vector2.Dot(bottle.turntableVelocity, guideNormal);
            if (velocityIntoGuide < 0f)
            {
                bottle.turntableVelocity -= guideNormal * velocityIntoGuide;
            }

            return true;
        }

        private bool TryGetAvailableInfeedGuideCapture(BottleProcessState bottle)
        {
            if (!HasInfeedGuidePath)
            {
                return false;
            }

            foreach (var other in lineBottles)
            {
                if (other == null || other == bottle || !IsInfeedGuideOccupant(other) || other.fillingCompleted || fillingSlotAssignments.ContainsKey(other))
                {
                    continue;
                }

                if (GetInfeedGuideProgress(other) < InfeedGuideBottleSpacingM - 0.001f)
                {
                    return false;
                }
            }

            return true;
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
                    rejectingBottles.Contains(bottle) ||
                    packingBottles.Contains(bottle))
                {
                    continue;
                }

                if (bottle.infeedState == InfeedBottleState.TransitioningToInfeedGuide)
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
                    // Rejected bottles remain visible in the collection tray.
                    continue;
                }

                if (bottle.status == BottleQualityStatus.RejectEscaped)
                {
                    position.z += ConveyorEffectiveSpeedMps * Time.deltaTime;
                    bottle.transform.position = position;
                    if (position.z >= RejectEscapeOutfeedZ)
                    {
                        CompleteRejectEscape(bottle);
                    }

                    continue;
                }

                var onInfeedGuide = bottle.infeedState == InfeedBottleState.OnInfeedGuide;
                if (!onInfeedGuide && splitLaneAssignments.TryGetValue(bottle, out var assignedLane))
                {
                    position.x = ResolveLaneX(assignedLane, position.z);
                }
                else if (!onInfeedGuide)
                {
                    position.x = lineX;
                }

                var canUseInfeedGuide = onInfeedGuide && IsBeforeInfeedGuideEntry(GetInfeedGuideProgress(bottle));
                if (!canUseInfeedGuide && IsConveyorStopped())
                {
                    bottle.transform.position = position;
                    continue;
                }

                if (canUseInfeedGuide)
                {
                    position = MoveBottleAlongInfeedGuide(bottle);
                    if (IsBeforeInfeedGuideEntry(GetInfeedGuideProgress(bottle)) && !IsInInfeedGuideCaptureZone(GetInfeedGuideProgress(bottle)))
                    {
                        bottle.transform.position = position;
                        continue;
                    }
                }

                if (!bottle.fillingCompleted)
                {
                    if (onInfeedGuide && !fillingSlotAssignments.ContainsKey(bottle) && IsInInfeedGuideCaptureZone(GetInfeedGuideProgress(bottle)))
                    {
                        var progress = GetInfeedGuideProgress(bottle);
                        progress = IsFrontBottleOnInfeedGuide(bottle) && IsReadyForWheelCapture(progress)
                            ? InfeedGuideLength
                            : ResolveInfeedGuideSpacing(bottle, progress);
                        infeedGuideProgresses[bottle] = progress;
                        position = InfeedGuidePositionAtProgress(progress);
                        bottle.transform.position = position;
                        continue;
                    }
                }

                if (bottle.status == BottleQualityStatus.Rejected && position.z >= rejectStationZ)
                {
                    if (rejectSweepRequestedBottles.Add(bottle))
                    {
                        StartCoroutine(SweepRejectedBottleToTray(bottle));
                    }
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

                if (bottle.status == BottleQualityStatus.Rejected &&
                    !rejectingBottles.Contains(bottle) &&
                    TwinProcessMath.HasBottlePassedRejectSweepZone(
                        position.z,
                        RejectSweepStationZ,
                        RejectSweepHalfLengthM,
                        turntableBottleRadius))
                {
                    MarkRejectEscaped(bottle);
                }

                // Keep the bottle neutral through filling and capping. Its pass/fail colour appears only
                // when its centre has just reached the physical QC beam, not at a separate fixed Z value.
                if (bottle.fillingCompleted && !bottle.inspectionCompleted && position.z >= QcSensorTriggerZ)
                {
                    InspectBottle(bottle);
                }
            }
        }

        private bool HasInfeedGuidePath
        {
            get
            {
                if (infeedGuidePathPoints == null || infeedGuidePathPoints.Count < 2)
                {
                    return false;
                }

                foreach (var point in infeedGuidePathPoints)
                {
                    if (point == null)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private float InfeedGuideLength
        {
            get
            {
                if (!HasInfeedGuidePath)
                {
                    return 0f;
                }

                var length = 0f;
                for (var i = 0; i < infeedGuidePathPoints.Count - 1; i++)
                {
                    length += Vector3.Distance(infeedGuidePathPoints[i].position, infeedGuidePathPoints[i + 1].position);
                }

                return length;
            }
        }

        private Vector3 MoveBottleAlongInfeedGuide(BottleProcessState bottle)
        {
            var progress = GetInfeedGuideProgress(bottle);
            progress += ConveyorEffectiveSpeedMps * Time.deltaTime;
            progress = ResolveInfeedGuideSpacing(bottle, progress);
            infeedGuideProgresses[bottle] = progress;
            return InfeedGuidePositionAtProgress(progress);
        }

        private float ResolveInfeedGuideSpacing(BottleProcessState currentBottle, float desiredProgress)
        {
            desiredProgress = Mathf.Clamp(desiredProgress, 0f, InfeedGuideLength);
            var nearestAheadProgress = float.PositiveInfinity;
            foreach (var otherBottle in lineBottles)
            {
                if (otherBottle == null || otherBottle == currentBottle || !IsInfeedGuideOccupant(otherBottle) || otherBottle.fillingCompleted || fillingSlotAssignments.ContainsKey(otherBottle))
                {
                    continue;
                }

                var otherProgress = GetInfeedGuideProgress(otherBottle);
                if (otherProgress > desiredProgress + 0.001f && otherProgress < nearestAheadProgress)
                {
                    nearestAheadProgress = otherProgress;
                }
            }

            if (!float.IsPositiveInfinity(nearestAheadProgress))
            {
                desiredProgress = Mathf.Min(desiredProgress, nearestAheadProgress - InfeedGuideBottleSpacingM);
            }

            return Mathf.Clamp(desiredProgress, 0f, InfeedGuideLength);
        }

        private float GetInfeedGuideProgress(BottleProcessState bottle)
        {
            return bottle != null && infeedGuideProgresses.TryGetValue(bottle, out var progress)
                ? Mathf.Clamp(progress, 0f, InfeedGuideLength)
                : 0f;
        }

        private Vector3 GetInfeedGuideTangentAtProgress(float progress)
        {
            if (!HasInfeedGuidePath)
            {
                return Vector3.forward;
            }

            var remaining = Mathf.Clamp(progress, 0f, InfeedGuideLength);
            for (var i = 0; i < infeedGuidePathPoints.Count - 1; i++)
            {
                var direction = infeedGuidePathPoints[i + 1].position - infeedGuidePathPoints[i].position;
                var segmentLength = direction.magnitude;
                if (segmentLength < 0.0001f)
                {
                    continue;
                }

                if (remaining <= segmentLength || i == infeedGuidePathPoints.Count - 2)
                {
                    return direction / segmentLength;
                }

                remaining -= segmentLength;
            }

            return Vector3.forward;
        }

        private Vector3 InfeedGuidePositionAtProgress(float progress)
        {
            if (!HasInfeedGuidePath)
            {
                return turntableCenter;
            }

            var remaining = Mathf.Clamp(progress, 0f, InfeedGuideLength);
            for (var i = 0; i < infeedGuidePathPoints.Count - 1; i++)
            {
                var start = infeedGuidePathPoints[i].position;
                var end = infeedGuidePathPoints[i + 1].position;
                var segmentLength = Vector3.Distance(start, end);
                if (remaining <= segmentLength || i == infeedGuidePathPoints.Count - 2)
                {
                    var ratio = segmentLength > 0.0001f ? Mathf.Clamp01(remaining / segmentLength) : 1f;
                    return Vector3.Lerp(start, end, ratio);
                }

                remaining -= segmentLength;
            }

            return infeedGuidePathPoints[infeedGuidePathPoints.Count - 1].position;
        }

        private BottleProcessState GetFrontBottleOnInfeedGuide(bool requireCaptureZone)
        {
            BottleProcessState frontBottle = null;
            var frontProgress = float.NegativeInfinity;
            foreach (var bottle in lineBottles)
            {
                if (bottle == null || bottle.infeedState != InfeedBottleState.OnInfeedGuide || bottle.fillingCompleted || fillingSlotAssignments.ContainsKey(bottle))
                {
                    continue;
                }

                var progress = GetInfeedGuideProgress(bottle);
                if (requireCaptureZone && !IsReadyForWheelCapture(progress))
                {
                    continue;
                }

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

        private float FillingEntryZ => StarWheelSlotPosition(0).z;
        private float FillingEntryX => StarWheelSlotPosition(0).x;

        private bool IsBeforeInfeedGuideEntry(float progress)
        {
            return progress < InfeedGuideLength - 0.001f;
        }

        private bool IsInInfeedGuideCaptureZone(float progress)
        {
            return InfeedGuideLength - progress <= InfeedGuideCaptureZoneM;
        }

        private bool IsReadyForWheelCapture(float progress)
        {
            var distanceToEntry = InfeedGuideLength - progress;
            return distanceToEntry >= -0.01f && distanceToEntry <= infeedGuideWheelCaptureDistanceM;
        }

        private bool IsFrontBottleOnInfeedGuide(BottleProcessState currentBottle)
        {
            if (currentBottle == null)
            {
                return false;
            }

            var currentProgress = GetInfeedGuideProgress(currentBottle);
            foreach (var otherBottle in lineBottles)
            {
                if (otherBottle == null || otherBottle == currentBottle || !IsInfeedGuideOccupant(otherBottle) || otherBottle.fillingCompleted || fillingSlotAssignments.ContainsKey(otherBottle))
                {
                    continue;
                }

                if (GetInfeedGuideProgress(otherBottle) > currentProgress + 0.001f)
                {
                    return false;
                }
            }

            return true;
        }

        private int CountBottlesWaitingOnInfeedGuide()
        {
            var count = 0;
            foreach (var bottle in lineBottles)
            {
                if (bottle == null || bottle.infeedState != InfeedBottleState.OnInfeedGuide || bottle.fillingCompleted || fillingSlotAssignments.ContainsKey(bottle))
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        private bool CanCaptureBottleForFilling()
        {
            return !fillingStationBusy && !fillingCaptureBusy && !cappingStationBusy && !StarWheelIndexing && fillingSlotAssignments.Count < starWheelPocketCount;
        }

        private bool CanIndexStarWheel()
        {
            return !fillingStationBusy && !fillingCaptureBusy && !cappingStationBusy && !StarWheelIndexing && fillingSlotAssignments.Count > 0;
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
            var captureTimeout = Mathf.Max(starWheelLockRecoverySeconds, StarWheelIndexDurationForSlots(1) * 3f + 1f);
            if (fillingCaptureBusy && fillingCaptureBusySince > 0f && Time.time - fillingCaptureBusySince > captureTimeout)
            {
                fillingCaptureBusy = false;
                fillingCaptureBusySince = -1f;
            }

            var indexTimeout = Mathf.Max(starWheelLockRecoverySeconds, StarWheelIndexDurationForSlots(StarWheelIndexStepPockets) * 2f + 1f);
            if (StarWheelIndexing && starWheelIndexingSince > 0f && Time.time - starWheelIndexingSince > indexTimeout)
            {
                StarWheelIndexing = false;
                starWheelIndexingSince = -1f;
            }

            var fillingTimeout = Mathf.Max(
                starWheelLockRecoverySeconds,
                EffectiveFillingDwellSeconds +
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

        private void TryStartStarWheelFeedFromInfeedGuide()
        {
            if (!fillingStationBusy && !fillingCaptureBusy && !cappingStationBusy && !StarWheelIndexing)
            {
                var readyBatch = GetReadyFillingBatch();
                if (readyBatch.Count >= ActiveFillingNozzleCount)
                {
                    StartCoroutine(FillBottleBatch(readyBatch));
                    return;
                }
            }

            var frontBottle = GetFrontBottleOnInfeedGuide(true);
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
                CountBottlesWaitingOnInfeedGuide() >= StarWheelFeedBatchSize)
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
            infeedGuideProgresses.Remove(bottle);
            infeedGuideTransitions.Remove(bottle);
            fillingBottles.Add(bottle);
            fillingSlotAssignments[bottle] = 0;
            bottle.infeedState = InfeedBottleState.OnStarWheel;
            bottle.transform.position = StarWheelSlotPosition(0);
        }

        private bool ShouldHoldStarWheelForIncompleteFillingBatch(BottleProcessState frontBottle, bool hasBottleWaitingInEntryPocket)
        {
            if (fillingStationBusy || fillingCaptureBusy || cappingStationBusy || StarWheelIndexing)
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
            if (GetFrontBottleOnInfeedGuide(true) == null && !hasBottleWaitingInEntryPocket && fillingSlotAssignments.Count == 0)
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
            yield return IndexStarWheelOnePitchWithInfeedGuideFeed(indexedBottles, feedSlotDelta, true);
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
                    pocketIndex < cappingPocketStartIndex + ActiveCappingHeadCount &&
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
            if (fillingStationBusy || fillingCaptureBusy || cappingStationBusy || StarWheelIndexing)
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

                // Fill quality is purely mechanical: the pump can only reach the full target when its
                // flow and the Disc dwell provide enough volume for this bottle.
                targets[bottle] = 1f;
            }

            yield return MoveFillingNozzles(activeSprings, springBasePositions, springDownPositions, fillingNozzleMoveSeconds, batch);
            SetFillingFlowVisuals(activeNozzles, true);

            var elapsed = 0f;
            var dwellSeconds = EffectiveFillingDwellSeconds;
            while (elapsed < dwellSeconds)
            {
                var frameDuration = Mathf.Min(Time.deltaTime, dwellSeconds - elapsed);
                elapsed += frameDuration;
                var activeBottleCount = 0;
                foreach (var bottle in batch)
                {
                    if (bottle != null && targets.TryGetValue(bottle, out var targetVolume) && bottle.liquidVolume01 < targetVolume)
                    {
                        activeBottleCount++;
                    }
                }

                if (activeBottleCount > 0 && LiquidLevelLiters > 0f)
                {
                    var availableLiters = TwinProcessMath.CalculateAvailablePumpOutputLiters(
                        pumpFlowLitersPerMinute,
                        frameDuration,
                        LiquidLevelLiters);
                    var litersPerBottle = availableLiters / activeBottleCount;
                    var dispensedLiters = 0f;

                    foreach (var bottle in batch)
                    {
                        if (bottle == null || !targets.TryGetValue(bottle, out var targetVolume) || bottle.liquidVolume01 >= targetVolume)
                        {
                            continue;
                        }

                        SnapBottleToFillingSlot(bottle);
                        var remainingLiters = Mathf.Max(0f, targetVolume - bottle.liquidVolume01) * bottleCapacityLiters;
                        var bottleLiters = Mathf.Min(litersPerBottle, remainingLiters);
                        bottle.SetVolume(bottle.liquidVolume01 + bottleLiters / Mathf.Max(0.0001f, bottleCapacityLiters));
                        dispensedLiters += bottleLiters;
                    }

                    LiquidLevelLiters = Mathf.Max(0f, LiquidLevelLiters - dispensedLiters);
                }
                yield return null;
            }

            var batchFillTotal = 0f;
            var batchFillCount = 0;
            foreach (var bottle in batch)
            {
                if (bottle == null || !targets.ContainsKey(bottle))
                {
                    continue;
                }

                bottle.isDefective = bottle.liquidVolume01 < passThreshold;
                bottle.status = BottleQualityStatus.Filled;
                bottle.fillingCompleted = true;
                batchFillTotal += bottle.liquidVolume01;
                batchFillCount++;
            }

            if (batchFillCount > 0)
            {
                var batchAverage = batchFillTotal / batchFillCount;
                LastBatchFillPercent = batchAverage * 100f;
                completedFillRatioTotal += batchFillTotal;
                completedFillSamples += batchFillCount;
            }

            LastFillingTimeSeconds = elapsed;
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

            while (CountBottlesWaitingOnInfeedGuide() < StarWheelFeedBatchSize)
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
            yield return IndexStarWheelOnePitchWithInfeedGuideFeed(indexedBottles, StarWheelIndexStepPockets, true);
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

        private void InitializeDigitalTwinDefaults()
        {
            // The default flow fills one bottle per nozzle during the configured stationary Disc dwell.
            if (pumpFlowLitersPerMinute <= 0f)
            {
                pumpFlowLitersPerMinute = ActiveFillingNozzleCount * bottleCapacityLiters * 60f / EffectiveFillingDwellSeconds;
            }

            if (referenceReleaseIntervalSeconds <= 0f)
            {
                referenceReleaseIntervalSeconds = releaseIntervalSeconds;
            }

            defaultSetpoints = GetSetpoints();
            defaultsCaptured = true;

            if (pendingResetSeed.HasValue)
            {
                randomSeed = pendingResetSeed.Value;
                pendingResetSeed = null;
            }

            if (pendingResetSetpoints != null)
            {
                ApplySetpoints(pendingResetSetpoints);
                pendingResetSetpoints = null;
            }

            Random.InitState(randomSeed);
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

        private IEnumerator IndexStarWheelOnePitchWithInfeedGuideFeed(Dictionary<BottleProcessState, int> indexedBottles, int slotDelta, bool allowInfeedGuideCapture)
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

            if (allowInfeedGuideCapture)
            {
                TryCaptureBottleFromInfeedGuideIntoPassingPocket(indexedBottles, capturedSteps, 0, slotDelta);
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var ratio = Mathf.Clamp01(elapsed / duration);
                if (allowInfeedGuideCapture)
                {
                    var passedStep = Mathf.Min(slotDelta - 1, Mathf.FloorToInt(ratio * slotDelta));
                    for (var step = 0; step <= passedStep; step++)
                    {
                        TryCaptureBottleFromInfeedGuideIntoPassingPocket(indexedBottles, capturedSteps, step, slotDelta);
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

        private void TryCaptureBottleFromInfeedGuideIntoPassingPocket(Dictionary<BottleProcessState, int> indexedBottles, HashSet<int> capturedSteps, int captureStep, int slotDelta)
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

            var bottle = GetFrontBottleOnInfeedGuide(true);
            if (bottle == null)
            {
                return;
            }

            lineBottles.Remove(bottle);
            infeedGuideProgresses.Remove(bottle);
            infeedGuideTransitions.Remove(bottle);
            fillingBottles.Add(bottle);
            bottle.infeedState = InfeedBottleState.OnStarWheel;
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
            return TwinProcessMath.CalculateStarWheelIndexDurationSeconds(
                starWheelPocketCount,
                slotDelta,
                starWheelIndexSpeedRpm);
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
            var tangentDuration = Mathf.Max(0.08f, StarWheelIndexDurationForSlots(1) * 0.35f);
            while (elapsed < tangentDuration)
            {
                elapsed += Time.deltaTime;
                var ratio = Mathf.SmoothStep(0f, 1f, elapsed / tangentDuration);
                bottle.transform.position = Vector3.Lerp(tangentStart, tangentEnd, ratio);
                yield return null;
            }

            bottle.transform.position = tangentEnd;
            bottle.infeedState = InfeedBottleState.None;
            infeedGuideProgresses.Remove(bottle);
            infeedGuideTransitions.Remove(bottle);
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

            CompleteCapping(bottle);

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
                    CompleteCapping(bottle);
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

                CompleteCapping(bottle);
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

        private void CompleteCapping(BottleProcessState bottle)
        {
            if (bottle == null)
            {
                return;
            }

            bottle.capPlaced = true;
            bottle.cappingCompleted = true;

            // Capping is a mechanical operation, not a quality result. Keep the original neutral bottle
            // colour until QC has evaluated the fill level at the QC Sensor Beam.
            if (!bottle.inspectionCompleted)
            {
                bottle.status = BottleQualityStatus.Filled;
            }
            else if (bottle.liquidVolume01 >= passThreshold)
            {
                bottle.status = BottleQualityStatus.Capped;
            }

            bottle.RefreshVisuals();
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

        private IEnumerator SweepRejectedBottleToTray(BottleProcessState bottle)
        {
            if (bottle == null)
            {
                yield break;
            }

            while (rejectSweepActive || rejectedTrayDischargeActive)
            {
                yield return null;
            }

            // The trigger bottle continues on the conveyor. If it has already cleared the physical
            // sweep zone, this request becomes a genuine missed reject rather than teleporting it back.
            if (bottle.status != BottleQualityStatus.Rejected)
            {
                rejectSweepRequestedBottles.Remove(bottle);
                yield break;
            }

            rejectSweepActive = true;
            var basePosition = rejectSweepBar != null ? rejectSweepBar.localPosition : Vector3.zero;
            var traySlot = GetNextRejectedTrayPosition(starWheelCenter.y);
            var extendedPosition = new Vector3(traySlot.x + 0.11f, basePosition.y, basePosition.z);

            yield return MoveRejectSweepBarAndCollect(basePosition, extendedPosition, 0.22f);
            yield return DepositSweptBottles();
            yield return MoveRejectSweepBarAndCollect(extendedPosition, basePosition, 0.22f);
            yield return DepositSweptBottles();

            rejectSweepActive = false;
            rejectSweepRequestedBottles.Remove(bottle);
        }

        private Bounds GetRejectSweepBounds()
        {
            if (rejectSweepBar != null)
            {
                var collider = rejectSweepBar.GetComponent<Collider>();
                if (collider != null)
                {
                    return collider.bounds;
                }

                var renderer = rejectSweepBar.GetComponent<Renderer>();
                if (renderer != null)
                {
                    return renderer.bounds;
                }

                return new Bounds(rejectSweepBar.position, Vector3.Scale(rejectSweepBar.lossyScale, new Vector3(0.07f, 0.30f, 0.42f)));
            }

            return new Bounds(new Vector3(lineX, starWheelCenter.y, rejectStationZ), new Vector3(0.07f, 0.30f, 0.42f));
        }

        private void CollectBottlesHitByRejectSweepBar()
        {
            var sweepBounds = GetRejectSweepBounds();
            foreach (var candidate in new List<BottleProcessState>(lineBottles))
            {
                if (candidate == null ||
                    rejectingBottles.Contains(candidate) ||
                    candidate.status == BottleQualityStatus.RejectedBin ||
                    candidate.status == BottleQualityStatus.RejectEscaped ||
                    !TwinProcessMath.IsBottleInsideRejectSweepBounds(candidate.transform.position, turntableBottleRadius, sweepBounds))
                {
                    continue;
                }

                rejectingBottles.Add(candidate);
                rejectSweepRequestedBottles.Remove(candidate);
                candidate.status = BottleQualityStatus.Rejected;
                candidate.RefreshVisuals();
                sweepCapturedBottles.Add(candidate);
            }
        }

        private IEnumerator DepositSweptBottles()
        {
            while (sweepCapturedBottles.Count > 0)
            {
                var bottle = sweepCapturedBottles[0];
                sweepCapturedBottles.RemoveAt(0);
                if (bottle == null)
                {
                    continue;
                }

                if (rejectedTrayBottles.Count >= Mathf.Max(1, rejectedTrayCapacity))
                {
                    yield return DischargeRejectedTray();
                }

                var start = bottle.transform.position;
                var destination = GetNextRejectedTrayPosition(start.y);
                yield return MoveBottleToRejectTray(bottle, start, destination, 0.12f);

                bottle.status = BottleQualityStatus.RejectedBin;
                bottle.RefreshVisuals();
                CountBottle(bottle, false);
                rejectedTrayBottles.Add(bottle);
                rejectingBottles.Remove(bottle);

                if (rejectedTrayBottles.Count >= Mathf.Max(1, rejectedTrayCapacity))
                {
                    yield return new WaitForSeconds(rejectedTrayDischargeDelaySeconds);
                    yield return DischargeRejectedTray();
                }
            }
        }

        private Vector3 GetNextRejectedTrayPosition(float bottleCenterY)
        {
            var trayCenter = rejectedBottleTray != null
                ? rejectedBottleTray.position
                : new Vector3(-0.69f, 0.50f, rejectStationZ);
            var slot = Mathf.Min(rejectedTrayBottles.Count, Mathf.Max(1, rejectedTrayCapacity) - 1);
            var column = slot % 2;
            var row = slot / 2;
            return new Vector3(
                trayCenter.x + (0.5f - column) * 0.34f,
                bottleCenterY,
                trayCenter.z + (0.5f - row) * 0.22f);
        }

        private IEnumerator DischargeRejectedTray()
        {
            if (rejectedTrayDischargeActive || rejectedTrayBottles.Count < Mathf.Max(1, rejectedTrayCapacity))
            {
                yield break;
            }

            rejectedTrayDischargeActive = true;
            var batch = new List<BottleProcessState>(rejectedTrayBottles);
            var bottleStarts = new List<Vector3>();
            foreach (var bottle in batch)
            {
                bottleStarts.Add(bottle != null ? bottle.transform.position : Vector3.zero);
            }

            var trayStart = rejectedBottleTray != null ? rejectedBottleTray.position : Vector3.zero;
            var trayTarget = trayStart + rejectedTrayDischargeOffset;
            var elapsed = 0f;
            var duration = Mathf.Max(0.05f, rejectedTrayDischargeSeconds);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var ratio = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                if (rejectedBottleTray != null)
                {
                    rejectedBottleTray.position = Vector3.Lerp(trayStart, trayTarget, ratio);
                }

                for (var i = 0; i < batch.Count; i++)
                {
                    if (batch[i] != null)
                    {
                        batch[i].transform.position = Vector3.Lerp(bottleStarts[i], bottleStarts[i] + rejectedTrayDischargeOffset, ratio);
                    }
                }

                yield return null;
            }

            foreach (var bottle in batch)
            {
                if (bottle != null)
                {
                    bottle.gameObject.SetActive(false);
                }
            }

            rejectedTrayBottles.Clear();
            if (rejectedBottleTray != null)
            {
                elapsed = 0f;
                var returnDuration = Mathf.Max(0.05f, rejectedTrayReturnSeconds);
                while (elapsed < returnDuration)
                {
                    elapsed += Time.deltaTime;
                    var ratio = Mathf.SmoothStep(0f, 1f, elapsed / returnDuration);
                    rejectedBottleTray.position = Vector3.Lerp(trayTarget, trayStart, ratio);
                    yield return null;
                }

                rejectedBottleTray.position = trayStart;
            }

            rejectedTrayDischargeActive = false;
        }

        private IEnumerator MoveBottleToRejectTray(BottleProcessState bottle, Vector3 from, Vector3 to, float duration)
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
                bottle.transform.position = Vector3.Lerp(from, to, ratio);
                yield return null;
            }

            bottle.transform.position = to;
        }

        private IEnumerator MoveRejectSweepBarAndCollect(Vector3 from, Vector3 to, float duration)
        {
            if (rejectSweepBar == null)
            {
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                rejectSweepBar.localPosition = Vector3.Lerp(from, to, elapsed / duration);
                CollectBottlesHitByRejectSweepBar();
                yield return null;
            }

            rejectSweepBar.localPosition = to;
            CollectBottlesHitByRejectSweepBar();
        }

        private void MarkRejectEscaped(BottleProcessState bottle)
        {
            if (bottle == null || !escapedRejectBottles.Add(bottle))
            {
                return;
            }

            rejectSweepRequestedBottles.Remove(bottle);
            bottle.status = BottleQualityStatus.RejectEscaped;
            bottle.RefreshVisuals();
            TotalRejectEscapes++;
        }

        private void CompleteRejectEscape(BottleProcessState bottle)
        {
            if (bottle == null || !escapedRejectBottles.Remove(bottle))
            {
                return;
            }

            lineBottles.Remove(bottle);
            splitLaneAssignments.Remove(bottle);
            splitGuidePassedBottles.Remove(bottle);
            packLaneABottles.Remove(bottle);
            packLaneBBottles.Remove(bottle);
            packingBottles.Remove(bottle);
            completedCount++;
            bottle.gameObject.SetActive(false);
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
