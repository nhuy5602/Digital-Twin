using ConveyorTwin;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ConveyorDemoSceneBuilder
{
    private const string DemoRootName = "Conveyor Twin Demo";
    private const string AutoBuildSessionKey = "ConveyorTwin.AutoBuildDemoScene";

    [InitializeOnLoadMethod]
    private static void AutoBuildDemoSceneOnce()
    {
        if (SessionState.GetBool(AutoBuildSessionKey, false))
        {
            return;
        }

        SessionState.SetBool(AutoBuildSessionKey, true);
        EditorApplication.delayCall += () =>
        {
            if (Application.isBatchMode)
            {
                return;
            }

            if (GameObject.Find(DemoRootName) != null)
            {
                return;
            }

            BuildDemoScene();
        };
    }

    [MenuItem("Tools/Conveyor Twin/Build Demo Scene")]
    public static void BuildDemoScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        }

        RemoveExistingDemoRoot();
        RemoveLegacyManualObjects();

        var root = new GameObject(DemoRootName);

        var floorMaterial = CreateMaterial("MAT_Floor", new Color(0.42f, 0.42f, 0.40f));
        var beltMaterial = CreateMaterial("MAT_Belt_Rubber", new Color(0.03f, 0.035f, 0.04f));
        var metalMaterial = CreateMaterial("MAT_Metal", new Color(0.55f, 0.57f, 0.58f));
        var packageMaterial = CreateMaterial("MAT_Package", new Color(0.95f, 0.62f, 0.24f));
        var sensorMaterial = CreateMaterial("MAT_Sensor", new Color(0.1f, 0.75f, 1f));

        CreateFloor(root.transform, floorMaterial);
        var telemetrySource = CreateTelemetrySource(root.transform);
        var belt = CreateConveyor(root.transform, beltMaterial, telemetrySource);
        CreatePulleysAndSupports(root.transform, metalMaterial);
        CreatePackages(root.transform, packageMaterial, belt);
        CreateSensors(root.transform, sensorMaterial, belt);
        CreateHud(root.transform, belt);
        ConfigureCameraAndLight();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/SampleScene.unity");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void RemoveExistingDemoRoot()
    {
        var existing = GameObject.Find(DemoRootName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
    }

    private static void RemoveLegacyManualObjects()
    {
        RemoveIfExists("ConveyorBelt");
        RemoveIfExists("TelemetrySource");
        RemoveIfExists("HUD");
    }

    private static void RemoveIfExists(string objectName)
    {
        var existing = GameObject.Find(objectName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
    }

    private static Material CreateMaterial(string name, Color color)
    {
        var path = $"Assets/Materials/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

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

    private static void CreateFloor(Transform parent, Material material)
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Factory Floor";
        floor.transform.SetParent(parent);
        floor.transform.position = new Vector3(0f, -0.05f, 0f);
        floor.transform.localScale = new Vector3(8f, 0.1f, 9f);
        floor.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static TwinTelemetrySimulator CreateTelemetrySource(Transform parent)
    {
        var source = new GameObject("Telemetry Source - Digital Shadow");
        source.transform.SetParent(parent);
        var telemetry = source.AddComponent<TwinTelemetrySimulator>();
        telemetry.baseSpeedMps = 1.2f;
        telemetry.baseLoadKg = 34f;
        telemetry.basePackagesPerMinute = 70f;
        telemetry.overloadStartSeconds = 35f;
        telemetry.overloadDurationSeconds = 12f;
        return telemetry;
    }

    private static ConveyorBeltDigitalTwin CreateConveyor(Transform parent, Material material, TwinTelemetrySimulator telemetry)
    {
        var beltObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beltObject.name = "Conveyor Belt - Digital Shadow";
        beltObject.transform.SetParent(parent);
        beltObject.transform.position = new Vector3(0f, 0.55f, 0f);
        beltObject.transform.localScale = new Vector3(0.8f, 0.12f, 6f);
        beltObject.GetComponent<Renderer>().sharedMaterial = material;

        var belt = beltObject.AddComponent<ConveyorBeltDigitalTwin>();
        belt.mode = TwinMode.DigitalShadow;
        belt.telemetrySource = telemetry;
        belt.beltSurface = beltObject.transform;
        belt.beltRenderer = beltObject.GetComponent<Renderer>();
        belt.textureProperty = "_BaseMap";
        belt.beltLengthM = 6f;
        belt.beltWidthM = 0.8f;
        belt.pulleyRadiusM = 0.18f;
        belt.maxSafeLoadKg = 120f;
        belt.maxSafeSpeedMps = 2.5f;
        return belt;
    }

    private static void CreatePulleysAndSupports(Transform parent, Material material)
    {
        CreatePulley(parent, "Input Pulley", new Vector3(0f, 0.55f, -3.1f), material);
        CreatePulley(parent, "Output Pulley", new Vector3(0f, 0.55f, 3.1f), material);

        CreateSupport(parent, "Left Support Rail", new Vector3(-0.55f, 0.32f, 0f), new Vector3(0.08f, 0.12f, 6.2f), material);
        CreateSupport(parent, "Right Support Rail", new Vector3(0.55f, 0.32f, 0f), new Vector3(0.08f, 0.12f, 6.2f), material);

        CreateSupport(parent, "Leg Front Left", new Vector3(-0.55f, 0.15f, -2.6f), new Vector3(0.08f, 0.4f, 0.08f), material);
        CreateSupport(parent, "Leg Front Right", new Vector3(0.55f, 0.15f, -2.6f), new Vector3(0.08f, 0.4f, 0.08f), material);
        CreateSupport(parent, "Leg Back Left", new Vector3(-0.55f, 0.15f, 2.6f), new Vector3(0.08f, 0.4f, 0.08f), material);
        CreateSupport(parent, "Leg Back Right", new Vector3(0.55f, 0.15f, 2.6f), new Vector3(0.08f, 0.4f, 0.08f), material);
    }

    private static void CreatePulley(Transform parent, string name, Vector3 position, Material material)
    {
        var pulley = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pulley.name = name;
        pulley.transform.SetParent(parent);
        pulley.transform.position = position;
        pulley.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        pulley.transform.localScale = new Vector3(0.18f, 0.95f, 0.18f);
        pulley.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void CreateSupport(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        var support = GameObject.CreatePrimitive(PrimitiveType.Cube);
        support.name = name;
        support.transform.SetParent(parent);
        support.transform.position = position;
        support.transform.localScale = scale;
        support.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void CreatePackages(Transform parent, Material material, ConveyorBeltDigitalTwin belt)
    {
        for (var i = 0; i < 5; i++)
        {
            var packageObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            packageObject.name = $"Package {i + 1:00}";
            packageObject.transform.SetParent(parent);
            packageObject.transform.position = new Vector3(0f, 0.86f, -2.35f + i * 1.1f);
            packageObject.transform.localScale = new Vector3(0.45f, 0.35f, 0.45f);
            packageObject.GetComponent<Renderer>().sharedMaterial = material;

            var body = packageObject.AddComponent<Rigidbody>();
            body.mass = 4f + i;
            body.isKinematic = true;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var package = packageObject.AddComponent<ConveyorPackage>();
            package.massKg = body.mass;
            package.belt = belt;
            package.forceKinematicMotion = true;
            package.loopOnBelt = true;
            package.startZ = -2.75f;
            package.endZ = 2.75f;
            package.laneX = 0f;
            package.rideHeightY = 0.86f;
        }
    }

    private static void CreateSensors(Transform parent, Material material, ConveyorBeltDigitalTwin belt)
    {
        CreateSensor(parent, "Input Sensor", new Vector3(0f, 0.95f, -2.8f), material, belt);
        CreateSensor(parent, "Output Sensor", new Vector3(0f, 0.95f, 2.8f), material, belt);
    }

    private static void CreateSensor(Transform parent, string name, Vector3 position, Material material, ConveyorBeltDigitalTwin belt)
    {
        var sensor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sensor.name = name;
        sensor.transform.SetParent(parent);
        sensor.transform.position = position;
        sensor.transform.localScale = new Vector3(1f, 0.05f, 0.08f);
        sensor.GetComponent<Renderer>().sharedMaterial = material;

        var collider = sensor.GetComponent<BoxCollider>();
        collider.isTrigger = true;

        var script = sensor.AddComponent<ConveyorSensor>();
        script.sensorId = name.Contains("Input") ? "S-IN-01" : "S-OUT-01";
        script.belt = belt;
    }

    private static void CreateHud(Transform parent, ConveyorBeltDigitalTwin belt)
    {
        var hudObject = new GameObject("HUD - Twin Metrics");
        hudObject.transform.SetParent(parent);
        var hud = hudObject.AddComponent<TwinMetricsHud>();
        hud.belt = belt;
        hud.position = new Vector2(16f, 16f);
        hud.size = new Vector2(500f, 235f);
    }

    private static void ConfigureCameraAndLight()
    {
        var camera = Camera.main;
        if (camera == null)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        camera.transform.position = new Vector3(3.8f, 3.1f, -6f);
        camera.transform.rotation = Quaternion.Euler(28f, -34f, 0f);
        camera.fieldOfView = 55f;

        var light = Object.FindFirstObjectByType<Light>();
        if (light == null)
        {
            var lightObject = new GameObject("Directional Light");
            light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
        }

        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        light.intensity = 1.4f;
    }
}
