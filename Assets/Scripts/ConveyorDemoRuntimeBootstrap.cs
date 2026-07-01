using System.Collections.Generic;
using UnityEngine;

namespace ConveyorTwin
{
    [ExecuteAlways]
    public class ConveyorDemoRuntimeBootstrap : MonoBehaviour
    {
        private const string GeneratedRootName = "Generated Filling Filtering Twin Demo";

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
            var sensorMaterial = CreateMaterial(new Color(0.1f, 0.75f, 1f));
            var rejectMaterial = CreateMaterial(new Color(1f, 0.35f, 0.22f));
            var acceptMaterial = CreateMaterial(new Color(0.25f, 0.9f, 0.35f));

            CreateFloor(root.transform, floorMaterial);
            CreateConveyor(root.transform, beltMaterial, metalMaterial, slatMaterial, ribMaterial);
            var turntable = CreateTurntable(root.transform, metalMaterial);
            var bottleSpawnPoint = CreateBottleDropper(root.transform, metalMaterial);
            var turntableOutlet = CreateTurntableOutlet(root.transform, metalMaterial);
            var vesselParts = CreateLiquidVessel(root.transform, metalMaterial, waterMaterial);
            var nozzles = CreateFillingNozzles(root.transform, metalMaterial, waterMaterial);
            var fillingStopGate = CreateFillingStopGate(root.transform, metalMaterial, rejectMaterial);
            var starWheel = CreateFillingStarWheel(root.transform, starWheelMaterial, metalMaterial, beltMaterial);
            var qcBeam = CreateQcSensor(root.transform, sensorMaterial, metalMaterial);
            var cappingHeads = CreateCappingStation(root.transform, metalMaterial, waterMaterial);
            var pusher = CreatePusher(root.transform, metalMaterial, rejectMaterial);
            var acceptChute = CreateChute(root.transform, "Accept Chute", new Vector3(0.95f, 0.28f, 5.45f), acceptMaterial, 18f);
            var rejectChute = CreateChute(root.transform, "Reject Chute", new Vector3(-1.15f, 0.35f, 2.45f), rejectMaterial, -25f);
            var bottleTemplate = CreateBottleTemplate(root.transform, bottleMaterial, waterMaterial);

            var processObject = new GameObject("Filling Filtering Process Controller");
            processObject.transform.SetParent(root.transform);
            var process = processObject.AddComponent<FillingFilteringDigitalTwin>();
            process.infeedTurntable = turntable;
            process.bottleSpawnPoint = bottleSpawnPoint;
            process.turntableOutlet = turntableOutlet;
            process.fillingNozzle = nozzles.Count > 0 ? nozzles[0] : null;
            process.fillingNozzles = nozzles;
            process.fillingStopGate = fillingStopGate;
            process.fillingStarWheel = starWheel;
            process.liquidVessel = vesselParts.vessel;
            process.vesselLiquidVisual = vesselParts.liquid;
            process.qcSensorBeam = qcBeam;
            process.cappingHead = cappingHeads.Count > 0 ? cappingHeads[0] : null;
            process.cappingHeads = cappingHeads;
            process.pneumaticPusher = pusher;
            process.acceptChute = acceptChute;
            process.rejectChute = rejectChute;
            process.bottleTemplate = bottleTemplate;
            process.conveyorSpeedMps = 0.85f;
            process.slatPitchM = 0.22f;
            process.conveyorSlipRatio = 0.02f;
            process.minimumBottleSpacingM = 0.46f;
            process.fillingNozzleCount = 4;
            process.fillingFirstZ = -1.95f;
            process.fillingPitchM = 0.48f;
            process.fillingQueueStopZ = -2.45f;
            process.cappingZ = 3.2f;
            process.cappingHeadCount = 4;
            process.cappingFirstZ = 3.2f;
            process.cappingPitchM = 0.48f;
            process.cappingQueueStopZ = 2.75f;
            process.cappingTimeSeconds = 0.75f;
            process.acceptEndZ = 5.25f;
            process.starWheelPocketCount = 8;
            process.starWheelAngularSpeedDps = 120f;
            process.infeedMotorSpeedRpm = 18f;
            process.fillingTimeSeconds = 1.35f;
            process.properFillProbability = 0.9f;
            process.passThreshold = 0.95f;
            process.turntableCenter = new Vector3(0f, 0.82f, -4.7f);
            process.turntableRadius = 0.95f;
            process.releaseThreshold = 7;
            process.initialTurntableBottleCount = 12;
            process.maxTurntableBuffer = 16;
            process.spawnIntervalSeconds = 0.85f;
            process.releaseIntervalSeconds = 0.62f;

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

