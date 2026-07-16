using System.Collections.Generic;
using UnityEngine;

namespace ConveyorTwin
{
    [ExecuteAlways]
    public class ConveyorDemoRuntimeBootstrap : MonoBehaviour
    {
        private const string GeneratedRootName = "Generated Filling Filtering Twin Demo";
        private const float FillingStarWheelCenterX = 0.72f;
        private const float FillingLineZ = -0.903f;
        private static readonly Vector3 FillingStarWheelVisualCenter = new Vector3(FillingStarWheelCenterX, 0f, FillingLineZ);
        // Only X/Z are shared by the pocket helpers. Bottle Y is calculated from BottleVerticalLayout.
        private static readonly Vector3 FillingStarWheelBottleCenter = new Vector3(FillingStarWheelCenterX, 0f, FillingLineZ);
        private const float MainConveyorStartZ = -2.431457f;
        private const float SplitSensorZ = 3.58f;
        private const float SplitGuideZ = 4.47733736f;
        private const float SplitGuideExitZ = 5.05f;
        private const float LaneBCenterX = 0.62f;
        private const float MainConveyorEndZ = 7.65f;
        private const float PackFrontRowZ = 7.12f;
        private const float PackRowPitch = 0.235f;
        private const float PackGateZ = PackFrontRowZ - PackRowPitch * 2f - 0.24f;
        private const float PackGateSensorZ = PackGateZ + 0.16f;
        private static readonly Vector3 PackCartonCenter = new Vector3(1.56f, 0.58f, PackFrontRowZ - PackRowPitch);
        private static readonly Vector3 InfeedTurntableBottleCenter = new Vector3(-3.253f, 1.05f, FillingLineZ);
        private const int FillingStarWheelPocketCount = 10;
        private const float StarWheelStepAngleDegrees = 360f / FillingStarWheelPocketCount;
        private const float FillingStarWheelPocketNotchRadius = 0.075f;
        private const float FillingStarWheelOuterRadius = 0.8f;
        private const float StarWheelContinuousBarrierRadius = FillingStarWheelOuterRadius + 0.045f;
        private const float FillingNozzleMainRailRadius = StarWheelContinuousBarrierRadius + 0.1f;
        private const float FillingStarWheelBottleRadius = FillingStarWheelOuterRadius - FillingStarWheelPocketNotchRadius;
        private const float FillingStarWheelEntryAngleDegrees = 180f;
        private const int FillingStationStartPocketIndex = 1;
        private static readonly int[] FillingNozzlePocketOrder = { 1, 2, 3 };
        private const int CapDropPocketIndex = 5;
        private const int CappingPocketStartIndex = 7;
        private static readonly Vector3 CapMagazineTubePosition = new Vector3(1.44500005f, 2.09599996f, -1.30999994f);
        private static readonly Vector3 CapMagazineTubeEulerAngles = new Vector3(321.961578f, 0f, 0f);
        private static readonly Vector3 CapMagazineAssemblyLocalOffset = new Vector3(0f, 0.106515377f, -0.136654712f);
        private static readonly Vector3 CapMagazineOutletCapLocalEulerAngles = new Vector3(316.305176f, 183.988342f, 176.259995f);
        private const float CapMagazineCapPitch = 0.11f;
        private const float CapMagazineCapBottomLocalY = -0.63f;
        private const float CapMagazineGuideHalfLength = 0.55f;
        private const float CapMagazineGuideCurveDepth = 0.025f;
        private const float FillingNozzleScaleY = 0.32f;
        private const float FillingNozzleClusterLift = FillingNozzleScaleY * 2f;
        private const float FillingFirstZ = -1.2f;
        private const float CappingFirstZ = 1.65f;
        private const float CappingPitch = 0.42f;
        private const float InfeedRightRailExtendNegativeXM = 0.13f;
        // The end nearest the star wheel has the greater X value and is the fixed reference point.
        // Both values are world-space rail-centre heights.
        private const float InfeedRailPositiveXEndY = 1.393f;
        private const float InfeedRailNegativeXEndY = 1.53f;

        [Header("Flexible bottle height")]
        [Tooltip("Vertical scale applied to the bottle. 0.85 makes the bottle 15% shorter while keeping its base on the supporting surface. Rebuild the demo after changing it.")]
        [Range(0.80f, 1.00f)] public float bottleHeightScale = 0.85f;
        public bool rebuildOnEnable = true;

        private struct BottleVerticalLayout
        {
            private const float ConveyorBottleBaseY = 0.50f;
            private const float TurntableBottleBaseY = 0.79f;
            private const float FullScaleBodyHalfHeight = 0.42f;
            private const float FullScaleNeckCenterOffsetY = 0.48f;
            private const float FullScaleNeckHalfHeight = 0.13f;
            private const float FullScaleCapCenterOffsetY = 0.66f;
            private const float FullScaleCapHalfHeight = 0.045f;
            private const float FullScaleDiscY = 1.485f;
            private const float FullScaleInfeedRailStartY = 1.64f;
            private const float FullScaleInfeedRailEndY = 1.41f;
            private const float FullScaleExitGuideY = 1.342f;
            private const float HubSupportBottomY = 0.08f;
            private const float FullScaleBottleSpawnY = 2.68f;
            private const float FullScaleFillingVesselY = 2.45f + ConveyorDemoRuntimeBootstrap.FillingNozzleClusterLift;
            private const float FullScaleFillingMainRailY = 1.45f + ConveyorDemoRuntimeBootstrap.FillingNozzleClusterLift;
            private const float FullScaleFillingSpringY = 1.34f + ConveyorDemoRuntimeBootstrap.FillingNozzleClusterLift;
            private const float FullScaleFillingNozzleY = 1.08f + ConveyorDemoRuntimeBootstrap.FillingNozzleClusterLift;
            private const float FullScaleCapMagazineAssemblyY = 2.09599996f;
            private const float FullScaleCapMagazineOutletY = 1.68f;
            private const float FullScaleCapTightenerToolY = 1.86f;
            private const float FullScaleCapTightenerMotorY = 1.88f;

            public BottleVerticalLayout(float heightScale)
            {
                HeightScale = heightScale;
            }

            public float HeightScale { get; }
            public float BodyHalfHeight => FullScaleBodyHalfHeight * HeightScale;
            public float NeckCenterOffsetY => FullScaleNeckCenterOffsetY * HeightScale;
            public float NeckHalfHeight => FullScaleNeckHalfHeight * HeightScale;
            public float CapCenterOffsetY => FullScaleCapCenterOffsetY * HeightScale;
            public float CapHalfHeight => FullScaleCapHalfHeight * HeightScale;
            public float LiquidBottomOffsetY => -0.32f * HeightScale;
            public float ConveyorBottleCenterY => ConveyorBottleBaseY + BodyHalfHeight;
            public float TurntableBottleCenterY => TurntableBottleBaseY + BodyHalfHeight;
            public float NeckTopY => ConveyorBottleCenterY + NeckCenterOffsetY + NeckHalfHeight;
            public float BottleTopY => ConveyorBottleCenterY + CapCenterOffsetY + CapHalfHeight;
            public float BottleHeightM => BottleTopY - ConveyorBottleBaseY;
            public float StarWheelDiscY => ScaleFromConveyorSurface(FullScaleDiscY);
            public float StarWheelHubSupportCenterY => (HubSupportBottomY + StarWheelDiscY) * 0.5f;
            public float StarWheelHubSupportHalfHeight => (StarWheelDiscY - HubSupportBottomY) * 0.5f;
            public float InfeedRailStartY => ScaleFromTurntableSurface(FullScaleInfeedRailStartY);
            public float InfeedRailEndY => ScaleFromConveyorSurface(FullScaleInfeedRailEndY);
            public float StarWheelExitGuideY => ScaleFromConveyorSurface(FullScaleExitGuideY);
            public float TurntableRimY => TurntableBottleCenterY - 0.20f;
            public float TurntableOutletY => TurntableBottleCenterY - 0.16f;
            public float BottleSpawnY => TurntableBottleCenterY + (FullScaleBottleSpawnY - FullScaleTurntableBottleCenterY);
            public float FillingToolYOffset => NeckTopY - FullScaleNeckTopY;
            public float CappingToolYOffset => BottleTopY - FullScaleBottleTopY;
            public float FillingVesselY => FullScaleFillingVesselY + FillingToolYOffset;
            public float FillingMainRailY => FullScaleFillingMainRailY + FillingToolYOffset;
            public float FillingSpringY => FullScaleFillingSpringY + FillingToolYOffset;
            public float FillingNozzleY => FullScaleFillingNozzleY + FillingToolYOffset;
            public float CapMagazineAssemblyY => FullScaleCapMagazineAssemblyY + CappingToolYOffset;
            public float CapMagazineOutletY => FullScaleCapMagazineOutletY + CappingToolYOffset;
            public float CapTightenerToolY => FullScaleCapTightenerToolY + CappingToolYOffset;
            public float CapTightenerMotorY => FullScaleCapTightenerMotorY + CappingToolYOffset;
            public float AirBlowerYOffset => InfeedRailStartY - FullScaleInfeedRailStartY;

            private float ScaleFromConveyorSurface(float fullScaleY)
            {
                return ConveyorBottleBaseY + (fullScaleY - ConveyorBottleBaseY) * HeightScale;
            }

            private float ScaleFromTurntableSurface(float fullScaleY)
            {
                return TurntableBottleBaseY + (fullScaleY - TurntableBottleBaseY) * HeightScale;
            }

            private static float FullScaleTurntableBottleCenterY => TurntableBottleBaseY + FullScaleBodyHalfHeight;
            private static float FullScaleNeckTopY => ConveyorBottleBaseY + FullScaleBodyHalfHeight + FullScaleNeckCenterOffsetY + FullScaleNeckHalfHeight;
            private static float FullScaleBottleTopY => ConveyorBottleBaseY + FullScaleBodyHalfHeight + FullScaleCapCenterOffsetY + FullScaleCapHalfHeight;
        }

        private void OnEnable()
        {
            if (rebuildOnEnable)
            {
                BuildDemo();
            }
        }

