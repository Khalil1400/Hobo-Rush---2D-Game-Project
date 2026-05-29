using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class HoboRushAutoRebuild
{
    private const string RequestPath = "Temp/HoboRushBuild/rebuild.request";

    static HoboRushAutoRebuild()
    {
        QueueTryRun();
        EditorApplication.update += TryRunOnUpdate;
        EditorApplication.projectChanged += QueueTryRun;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            QueueTryRun();
        }
    }

    private static void QueueTryRun()
    {
        EditorApplication.delayCall -= TryRun;
        EditorApplication.delayCall += TryRun;
    }

    private static void TryRunOnUpdate()
    {
        if (File.Exists(RequestPath))
        {
            QueueTryRun();
        }
    }

    private static void TryRun()
    {
        if (!File.Exists(RequestPath))
        {
            return;
        }

        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += TryRun;
            return;
        }

        try
        {
            File.Delete(RequestPath);
            HoboRushSceneBuilder.BuildGameScene();
            HoboRushSceneBuilder.ValidateGameScene();
            Debug.Log("Hobo Rush auto rebuild completed.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}