        private void CreateConveyor(Transform parent, Material beltMaterial, Material metalMaterial, Material slatMaterial, Material ribMaterial)
        {
            CreateCube(parent, "Slat Chain Conveyor Base", new Vector3(0f, 0.38f, 0.55f), new Vector3(0.52f, 0.08f, 10.7f), beltMaterial);

            const float pitch = 0.22f;
            const float slatLength = 0.17f;
            const float startZ = -4.1f;
            const int slatCount = 48;
            for (var i = 0; i < slatCount; i++)
            {
                var z = startZ + i * pitch;
                CreateCube(parent, "Modular Slat Plate", new Vector3(0f, 0.46f, z), new Vector3(0.46f, 0.035f, slatLength), slatMaterial);
                CreateCube(parent, "Slat Gap Shadow", new Vector3(0f, 0.482f, z + slatLength * 0.5f + 0.017f), new Vector3(0.47f, 0.012f, 0.028f), beltMaterial);

                if (i % 2 == 0)
                {
                    CreateCube(parent, "Anti Slip Cross Rib", new Vector3(0f, 0.515f, z - 0.055f), new Vector3(0.42f, 0.026f, 0.022f), ribMaterial);
                }
            }

            CreateCube(parent, "Left Narrow Guide Rail", new Vector3(-0.28f, 0.74f, 0.55f), new Vector3(0.035f, 0.1f, 10.7f), metalMaterial);
            CreateCube(parent, "Right Narrow Guide Rail", new Vector3(0.28f, 0.74f, 0.55f), new Vector3(0.035f, 0.1f, 10.7f), metalMaterial);
            CreateCube(parent, "Narrow Conveyor Support", new Vector3(0f, 0.2f, 0.55f), new Vector3(0.68f, 0.15f, 10.9f), metalMaterial);
        }

        private Transform CreateTurntable(Transform parent, Material material)
        {
            var table = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            table.name = "Infeed Turntable";
            table.transform.SetParent(parent);
            table.transform.position = new Vector3(0f, 0.48f, -4.7f);
            table.transform.localScale = new Vector3(1.35f, 0.08f, 1.35f);
            table.GetComponent<Renderer>().sharedMaterial = material;

            for (var i = 0; i < 20; i++)
            {
                var angle = i * Mathf.PI * 2f / 20f;
                var rim = CreateCube(
                    parent,
                    "Turntable Safety Rim",
                    new Vector3(Mathf.Cos(angle) * 1.38f, 0.78f, -4.7f + Mathf.Sin(angle) * 1.38f),
                    new Vector3(0.16f, 0.34f, 0.08f),
                    material);
                rim.transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
            }

            CreateCube(parent, "Turntable Outlet Gate", new Vector3(0f, 0.78f, -3.45f), new Vector3(0.55f, 0.26f, 0.08f), material);
            return table.transform;
        }

        private Transform CreateBottleDropper(Transform parent, Material material)
        {
            CreateCube(parent, "Bottle Dropper Stand", new Vector3(-1.25f, 1.45f, -4.7f), new Vector3(0.08f, 1.7f, 0.08f), material);
            CreateCube(parent, "Bottle Dropper Arm", new Vector3(-0.62f, 2.25f, -4.7f), new Vector3(1.25f, 0.08f, 0.08f), material);

            var spawn = new GameObject("Bottle Spawn Point");
            spawn.transform.SetParent(parent);
            spawn.transform.position = new Vector3(0f, 2.45f, -4.7f);
            return spawn.transform;
        }

        private Transform CreateTurntableOutlet(Transform parent, Material material)
        {
            CreateCube(parent, "Turntable Outlet Guide Left", new Vector3(-0.32f, 0.72f, -3.7f), new Vector3(0.06f, 0.18f, 0.9f), material);
            CreateCube(parent, "Turntable Outlet Guide Right", new Vector3(0.32f, 0.72f, -3.7f), new Vector3(0.06f, 0.18f, 0.9f), material);

            var outlet = new GameObject("Turntable Outlet");
            outlet.transform.SetParent(parent);
            outlet.transform.position = new Vector3(0f, 0.82f, -4.15f);
            return outlet.transform;
        }

