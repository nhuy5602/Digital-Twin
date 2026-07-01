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
            var bottleMaterial = CreateMaterial(new Color(0.82f, 0.95f, 1f, 0.35f));
            var waterMaterial = CreateMaterial(new Color(0.1f, 0.55f, 1f, 0.85f));
            var sensorMaterial = CreateMaterial(new Color(0.1f, 0.75f, 1f));
            var rejectMaterial = CreateMaterial(new Color(1f, 0.35f, 0.22f));
            var acceptMaterial = CreateMaterial(new Color(0.25f, 0.9f, 0.35f));

            CreateFloor(root.transform, floorMaterial);
            CreateConveyor(root.transform, beltMaterial, metalMaterial);
            var turntable = CreateTurntable(root.transform, metalMaterial);
            var bottleSpawnPoint = CreateBottleDropper(root.transform, metalMaterial);
            var turntableOutlet = CreateTurntableOutlet(root.transform, metalMaterial);
            var vesselParts = CreateLiquidVessel(root.transform, metalMaterial, waterMaterial);
            var nozzle = CreateFillingNozzle(root.transform, metalMaterial, waterMaterial);
            var qcBeam = CreateQcSensor(root.transform, sensorMaterial, metalMaterial);
            var pusher = CreatePusher(root.transform, metalMaterial, rejectMaterial);
            var acceptChute = CreateChute(root.transform, "Accept Chute", new Vector3(0.95f, 0.28f, 4.4f), acceptMaterial, 18f);
            var rejectChute = CreateChute(root.transform, "Reject Chute", new Vector3(-1.15f, 0.35f, 2.45f), rejectMaterial, -25f);
            var bottleTemplate = CreateBottleTemplate(root.transform, bottleMaterial, waterMaterial);

            var processObject = new GameObject("Filling Filtering Process Controller");
            processObject.transform.SetParent(root.transform);
            var process = processObject.AddComponent<FillingFilteringDigitalTwin>();
            process.infeedTurntable = turntable;
            process.bottleSpawnPoint = bottleSpawnPoint;
            process.turntableOutlet = turntableOutlet;
            process.fillingNozzle = nozzle;
            process.liquidVessel = vesselParts.vessel;
            process.vesselLiquidVisual = vesselParts.liquid;
            process.qcSensorBeam = qcBeam;
            process.pneumaticPusher = pusher;
            process.acceptChute = acceptChute;
            process.rejectChute = rejectChute;
            process.bottleTemplate = bottleTemplate;
            process.conveyorSpeedMps = 0.85f;
            process.infeedMotorSpeedRpm = 18f;
            process.fillingTimeSeconds = 1.35f;
            process.properFillProbability = 0.9f;
            process.passThreshold = 0.95f;
            process.turntableCenter = new Vector3(0f, 0.82f, -4.7f);
            process.turntableRadius = 0.95f;
            process.releaseThreshold = 7;
            process.maxTurntableBuffer = 16;
            process.spawnIntervalSeconds = 0.42f;
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
            floor.transform.position = new Vector3(0f, -0.05f, 0f);
            floor.transform.localScale = new Vector3(8f, 0.1f, 10f);
            floor.GetComponent<Renderer>().sharedMaterial = material;
        }

        private void CreateConveyor(Transform parent, Material beltMaterial, Material metalMaterial)
        {
            var belt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            belt.name = "Bottle Conveyor";
            belt.transform.SetParent(parent);
            belt.transform.position = new Vector3(0f, 0.42f, 0f);
            belt.transform.localScale = new Vector3(0.75f, 0.1f, 8.5f);
            belt.GetComponent<Renderer>().sharedMaterial = beltMaterial;

            CreateCube(parent, "Left Guide Rail", new Vector3(-0.48f, 0.74f, 0f), new Vector3(0.06f, 0.08f, 8.5f), metalMaterial);
            CreateCube(parent, "Right Guide Rail", new Vector3(0.48f, 0.74f, 0f), new Vector3(0.06f, 0.08f, 8.5f), metalMaterial);
            CreateCube(parent, "Conveyor Support", new Vector3(0f, 0.2f, 0f), new Vector3(1.1f, 0.15f, 8.7f), metalMaterial);
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

        private Transform CreateFillingNozzle(Transform parent, Material metalMaterial, Material waterMaterial)
        {
            CreateCube(parent, "Nozzle Support Arm", new Vector3(-0.6f, 1.45f, -1.65f), new Vector3(1.2f, 0.08f, 0.08f), metalMaterial);

            var nozzle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            nozzle.name = "Filling Nozzle";
            nozzle.transform.SetParent(parent);
            nozzle.transform.position = new Vector3(0f, 1.08f, -1.65f);
            nozzle.transform.localScale = new Vector3(0.08f, 0.32f, 0.08f);
            nozzle.GetComponent<Renderer>().sharedMaterial = metalMaterial;

            var flow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flow.name = "Liquid Flow Visual";
            flow.transform.SetParent(nozzle.transform);
            flow.transform.localPosition = new Vector3(0f, -0.7f, 0f);
            flow.transform.localScale = new Vector3(0.25f, 0.55f, 0.25f);
            flow.GetComponent<Renderer>().sharedMaterial = waterMaterial;

            return nozzle.transform;
        }

        private Transform CreateQcSensor(Transform parent, Material sensorMaterial, Material metalMaterial)
        {
            CreateCube(parent, "QC Sensor Head Left", new Vector3(-0.72f, 0.95f, 0.85f), new Vector3(0.18f, 0.28f, 0.18f), metalMaterial);
            CreateCube(parent, "QC Sensor Head Right", new Vector3(0.72f, 0.95f, 0.85f), new Vector3(0.18f, 0.28f, 0.18f), metalMaterial);
            return CreateCube(parent, "QC Sensor Beam", new Vector3(0f, 0.92f, 0.85f), new Vector3(1.3f, 0.035f, 0.035f), sensorMaterial).transform;
        }

        private Transform CreatePusher(Transform parent, Material metalMaterial, Material rejectMaterial)
        {
            CreateCube(parent, "Pneumatic Cylinder Body", new Vector3(0.9f, 0.78f, 2.25f), new Vector3(0.42f, 0.22f, 0.22f), metalMaterial);
            return CreateCube(parent, "Pneumatic Pusher", new Vector3(0.55f, 0.78f, 2.25f), new Vector3(0.12f, 0.32f, 0.48f), rejectMaterial).transform;
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
            body.transform.localScale = new Vector3(0.18f, 0.42f, 0.18f);
            body.GetComponent<Renderer>().sharedMaterial = bottleMaterial;

            var neck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            neck.name = "Bottle Neck";
            neck.transform.SetParent(bottleRoot.transform);
            neck.transform.localPosition = new Vector3(0f, 0.47f, 0f);
            neck.transform.localScale = new Vector3(0.09f, 0.16f, 0.09f);
            neck.GetComponent<Renderer>().sharedMaterial = bottleMaterial;

            var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cap.name = "Bottle Cap";
            cap.transform.SetParent(bottleRoot.transform);
            cap.transform.localPosition = new Vector3(0f, 0.67f, 0f);
            cap.transform.localScale = new Vector3(0.08f, 0.05f, 0.08f);
            cap.GetComponent<Renderer>().sharedMaterial = waterMaterial;

            var liquid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            liquid.name = "Bottle Liquid";
            liquid.transform.SetParent(bottleRoot.transform);
            liquid.transform.localPosition = new Vector3(0f, -0.32f, 0f);
            liquid.transform.localScale = new Vector3(0.14f, 0.02f, 0.14f);
            liquid.GetComponent<Renderer>().sharedMaterial = waterMaterial;

            var state = bottleRoot.AddComponent<BottleProcessState>();
            state.bottleRenderer = body.GetComponent<Renderer>();
            state.liquidRenderer = liquid.GetComponent<Renderer>();
            state.liquidVisual = liquid.transform;
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
            hud.size = new Vector2(560f, 300f);
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

            camera.transform.position = new Vector3(4.3f, 3.5f, -6.8f);
            camera.transform.rotation = Quaternion.Euler(29f, -33f, 0f);
            camera.fieldOfView = 56f;

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
