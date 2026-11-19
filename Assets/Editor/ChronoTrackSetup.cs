using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// On first open: creates Assets/Scenes/Main.unity, adds it to the build,
// and makes sure the shaders used at runtime are included in builds.
[InitializeOnLoad]
public static class ChronoTrackSetup
{
    const string ScenePath = "Assets/Scenes/Main.unity";

    static ChronoTrackSetup()
    {
        EditorApplication.delayCall += Run;
    }

    [MenuItem("ChronoTrack/Setup Project")]
    static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        try
        {
            EnsureScene();
            EnsureShaders();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("ChronoTrack setup: " + e.Message);
        }
    }

    static void EnsureScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return;
        if (EditorBuildSettings.scenes.Length > 0) return;
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.productName = "ChronoTrack";
        Debug.Log("ChronoTrack: created " + ScenePath + ". Press Play!");
    }

    static void EnsureShaders()
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
        if (assets == null || assets.Length == 0) return;
        SerializedObject so = new SerializedObject(assets[0]);
        SerializedProperty arr = so.FindProperty("m_AlwaysIncludedShaders");
        if (arr == null) return;
        bool changed = false;
        string[] names = { "Standard", "Universal Render Pipeline/Lit", "Unlit/Color", "Sprites/Default" };
        foreach (string n in names)
        {
            Shader sh = Shader.Find(n);
            if (sh == null) continue;
            bool has = false;
            for (int i = 0; i < arr.arraySize; i++)
                if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) { has = true; break; }
            if (has) continue;
            arr.arraySize++;
            arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh;
            changed = true;
        }
        if (changed) { so.ApplyModifiedProperties(); AssetDatabase.SaveAssets(); }
    }
}