        private (Transform vessel, Transform liquid) CreateLiquidVessel(Transform parent, Material metalMaterial, Material waterMaterial)
        {
            var vessel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            vessel.name = "Liquid Vessel";
            vessel.transform.SetParent(parent);
            vessel.transform.position = new Vector3(-1.7f, 1.8f, -1.55f);
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

        private List<Transform> CreateFillingNozzles(Transform parent, Material metalMaterial, Material waterMaterial)
        {
            var nozzles = new List<Transform>();
            CreateCube(parent, "Filling Nozzle Main Rail", new Vector3(-0.54f, 1.45f, -1.23f), new Vector3(1.1f, 0.08f, 2.1f), metalMaterial);

            const int nozzleCount = 4;
            const float firstZ = -1.95f;
            const float pitch = 0.48f;
            for (var i = 0; i < nozzleCount; i++)
            {
                var z = firstZ + i * pitch;
                CreateCube(parent, $"Nozzle Spring {i + 1}", new Vector3(0f, 1.34f, z), new Vector3(0.08f, 0.18f, 0.08f), metalMaterial);

                var nozzle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                nozzle.name = $"Filling Nozzle {i + 1}";
                nozzle.transform.SetParent(parent);
                nozzle.transform.position = new Vector3(0f, 1.08f, z);
                nozzle.transform.localScale = new Vector3(0.07f, 0.32f, 0.07f);
                nozzle.GetComponent<Renderer>().sharedMaterial = metalMaterial;

                var flow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                flow.name = $"Liquid Flow Visual {i + 1}";
                flow.transform.SetParent(nozzle.transform);
                flow.transform.localPosition = new Vector3(0f, -0.7f, 0f);
                flow.transform.localScale = new Vector3(0.22f, 0.55f, 0.22f);
                flow.GetComponent<Renderer>().sharedMaterial = waterMaterial;

                nozzles.Add(nozzle.transform);
            }

            return nozzles;
        }

        private Transform CreateFillingStopGate(Transform parent, Material metalMaterial, Material gateMaterial)
        {
            CreateCube(parent, "Filling Gate Frame Left", new Vector3(-0.35f, 0.96f, -2.45f), new Vector3(0.05f, 0.6f, 0.05f), metalMaterial);
            CreateCube(parent, "Filling Gate Frame Right", new Vector3(0.35f, 0.96f, -2.45f), new Vector3(0.05f, 0.6f, 0.05f), metalMaterial);
            return CreateCube(parent, "Filling Stop Gate", new Vector3(0f, 1.1f, -2.45f), new Vector3(0.58f, 0.16f, 0.06f), gateMaterial).transform;
        }

        private Transform CreateQcSensor(Transform parent, Material sensorMaterial, Material metalMaterial)
        {
            CreateCube(parent, "QC Sensor Head Left", new Vector3(-0.46f, 0.95f, 0.85f), new Vector3(0.16f, 0.28f, 0.16f), metalMaterial);
            CreateCube(parent, "QC Sensor Head Right", new Vector3(0.46f, 0.95f, 0.85f), new Vector3(0.16f, 0.28f, 0.16f), metalMaterial);
            return CreateCube(parent, "QC Sensor Beam", new Vector3(0f, 0.92f, 0.85f), new Vector3(0.86f, 0.035f, 0.035f), sensorMaterial).transform;
        }

        private Transform CreatePusher(Transform parent, Material metalMaterial, Material rejectMaterial)
        {
            CreateCube(parent, "Pneumatic Cylinder Body", new Vector3(0.72f, 0.78f, 2.25f), new Vector3(0.36f, 0.22f, 0.22f), metalMaterial);
            return CreateCube(parent, "Pneumatic Pusher", new Vector3(0.43f, 0.78f, 2.25f), new Vector3(0.1f, 0.32f, 0.42f), rejectMaterial).transform;
        }

        private Transform CreateChute(Transform parent, string name, Vector3 position, Material material, float zRotation)
        {
            var chute = CreateCube(parent, name, position, new Vector3(0.9f, 0.08f, 1.35f), material);
            chute.transform.rotation = Quaternion.Euler(0f, 0f, zRotation);
            return chute.transform;
        }

        private BottleProcessState CreateBottleTemplate(Transform parent, Material bottleMaterial, Material waterMaterial)
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
            neck.transform.localPosition = new Vector3(0f, 0.47f, 0f);
            neck.transform.localScale = new Vector3(0.07f, 0.16f, 0.07f);
            neck.GetComponent<Renderer>().sharedMaterial = bottleMaterial;

            var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cap.name = "Bottle Cap";
            cap.transform.SetParent(bottleRoot.transform);
            cap.transform.localPosition = new Vector3(0f, 0.67f, 0f);
            cap.transform.localScale = new Vector3(0.065f, 0.05f, 0.065f);
            cap.GetComponent<Renderer>().sharedMaterial = waterMaterial;
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
            hud.size = new Vector2(580f, 325f);
        }