        [ContextMenu("Rebuild Filling & Filtering Demo")]
        public void BuildDemo()
        {
            var bottleLayout = GetBottleVerticalLayout();
            ClearExistingDemo();

            var root = new GameObject(GeneratedRootName);
            root.transform.SetParent(transform);

            var floorMaterial = CreateMaterial(new Color(0.42f, 0.42f, 0.40f));
            var beltMaterial = CreateMaterial(new Color(0.035f, 0.04f, 0.045f));
            var metalMaterial = CreateMaterial(new Color(0.55f, 0.57f, 0.58f));
            var slatMaterial = CreateMaterial(new Color(0.78f, 0.72f, 0.62f));
            var ribMaterial = CreateMaterial(new Color(0.42f, 0.42f, 0.38f));
            var starWheelMaterial = CreateMaterial(new Color(0.86f, 0.86f, 0.82f));
            var bottleMaterial = CreateMaterial(new Color(0.82f, 0.95f, 1f, 0.35f));
            var waterMaterial = CreateMaterial(new Color(0.1f, 0.55f, 1f, 0.85f));
            var capMaterial = CreateMaterial(new Color(0.02f, 0.35f, 0.95f));
            var labelMaterial = CreateMaterial(new Color(0.02f, 0.28f, 0.75f));
            var capTubeMaterial = CreateMaterial(new Color(0.65f, 0.9f, 1f, 0.28f));
            var sensorMaterial = CreateMaterial(new Color(0.1f, 0.75f, 1f));
            var rejectMaterial = CreateMaterial(new Color(1f, 0.35f, 0.22f));
            var cartonMaterial = CreateMaterial(new Color(0.62f, 0.42f, 0.22f));

            CreateFloor(root.transform, floorMaterial);
            var infeedNeckSupportRailLeft = CreateConveyor(root.transform, beltMaterial, metalMaterial, slatMaterial, ribMaterial, sensorMaterial, bottleLayout);
            var turntableParts = CreateTurntable(root.transform, metalMaterial, bottleLayout);
            var turntable = turntableParts.turntable;
            var bottleSpawnPoint = CreateBottleDropper(root.transform, bottleLayout);
            var turntableOutlet = CreateTurntableOutlet(root.transform, bottleLayout);
            var vesselParts = CreateLiquidVessel(root.transform, metalMaterial, waterMaterial, bottleLayout);
            var nozzleParts = CreateFillingNozzles(root.transform, metalMaterial, waterMaterial, bottleLayout);
            Transform fillingStopGate = null;
            // CreateFillingStopGate(root.transform, metalMaterial, rejectMaterial);
            var starWheel = CreateFillingStarWheel(root.transform, starWheelMaterial, metalMaterial, beltMaterial, bottleLayout);
            var qcBeam = CreateQcSensor(root.transform, sensorMaterial, metalMaterial);
            var cappingStation = CreateCappingStation(root.transform, metalMaterial, capMaterial, capTubeMaterial, sensorMaterial, bottleLayout);
            var pusher = CreatePusher(root.transform, metalMaterial, rejectMaterial);
            var packingStation = CreateSplitterAndPackingStation(root.transform, metalMaterial, sensorMaterial, cartonMaterial);
            var bottleTemplate = CreateBottleTemplate(root.transform, bottleMaterial, waterMaterial, capMaterial, labelMaterial, bottleLayout);

            var processObject = new GameObject("Filling Filtering Process Controller");
            processObject.transform.SetParent(root.transform);
            var process = processObject.AddComponent<FillingFilteringDigitalTwin>();
            process.infeedTurntable = turntable;
            process.bottleSpawnPoint = bottleSpawnPoint;
            process.turntableOutlet = turntableOutlet;
            process.fillingNozzle = nozzleParts.nozzles.Count > 0 ? nozzleParts.nozzles[0] : null;
            process.fillingNozzles = nozzleParts.nozzles;
            process.fillingNozzleSprings = nozzleParts.springs;
            process.fillingStopGate = fillingStopGate;
            process.fillingStarWheel = starWheel;
            process.liquidVessel = vesselParts.vessel;
            process.vesselLiquidVisual = vesselParts.liquid;
            process.qcSensorBeam = qcBeam;
            process.cappingHead = cappingStation.heads.Count > 0 ? cappingStation.heads[0] : null;
            process.cappingHeads = cappingStation.heads;
            process.capDropper = cappingStation.dropper;
            process.capSensorBeam = cappingStation.sensor;
            process.capMagazineCaps = cappingStation.magazineCaps;
            process.pneumaticPusher = pusher;
            process.splitSensorBeam = packingStation.sensor;
            process.splitGuidePivot = packingStation.guidePivot;
            process.packCarton = packingStation.carton;
            process.packPusher = packingStation.pusher;
            process.packStopGateA = packingStation.stopGateA;
            process.packStopGateB = packingStation.stopGateB;
            process.packGateSensorA = packingStation.gateSensorA;
            process.packGateSensorB = packingStation.gateSensorB;
            process.infeedNeckSupportRailLeft = infeedNeckSupportRailLeft;
            process.bottleTemplate = bottleTemplate;
            process.conveyorSpeedMps = 0.85f;
            process.slatPitchM = 0.22f;
            process.conveyorSlipRatio = 0f;
            process.minimumBottleSpacingM = 0.32f;
            process.fillingNozzleCount = 3;
            process.fillingFirstZ = FillingFirstZ;
            process.fillingQueueStopZ = -1.34f;
            process.qcZ = 0.65f;
            process.pusherZ = 1.15f;
            process.cappingZ = CappingFirstZ;
            process.cappingHeadCount = 3;
            process.cappingFirstZ = CappingFirstZ;
            process.cappingPitchM = CappingPitch;
            process.cappingQueueStopZ = 1.45f;
            process.capDropZ = 1.36f;
            process.capTightenZ = 1.78f;
            process.capperMoveSeconds = 0.14f;
            process.capperStrokeM = 0.2f;
            process.cappingTimeSeconds = 0.18f;
            process.cappingSpeedMultiplier = 10f;
            process.splitSensorZ = SplitSensorZ;
            process.splitGuideZ = SplitGuideZ;
            process.splitGuideExitZ = SplitGuideExitZ;
            process.laneBCenterX = LaneBCenterX;
            process.splitGuideMoveSeconds = 0.08f;
            process.packFrontRowZ = PackFrontRowZ;
            process.packRowPitchM = PackRowPitch;
            process.packGateZ = PackGateZ;
            process.packGateSensorZ = PackGateSensorZ;
            process.packCartonLoadPosition = PackCartonCenter;
            process.packCartonExitPosition = PackCartonCenter + new Vector3(1.15f, 0f, 0f);
            process.starWheelPocketCount = FillingStarWheelPocketCount;
            var starWheelBottleCenter = new Vector3(FillingStarWheelCenterX, bottleLayout.ConveyorBottleCenterY, FillingLineZ);
            process.starWheelCenter = starWheelBottleCenter;
            process.starWheelPocketRadius = FillingStarWheelBottleRadius;
            process.starWheelEntryAngleDegrees = FillingStarWheelEntryAngleDegrees;
            process.fillingStationStartPocketIndex = FillingStationStartPocketIndex;
            process.starWheelIndexStepPockets = 3;
            process.capDropPocketIndex = CapDropPocketIndex;
            process.cappingPocketStartIndex = CappingPocketStartIndex;
            process.starWheelIndexDurationSeconds = 0.9f;
            process.starWheelContinuousSpeedRpm = 7.5f;
            process.infeedMotorSpeedRpm = 18f;
            process.fillingTimeSeconds = 1.35f;
            process.properFillProbability = 0.9f;
            process.passThreshold = 0.95f;
            // Keep each bottle base on its supporting surface while the bottle height changes.
            process.turntableCenter = new Vector3(InfeedTurntableBottleCenter.x, bottleLayout.TurntableBottleCenterY, InfeedTurntableBottleCenter.z);
            process.turntableRadius = 0.95f;
            process.turntableBottleRadius = 0.11f;
            process.releaseThreshold = 7;
            process.initialTurntableBottleCount = 12;
            process.maxTurntableBuffer = 16;
            process.spawnIntervalSeconds = 0.85f;
            process.releaseIntervalSeconds = 0.62f;
            process.neckRailStartX = InfeedTurntableBottleCenter.x + 0.65f;
            process.neckRailEndX = StarWheelPocketPosition(0, starWheelBottleCenter.y).x;
            process.neckRailZ = starWheelBottleCenter.z;
            process.neckRailStartZ = process.neckRailZ;
            process.neckRailEndZ = process.neckRailZ;
            // Keep the bottle centre a constant distance below the two neck rails.  This makes
            // the bottle trajectory parallel to the rail slope while preserving the fixed
            // positive-X handoff height at the star wheel.
            var railToBottleCenterOffsetY = InfeedRailPositiveXEndY - starWheelBottleCenter.y;
            process.neckRailStartBottleY = InfeedRailNegativeXEndY - railToBottleCenterOffsetY;
            process.neckRailEndBottleY = starWheelBottleCenter.y;
            process.airBlowerWindSpeedMps = 0.8f;

            foreach (var conveyorAnimator in root.GetComponentsInChildren<SlatChainConveyorAnimator>())
            {
                conveyorAnimator.speedSource = process;
            }

            CreateHud(root.transform, process);
            ConfigureCameraAndLight();
        }

        private BottleVerticalLayout GetBottleVerticalLayout()
        {
            var heightScale = Mathf.Max(0.1f, bottleHeightScale);
            if (heightScale < 0.80f || heightScale > 1.00f)
            {
                Debug.LogWarning(
                    $"Bottle height scale {heightScale:0.00} is outside the verified 0.80-1.00 range. Recheck the rail-to-wheel handoff, filling nozzle clearance, and capping clearance.",
                    this);
            }

            return new BottleVerticalLayout(heightScale);
        }

        private void ClearExistingDemo()
        {
            DestroyIfExists(GeneratedRootName);
            DestroyIfExists("Generated Conveyor Twin Demo");
            DestroyIfExists("Conveyor Twin Demo");
            DestroyIfExists("ConveyorBelt");
            DestroyIfExists("TelemetrySource");
            DestroyIfExists("HUD");
        }

        private void DestroyIfExists(string objectName)
        {
            var existing = GameObject.Find(objectName);
            if (existing == null || existing == gameObject)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(existing);
            }
            else
            {
                DestroyImmediate(existing);
            }
        }

        private Material CreateMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (color.a < 0.99f)
            {
                if (material.HasProperty("_Surface"))
                {
                    material.SetFloat("_Surface", 1f);
                }

                if (material.HasProperty("_Blend"))
                {
                    material.SetFloat("_Blend", 0f);
                }

                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            return material;
        }

