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
        private static readonly Vector3 FillingStarWheelBottleCenter = new Vector3(FillingStarWheelCenterX, 0.82f, FillingLineZ);
        private static readonly Vector3 ScallopedStarWheelDiscLocalPosition = new Vector3(0f, 1.485f, 0f);
        private static readonly Vector3 InfeedTurntableBottleCenter = new Vector3(-3.253f, 1.05f, FillingLineZ);
        private const int FillingStarWheelPocketCount = 10;
        private const float StarWheelStepAngleDegrees = 360f / FillingStarWheelPocketCount;
        private const float FillingStarWheelPocketNotchRadius = 0.075f;
        private const float FillingStarWheelOuterRadius = 0.8f;
        private const float FillingStarWheelBottleRadius = FillingStarWheelOuterRadius - FillingStarWheelPocketNotchRadius;
        private const float FillingStarWheelEntryAngleDegrees = 180f;
        private const int FillingStationStartPocketIndex = 1;
        private static readonly int[] FillingNozzlePocketOrder = { 1, 2, 3 };
        private const int CapDropPocketIndex = 5;
        private const int CappingPocketStartIndex = 7;
        private const float FillingNozzleScaleY = 0.32f;
        private const float FillingNozzleClusterLift = FillingNozzleScaleY * 2f;
        private const float FillingFirstZ = -1.2f;
        private const float CappingFirstZ = 1.65f;
        private const float CappingPitch = 0.42f;

        public bool rebuildOnEnable = true;

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
            var acceptMaterial = CreateMaterial(new Color(0.25f, 0.9f, 0.35f));
            var clearGuardMaterial = CreateMaterial(new Color(0.78f, 0.95f, 1f, 0.22f));
            var hoseMaterial = CreateMaterial(new Color(0.02f, 0.32f, 0.85f));
            var panelMaterial = CreateMaterial(new Color(0.12f, 0.14f, 0.14f));
            var curtainMaterial = CreateMaterial(new Color(0.96f, 0.96f, 0.9f, 0.45f));

            CreateFloor(root.transform, floorMaterial);
            CreateConveyor(root.transform, beltMaterial, metalMaterial, slatMaterial, ribMaterial, sensorMaterial);
            var turntable = CreateTurntable(root.transform, metalMaterial);
            var bottleSpawnPoint = CreateBottleDropper(root.transform, metalMaterial);
            var turntableOutlet = CreateTurntableOutlet(root.transform, metalMaterial);
            var vesselParts = CreateLiquidVessel(root.transform, metalMaterial, waterMaterial);
            var nozzleParts = CreateFillingNozzles(root.transform, metalMaterial, waterMaterial);
            Transform fillingStopGate = null;
            // CreateFillingStopGate(root.transform, metalMaterial, rejectMaterial);
            var starWheel = CreateFillingStarWheel(root.transform, starWheelMaterial, metalMaterial, beltMaterial);
            var qcBeam = CreateQcSensor(root.transform, sensorMaterial, metalMaterial);
            var cappingStation = CreateCappingStation(root.transform, metalMaterial, capMaterial, capTubeMaterial, sensorMaterial);
            var pusher = CreatePusher(root.transform, metalMaterial, rejectMaterial);
            var acceptChute = CreateChute(root.transform, "Accept Chute", new Vector3(0.95f, 0.28f, 3.95f), acceptMaterial, 18f);
            var rejectChute = CreateChute(root.transform, "Reject Chute", new Vector3(-1.15f, 0.35f, 1.25f), rejectMaterial, -25f);
            CreateVideoStyleMachineDetails(root.transform, metalMaterial, clearGuardMaterial, panelMaterial, hoseMaterial, capMaterial, sensorMaterial, rejectMaterial, curtainMaterial);
            CreateFinishedBottleAccumulationTable(root.transform, metalMaterial, bottleMaterial, waterMaterial, capMaterial, labelMaterial);
            var bottleTemplate = CreateBottleTemplate(root.transform, bottleMaterial, waterMaterial, capMaterial, labelMaterial);

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
            process.acceptChute = acceptChute;
            process.rejectChute = rejectChute;
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
            process.cappingTimeSeconds = 0.35f;
            process.acceptEndZ = 3.75f;
            process.starWheelPocketCount = FillingStarWheelPocketCount;
            process.starWheelCenter = FillingStarWheelBottleCenter;
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
            process.turntableCenter = InfeedTurntableBottleCenter;
            process.turntableRadius = 0.95f;
            process.turntableBottleRadius = 0.11f;
            process.outletCaptureRadius = 0.78f;
            process.releaseThreshold = 7;
            process.initialTurntableBottleCount = 12;
            process.maxTurntableBuffer = 16;
            process.spawnIntervalSeconds = 0.85f;
            process.releaseIntervalSeconds = 0.62f;
            process.neckRailStartX = InfeedTurntableBottleCenter.x + 0.65f;
            process.neckRailEndX = StarWheelPocketPosition(0, FillingStarWheelBottleCenter.y).x;
            process.neckRailZ = FillingStarWheelBottleCenter.z;
            process.neckRailStartZ = process.neckRailZ;
            process.neckRailEndZ = process.neckRailZ;
            process.neckRailStartBottleY = FillingStarWheelBottleCenter.y;
            process.neckRailEndBottleY = FillingStarWheelBottleCenter.y;
            process.airBlowerWindSpeedMps = 0.8f;

            CreateHud(root.transform, process);
            ConfigureCameraAndLight();
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
            floor.transform.position = new Vector3(0f, -0.05f, 0.45f);
            floor.transform.localScale = new Vector3(8f, 0.1f, 11.4f);
            floor.GetComponent<Renderer>().sharedMaterial = material;
        }

        private void CreateConveyor(Transform parent, Material beltMaterial, Material metalMaterial, Material slatMaterial, Material ribMaterial, Material sensorMaterial)
        {
            CreateCube(parent, "Slat Chain Conveyor Base", new Vector3(0f, 0.38f, 0.55f), new Vector3(0.52f, 0.08f, 10.7f), beltMaterial);

            const float pitch = 0.22f;
            const float slatLength = 0.17f;
            const float startZ = -4.1f;
            const int slatCount = 48;
            var movingParts = new List<Transform>();
            for (var i = 0; i < slatCount; i++)
            {
                var z = startZ + i * pitch;
                movingParts.Add(CreateCube(parent, "Modular Slat Plate", new Vector3(0f, 0.46f, z), new Vector3(0.46f, 0.035f, slatLength), slatMaterial).transform);
                movingParts.Add(CreateCube(parent, "Slat Gap Shadow", new Vector3(0f, 0.482f, z + slatLength * 0.5f + 0.017f), new Vector3(0.47f, 0.012f, 0.028f), beltMaterial).transform);

                if (i % 2 == 0)
                {
                    movingParts.Add(CreateCube(parent, "Anti Slip Cross Rib", new Vector3(0f, 0.515f, z - 0.055f), new Vector3(0.42f, 0.026f, 0.022f), ribMaterial).transform);
                }
            }

            CreateConveyorAnimator(parent, "Main Slat Chain Motion", movingParts, Vector3.forward, 0.85f, startZ - pitch, startZ + slatCount * pitch);

            CreateCube(parent, "Left Narrow Guide Rail", new Vector3(-0.28f, 0.74f, 0.55f), new Vector3(0.035f, 0.1f, 10.7f), metalMaterial);
            CreateCube(parent, "Right Narrow Guide Rail", new Vector3(0.28f, 0.74f, 0.55f), new Vector3(0.035f, 0.1f, 10.7f), metalMaterial);
            CreateCube(parent, "Narrow Conveyor Support", new Vector3(0f, 0.2f, 0.55f), new Vector3(0.68f, 0.15f, 10.9f), metalMaterial);

            CreateInfeedTransferConveyor(parent, beltMaterial, metalMaterial, slatMaterial, ribMaterial);
            CreateHorizontalNeckSupportRail(parent, "Infeed Neck Support Rail", InfeedTurntableBottleCenter.x + 0.65f, StarWheelPocketPosition(0, FillingStarWheelBottleCenter.y).x, FillingStarWheelBottleCenter.z, 1.64f, 1.41f, metalMaterial, true, false);
            CreateAirBlower(parent, metalMaterial, sensorMaterial);
            // Outfeed neck support rail is disabled while the star wheel exit rail is being redesigned.
        }

        private void CreateInfeedTransferConveyor(Transform parent, Material beltMaterial, Material metalMaterial, Material slatMaterial, Material ribMaterial)
        {
            var startX = InfeedTurntableBottleCenter.x + 0.72f;
            var endX = StarWheelPocketPosition(0, FillingStarWheelBottleCenter.y).x + 0.12f;
            var length = Mathf.Abs(endX - startX);
            var centerX = (startX + endX) * 0.5f;
            var z = FillingStarWheelBottleCenter.z;

            var baseBelt = CreateCube(parent, "Infeed Transfer Slat Conveyor Base", new Vector3(centerX, 0.38f, z), new Vector3(0.42f, 0.08f, length), beltMaterial);
            baseBelt.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            var support = CreateCube(parent, "Infeed Transfer Conveyor Support", new Vector3(centerX, 0.2f, z), new Vector3(0.55f, 0.15f, length + 0.18f), metalMaterial);
            support.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            const float pitch = 0.2f;
            var slatCount = Mathf.Max(6, Mathf.CeilToInt(length / pitch));
            var movingParts = new List<Transform>();
            for (var i = 0; i < slatCount; i++)
            {
                var ratio = slatCount == 1 ? 0f : i / (float)(slatCount - 1);
                var x = Mathf.Lerp(startX, endX, ratio);
                var slat = CreateCube(parent, "Infeed Transfer Modular Slat", new Vector3(x, 0.46f, z), new Vector3(0.15f, 0.035f, 0.36f), slatMaterial);
                slat.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                movingParts.Add(slat.transform);
                if (i % 2 == 0)
                {
                    var rib = CreateCube(parent, "Infeed Transfer Anti Slip Rib", new Vector3(x, 0.515f, z), new Vector3(0.022f, 0.026f, 0.32f), ribMaterial);
                    rib.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                    movingParts.Add(rib.transform);
                }
            }

            CreateConveyorAnimator(parent, "Infeed Transfer Slat Motion", movingParts, Vector3.right, 0.65f, Mathf.Min(startX, endX) - pitch, Mathf.Max(startX, endX) + pitch);

            CreateCube(parent, "Infeed Transfer Left Guide Rail", new Vector3(centerX, 0.76f, z - 0.19f), new Vector3(0.035f, 0.09f, length), metalMaterial).transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            CreateCube(parent, "Infeed Transfer Right Guide Rail", new Vector3(centerX, 0.76f, z + 0.19f), new Vector3(0.035f, 0.09f, length), metalMaterial).transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        }

        private void CreateConveyorAnimator(Transform parent, string name, List<Transform> movingParts, Vector3 worldAxis, float speedMps, float minCoordinate, float maxCoordinate)
        {
            var animatorObject = new GameObject(name);
            animatorObject.transform.SetParent(parent);
            var animator = animatorObject.AddComponent<SlatChainConveyorAnimator>();
            animator.movingParts = movingParts;
            animator.worldAxis = worldAxis;
            animator.speedMps = speedMps;
            animator.minCoordinate = minCoordinate;
            animator.maxCoordinate = maxCoordinate;
        }

        private void CreateHorizontalNeckSupportRail(Transform parent, string namePrefix, float startX, float endX, float z, float startY, float endY, Material material, bool shortenRightRail = false, bool createSupports = true)
        {
            const float railHalfGap = 0.105f;
            const float railWidth = 0.026f;
            const float railHeight = 0.035f;
            const float shortRightStartOffset = 0.42f;
            var horizontalLength = Mathf.Abs(startX - endX);
            var rise = endY - startY;
            var length = Mathf.Sqrt(horizontalLength * horizontalLength + rise * rise);
            var centerX = (startX + endX) * 0.5f;
            var centerY = (startY + endY) * 0.5f;
            var pitchDegrees = Mathf.Atan2(rise, horizontalLength) * Mathf.Rad2Deg;

            var leftRail = CreateCube(parent, $"{namePrefix} Left", new Vector3(centerX, centerY, z - railHalfGap), new Vector3(railWidth, railHeight, length), material);
            leftRail.transform.rotation = Quaternion.Euler(0f, 90f, pitchDegrees);

            var railDirection = Mathf.Sign(endX - startX);
            if (Mathf.Approximately(railDirection, 0f))
            {
                railDirection = 1f;
            }

            var rightStartX = shortenRightRail ? startX + railDirection * shortRightStartOffset : startX;
            var rightStartRatio = Mathf.InverseLerp(startX, endX, rightStartX);
            var rightStartY = Mathf.Lerp(startY, endY, rightStartRatio);
            var rightHorizontalLength = Mathf.Abs(rightStartX - endX);
            var rightRise = endY - rightStartY;
            var rightLength = Mathf.Sqrt(rightHorizontalLength * rightHorizontalLength + rightRise * rightRise);
            var rightCenterX = (rightStartX + endX) * 0.5f;
            var rightCenterY = (rightStartY + endY) * 0.5f;
            var rightRail = CreateCube(parent, $"{namePrefix} Right", new Vector3(rightCenterX, rightCenterY, z + railHalfGap), new Vector3(railWidth, railHeight, rightLength), material);
            rightRail.transform.rotation = Quaternion.Euler(0f, 90f, pitchDegrees);

            if (!createSupports)
            {
                return;
            }

            const float postSpacing = 0.8f;
            var postCount = Mathf.Max(2, Mathf.CeilToInt(horizontalLength / postSpacing) + 1);
            for (var i = 0; i < postCount; i++)
            {
                var ratio = postCount == 1 ? 0f : i / (float)(postCount - 1);
                var x = Mathf.Lerp(startX, endX, ratio);
                var railY = Mathf.Lerp(startY, endY, ratio);
                var postCenterY = (0.48f + railY) * 0.5f;
                var postHeight = Mathf.Max(0.1f, railY - 0.48f);
                CreateCube(parent, $"{namePrefix} Left Support", new Vector3(x, postCenterY, z - 0.18f), new Vector3(0.035f, postHeight, 0.035f), material);
                if (!shortenRightRail || ratio >= rightStartRatio)
                {
                    CreateCube(parent, $"{namePrefix} Right Support", new Vector3(x, postCenterY, z + 0.18f), new Vector3(0.035f, postHeight, 0.035f), material);
                }
            }
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

        private void CreateAirBlower(Transform parent, Material metalMaterial, Material airMaterial)
        {
            var blowerX = InfeedTurntableBottleCenter.x + 0.85f;
            var blowerZ = FillingStarWheelBottleCenter.z + 0.36f;
            CreateCube(parent, "Infeed Air Blower Stand", new Vector3(blowerX, 1.04f, blowerZ + 0.28f), new Vector3(0.06f, 1.08f, 0.06f), metalMaterial);
            CreateCube(parent, "Infeed Air Blower Arm", new Vector3(blowerX, 1.55f, blowerZ + 0.12f), new Vector3(0.055f, 0.055f, 0.52f), metalMaterial);

            var blower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            blower.name = "Infeed Air Blower Nozzle";
            blower.transform.SetParent(parent);
            blower.transform.position = new Vector3(blowerX, 1.55f, FillingStarWheelBottleCenter.z + 0.11f);
            blower.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            blower.transform.localScale = new Vector3(0.12f, 0.18f, 0.12f);
            blower.GetComponent<Renderer>().sharedMaterial = metalMaterial;

            for (var i = 0; i < 4; i++)
            {
                var gust = CreateCube(parent, $"Infeed Air Jet {i + 1}", new Vector3(blowerX + i * 0.11f, 1.55f, FillingStarWheelBottleCenter.z + 0.05f), new Vector3(0.08f, 0.012f, 0.012f), airMaterial);
                gust.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }
        }

        private Transform CreateTurntable(Transform parent, Material material)
        {
            var table = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            table.name = "Infeed Turntable";
            table.transform.SetParent(parent);
            table.transform.position = new Vector3(InfeedTurntableBottleCenter.x, InfeedTurntableBottleCenter.y - 0.34f, InfeedTurntableBottleCenter.z);
            table.transform.localScale = new Vector3(1.9f, 0.08f, 1.9f);
            table.GetComponent<Renderer>().sharedMaterial = material;

            const int segments = 120;
            const float rimRadius = 1.08f;
            const float outletGapStartDegrees = 335f;
            const float outletGapEndDegrees = 25f;
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
                    InfeedTurntableBottleCenter.x + Mathf.Cos(angleRad) * rimRadius,
                    InfeedTurntableBottleCenter.y - 0.04f,
                    InfeedTurntableBottleCenter.z + Mathf.Sin(angleRad) * rimRadius);
                var rim = CreateCube(parent, "Turntable Safety Rim", position, new Vector3(segmentWidth, 0.34f, 0.04f), material);
                rim.transform.rotation = Quaternion.Euler(0f, -angleDegrees + 90f, 0f);
            }

            CreateCube(parent, "Turntable Outlet Gate", new Vector3(InfeedTurntableBottleCenter.x + 1.18f, InfeedTurntableBottleCenter.y - 0.04f, InfeedTurntableBottleCenter.z), new Vector3(0.08f, 0.26f, 0.55f), material);
            return table.transform;
        }

        private Transform CreateBottleDropper(Transform parent, Material material)
        {
            CreateCube(parent, "Bottle Dropper Stand", new Vector3(InfeedTurntableBottleCenter.x + 1.25f, 1.56f, InfeedTurntableBottleCenter.z), new Vector3(0.08f, 1.92f, 0.08f), material);
            CreateCube(parent, "Bottle Dropper Arm", new Vector3(InfeedTurntableBottleCenter.x + 0.62f, 2.48f, InfeedTurntableBottleCenter.z), new Vector3(1.25f, 0.08f, 0.08f), material);

            var spawn = new GameObject("Bottle Spawn Point");
            spawn.transform.SetParent(parent);
            spawn.transform.position = new Vector3(InfeedTurntableBottleCenter.x, 2.68f, InfeedTurntableBottleCenter.z);
            return spawn.transform;
        }

        private Transform CreateTurntableOutlet(Transform parent, Material material)
        {
            CreateCube(parent, "Turntable Outlet Guide Left", new Vector3(InfeedTurntableBottleCenter.x + 0.58f, 0.95f, InfeedTurntableBottleCenter.z - 0.32f), new Vector3(0.9f, 0.18f, 0.06f), material);

            var outlet = new GameObject("Turntable Outlet");
            outlet.transform.SetParent(parent);
            outlet.transform.position = new Vector3(InfeedTurntableBottleCenter.x + 0.95f, InfeedTurntableBottleCenter.y, InfeedTurntableBottleCenter.z);
            return outlet.transform;
        }

        private (Transform vessel, Transform liquid) CreateLiquidVessel(Transform parent, Material metalMaterial, Material waterMaterial)
        {
            var vessel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            vessel.name = "Liquid Vessel";
            vessel.transform.SetParent(parent);
            vessel.transform.position = new Vector3(FillingStarWheelCenterX + 0.33f, 2.45f + FillingNozzleClusterLift, -0.72f);
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

        private (List<Transform> nozzles, List<Transform> springs) CreateFillingNozzles(Transform parent, Material metalMaterial, Material waterMaterial)
        {
            var nozzles = new List<Transform>();
            var springs = new List<Transform>();
            CreateCube(parent, "Filling Nozzle Main Rail", new Vector3(FillingStarWheelCenterX + 0.33f, 1.45f + FillingNozzleClusterLift, -0.72f), new Vector3(0.95f, 0.08f, 1.2f), metalMaterial);

            const int nozzleCount = 3;
            for (var i = 0; i < nozzleCount; i++)
            {
                var stationPosition = StarWheelPocketPosition(FillingNozzlePocketOrder[i], 0f);
                var nozzleAssembly = new GameObject($"Filling Nozzle Assembly {i + 1}");
                nozzleAssembly.transform.SetParent(parent);
                nozzleAssembly.transform.position = Vector3.zero;

                var spring = CreateCube(nozzleAssembly.transform, $"Nozzle Spring {i + 1}", new Vector3(stationPosition.x, 1.34f + FillingNozzleClusterLift, stationPosition.z), new Vector3(0.08f, 0.18f, 0.08f), metalMaterial);

                var nozzle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                nozzle.name = $"Filling Nozzle {i + 1}";
                nozzle.transform.SetParent(nozzleAssembly.transform);
                nozzle.transform.position = new Vector3(stationPosition.x, 1.08f + FillingNozzleClusterLift, stationPosition.z);
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
            return CreateCube(parent, "QC Sensor Beam", new Vector3(0f, 0.92f, 0.4f), new Vector3(0.86f, 0.035f, 0.035f), sensorMaterial).transform;
        }

        private Transform CreatePusher(Transform parent, Material metalMaterial, Material rejectMaterial)
        {
            CreateCube(parent, "Pneumatic Cylinder Body", new Vector3(0.72f, 0.78f, 0.85f), new Vector3(0.36f, 0.22f, 0.22f), metalMaterial);
            return CreateCube(parent, "Pneumatic Pusher", new Vector3(0.43f, 0.78f, 0.85f), new Vector3(0.1f, 0.32f, 0.42f), rejectMaterial).transform;
        }

        private Transform CreateChute(Transform parent, string name, Vector3 position, Material material, float zRotation)
        {
            var chute = CreateCube(parent, name, position, new Vector3(0.9f, 0.08f, 1.35f), material);
            chute.transform.rotation = Quaternion.Euler(0f, 0f, zRotation);
            return chute.transform;
        }

        private BottleProcessState CreateBottleTemplate(Transform parent, Material bottleMaterial, Material waterMaterial, Material capMaterial, Material labelMaterial)
        {
            var bottleRoot = new GameObject("Bottle Template");
            bottleRoot.transform.SetParent(parent);
            bottleRoot.transform.position = new Vector3(0f, 0.82f, -4.7f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Bottle Body";
            body.transform.SetParent(bottleRoot.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.14f, 0.42f, 0.14f);
            body.GetComponent<Renderer>().sharedMaterial = bottleMaterial;

            var neck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            neck.name = "Bottle Neck";
            neck.transform.SetParent(bottleRoot.transform);
            neck.transform.localPosition = new Vector3(0f, 0.48f, 0f);
            neck.transform.localScale = new Vector3(0.065f, 0.13f, 0.065f);
            neck.GetComponent<Renderer>().sharedMaterial = bottleMaterial;

            var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cap.name = "Bottle Cap";
            cap.transform.SetParent(bottleRoot.transform);
            cap.transform.localPosition = new Vector3(0f, 0.66f, 0f);
            cap.transform.localScale = new Vector3(0.075f, 0.045f, 0.075f);
            cap.GetComponent<Renderer>().sharedMaterial = capMaterial;
            cap.SetActive(false);

            var liquid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            liquid.name = "Bottle Liquid";
            liquid.transform.SetParent(bottleRoot.transform);
            liquid.transform.localPosition = new Vector3(0f, -0.32f, 0f);
            liquid.transform.localScale = new Vector3(0.105f, 0.02f, 0.105f);
            liquid.GetComponent<Renderer>().sharedMaterial = waterMaterial;

            var state = bottleRoot.AddComponent<BottleProcessState>();
            state.bottleRenderer = body.GetComponent<Renderer>();
            state.liquidRenderer = liquid.GetComponent<Renderer>();
            state.liquidVisual = liquid.transform;
            state.capVisual = cap.transform;
            state.capRenderer = cap.GetComponent<Renderer>();
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

        private Transform CreateFillingStarWheel(Transform parent, Material wheelMaterial, Material metalMaterial, Material pocketMaterial)
        {
            var starWheelRoot = new GameObject("Filling Star Wheel");
            starWheelRoot.transform.SetParent(parent);
            starWheelRoot.transform.position = FillingStarWheelVisualCenter;

            var rotatingAssembly = new GameObject("Scalloped Star Wheel Rotating Assembly");
            rotatingAssembly.transform.SetParent(starWheelRoot.transform);
            rotatingAssembly.transform.localPosition = ScallopedStarWheelDiscLocalPosition;

            var disc = new GameObject("Scalloped Star Wheel Disc");
            disc.transform.SetParent(rotatingAssembly.transform);
            disc.transform.localPosition = Vector3.zero;
            var meshFilter = disc.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateScallopedStarWheelMesh(FillingStarWheelPocketCount, FillingStarWheelOuterRadius, FillingStarWheelPocketNotchRadius, 0.08f, 18, FillingStarWheelEntryAngleDegrees);
            disc.AddComponent<MeshRenderer>().sharedMaterial = wheelMaterial;

            CreateCube(
                rotatingAssembly.transform,
                "Scalloped Star Wheel Rotation Marker",
                starWheelRoot.transform.position + ScallopedStarWheelDiscLocalPosition + new Vector3(0.45f, 0.055f, 0f),
                new Vector3(0.18f, 0.035f, 0.04f),
                pocketMaterial);

            var hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hub.name = "Star Wheel Center Hub";
            hub.transform.SetParent(rotatingAssembly.transform);
            hub.transform.localPosition = new Vector3(0f, 0.055f, 0f);
            hub.transform.localScale = new Vector3(0.22f, 0.055f, 0.22f);
            hub.GetComponent<Renderer>().sharedMaterial = metalMaterial;

            CreateFixedStarWheelBarrier(parent, metalMaterial);

            CreateCube(parent, "Star Wheel Outfeed Tangent Guide", new Vector3(FillingStarWheelCenterX - 0.73f, 0.9f, -0.1f), new Vector3(0.5f, 0.12f, 0.055f), metalMaterial);
            CreateStarWheelOutfeedReleaseGuide(parent, metalMaterial, pocketMaterial);
            CreateCube(parent, "Filling Star Wheel Base", new Vector3(FillingStarWheelCenterX, 0.31f, -0.68f), new Vector3(1.7617f, 0.16f, 1.7f), metalMaterial);
            return rotatingAssembly.transform;
        }

        private void CreateStarWheelOutfeedReleaseGuide(Transform parent, Material metalMaterial, Material beltMaterial)
        {
            var exitPoint = StarWheelPocketPosition(FillingStarWheelPocketCount - 1, FillingStarWheelBottleCenter.y);
            var conveyorPoint = new Vector3(0f, FillingStarWheelBottleCenter.y, exitPoint.z + 0.22f);
            var center = Vector3.Lerp(exitPoint, conveyorPoint, 0.5f);
            var length = Vector3.Distance(exitPoint, conveyorPoint);
            var yaw = Mathf.Atan2(conveyorPoint.x - exitPoint.x, conveyorPoint.z - exitPoint.z) * Mathf.Rad2Deg;

            var plate = CreateCube(parent, "Star Wheel Exit Release Plate", new Vector3(center.x, 0.49f, center.z), new Vector3(0.32f, 0.035f, length), beltMaterial);
            plate.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var leftRail = CreateCube(parent, "Star Wheel Exit Left Guide Rail", center + new Vector3(0.08f, 0.22f, -0.13f), new Vector3(0.035f, 0.08f, length), metalMaterial);
            leftRail.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var rightRail = CreateCube(parent, "Star Wheel Exit Right Guide Rail", center + new Vector3(-0.08f, 0.22f, 0.13f), new Vector3(0.035f, 0.08f, length), metalMaterial);
            rightRail.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private void CreateFixedStarWheelBarrier(Transform parent, Material material)
        {
            const int starBarrierSegments = 96;
            const float starBarrierRadius = FillingStarWheelOuterRadius + 0.045f;
            var starBarrierY = ScallopedStarWheelDiscLocalPosition.y;
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

        private (List<Transform> heads, Transform dropper, Transform sensor, List<Transform> magazineCaps) CreateCappingStation(Transform parent, Material metalMaterial, Material capMaterial, Material capTubeMaterial, Material sensorMaterial)
        {
            var heads = new List<Transform>();
            var magazineCaps = new List<Transform>();
            var capDropPosition = StarWheelPocketPosition(CapDropPocketIndex, 0f);

            CreateCube(parent, "Cap Drop Station Frame", new Vector3(capDropPosition.x, 1.25f, capDropPosition.z + 0.28f), new Vector3(0.08f, 0.9f, 0.08f), metalMaterial);

            var dropperTool = new GameObject("Star Wheel Cap Dropper Moving Tool");
            dropperTool.transform.SetParent(parent);
            dropperTool.transform.position = new Vector3(capDropPosition.x, 1.28f, capDropPosition.z);
            var slideBlock = CreateCube(dropperTool.transform, "Cap Dropper Slide Block", dropperTool.transform.position, new Vector3(0.22f, 0.1f, 0.22f), metalMaterial);
            slideBlock.transform.localPosition = Vector3.zero;
            var dropperNozzle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dropperNozzle.name = "Cap Dropper Pick Tube";
            dropperNozzle.transform.SetParent(dropperTool.transform);
            dropperNozzle.transform.localPosition = new Vector3(0f, -0.12f, 0f);
            dropperNozzle.transform.localScale = new Vector3(0.065f, 0.16f, 0.065f);
            dropperNozzle.GetComponent<Renderer>().sharedMaterial = metalMaterial;

            var capTube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            capTube.name = "Transparent Cap Magazine Tube";
            capTube.transform.SetParent(parent);
            capTube.transform.position = new Vector3(capDropPosition.x, 1.9f, capDropPosition.z);
            capTube.transform.localScale = new Vector3(0.16f, 0.42f, 0.16f);
            capTube.GetComponent<Renderer>().sharedMaterial = capTubeMaterial;

            for (var i = 0; i < 5; i++)
            {
                var capInTube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                capInTube.name = $"Cap Magazine Tube Cap {i + 1}";
                capInTube.transform.SetParent(parent);
                capInTube.transform.position = new Vector3(capDropPosition.x, 1.57f + i * 0.13f, capDropPosition.z);
                capInTube.transform.localScale = new Vector3(0.105f, 0.018f, 0.105f);
                capInTube.GetComponent<Renderer>().sharedMaterial = capMaterial;
                magazineCaps.Add(capInTube.transform);
            }

            var sensor = CreateCube(parent, "Cap Drop Sensor Beam", new Vector3(capDropPosition.x, 0.92f, capDropPosition.z), new Vector3(0.42f, 0.035f, 0.035f), sensorMaterial).transform;

            for (var i = 0; i < 3; i++)
            {
                var pocketIndex = CappingPocketStartIndex + i;
                var stationPosition = StarWheelPocketPosition(pocketIndex, 0f);
                var tightenerTool = new GameObject($"Rotary Cap Tightener Moving Tool {pocketIndex}");
                tightenerTool.transform.SetParent(parent);
                tightenerTool.transform.position = new Vector3(stationPosition.x, 1.86f, stationPosition.z);

                var tightenerMotor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tightenerMotor.name = $"Rotary Cap Tightener Fixed Motor {pocketIndex}";
                tightenerMotor.transform.SetParent(parent);
                tightenerMotor.transform.position = new Vector3(stationPosition.x, 1.88f, stationPosition.z);
                tightenerMotor.transform.localScale = new Vector3(0.16f, 0.28f, 0.16f);
                tightenerMotor.GetComponent<Renderer>().sharedMaterial = metalMaterial;

                var driveBlock = CreateCube(tightenerTool.transform, $"Rotary Cap Tightener Drive Block {pocketIndex}", Vector3.zero, new Vector3(0.28f, 0.13f, 0.28f), metalMaterial);
                driveBlock.transform.localPosition = new Vector3(0f, -0.06f, 0f);
                var rotationMarker = CreateCube(tightenerTool.transform, $"Rotary Cap Tightener Rotation Marker {pocketIndex}", Vector3.zero, new Vector3(0.06f, 0.035f, 0.12f), sensorMaterial);
                rotationMarker.transform.localPosition = new Vector3(0.14f, 0.04f, 0f);

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

        private void CreateFinishedBottleAccumulationTable(Transform parent, Material metalMaterial, Material bottleMaterial, Material waterMaterial, Material capMaterial, Material labelMaterial)
        {
            CreateCube(parent, "Finished Bottle Accumulation Table", new Vector3(1.18f, 0.34f, 4.35f), new Vector3(2.3f, 0.08f, 1.15f), metalMaterial);
            for (var i = 0; i < 14; i++)
            {
                var col = i % 7;
                var row = i / 7;
                var bottle = CreateBottleTemplate(parent, bottleMaterial, waterMaterial, capMaterial, labelMaterial);
                bottle.name = $"Finished Blue Bottle Prop {i + 1}";
                bottle.transform.position = new Vector3(0.42f + col * 0.24f, 0.82f, 4.0f + row * 0.34f);
                bottle.gameObject.SetActive(true);
                bottle.SetVolume(1f);
                bottle.capPlaced = true;
                bottle.cappingCompleted = true;
                bottle.RefreshVisuals();
            }
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

        private void Update()
        {
            if (!Application.isPlaying || movingParts.Count == 0)
            {
                return;
            }

            var axis = worldAxis.sqrMagnitude > 0.0001f ? worldAxis.normalized : Vector3.forward;
            var loopLength = Mathf.Max(0.01f, maxCoordinate - minCoordinate);
            var delta = axis * (speedMps * Time.deltaTime);

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