        private Transform CreateFillingStarWheel(Transform parent, Material wheelMaterial, Material metalMaterial, Material pocketMaterial)
        {
            var starWheelRoot = new GameObject("Filling Star Wheel");
            starWheelRoot.transform.SetParent(parent);
            starWheelRoot.transform.position = new Vector3(0.58f, 0.72f, -1.23f);

            var disc = new GameObject("Scalloped Star Wheel Disc");
            disc.transform.SetParent(starWheelRoot.transform);
            disc.transform.localPosition = Vector3.zero;
            var meshFilter = disc.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateScallopedStarWheelMesh(16, 1.02f, 0.22f, 0.08f, 10);
            disc.AddComponent<MeshRenderer>().sharedMaterial = wheelMaterial;

            var hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hub.name = "Star Wheel Center Hub";
            hub.transform.SetParent(starWheelRoot.transform);
            hub.transform.localPosition = new Vector3(0f, 0.055f, 0f);
            hub.transform.localScale = new Vector3(0.14f, 0.055f, 0.14f);
            hub.GetComponent<Renderer>().sharedMaterial = metalMaterial;

            const int pockets = 16;
            for (var i = 0; i < pockets; i++)
            {
                var angle = i * Mathf.PI * 2f / pockets;
                var pocket = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pocket.name = $"Star Wheel Rim Pocket Shadow {i + 1}";
                pocket.transform.SetParent(starWheelRoot.transform);
                pocket.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.88f, 0.065f, Mathf.Sin(angle) * 0.88f);
                pocket.transform.localScale = new Vector3(0.115f, 0.012f, 0.115f);
                pocket.GetComponent<Renderer>().sharedMaterial = pocketMaterial;

                var divider = CreateCube(
                    starWheelRoot.transform,
                    $"Star Wheel Pocket Tooth {i + 1}",
                    starWheelRoot.transform.position + new Vector3(Mathf.Cos(angle + 0.09f) * 0.78f, 0.075f, Mathf.Sin(angle + 0.09f) * 0.78f),
                    new Vector3(0.05f, 0.045f, 0.18f),
                    wheelMaterial);
                divider.transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
            }

            const float firstZ = -1.95f;
            const float pitch = 0.48f;
            for (var i = 0; i < 4; i++)
            {
                var z = firstZ + i * pitch;
                var localPocketPosition = new Vector3(-0.58f, 0.072f, z + 1.23f);

                var socket = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                socket.name = $"Indexed Bottle Pocket Socket {i + 1}";
                socket.transform.SetParent(starWheelRoot.transform);
                socket.transform.localPosition = localPocketPosition;
                socket.transform.localScale = new Vector3(0.16f, 0.014f, 0.16f);
                socket.GetComponent<Renderer>().sharedMaterial = pocketMaterial;

                CreateCube(starWheelRoot.transform, $"Star Wheel Pocket Jaw A {i + 1}", starWheelRoot.transform.position + localPocketPosition + new Vector3(-0.11f, 0.04f, -0.15f), new Vector3(0.24f, 0.07f, 0.055f), wheelMaterial);
                CreateCube(starWheelRoot.transform, $"Star Wheel Pocket Jaw B {i + 1}", starWheelRoot.transform.position + localPocketPosition + new Vector3(-0.11f, 0.04f, 0.15f), new Vector3(0.24f, 0.07f, 0.055f), wheelMaterial);
                CreateCube(parent, $"Fixed Star Wheel Back Guide {i + 1}", new Vector3(-0.27f, 0.82f, z), new Vector3(0.055f, 0.24f, 0.24f), metalMaterial);
            }

