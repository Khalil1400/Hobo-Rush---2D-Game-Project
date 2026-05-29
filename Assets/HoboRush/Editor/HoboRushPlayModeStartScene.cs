using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class HoboRushPlayModeStartScene
{
    private const string MainMenuScenePath = "Assets/HoboRush/Scenes/MainMenu.unity";

    static HoboRushPlayModeStartScene()
    {
        EditorApplication.delayCall += Configure;
    }

    private static void Configure()
    {
        SceneAsset mainMenuScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
        if (mainMenuScene != null)
        {
            EditorSceneManager.playModeStartScene = mainMenuScene;
        }
    }
}