        private void CreateFloor(Transform parent, Material material)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Factory Floor";
            floor.transform.SetParent(parent);
            floor.transform.position = new Vector3(-1.06f, -0.05f, 2.32f);
            floor.transform.localScale = new Vector3(8f, 0.1f, 11.4f);
            floor.GetComponent<Renderer>().sharedMaterial = material;
        }

        private Collider CreateConveyor(Transform parent, Material beltMaterial, Material metalMaterial, Material slatMaterial, Material ribMaterial, Material sensorMaterial, BottleVerticalLayout bottleLayout)
        {
            CreateSlatConveyorLane(parent, "A", 0f, MainConveyorStartZ, MainConveyorEndZ, beltMaterial, metalMaterial, slatMaterial, ribMaterial);
            CreateSlatConveyorLane(parent, "B", LaneBCenterX, SplitGuideZ, MainConveyorEndZ, beltMaterial, metalMaterial, slatMaterial, ribMaterial);

            const float pusherGapEndZ = 1.61f;
            CreateGuideRailSegment(parent, "Left Narrow Guide Rail After Pusher Gap", -0.28f, pusherGapEndZ, PackGateZ, metalMaterial);
            CreateGuideRailSegment(parent, "Right Narrow Guide Rail Before Split", 0.28f, pusherGapEndZ, SplitGuideZ - 0.12f, metalMaterial);
            CreateGuideRailSegment(parent, "Right Narrow Guide Rail After Split", 0.28f, SplitGuideExitZ, PackGateZ, metalMaterial);
            CreateGuideRailSegment(parent, "B Right Narrow Guide Rail", LaneBCenterX + 0.28f, SplitGuideZ, PackGateZ, metalMaterial);
            CreateGuideRailSegment(parent, "B Left Narrow Guide Rail After Split", LaneBCenterX - 0.28f, SplitGuideExitZ, PackGateZ, metalMaterial);

            var infeedNeckSupportRailLeft = CreateHorizontalNeckSupportRail(
                parent,
                "Infeed Neck Support Rail",
                InfeedTurntableBottleCenter.x + 0.65f,
                StarWheelPocketPosition(0, bottleLayout.ConveyorBottleCenterY).x,
                FillingStarWheelBottleCenter.z,
                InfeedRailNegativeXEndY,
                InfeedRailPositiveXEndY,
                metalMaterial,
                true,
                false);
            CreateAirBlower(parent, metalMaterial, sensorMaterial, bottleLayout);
            // Outfeed neck support rail is disabled while the star wheel exit rail is being redesigned.
            return infeedNeckSupportRailLeft;
        }

        private void CreateSlatConveyorLane(Transform parent, string laneName, float centerX, float conveyorStartZ, float conveyorEndZ, Material beltMaterial, Material metalMaterial, Material slatMaterial, Material ribMaterial)
        {
            const float pitch = 0.22f;
            const float slatLength = 0.17f;
            var conveyorLength = conveyorEndZ - conveyorStartZ;
            var conveyorCenterZ = (conveyorStartZ + conveyorEndZ) * 0.5f;
            CreateCube(parent, $"{laneName} Slat Chain Conveyor Base", new Vector3(centerX, 0.38f, conveyorCenterZ), new Vector3(0.52f, 0.08f, conveyorLength), beltMaterial);
            CreateCube(parent, $"{laneName} Narrow Conveyor Support", new Vector3(centerX, 0.2f, conveyorCenterZ), new Vector3(0.68f, 0.15f, conveyorLength + 0.2f), metalMaterial);

            var slatCount = Mathf.CeilToInt(conveyorLength / pitch);
            var movingParts = new List<Transform>();
            for (var i = 0; i < slatCount; i++)
            {
                var z = conveyorStartZ + i * pitch;
                movingParts.Add(CreateCube(parent, $"{laneName} Modular Slat Plate", new Vector3(centerX, 0.46f, z), new Vector3(0.46f, 0.035f, slatLength), slatMaterial).transform);
                movingParts.Add(CreateCube(parent, $"{laneName} Slat Gap Shadow", new Vector3(centerX, 0.482f, z + slatLength * 0.5f + 0.017f), new Vector3(0.47f, 0.012f, 0.028f), beltMaterial).transform);

                if (i % 2 == 0)
                {
                    movingParts.Add(CreateCube(parent, $"{laneName} Anti Slip Cross Rib", new Vector3(centerX, 0.515f, z - 0.055f), new Vector3(0.42f, 0.026f, 0.022f), ribMaterial).transform);
                }
            }

            CreateConveyorAnimator(parent, $"{laneName} Slat Chain Motion", movingParts, Vector3.forward, 0.85f, conveyorStartZ - pitch, conveyorStartZ + slatCount * pitch);
        }

        private void CreateGuideRailSegment(Transform parent, string name, float x, float startZ, float endZ, Material material)
        {
            var length = Mathf.Max(0.05f, endZ - startZ);
            var centerZ = (startZ + endZ) * 0.5f;
            CreateCube(parent, name, new Vector3(x, 0.74f, centerZ), new Vector3(0.035f, 0.1f, length), material);

            var inset = Mathf.Min(0.14f, length * 0.25f);
            CreateGuideRailBrace(parent, $"{name} Start Brace", x, startZ + inset, material);
            CreateGuideRailBrace(parent, $"{name} End Brace", x, endZ - inset, material);
        }

        private void CreateGuideRailBrace(Transform parent, string name, float railX, float z, Material material)
        {
            const float supportTopY = 0.29f;
            const float railBottomY = 0.70f;
            CreateCube(parent, name, new Vector3(railX, (supportTopY + railBottomY) * 0.5f, z), new Vector3(0.045f, railBottomY - supportTopY, 0.045f), material);
        }

        private SlatChainConveyorAnimator CreateConveyorAnimator(Transform parent, string name, List<Transform> movingParts, Vector3 worldAxis, float speedMps, float minCoordinate, float maxCoordinate)
        {
            var animatorObject = new GameObject(name);
            animatorObject.transform.SetParent(parent);
            var animator = animatorObject.AddComponent<SlatChainConveyorAnimator>();
            animator.movingParts = movingParts;
            animator.worldAxis = worldAxis;
            animator.speedMps = speedMps;
            animator.minCoordinate = minCoordinate;
            animator.maxCoordinate = maxCoordinate;
            return animator;
        }

        private Collider CreateHorizontalNeckSupportRail(Transform parent, string namePrefix, float negativeEndX, float positiveEndX, float z, float negativeEndY, float positiveEndY, Material material, bool shortenRightRail = false, bool createVerticalSupports = true)
        {
            // The left rail remains the infeed-capture collider, but is offset toward the neck to create
            // the requested visibly clamped guide at Z = -0.852 m on the filling line.
            const float leftRailZOffset = 0.051f;
            const float rightRailZOffset = 0.055f;
            const float railWidth = 0.026f;
            const float railHeight = 0.035f;
            const float shortRightStartOffset = 0.42f;
            const float rightSupportRailOverlap = 0.003f;
            var horizontalLength = Mathf.Abs(positiveEndX - negativeEndX);
            var rise = positiveEndY - negativeEndY;
            var length = Mathf.Sqrt(horizontalLength * horizontalLength + rise * rise);
            var centerX = (negativeEndX + positiveEndX) * 0.5f;
            var centerY = (negativeEndY + positiveEndY) * 0.5f;
            var railDirection = Mathf.Sign(positiveEndX - negativeEndX);
            if (Mathf.Approximately(railDirection, 0f))
            {
                railDirection = 1f;
            }

            // Rotate the cube's long (local Z) axis directly onto the X/Y rail vector.  Euler
            // rotation around Z after a 90-degree yaw leaves the long axis horizontal, so it
            // cannot represent this incline reliably.
            var leftRailDirection = new Vector3(positiveEndX - negativeEndX, rise, 0f);
            var leftRailRotation = Quaternion.FromToRotation(Vector3.forward, leftRailDirection.normalized);
            var leftRail = CreateCube(parent, $"{namePrefix} Left", new Vector3(centerX, centerY, z + leftRailZOffset), new Vector3(railWidth, railHeight, length), material);
            leftRail.transform.rotation = leftRailRotation;

            var rightStartX = shortenRightRail
                ? negativeEndX + railDirection * shortRightStartOffset - InfeedRightRailExtendNegativeXM
                : negativeEndX;
            var rightStartRatio = Mathf.InverseLerp(negativeEndX, positiveEndX, rightStartX);
            var rightStartY = Mathf.Lerp(negativeEndY, positiveEndY, rightStartRatio);
            var rightHorizontalLength = Mathf.Abs(rightStartX - positiveEndX);
            var rightRise = positiveEndY - rightStartY;
            var rightLength = Mathf.Sqrt(rightHorizontalLength * rightHorizontalLength + rightRise * rightRise);
            var rightCenterX = (rightStartX + positiveEndX) * 0.5f;
            var rightCenterY = (rightStartY + positiveEndY) * 0.5f;
            var rightRailDirection = new Vector3(positiveEndX - rightStartX, rightRise, 0f);
            var rightRailRotation = Quaternion.FromToRotation(Vector3.forward, rightRailDirection.normalized);
            var rightRail = CreateCube(parent, $"{namePrefix} Right", new Vector3(rightCenterX, rightCenterY, z - rightRailZOffset), new Vector3(railWidth, railHeight, rightLength), material);
            rightRail.transform.rotation = rightRailRotation;

            var basePlate = CreateCube(parent, $"{namePrefix} Base", new Vector3(centerX, 0.20f, z), new Vector3(0.56f, 0.10f, horizontalLength + 0.22f), material);
            basePlate.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            if (!createVerticalSupports)
            {
                return leftRail.GetComponent<Collider>();
            }

            const float postSpacing = 0.8f;
            var postCount = Mathf.Max(2, Mathf.CeilToInt(horizontalLength / postSpacing) + 1);
            for (var i = 0; i < postCount; i++)
            {
                var ratio = postCount == 1 ? 0f : i / (float)(postCount - 1);
                var x = Mathf.Lerp(negativeEndX, positiveEndX, ratio);
                var railY = Mathf.Lerp(negativeEndY, positiveEndY, ratio);
                var postCenterY = (0.48f + railY) * 0.5f;
                var postHeight = Mathf.Max(0.1f, railY - 0.48f);
                var isEndPost = i == 0 || i == postCount - 1;
                if (!isEndPost)
                {
                    CreateCube(parent, $"{namePrefix} Left Support", new Vector3(x, postCenterY, z + leftRailZOffset), new Vector3(0.035f, postHeight, 0.035f), material);
                }

                if (i != postCount - 1 && (!shortenRightRail || ratio >= rightStartRatio))
                {
                    var rightSupportTopY = railY - railHeight * 0.5f + rightSupportRailOverlap;
                    var rightSupportCenterY = (0.48f + rightSupportTopY) * 0.5f;
                    var rightSupportHeight = Mathf.Max(0.1f, rightSupportTopY - 0.48f);
                    CreateCube(parent, $"{namePrefix} Right Support", new Vector3(x, rightSupportCenterY, z - rightRailZOffset), new Vector3(0.035f, rightSupportHeight, 0.035f), material);
                }
            }

            return leftRail.GetComponent<Collider>();
        }

        private void CreateNeckSupportRail(Transform parent, string namePrefix, float startZ, float endZ, float startY, float endY, Material material, bool shortenRightRail = false)
        {
            const float railHalfGap = 0.105f;
            const float railWidth = 0.026f;
            const float railHeight = 0.035f;
            var horizontalLength = endZ - startZ;
            var rise = endY - startY;
            var length = Mathf.Sqrt(horizontalLength * horizontalLength + rise * rise);
            var centerZ = (startZ + endZ) * 0.5f;
            var centerY = (startY + endY) * 0.5f;
            var pitchDegrees = -Mathf.Atan2(rise, horizontalLength) * Mathf.Rad2Deg;
            const float shortRightStartOffset = 0.42f;

            var leftRail = CreateCube(parent, $"{namePrefix} Left", new Vector3(-railHalfGap, centerY, centerZ), new Vector3(railWidth, railHeight, length), material);
            leftRail.transform.rotation = Quaternion.Euler(pitchDegrees, 0f, 0f);

            if (shortenRightRail)
            {
                var shortStartZ = startZ + shortRightStartOffset;
                var shortStartRatio = Mathf.InverseLerp(startZ, endZ, shortStartZ);
                var shortStartY = Mathf.Lerp(startY, endY, shortStartRatio);
                var shortHorizontalLength = endZ - shortStartZ;
                var shortRise = endY - shortStartY;
                var shortLength = Mathf.Sqrt(shortHorizontalLength * shortHorizontalLength + shortRise * shortRise);
                var shortCenterZ = (shortStartZ + endZ) * 0.5f;
                var shortCenterY = (shortStartY + endY) * 0.5f;
                var rightRail = CreateCube(parent, $"{namePrefix} Right", new Vector3(railHalfGap, shortCenterY, shortCenterZ), new Vector3(railWidth, railHeight, shortLength), material);
                rightRail.transform.rotation = Quaternion.Euler(pitchDegrees, 0f, 0f);
            }
            else
            {
                var rightRail = CreateCube(parent, $"{namePrefix} Right", new Vector3(railHalfGap, centerY, centerZ), new Vector3(railWidth, railHeight, length), material);
                rightRail.transform.rotation = Quaternion.Euler(pitchDegrees, 0f, 0f);
            }

            const float postSpacing = 0.8f;
            var postCount = Mathf.Max(2, Mathf.CeilToInt(horizontalLength / postSpacing) + 1);
            for (var i = 0; i < postCount; i++)
            {
                var ratio = postCount == 1 ? 0f : i / (float)(postCount - 1);
                var z = Mathf.Lerp(startZ, endZ, ratio);
                var railY = Mathf.Lerp(startY, endY, ratio);
                var postCenterY = (0.48f + railY) * 0.5f;
                var postHeight = Mathf.Max(0.1f, railY - 0.48f);
                CreateCube(parent, $"{namePrefix} Left Support", new Vector3(-0.18f, postCenterY, z), new Vector3(0.035f, postHeight, 0.035f), material);
                if (!shortenRightRail || z >= startZ + shortRightStartOffset)
                {
                    CreateCube(parent, $"{namePrefix} Right Support", new Vector3(0.18f, postCenterY, z), new Vector3(0.035f, postHeight, 0.035f), material);
                }
            }
        }

        private void CreateAirBlower(Transform parent, Material metalMaterial, Material airMaterial, BottleVerticalLayout bottleLayout)
        {
            const float armLengthM = 0.48f;
            const float armThicknessM = 0.06f;
            var nozzlePosition = new Vector3(-2.93400002f, 1.99999995f + bottleLayout.AirBlowerYOffset, -0.89200002f);
            var armPosition = new Vector3(-2.94499993f, nozzlePosition.y - armThicknessM * 0.5f, -0.65200001f);
            var standHeightM = armPosition.y - armThicknessM * 0.5f;
            var standPosition = new Vector3(-2.10599995f, standHeightM * 0.5f, -0.41200003f);

            // The stand rises from the floor to the arm; the arm reaches the nozzle body from behind.
            CreateCube(parent, "Infeed Air Blower Stand", standPosition, new Vector3(0.07f, standHeightM, 0.07f), metalMaterial);
            CreateCube(parent, "Infeed Air Blower Arm", armPosition, new Vector3(0.07f, armThicknessM, armLengthM), metalMaterial);

            var armRearEnd = armPosition + Vector3.forward * (armLengthM * 0.5f);
            var standTop = standPosition + Vector3.up * (standHeightM * 0.5f);
            var connectorDirection = standTop - armRearEnd;
            var connector = CreateCube(
                parent,
                "Infeed Air Blower Arm Stand Connector",
                Vector3.Lerp(armRearEnd, standTop, 0.5f),
                new Vector3(0.07f, 0.07f, connectorDirection.magnitude),
                metalMaterial);
            connector.transform.rotation = Quaternion.FromToRotation(Vector3.forward, connectorDirection.normalized);

            var blower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            blower.name = "Infeed Air Blower Nozzle";
            blower.transform.SetParent(parent);
            blower.transform.position = nozzlePosition;
            var jetDirection = Quaternion.Euler(0f, 0f, -15f) * Vector3.right;
            blower.transform.rotation = Quaternion.FromToRotation(Vector3.up, jetDirection);
            blower.transform.localScale = new Vector3(0.12f, 0.18f, 0.12f);
            blower.GetComponent<Renderer>().sharedMaterial = metalMaterial;

            const float jetLengthM = 0.18f;
            const float jetSpacingM = 0.026f;
            for (var i = 0; i < 4; i++)
            {
                var lateralOffset = Vector3.forward * ((i - 1.5f) * jetSpacingM);
                var jetCenter = nozzlePosition + lateralOffset + jetDirection * (jetLengthM * 0.5f + 0.04f);
                var gust = CreateCube(parent, $"Infeed Air Jet {i + 1}", jetCenter, new Vector3(jetLengthM, 0.012f, 0.012f), airMaterial);
                gust.transform.rotation = Quaternion.Euler(0f, 0f, -15f);
            }
        }

        private (Transform turntable, Transform outletGate) CreateTurntable(Transform parent, Material material, BottleVerticalLayout bottleLayout)
        {
            return CreateTurntableVisual(parent, material, InfeedTurntableBottleCenter, "Infeed", false, bottleLayout.TurntableRimY);
        }

        private (Transform turntable, Transform outletGate) CreateTurntableVisual(Transform parent, Material material, Vector3 bottleCenter, string namePrefix, bool createOutletGate, float rimY)
        {
            var table = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            table.name = $"{namePrefix} Turntable";
            table.transform.SetParent(parent);
            table.transform.position = new Vector3(bottleCenter.x, bottleCenter.y - 0.34f, bottleCenter.z);
            table.transform.localScale = new Vector3(1.9f, 0.08f, 1.9f);
            table.GetComponent<Renderer>().sharedMaterial = material;

            const int segments = 120;
            const float rimRadius = 1.08f;
            const float outletGapStartDegrees = 350f;
            const float outletGapEndDegrees = 10f;
            var segmentWidth = (2f * Mathf.PI * rimRadius / segments) * 1.12f;
            for (var i = 0; i < segments; i++)
            {
                var angleDegrees = i * 360f / segments;
                if (angleDegrees >= outletGapStartDegrees || angleDegrees <= outletGapEndDegrees)
                {
                    continue;
                }

                var angleRad = angleDegrees * Mathf.Deg2Rad;
                var position = new Vector3(
                    bottleCenter.x + Mathf.Cos(angleRad) * rimRadius,
                    rimY,
                    bottleCenter.z + Mathf.Sin(angleRad) * rimRadius);
                var rim = CreateCube(parent, $"{namePrefix} Turntable Safety Rim", position, new Vector3(segmentWidth, 0.34f, 0.04f), material);
                rim.transform.rotation = Quaternion.Euler(0f, -angleDegrees + 90f, 0f);
            }

            Transform outletGate = null;
            if (createOutletGate)
            {
                outletGate = CreateCube(parent, $"{namePrefix} Turntable Outlet Gate", new Vector3(bottleCenter.x + 1.18f, rimY, bottleCenter.z), new Vector3(0.08f, 0.26f, 0.55f), material).transform;
            }

            return (table.transform, outletGate);
        }

        private Transform CreateBottleDropper(Transform parent, BottleVerticalLayout bottleLayout)
        {
            var spawn = new GameObject("Bottle Spawn Point");
            spawn.transform.SetParent(parent);
            spawn.transform.position = new Vector3(InfeedTurntableBottleCenter.x, bottleLayout.BottleSpawnY, InfeedTurntableBottleCenter.z);
            return spawn.transform;
        }

        private Transform CreateTurntableOutlet(Transform parent, BottleVerticalLayout bottleLayout)
        {
            var outlet = new GameObject("Turntable Outlet");
            outlet.transform.SetParent(parent);
            outlet.transform.position = new Vector3(InfeedTurntableBottleCenter.x + 0.95f, bottleLayout.TurntableOutletY, InfeedTurntableBottleCenter.z);
            return outlet.transform;
        }

        private (Transform vessel, Transform liquid) CreateLiquidVessel(Transform parent, Material metalMaterial, Material waterMaterial, BottleVerticalLayout bottleLayout)
        {
            var vessel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            vessel.name = "Liquid Vessel";
            vessel.transform.SetParent(parent);
            vessel.transform.position = new Vector3(FillingStarWheelCenterX + 0.33f, bottleLayout.FillingVesselY, -0.72f);
            vessel.transform.localScale = new Vector3(0.55f, 0.95f, 0.55f);
            vessel.GetComponent<Renderer>().sharedMaterial = metalMaterial;

            var liquid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            liquid.name = "Liquid Level Visual";
            liquid.transform.SetParent(vessel.transform);
            liquid.transform.localPosition = new Vector3(0f, -0.08f, 0f);
            liquid.transform.localScale = new Vector3(0.82f, 0.75f, 0.82f);
            liquid.GetComponent<Renderer>().sharedMaterial = waterMaterial;

            return (vessel.transform, liquid.transform);
        }

        private static float FillingPocketPitch()
        {
            return Mathf.PI * 2f * FillingStarWheelBottleRadius / FillingStarWheelPocketCount;
        }

        private static float StarWheelPocketAngleDegrees(int pocketIndex)
        {
            return FillingStarWheelEntryAngleDegrees + pocketIndex * StarWheelStepAngleDegrees;
        }

        private static float FillingSlotAngleDegrees(int slotIndex)
        {
            return StarWheelPocketAngleDegrees(FillingStationStartPocketIndex + slotIndex);
        }

        private static Vector3 FillingSlotPosition(int slotIndex, float y)
        {
            return StarWheelPocketPosition(FillingStationStartPocketIndex + slotIndex, y);
        }

        private static Vector3 StarWheelPocketPosition(int pocketIndex, float y)
        {
            var angle = StarWheelPocketAngleDegrees(pocketIndex) * Mathf.Deg2Rad;
            return new Vector3(
                FillingStarWheelBottleCenter.x + Mathf.Cos(angle) * FillingStarWheelBottleRadius,
                y,
                FillingStarWheelBottleCenter.z + Mathf.Sin(angle) * FillingStarWheelBottleRadius);
        }

        private static Vector3 CappingSlotPosition(int slotIndex, float y)
        {
            return new Vector3(0f, y, CappingFirstZ + slotIndex * CappingPitch);
        }

        private (List<Transform> nozzles, List<Transform> springs) CreateFillingNozzles(Transform parent, Material metalMaterial, Material waterMaterial, BottleVerticalLayout bottleLayout)
        {
            var nozzles = new List<Transform>();
            var springs = new List<Transform>();
            var mainRailDiameter = FillingNozzleMainRailRadius * 2f;
            CreateCube(
                parent,
                "Filling Nozzle Main Rail",
                new Vector3(FillingStarWheelCenterX, bottleLayout.FillingMainRailY, FillingLineZ),
                new Vector3(mainRailDiameter, 0.12f, mainRailDiameter),
                metalMaterial);

            const int nozzleCount = 3;
            for (var i = 0; i < nozzleCount; i++)
            {
                var stationPosition = StarWheelPocketPosition(FillingNozzlePocketOrder[i], 0f);
                var nozzleAssembly = new GameObject($"Filling Nozzle Assembly {i + 1}");
                nozzleAssembly.transform.SetParent(parent);
                nozzleAssembly.transform.position = Vector3.zero;

                var spring = CreateCube(nozzleAssembly.transform, $"Nozzle Spring {i + 1}", new Vector3(stationPosition.x, bottleLayout.FillingSpringY, stationPosition.z), new Vector3(0.08f, 0.18f, 0.08f), metalMaterial);

                var nozzle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                nozzle.name = $"Filling Nozzle {i + 1}";
                nozzle.transform.SetParent(nozzleAssembly.transform);
                nozzle.transform.position = new Vector3(stationPosition.x, bottleLayout.FillingNozzleY, stationPosition.z);
                nozzle.transform.localScale = new Vector3(0.07f, FillingNozzleScaleY, 0.07f);
                nozzle.GetComponent<Renderer>().sharedMaterial = metalMaterial;

                var flow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                flow.name = $"Liquid Flow Visual {i + 1}";
                flow.transform.SetParent(nozzle.transform);
                flow.transform.localPosition = new Vector3(0f, -0.7f, 0f);
                flow.transform.localScale = new Vector3(0.22f, 0.55f, 0.22f);
                flow.GetComponent<Renderer>().sharedMaterial = waterMaterial;
                flow.SetActive(false);

                nozzles.Add(nozzle.transform);
                springs.Add(spring.transform);
            }

            return (nozzles, springs);
        }

        private Transform CreateFillingStopGate(Transform parent, Material metalMaterial, Material gateMaterial)
        {
            CreateCube(parent, "Filling Gate Frame Left", new Vector3(-0.35f, 0.96f, -1.72f), new Vector3(0.05f, 0.6f, 0.05f), metalMaterial);
            CreateCube(parent, "Filling Gate Frame Right", new Vector3(0.35f, 0.96f, -1.72f), new Vector3(0.05f, 0.6f, 0.05f), metalMaterial);
            return CreateCube(parent, "Filling Stop Gate", new Vector3(0f, 1.1f, -1.72f), new Vector3(0.58f, 0.16f, 0.06f), gateMaterial).transform;
        }

        private Transform CreateQcSensor(Transform parent, Material sensorMaterial, Material metalMaterial)
        {
            CreateCube(parent, "QC Sensor Head Left", new Vector3(-0.46f, 0.95f, 0.4f), new Vector3(0.16f, 0.28f, 0.16f), metalMaterial);
            CreateCube(parent, "QC Sensor Head Right", new Vector3(0.46f, 0.95f, 0.4f), new Vector3(0.16f, 0.28f, 0.16f), metalMaterial);
            CreateFloorSupportLeg(parent, "QC Sensor Head Left Floor Support", new Vector3(-0.46f, 0.81f, 0.4f), metalMaterial);
            CreateFloorSupportLeg(parent, "QC Sensor Head Right Floor Support", new Vector3(0.46f, 0.81f, 0.4f), metalMaterial);
            return CreateCube(parent, "QC Sensor Beam", new Vector3(0f, 0.92f, 0.4f), new Vector3(0.86f, 0.035f, 0.035f), sensorMaterial).transform;
        }

        private Transform CreatePusher(Transform parent, Material metalMaterial, Material rejectMaterial)
        {
            CreateCube(parent, "Pneumatic Cylinder Body", new Vector3(0.72f, 0.78f, 0.85f), new Vector3(0.36f, 0.22f, 0.22f), metalMaterial);
            CreateFloorSupportLeg(parent, "Pneumatic Cylinder Left Floor Support", new Vector3(0.72f, 0.67f, 0.76f), metalMaterial);
            CreateFloorSupportLeg(parent, "Pneumatic Cylinder Right Floor Support", new Vector3(0.72f, 0.67f, 0.94f), metalMaterial);
            return CreateCube(parent, "Pneumatic Pusher", new Vector3(0.43f, 0.78f, 0.85f), new Vector3(0.1f, 0.32f, 0.42f), rejectMaterial).transform;
        }

        private (Transform sensor, Transform guidePivot, Transform carton, Transform pusher, Transform stopGateA, Transform stopGateB, Transform gateSensorA, Transform gateSensorB) CreateSplitterAndPackingStation(Transform parent, Material metalMaterial, Material sensorMaterial, Material cartonMaterial)
        {
            var sensor = CreateCube(parent, "Split Counting Sensor", new Vector3(0f, 0.92f, SplitSensorZ), new Vector3(0.86f, 0.035f, 0.035f), sensorMaterial).transform;
            CreateCube(parent, "Split Sensor Head Left", new Vector3(-0.46f, 0.96f, SplitSensorZ), new Vector3(0.14f, 0.24f, 0.14f), metalMaterial);
            CreateCube(parent, "Split Sensor Head Right", new Vector3(0.46f, 0.96f, SplitSensorZ), new Vector3(0.14f, 0.24f, 0.14f), metalMaterial);
            CreateFloorSupportLeg(parent, "Split Sensor Head Left Floor Support", new Vector3(-0.46f, 0.84f, SplitSensorZ), metalMaterial);
            CreateFloorSupportLeg(parent, "Split Sensor Head Right Floor Support", new Vector3(0.46f, 0.84f, SplitSensorZ), metalMaterial);

            var pivotObject = new GameObject("A/B Split Guide Pivot");
            pivotObject.transform.SetParent(parent);
            pivotObject.transform.position = new Vector3(-0.28f, 0.74f, SplitGuideZ);
            var guide = CreateCube(pivotObject.transform, "A/B Split Guide", pivotObject.transform.position + new Vector3(0f, 0.02f, 0.32f), new Vector3(0.04f, 0.11f, 0.64f), metalMaterial).transform;
            guide.localPosition = new Vector3(0f, 0.02f, 0.32f);
            CreateCube(parent, "A/B Split Guide Pneumatic Cylinder", new Vector3(-0.43f, 0.77f, SplitGuideZ - 0.16f), new Vector3(0.12f, 0.18f, 0.34f), metalMaterial);
            CreateFloorSupportLeg(parent, "A/B Split Guide Floor Support", new Vector3(-0.28f, 0.69f, SplitGuideZ), metalMaterial);

            var packCenterZ = PackCartonCenter.z;
            CreateCube(parent, "A Pack Zone Backstop", new Vector3(0f, 0.74f, PackFrontRowZ + 0.16f), new Vector3(0.52f, 0.12f, 0.05f), metalMaterial);
            CreateCube(parent, "B Pack Zone Backstop", new Vector3(LaneBCenterX, 0.74f, PackFrontRowZ + 0.16f), new Vector3(0.52f, 0.12f, 0.05f), metalMaterial);

            var gateSensorA = CreateCube(parent, "A Pack Stop Gate Sensor Beam", new Vector3(0f, 0.94f, PackGateSensorZ), new Vector3(0.52f, 0.035f, 0.035f), sensorMaterial).transform;
            var gateSensorB = CreateCube(parent, "B Pack Stop Gate Sensor Beam", new Vector3(LaneBCenterX, 0.94f, PackGateSensorZ), new Vector3(0.52f, 0.035f, 0.035f), sensorMaterial).transform;
            CreateCube(parent, "A Pack Stop Gate Sensor Outer Head", new Vector3(-0.37f, 0.96f, PackGateSensorZ), new Vector3(0.12f, 0.22f, 0.12f), metalMaterial);
            CreateCube(parent, "B Pack Stop Gate Sensor Outer Head", new Vector3(LaneBCenterX + 0.37f, 0.96f, PackGateSensorZ), new Vector3(0.12f, 0.22f, 0.12f), metalMaterial);
            CreateFloorSupportLeg(parent, "A Pack Stop Gate Sensor Support", new Vector3(-0.37f, 0.84f, PackGateSensorZ), metalMaterial);
            CreateFloorSupportLeg(parent, "B Pack Stop Gate Sensor Support", new Vector3(LaneBCenterX + 0.37f, 0.84f, PackGateSensorZ), metalMaterial);

            var stopGateAObject = new GameObject("A Pack Stop Gate");
            stopGateAObject.transform.SetParent(parent);
            stopGateAObject.transform.position = new Vector3(-0.28f, 0.82f, PackGateZ);
            var stopGateA = stopGateAObject.transform;
            var stopGateAArm = CreateCube(stopGateA, "A Pack Stop Gate Arm", stopGateA.position + Vector3.up * 0.27f, new Vector3(0.055f, 0.54f, 0.055f), metalMaterial).transform;
            stopGateAArm.localPosition = new Vector3(0f, 0.27f, 0f);
            var stopGateBObject = new GameObject("B Pack Stop Gate");
            stopGateBObject.transform.SetParent(parent);
            stopGateBObject.transform.position = new Vector3(LaneBCenterX + 0.28f, 0.82f, PackGateZ);
            var stopGateB = stopGateBObject.transform;
            var stopGateBArm = CreateCube(stopGateB, "B Pack Stop Gate Arm", stopGateB.position + Vector3.up * 0.27f, new Vector3(0.055f, 0.54f, 0.055f), metalMaterial).transform;
            stopGateBArm.localPosition = new Vector3(0f, 0.27f, 0f);
            CreateCube(parent, "A Pack Stop Gate Outer Actuator", new Vector3(-0.36f, 0.63f, PackGateZ), new Vector3(0.10f, 0.14f, 0.22f), metalMaterial);
            CreateCube(parent, "B Pack Stop Gate Outer Actuator", new Vector3(LaneBCenterX + 0.36f, 0.63f, PackGateZ), new Vector3(0.10f, 0.14f, 0.22f), metalMaterial);

            var carton = CreateCartonBox(parent, "Active Six-Pack Carton", PackCartonCenter, new Vector2(0.96f, 0.82f), cartonMaterial);
            var pusher = CreateCube(parent, "Six-Pack Carton Pusher", new Vector3(-0.24f, 0.78f, packCenterZ), new Vector3(0.12f, 0.42f, 0.84f), metalMaterial).transform;

            return (sensor, pivotObject.transform, carton, pusher, stopGateA, stopGateB, gateSensorA, gateSensorB);
        }

        private Transform CreateCartonBox(Transform parent, string name, Vector3 center, Vector2 footprint, Material material)
        {
            var cartonRoot = new GameObject(name);
            cartonRoot.transform.SetParent(parent);
            cartonRoot.transform.position = center;
            var halfX = footprint.x * 0.5f;
            var halfZ = footprint.y * 0.5f;
            CreateCube(cartonRoot.transform, "Carton Bottom", center + new Vector3(0f, -0.23f, 0f), new Vector3(footprint.x, 0.06f, footprint.y), material);
            CreateCube(cartonRoot.transform, "Carton Front Wall", center + new Vector3(0f, 0f, -halfZ), new Vector3(footprint.x, 0.46f, 0.05f), material);
            CreateCube(cartonRoot.transform, "Carton Back Wall", center + new Vector3(0f, 0f, halfZ), new Vector3(footprint.x, 0.46f, 0.05f), material);
            CreateCube(cartonRoot.transform, "Carton Right Wall", center + new Vector3(halfX, 0f, 0f), new Vector3(0.05f, 0.46f, footprint.y), material);
            return cartonRoot.transform;
        }

        private BottleProcessState CreateBottleTemplate(Transform parent, Material bottleMaterial, Material waterMaterial, Material capMaterial, Material labelMaterial, BottleVerticalLayout bottleLayout)
        {
            var bottleRoot = new GameObject("Bottle Template");
            bottleRoot.transform.SetParent(parent);
            bottleRoot.transform.position = new Vector3(0f, bottleLayout.ConveyorBottleCenterY, -4.7f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Bottle Body";
            body.transform.SetParent(bottleRoot.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.14f, bottleLayout.BodyHalfHeight, 0.14f);
            body.GetComponent<Renderer>().sharedMaterial = bottleMaterial;

            var neck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            neck.name = "Bottle Neck";
            neck.transform.SetParent(bottleRoot.transform);
            neck.transform.localPosition = new Vector3(0f, bottleLayout.NeckCenterOffsetY, 0f);
            neck.transform.localScale = new Vector3(0.065f, bottleLayout.NeckHalfHeight, 0.065f);
            neck.GetComponent<Renderer>().sharedMaterial = bottleMaterial;

            var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cap.name = "Bottle Cap";
            cap.transform.SetParent(bottleRoot.transform);
            cap.transform.localPosition = new Vector3(0f, bottleLayout.CapCenterOffsetY, 0f);
            cap.transform.localScale = new Vector3(0.075f, bottleLayout.CapHalfHeight, 0.075f);
            cap.GetComponent<Renderer>().sharedMaterial = capMaterial;
            cap.SetActive(false);

            var liquid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            liquid.name = "Bottle Liquid";
            liquid.transform.SetParent(bottleRoot.transform);
            liquid.transform.localPosition = new Vector3(0f, bottleLayout.LiquidBottomOffsetY, 0f);
            liquid.transform.localScale = new Vector3(0.105f, 0.02f * bottleLayout.HeightScale, 0.105f);
            liquid.GetComponent<Renderer>().sharedMaterial = waterMaterial;

            var state = bottleRoot.AddComponent<BottleProcessState>();
            state.bottleRenderer = body.GetComponent<Renderer>();
            state.liquidRenderer = liquid.GetComponent<Renderer>();
            state.liquidVisual = liquid.transform;
            state.capVisual = cap.transform;
            state.capRenderer = cap.GetComponent<Renderer>();
            state.liquidVerticalScale = bottleLayout.HeightScale;
            state.SetVolume(0f);
            bottleRoot.SetActive(false);
            return state;
        }

        private void CreateHud(Transform parent, FillingFilteringDigitalTwin process)
        {
            var hudObject = new GameObject("HUD - Filling Filtering Metrics");
            hudObject.transform.SetParent(parent);
            var hud = hudObject.AddComponent<FillingFilteringHud>();
            hud.process = process;
            hud.position = new Vector2(16f, 16f);
            hud.size = new Vector2(620f, 560f);
        }

        private Mesh CreateTaperedCylinderMesh(int segments, float bottomRadius, float topRadius, float height)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var halfHeight = height * 0.5f;

            for (var i = 0; i < segments; i++)
            {
                var angle = i * Mathf.PI * 2f / segments;
                var cos = Mathf.Cos(angle);
                var sin = Mathf.Sin(angle);
                vertices.Add(new Vector3(cos * bottomRadius, -halfHeight, sin * bottomRadius));
                vertices.Add(new Vector3(cos * topRadius, halfHeight, sin * topRadius));
            }

            vertices.Add(new Vector3(0f, -halfHeight, 0f));
            vertices.Add(new Vector3(0f, halfHeight, 0f));
            var bottomCenterIndex = segments * 2;
            var topCenterIndex = bottomCenterIndex + 1;

            for (var i = 0; i < segments; i++)
            {
                var next = (i + 1) % segments;
                var bottom = i * 2;
                var top = bottom + 1;
                var nextBottom = next * 2;
                var nextTop = nextBottom + 1;

                triangles.Add(bottom);
                triangles.Add(top);
                triangles.Add(nextTop);
                triangles.Add(bottom);
                triangles.Add(nextTop);
                triangles.Add(nextBottom);

                triangles.Add(bottomCenterIndex);
                triangles.Add(nextBottom);
                triangles.Add(bottom);

                triangles.Add(topCenterIndex);
                triangles.Add(top);
                triangles.Add(nextTop);
            }

            var mesh = new Mesh
            {
                name = "Bottle Tapered Shoulder Mesh",
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray()
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private Transform CreateFillingStarWheel(Transform parent, Material wheelMaterial, Material metalMaterial, Material pocketMaterial, BottleVerticalLayout bottleLayout)
        {
            var starWheelRoot = new GameObject("Filling Star Wheel");
            starWheelRoot.transform.SetParent(parent);
            starWheelRoot.transform.position = FillingStarWheelVisualCenter;

            var rotatingAssembly = new GameObject("Scalloped Star Wheel Rotating Assembly");
            rotatingAssembly.transform.SetParent(starWheelRoot.transform);
            rotatingAssembly.transform.localPosition = new Vector3(0f, bottleLayout.StarWheelDiscY, 0f);

            var disc = new GameObject("Scalloped Star Wheel Disc");
            disc.transform.SetParent(rotatingAssembly.transform);
            disc.transform.localPosition = Vector3.zero;
            var meshFilter = disc.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateScallopedStarWheelMesh(FillingStarWheelPocketCount, FillingStarWheelOuterRadius, FillingStarWheelPocketNotchRadius, 0.08f, 18, FillingStarWheelEntryAngleDegrees);
            disc.AddComponent<MeshRenderer>().sharedMaterial = wheelMaterial;

            var hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hub.name = "Star Wheel Center Hub";
            hub.transform.SetParent(rotatingAssembly.transform);
            hub.transform.localPosition = new Vector3(0f, 0.055f, 0f);
            hub.transform.localScale = new Vector3(0.22f, 0.055f, 0.22f);
            hub.GetComponent<Renderer>().sharedMaterial = metalMaterial;

            var hubSupport = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hubSupport.name = "Star Wheel Center Hub Support";
            hubSupport.transform.SetParent(starWheelRoot.transform);
            hubSupport.transform.position = new Vector3(FillingStarWheelCenterX, bottleLayout.StarWheelHubSupportCenterY, FillingLineZ);
            hubSupport.transform.localScale = new Vector3(0.16f, bottleLayout.StarWheelHubSupportHalfHeight, 0.16f);
            hubSupport.GetComponent<Renderer>().sharedMaterial = metalMaterial;

            var hubSupportFoot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hubSupportFoot.name = "Star Wheel Center Hub Support Foot";
            hubSupportFoot.transform.SetParent(starWheelRoot.transform);
            hubSupportFoot.transform.position = new Vector3(FillingStarWheelCenterX, 0.08f, FillingLineZ);
            hubSupportFoot.transform.localScale = new Vector3(0.34f, 0.04f, 0.34f);
            hubSupportFoot.GetComponent<Renderer>().sharedMaterial = metalMaterial;

            CreateFixedStarWheelBarrier(parent, metalMaterial, bottleLayout);

            CreateStarWheelOutfeedReleaseGuide(parent, metalMaterial, bottleLayout);
            CreateCube(parent, "Filling Star Wheel Base", new Vector3(FillingStarWheelCenterX, 0.31f, -0.68f), new Vector3(1.7617f, 0.16f, 1.7f), metalMaterial);
            return rotatingAssembly.transform;
        }

        private void CreateStarWheelOutfeedReleaseGuide(Transform parent, Material metalMaterial, BottleVerticalLayout bottleLayout)
        {
            var exitPoint = StarWheelPocketPosition(FillingStarWheelPocketCount - 1, bottleLayout.ConveyorBottleCenterY);
            var conveyorPoint = new Vector3(0f, bottleLayout.ConveyorBottleCenterY, exitPoint.z + 0.22f);
            var length = Vector3.Distance(exitPoint, conveyorPoint);
            var yaw = Mathf.Atan2(conveyorPoint.x - exitPoint.x, conveyorPoint.z - exitPoint.z) * Mathf.Rad2Deg;

            var guidePosition = new Vector3(0.009f, bottleLayout.StarWheelExitGuideY, -0.436f);
            var guideRail = CreateCube(parent, "Star Wheel Exit Guide Rail", guidePosition, new Vector3(0.035f, 0.08f, length + 0.2f), metalMaterial);
            guideRail.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var supportStart = new Vector3(FillingStarWheelCenterX, guidePosition.y, FillingLineZ);
            var supportVector = guidePosition - supportStart;
            var supportLength = supportVector.magnitude;
            var supportArm = CreateCube(parent, "Star Wheel Exit Guide Support Arm", Vector3.Lerp(supportStart, guidePosition, 0.5f), new Vector3(0.055f, 0.055f, supportLength), metalMaterial);
            supportArm.transform.rotation = Quaternion.Euler(0f, Mathf.Atan2(supportVector.x, supportVector.z) * Mathf.Rad2Deg, 0f);
        }

        private void CreateFixedStarWheelBarrier(Transform parent, Material material, BottleVerticalLayout bottleLayout)
        {
            const int starBarrierSegments = 96;
            const float starBarrierRadius = FillingStarWheelOuterRadius + 0.045f;
            var starBarrierY = bottleLayout.StarWheelDiscY;
            var startDegrees = StarWheelPocketAngleDegrees(0) + StarWheelStepAngleDegrees * 0.5f;
            var endDegrees = StarWheelPocketAngleDegrees(FillingStarWheelPocketCount - 1) - StarWheelStepAngleDegrees * 0.5f;
            if (endDegrees <= startDegrees)
            {
                endDegrees += 360f;
            }

            var arcDegrees = endDegrees - startDegrees;
            var starBarrierSegmentLength = starBarrierRadius * Mathf.Deg2Rad * (arcDegrees / starBarrierSegments) * 1.08f;

            for (var i = 0; i <= starBarrierSegments; i++)
            {
                var angleDegrees = startDegrees + i * arcDegrees / starBarrierSegments;
                var angleRad = angleDegrees * Mathf.Deg2Rad;
                var position = FillingStarWheelVisualCenter + new Vector3(
                    Mathf.Cos(angleRad) * starBarrierRadius,
                    starBarrierY,
                    Mathf.Sin(angleRad) * starBarrierRadius);
                var barrierSeg = CreateCube(
                    parent,
                    "Star Wheel Continuous Barrier",
                    position,
                    new Vector3(starBarrierSegmentLength, 0.16f, 0.035f),
                    material);
                barrierSeg.transform.rotation = Quaternion.Euler(0f, -angleDegrees + 90f, 0f);
            }
        }

        private Mesh CreateScallopedStarWheelMesh(int pocketCount, float outerRadius, float pocketDepth, float thickness, int samplesPerPocket, float pocketAngleOffsetDegrees)
        {
            var ringCount = pocketCount * samplesPerPocket;
            var vertices = new Vector3[2 + ringCount * 2];
            var triangles = new List<int>(ringCount * 12);
            var pocketAngle = Mathf.PI * 2f / pocketCount;
            var pocketAngleOffset = pocketAngleOffsetDegrees * Mathf.Deg2Rad;
            var halfPocketWidth = Mathf.Asin(Mathf.Clamp(pocketDepth / outerRadius, 0.01f, 0.45f));

            vertices[0] = new Vector3(0f, thickness * 0.5f, 0f);
            vertices[1] = new Vector3(0f, -thickness * 0.5f, 0f);

            for (var i = 0; i < ringCount; i++)
            {
                var angle = pocketAngleOffset + i * Mathf.PI * 2f / ringCount;
                var centered = Mathf.Repeat(angle - pocketAngleOffset + pocketAngle * 0.5f, pocketAngle) - pocketAngle * 0.5f;
                var tangentDistance = Mathf.Sin(centered) * outerRadius;
                var cutAmount = Mathf.Abs(centered) <= halfPocketWidth && Mathf.Abs(tangentDistance) <= pocketDepth
                    ? Mathf.Sqrt(Mathf.Max(0f, pocketDepth * pocketDepth - tangentDistance * tangentDistance))
                    : 0f;
                var radius = outerRadius - cutAmount;
                var x = Mathf.Cos(angle) * radius;
                var z = Mathf.Sin(angle) * radius;

                vertices[2 + i] = new Vector3(x, thickness * 0.5f, z);
                vertices[2 + ringCount + i] = new Vector3(x, -thickness * 0.5f, z);
            }

            for (var i = 0; i < ringCount; i++)
            {
                var next = (i + 1) % ringCount;
                var top = 2 + i;
                var nextTop = 2 + next;
                var bottom = 2 + ringCount + i;
                var nextBottom = 2 + ringCount + next;

                triangles.Add(0);
                triangles.Add(nextTop);
                triangles.Add(top);

                triangles.Add(1);
                triangles.Add(bottom);
                triangles.Add(nextBottom);

                triangles.Add(top);
                triangles.Add(nextTop);
                triangles.Add(nextBottom);

                triangles.Add(top);
                triangles.Add(nextBottom);
                triangles.Add(bottom);
            }

            var mesh = new Mesh
            {
                name = "Scalloped Star Wheel Mesh",
                vertices = vertices,
                triangles = triangles.ToArray()
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private (List<Transform> heads, Transform dropper, Transform sensor, List<Transform> magazineCaps) CreateCappingStation(Transform parent, Material metalMaterial, Material capMaterial, Material capTubeMaterial, Material sensorMaterial, BottleVerticalLayout bottleLayout)
        {
            var heads = new List<Transform>();
            var magazineCaps = new List<Transform>();
            var capDropPosition = StarWheelPocketPosition(CapDropPocketIndex, 0f);

            var dropperTool = new GameObject("Star Wheel Cap Catch Point");
            dropperTool.transform.SetParent(parent);


            var capMagazineAssembly = new GameObject("Cap Magazine Assembly");
            capMagazineAssembly.transform.SetParent(parent);
            var capMagazinePosition = CapMagazineTubePosition;
            capMagazinePosition.y = bottleLayout.CapMagazineAssemblyY;
            capMagazineAssembly.transform.position = capMagazinePosition;
            capMagazineAssembly.transform.rotation = Quaternion.Euler(CapMagazineTubeEulerAngles);

            var capTube = new GameObject("Transparent Cap Magazine Tube");
            capTube.transform.SetParent(capMagazineAssembly.transform);
            capTube.transform.localPosition = CapMagazineAssemblyLocalOffset;
            capTube.transform.localRotation = Quaternion.identity;
            var tubeMeshFilter = capTube.AddComponent<MeshFilter>();
            tubeMeshFilter.sharedMesh = CreateCurvedCapMagazineMesh(28, 0.20f, 0.10f);
            capTube.AddComponent<MeshRenderer>().sharedMaterial = capTubeMaterial;

            var tubeOutlet = capMagazineAssembly.transform.TransformPoint(
                CapMagazineAssemblyLocalOffset + CapMagazineLocalPosition(-CapMagazineGuideHalfLength));
            var defaultOutletPosition = new Vector3(capDropPosition.x, bottleLayout.CapMagazineOutletY, capDropPosition.z)
                + capMagazineAssembly.transform.TransformVector(CapMagazineAssemblyLocalOffset);
            var guideRailLength = Vector3.Distance(tubeOutlet, defaultOutletPosition);
            var outletCapLocalRotation = Quaternion.Euler(CapMagazineOutletCapLocalEulerAngles);
            var guideRailWorldDirection = capMagazineAssembly.transform.TransformDirection(CapMagazinePathDirectionFromCapRotation(outletCapLocalRotation)).normalized;
            var outletPosition = tubeOutlet + guideRailWorldDirection * guideRailLength;
            var outletLocalPosition = capMagazineAssembly.transform.InverseTransformPoint(outletPosition);
            dropperTool.transform.position = outletPosition;

            for (var i = 0; i < 10; i++)
            {
                var capInTube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                capInTube.name = $"Cap Magazine Tube Cap {i + 1}";
                capInTube.transform.SetParent(capMagazineAssembly.transform);
                var capLocalY = CapMagazineCapBottomLocalY + (9 - i) * CapMagazineCapPitch;
                capInTube.transform.localPosition = CapMagazineCapLocalPosition(capLocalY, outletLocalPosition);
                capInTube.transform.localRotation = CapMagazineCapLocalRotation(capLocalY);
                capInTube.transform.localScale = new Vector3(0.105f, 0.018f, 0.105f);
                capInTube.GetComponent<Renderer>().sharedMaterial = capMaterial;
                magazineCaps.Add(capInTube.transform);
            }

            CreateCapGuideRail(parent, "Cap Guide Rail Left", tubeOutlet + Vector3.left * 0.075f, outletPosition + Vector3.left * 0.075f, metalMaterial);
            CreateCapGuideRail(parent, "Cap Guide Rail Right", tubeOutlet + Vector3.right * 0.075f, outletPosition + Vector3.right * 0.075f, metalMaterial);

            Transform sensor = null;

            for (var i = 0; i < 3; i++)
            {
                var pocketIndex = CappingPocketStartIndex + i;
                var stationPosition = StarWheelPocketPosition(pocketIndex, 0f);
                var tightenerTool = new GameObject($"Rotary Cap Tightener Moving Tool {pocketIndex}");
                tightenerTool.transform.SetParent(parent);
                tightenerTool.transform.position = new Vector3(stationPosition.x, bottleLayout.CapTightenerToolY, stationPosition.z);

                var tightenerMotor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tightenerMotor.name = $"Rotary Cap Tightener Fixed Motor {pocketIndex}";
                tightenerMotor.transform.SetParent(parent);
                tightenerMotor.transform.position = new Vector3(stationPosition.x, bottleLayout.CapTightenerMotorY, stationPosition.z);
                tightenerMotor.transform.localScale = new Vector3(0.16f, 0.28f, 0.16f);
                tightenerMotor.GetComponent<Renderer>().sharedMaterial = metalMaterial;

                var driveBlock = CreateCube(tightenerTool.transform, $"Rotary Cap Tightener Drive Block {pocketIndex}", Vector3.zero, new Vector3(0.28f, 0.13f, 0.28f), metalMaterial);
                driveBlock.transform.localPosition = new Vector3(0f, -0.06f, 0f);
                var tightenerHead = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tightenerHead.name = $"Rotary Cap Tightener Head {pocketIndex}";
                tightenerHead.transform.SetParent(tightenerTool.transform);
                tightenerHead.transform.localPosition = new Vector3(0f, -0.12f, 0f);
                tightenerHead.transform.localScale = new Vector3(0.09f, 0.08f, 0.09f);
                tightenerHead.GetComponent<Renderer>().sharedMaterial = metalMaterial;
                heads.Add(tightenerTool.transform);
            }

            return (heads, dropperTool.transform, sensor, magazineCaps);
        }

        private static Vector3 CapMagazineLocalPosition(float localY)
        {
            var normalizedY = Mathf.Clamp(localY / CapMagazineGuideHalfLength, -1f, 1f);
            return new Vector3(0f, localY, CapMagazineGuideCurveDepth * (normalizedY * normalizedY - 1f));
        }

        private Mesh CreateCurvedCapMagazineMesh(int segmentCount, float width, float depth)
        {
            var sectionCount = Mathf.Max(2, segmentCount) + 1;
            var vertices = new Vector3[sectionCount * 4];
            var triangles = new List<int>((sectionCount - 1) * 24 + 12);
            var halfWidth = width * 0.5f;
            var halfDepth = depth * 0.5f;

            for (var i = 0; i < sectionCount; i++)
            {
                var ratio = i / (float)(sectionCount - 1);
                var localY = Mathf.Lerp(-CapMagazineGuideHalfLength, CapMagazineGuideHalfLength, ratio);
                var normalizedY = localY / CapMagazineGuideHalfLength;
                var slope = 2f * CapMagazineGuideCurveDepth * normalizedY / CapMagazineGuideHalfLength;
                var tangent = new Vector3(0f, 1f, slope).normalized;
                var crossNormal = new Vector3(0f, -tangent.z, tangent.y);
                var center = CapMagazineLocalPosition(localY);
                var baseIndex = i * 4;
                vertices[baseIndex] = center - Vector3.right * halfWidth - crossNormal * halfDepth;
                vertices[baseIndex + 1] = center + Vector3.right * halfWidth - crossNormal * halfDepth;
                vertices[baseIndex + 2] = center + Vector3.right * halfWidth + crossNormal * halfDepth;
                vertices[baseIndex + 3] = center - Vector3.right * halfWidth + crossNormal * halfDepth;
            }

            for (var i = 0; i < sectionCount - 1; i++)
            {
                var current = i * 4;
                var next = current + 4;
                AddMeshQuad(triangles, current, next, next + 1, current + 1);
                AddMeshQuad(triangles, current + 1, next + 1, next + 2, current + 2);
                AddMeshQuad(triangles, current + 2, next + 2, next + 3, current + 3);
                AddMeshQuad(triangles, current + 3, next + 3, next, current);
            }

            AddMeshQuad(triangles, 3, 2, 1, 0);
            var end = (sectionCount - 1) * 4;
            AddMeshQuad(triangles, end, end + 1, end + 2, end + 3);

            var mesh = new Mesh
            {
                name = "Transparent Cap Magazine Smooth Curved Mesh",
                vertices = vertices,
                triangles = triangles.ToArray()
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddMeshQuad(List<int> triangles, int a, int b, int c, int d)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(d);
        }

        private static Vector3 CapMagazineCapLocalPosition(float localY, Vector3 outletLocalPosition)
        {
            var tubeExitPosition = CapMagazineAssemblyLocalOffset + CapMagazineLocalPosition(-CapMagazineGuideHalfLength);
            if (localY >= -CapMagazineGuideHalfLength)
            {
                return CapMagazineAssemblyLocalOffset + CapMagazineLocalPosition(localY);
            }

            var railRatio = Mathf.InverseLerp(-CapMagazineGuideHalfLength, CapMagazineCapBottomLocalY, localY);
            return Vector3.Lerp(tubeExitPosition, outletLocalPosition, railRatio);
        }

        private static Quaternion CapMagazineCapLocalRotation(float localY)
        {
            if (localY < -CapMagazineGuideHalfLength)
            {
                return Quaternion.Euler(CapMagazineOutletCapLocalEulerAngles);
            }

            Vector3 pathDirection;
            var normalizedY = Mathf.Clamp(localY / CapMagazineGuideHalfLength, -1f, 1f);
            var slope = 2f * CapMagazineGuideCurveDepth * normalizedY / CapMagazineGuideHalfLength;
            pathDirection = new Vector3(0f, 1f, slope);

            return Quaternion.FromToRotation(Vector3.up, pathDirection.normalized) * Quaternion.Euler(90f, 0f, 0f);
        }

        private static Vector3 CapMagazinePathDirectionFromCapRotation(Quaternion capLocalRotation)
        {
            return (capLocalRotation * Quaternion.Euler(-90f, 0f, 0f)) * Vector3.up;
        }

        private Transform CreateCapGuideRail(Transform parent, string name, Vector3 start, Vector3 end, Material material, float thickness = 0.024f)
        {
            var direction = end - start;
            var length = Mathf.Max(0.01f, direction.magnitude);
            var rail = CreateCube(parent, name, Vector3.Lerp(start, end, 0.5f), new Vector3(thickness, thickness, length), material);
            rail.transform.rotation = Quaternion.FromToRotation(Vector3.forward, direction.normalized);
            return rail.transform;
        }

        private void CreateVideoStyleMachineDetails(Transform parent, Material metalMaterial, Material clearGuardMaterial, Material panelMaterial, Material hoseMaterial, Material capMaterial, Material sensorMaterial, Material rejectMaterial, Material curtainMaterial)
        {
            CreateMachineGuardCabinet(parent, metalMaterial, clearGuardMaterial, curtainMaterial);
            CreateOperatorControlPanel(parent, metalMaterial, panelMaterial, sensorMaterial, rejectMaterial);
            CreateCapElevatorBowl(parent, metalMaterial, capMaterial);
            CreateBluePneumaticHoses(parent, hoseMaterial, metalMaterial);
            CreateUnderFrameUtilities(parent, metalMaterial, rejectMaterial);
        }

        private void CreateMachineGuardCabinet(Transform parent, Material metalMaterial, Material clearGuardMaterial, Material curtainMaterial)
        {
            var cabinetCenter = new Vector3(FillingStarWheelCenterX + 0.1f, 1.35f, FillingLineZ);
            CreateCube(parent, "Stainless Guard Cabinet Back Panel", cabinetCenter + new Vector3(0.72f, 0.2f, 0f), new Vector3(0.06f, 1.55f, 2.1f), metalMaterial);
            CreateCube(parent, "Transparent Guard Door Front", cabinetCenter + new Vector3(-0.65f, 0.2f, 0f), new Vector3(0.045f, 1.35f, 2.0f), clearGuardMaterial);
            CreateCube(parent, "Stainless Guard Cabinet Top", cabinetCenter + new Vector3(0.05f, 0.96f, 0f), new Vector3(1.55f, 0.08f, 2.18f), metalMaterial);
            CreateCube(parent, "Stainless Guard Cabinet Bottom Tray", cabinetCenter + new Vector3(0.05f, -0.62f, 0f), new Vector3(1.65f, 0.08f, 2.18f), metalMaterial);

            for (var i = 0; i < 4; i++)
            {
                var z = cabinetCenter.z - 0.78f + i * 0.52f;
                CreateCube(parent, "Guard Cabinet Vertical Post", new Vector3(cabinetCenter.x - 0.7f, 1.32f, z), new Vector3(0.045f, 1.75f, 0.045f), metalMaterial);
                CreateCube(parent, "Guard Cabinet Rear Post", new Vector3(cabinetCenter.x + 0.76f, 1.32f, z), new Vector3(0.045f, 1.75f, 0.045f), metalMaterial);
            }

            for (var i = 0; i < 6; i++)
            {
                var strip = CreateCube(parent, "White Plastic Strip Curtain", new Vector3(FillingStarWheelCenterX - 0.82f + i * 0.08f, 1.17f, FillingLineZ - 1.16f), new Vector3(0.045f, 0.82f, 0.018f), curtainMaterial);
                strip.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(i) * 4f);
            }
        }

        private void CreateOperatorControlPanel(Transform parent, Material metalMaterial, Material panelMaterial, Material greenMaterial, Material redMaterial)
        {
            var panel = CreateCube(parent, "Operator Control Panel", new Vector3(1.98f, 1.58f, -0.1f), new Vector3(0.12f, 0.95f, 0.62f), panelMaterial);
            panel.transform.rotation = Quaternion.Euler(0f, -8f, 0f);
            CreateCube(parent, "Control Panel Stainless Mast", new Vector3(1.98f, 0.9f, -0.1f), new Vector3(0.06f, 1.15f, 0.06f), metalMaterial);

            for (var row = 0; row < 4; row++)
            {
                for (var col = 0; col < 3; col++)
                {
                    var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    lamp.name = row == 0 ? "Green Running Indicator Lamp" : "Panel Push Button";
                    lamp.transform.SetParent(parent);
                    lamp.transform.position = new Vector3(1.9f, 1.88f - row * 0.18f, -0.32f + col * 0.2f);
                    lamp.transform.localScale = Vector3.one * 0.055f;
                    lamp.GetComponent<Renderer>().sharedMaterial = (row + col) % 3 == 0 ? redMaterial : greenMaterial;
                }
            }
        }

        private void CreateCapElevatorBowl(Transform parent, Material metalMaterial, Material capMaterial)
        {
            var bowl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bowl.name = "Inclined Cap Feeder Bowl";
            bowl.transform.SetParent(parent);
            bowl.transform.position = new Vector3(1.85f, 2.55f, 0.15f);
            bowl.transform.rotation = Quaternion.Euler(0f, 0f, -18f);
            bowl.transform.localScale = new Vector3(0.46f, 0.18f, 0.46f);
            bowl.GetComponent<Renderer>().sharedMaterial = metalMaterial;

            for (var i = 0; i < 10; i++)
            {
                var angle = i * Mathf.PI * 2f / 10f;
                var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cap.name = "Blue Cap In Feeder Bowl";
                cap.transform.SetParent(parent);
                cap.transform.position = new Vector3(1.85f + Mathf.Cos(angle) * 0.22f, 2.72f, 0.15f + Mathf.Sin(angle) * 0.22f);
                cap.transform.localScale = new Vector3(0.055f, 0.015f, 0.055f);
                cap.GetComponent<Renderer>().sharedMaterial = capMaterial;
            }

            var chute = CreateCube(parent, "Cap Feed Chute From Bowl", new Vector3(1.42f, 2.05f, -0.1f), new Vector3(0.08f, 0.04f, 1.05f), metalMaterial);
            chute.transform.rotation = Quaternion.Euler(28f, 0f, -28f);
        }

        private void CreateBluePneumaticHoses(Transform parent, Material hoseMaterial, Material metalMaterial)
        {
            var startBase = new Vector3(FillingStarWheelCenterX + 0.55f, 2.32f, FillingLineZ - 0.65f);
            for (var i = 0; i < 6; i++)
            {
                var hose = CreateCube(parent, "Blue Pneumatic Hose", startBase + new Vector3(-0.18f + i * 0.08f, -0.28f, 0.2f + i * 0.06f), new Vector3(0.025f, 0.025f, 1.15f), hoseMaterial);
                hose.transform.rotation = Quaternion.Euler(35f, 10f + i * 4f, 0f);
            }

            CreateCube(parent, "Compressed Air Manifold", new Vector3(FillingStarWheelCenterX + 0.52f, 1.55f, FillingLineZ - 0.82f), new Vector3(0.5f, 0.08f, 0.08f), metalMaterial);
        }

        private void CreateUnderFrameUtilities(Transform parent, Material metalMaterial, Material redMaterial)
        {
            CreateCube(parent, "Open Stainless Machine Frame Front", new Vector3(0.4f, 0.72f, -2.05f), new Vector3(2.7f, 0.06f, 0.06f), metalMaterial);
            CreateCube(parent, "Open Stainless Machine Frame Rear", new Vector3(0.4f, 0.72f, 1.45f), new Vector3(2.7f, 0.06f, 0.06f), metalMaterial);
            for (var i = 0; i < 5; i++)
            {
                var x = -0.85f + i * 0.62f;
                CreateCube(parent, "Stainless Frame Leg", new Vector3(x, 0.28f, -2.05f), new Vector3(0.055f, 0.62f, 0.055f), metalMaterial);
                CreateCube(parent, "Stainless Frame Leg", new Vector3(x, 0.28f, 1.45f), new Vector3(0.055f, 0.62f, 0.055f), metalMaterial);
            }

            var compressor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            compressor.name = "Red Air Compressor Tank";
            compressor.transform.SetParent(parent);
            compressor.transform.position = new Vector3(-1.18f, 0.22f, 1.6f);
            compressor.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            compressor.transform.localScale = new Vector3(0.18f, 0.48f, 0.18f);
            compressor.GetComponent<Renderer>().sharedMaterial = redMaterial;
        }

        private GameObject CreateCube(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private void CreateFloorSupportLeg(Transform parent, string name, Vector3 topPosition, Material material)
        {
            const float floorY = 0f;
            const float legWidth = 0.055f;
            var height = Mathf.Max(0.1f, topPosition.y - floorY);
            CreateCube(parent, name, new Vector3(topPosition.x, floorY + height * 0.5f, topPosition.z), new Vector3(legWidth, height, legWidth), material);
            CreateCube(parent, $"{name} Foot", new Vector3(topPosition.x, 0.025f, topPosition.z), new Vector3(0.18f, 0.05f, 0.18f), material);
        }

        private void ConfigureCameraAndLight()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.transform.position = new Vector3(4.8f, 3.7f, -7.4f);
            camera.transform.rotation = Quaternion.Euler(29f, -32f, 0f);
            camera.fieldOfView = 58f;

            var light = FindAnyObjectByType<Light>();
            if (light == null)
            {
                var lightObject = new GameObject("Directional Light");
                light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
            }

            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            light.intensity = 1.45f;
        }
    }

    public class SlatChainConveyorAnimator : MonoBehaviour
    {
        public List<Transform> movingParts = new List<Transform>();
        public Vector3 worldAxis = Vector3.forward;
        public float speedMps = 0.85f;
        public float minCoordinate;
        public float maxCoordinate = 1f;
        public FillingFilteringDigitalTwin speedSource;

        private void Update()
        {
            if (!Application.isPlaying || movingParts.Count == 0)
            {
                return;
            }

            if (speedSource != null && speedSource.SplitConveyorPaused)
            {
                return;
            }

            var axis = worldAxis.sqrMagnitude > 0.0001f ? worldAxis.normalized : Vector3.forward;
            var loopLength = Mathf.Max(0.01f, maxCoordinate - minCoordinate);
            var activeSpeed = speedSource != null ? speedSource.ConveyorEffectiveSpeedMps : speedMps;
            var delta = axis * (activeSpeed * Time.deltaTime);

            foreach (var part in movingParts)
            {
                if (part == null)
                {
                    continue;
                }

                var position = part.position + delta;
                var coordinate = Vector3.Dot(position, axis);
                while (coordinate > maxCoordinate)
                {
                    position -= axis * loopLength;
                    coordinate -= loopLength;
                }

                while (coordinate < minCoordinate)
                {
                    position += axis * loopLength;
                    coordinate += loopLength;
                }

                part.position = position;
            }
        }
    }
}