            CreateCube(parent, "Star Wheel Guard Rail", new Vector3(-0.23f, 0.9f, -1.23f), new Vector3(0.05f, 0.18f, 1.75f), metalMaterial);
            return starWheelRoot.transform;
        }

        private Mesh CreateScallopedStarWheelMesh(int pocketCount, float outerRadius, float pocketDepth, float thickness, int samplesPerPocket)
        {
            var ringCount = pocketCount * samplesPerPocket;
            var vertices = new Vector3[2 + ringCount * 2];
            var triangles = new List<int>(ringCount * 12);
            var pocketAngle = Mathf.PI * 2f / pocketCount;
            var halfPocketWidth = pocketAngle * 0.36f;

            vertices[0] = new Vector3(0f, thickness * 0.5f, 0f);
            vertices[1] = new Vector3(0f, -thickness * 0.5f, 0f);

            for (var i = 0; i < ringCount; i++)
            {
                var angle = i * Mathf.PI * 2f / ringCount;
                var centered = Mathf.Repeat(angle + pocketAngle * 0.5f, pocketAngle) - pocketAngle * 0.5f;
                var pocketRatio = Mathf.Clamp01(Mathf.Abs(centered) / halfPocketWidth);
                var cutAmount = Mathf.Cos(pocketRatio * Mathf.PI * 0.5f);
                var radius = outerRadius - pocketDepth * cutAmount * cutAmount;
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

        private List<Transform> CreateCappingStation(Transform parent, Material metalMaterial, Material capMaterial)
        {
            var heads = new List<Transform>();
            const int headCount = 4;
            const float firstZ = 3.2f;
            const float pitch = 0.48f;
            const float centerZ = firstZ + pitch * 1.5f;

            CreateCube(parent, "Capping Station Frame Left", new Vector3(-0.42f, 1.24f, centerZ), new Vector3(0.08f, 0.9f, 2.15f), metalMaterial);
            CreateCube(parent, "Capping Station Frame Right", new Vector3(0.42f, 1.24f, centerZ), new Vector3(0.08f, 0.9f, 2.15f), metalMaterial);
            CreateCube(parent, "Capping Head Rail", new Vector3(0f, 1.48f, centerZ), new Vector3(0.72f, 0.08f, 2.1f), metalMaterial);

            for (var i = 0; i < headCount; i++)
            {
                var z = firstZ + i * pitch;
                CreateCube(parent, $"Capping Spring {i + 1}", new Vector3(0f, 1.34f, z), new Vector3(0.08f, 0.18f, 0.08f), metalMaterial);

                var head = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                head.name = $"Capping Head {i + 1}";
                head.transform.SetParent(parent);
                head.transform.position = new Vector3(0f, 1.1f, z);
                head.transform.localScale = new Vector3(0.1f, 0.18f, 0.1f);
                head.GetComponent<Renderer>().sharedMaterial = metalMaterial;
                heads.Add(head.transform);

                var capReady = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                capReady.name = $"Queued Cap {i + 1}";
                capReady.transform.SetParent(parent);
                capReady.transform.position = new Vector3(0f, 0.98f, z);
                capReady.transform.localScale = new Vector3(0.07f, 0.018f, 0.07f);
                capReady.GetComponent<Renderer>().sharedMaterial = capMaterial;

                CreateCube(parent, $"Capping Bottle Stop {i + 1}", new Vector3(-0.22f, 0.78f, z), new Vector3(0.07f, 0.18f, 0.12f), metalMaterial);
            }

            var capFeed = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            capFeed.name = "Cap Feeder Bowl";
            capFeed.transform.SetParent(parent);
            capFeed.transform.position = new Vector3(0.82f, 1.2f, centerZ);
            capFeed.transform.localScale = new Vector3(0.32f, 0.12f, 0.32f);
            capFeed.GetComponent<Renderer>().sharedMaterial = capMaterial;

            CreateCube(parent, "Cap Feed Rail", new Vector3(0.4f, 1.28f, centerZ), new Vector3(0.75f, 0.04f, 2f), metalMaterial);

            return heads;
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

            var light = FindFirstObjectByType<Light>();
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
}
