using ConveyorTwin;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ConveyorDemoSceneBuilder
{
    private const string BootstrapName = "Filling Filtering Demo Bootstrap";

    [MenuItem("Tools/Conveyor Twin/Build Demo Scene")]
    public static void BuildDemoScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        }

        var bootstrapObject = GameObject.Find(BootstrapName);
        if (bootstrapObject == null)
        {
            bootstrapObject = new GameObject(BootstrapName);
        }

        var bootstrap = bootstrapObject.GetComponent<ConveyorDemoRuntimeBootstrap>();
        if (bootstrap == null)
        {
            bootstrap = bootstrapObject.AddComponent<ConveyorDemoRuntimeBootstrap>();
        }

        bootstrap.rebuildOnEnable = false;
        bootstrap.BuildDemo();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/SampleScene.unity");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
