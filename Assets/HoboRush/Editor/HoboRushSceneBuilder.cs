using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using UnityEngine.InputSystem.UI;

public static class HoboRushSceneBuilder
{
    private const string RootFolder = "Assets/HoboRush";
    private const string KenneyFolder = RootFolder + "/Art/KenneyPixelPlatformer";
    private const string TileFolder = KenneyFolder + "/Tiles";
    private const string CharacterFolder = TileFolder + "/Characters";
    private const string BackgroundFolder = TileFolder + "/Backgrounds";
    private const string RunnerCharacterFolder = RootFolder + "/Art/RunnerCharacter";
    private const string PixelUiFolder = RootFolder + "/Art/KenneyPixelUI";
    private const string AudioFolder = RootFolder + "/Audio";
    private const string FontFolder = RootFolder + "/Fonts";
    private const string UiFontPath = FontFolder + "/PressStart2P-Regular.ttf";
    private const string PrefabFolder = RootFolder + "/Prefabs";
    private const string DataFolder = RootFolder + "/Data";
    private const string ObstacleDataFolder = DataFolder + "/Obstacles";
    private const string DifficultyDataFolder = DataFolder + "/Difficulty";
    private const string SceneFolder = RootFolder + "/Scenes";
    private const string ScenePath = SceneFolder + "/HoboRush.unity";
    private const string MainMenuScenePath = SceneFolder + "/MainMenu.unity";

    private const float GroundTopY = -2.25f;
    private const float TileWorldSize = 1f;
    private const float SegmentWidth = 16f;

    [MenuItem("Tools/Hobo Rush/Rebuild Complete Runner Scene")]
    public static void BuildGameScene()
    {
        ConfigureProjectIdentity();
        EnsureFolders();
        EnsureKenneyAssetsPresent();
        ImportKenneySprites();
        DeleteLegacyAssets();

        int groundLayer = EnsureLayer("Ground");
        int playerLayer = EnsureLayer("Player");
        int obstacleLayer = EnsureLayer("Obstacle");

        Sprite square = CreateSquareSprite();
        Sprite dirtScatter = LoadSprite(RootFolder + "/Art/DirtFleckScatter.png");
        Sprite spike = Tile(68);
        Sprite crate = Tile(47);
        Sprite[] batFrames = { Character(24), Character(25), Character(26), Character(25) };
        Sprite[] runFrames = { RunnerCharacter("HelmetlessRunner_Run1"), RunnerCharacter("HelmetlessRunner_Run2") };
        Sprite[] duckFrames = { RunnerCharacter("HelmetlessRunner_Duck") };
        Sprite jumpFrame = RunnerCharacter("HelmetlessRunner_Run1");
        Sprite deadFrame = RunnerCharacter("HelmetlessRunner_Duck");

        GameObject spikePrefab = CreateObstaclePrefab("SpikeClusterObstacle", spike, new Vector2(0.86f, 0.34f), new Vector2(0f, -0.28f), Vector3.one * 1.2f, obstacleLayer);
        GameObject spikeRowPrefab = CreateObstaclePrefab("SpikeRowObstacle", spike, new Vector2(0.86f, 0.34f), new Vector2(0f, -0.28f), Vector3.one * 1.28f, obstacleLayer);
        GameObject cratePrefab = CreateObstaclePrefab("SupplyCrateObstacle", crate, new Vector2(0.86f, 0.86f), Vector2.zero, Vector3.one * 1.08f, obstacleLayer);
        GameObject batPrefab = CreateObstaclePrefab("CaveBatObstacle", batFrames[0], new Vector2(0.78f, 0.42f), Vector2.zero, Vector3.one * 1.24f, obstacleLayer, batFrames);
        RunnerDifficultySettings difficultySettings = EnsureDifficultySettingsAsset();
        ObstacleData[] obstacleData = EnsureObstacleDataAssets(spikePrefab, spikeRowPrefab, cratePrefab, batPrefab);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "HoboRush";

        CreateCamera();
        Transform worldRoot = new GameObject("World").transform;
        CreateBackground(worldRoot, square);
        CreateGround(worldRoot, square, dirtScatter, groundLayer);
        RunnerPlayerController player = CreatePlayer(runFrames, duckFrames, jumpFrame, deadFrame, playerLayer, groundLayer);
        Transform obstacleRoot = new GameObject("Runtime Obstacles").transform;
        RunnerObstacleSpawner spawner = CreateSpawner(obstacleRoot, obstacleData, difficultySettings);
        RunnerUIManager uiManager = CreateUi();
        CreateAudioManager();
        CreateGameManager(uiManager, spawner, player, difficultySettings);

        EditorSceneManager.SaveScene(scene, ScenePath);
        ConfigureBuildScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Hobo Rush scene rebuilt at " + ScenePath);
    }

    private static void ConfigureBuildScenes()
    {
        bool hasMainMenu = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath) != null;
        EditorBuildSettings.scenes = hasMainMenu
            ? new[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true)
            }
            : new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }

    public static void BuildGameSceneBatch()
    {
        try
        {
            BuildGameScene();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    [MenuItem("Tools/Hobo Rush/Validate Runner Scene")]
    public static void ValidateGameScene()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        if (FindSceneObject<RunnerGameManager>() == null ||
            FindSceneObject<RunnerPlayerController>() == null ||
            FindSceneObject<RunnerObstacleSpawner>() == null ||
            FindSceneObject<RunnerAudioManager>() == null ||
            FindSceneObject<RunnerUIManager>() == null)
        {
            throw new InvalidOperationException("Hobo Rush scene is missing one or more required systems.");
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/SpikeClusterObstacle.prefab") == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/SpikeRowObstacle.prefab") == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/SupplyCrateObstacle.prefab") == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/CaveBatObstacle.prefab") == null)
        {
            throw new InvalidOperationException("Hobo Rush obstacle prefabs are missing.");
        }

        RunnerPlayerController player = FindSceneObject<RunnerPlayerController>();
        if (player == null || Mathf.Abs(player.transform.position.y - (GroundTopY + 0.5f)) > 0.02f)
        {
            throw new InvalidOperationException("Hobo Rush player is not aligned to the ground.");
        }

        RunnerObstacleSpawner spawner = FindSceneObject<RunnerObstacleSpawner>();
        RunnerGameManager manager = FindSceneObject<RunnerGameManager>();
        ValidateDataDrivenSetup(spawner, manager);
        ValidatePlayableGaps(player, spawner, manager);

        Debug.Log("Validation succeeded for Hobo Rush.");
    }

    public static void ValidateGameSceneBatch()
    {
        try
        {
            ValidateGameScene();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    [MenuItem("Tools/Hobo Rush/Apply 95 Rubric Data Upgrade")]
    public static void ApplyRubricDataUpgrade()
    {
        EnsureFolders();
        RunnerDifficultySettings difficultySettings = EnsureDifficultySettingsAsset();
        ObstacleData[] obstacleData = EnsureObstacleDataAssets(
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/SpikeClusterObstacle.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/SpikeRowObstacle.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/SupplyCrateObstacle.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/CaveBatObstacle.prefab"));

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        RunnerObstacleSpawner spawner = FindSceneObject<RunnerObstacleSpawner>();
        RunnerGameManager manager = FindSceneObject<RunnerGameManager>();
        if (spawner == null || manager == null)
        {
            throw new InvalidOperationException("Cannot apply rubric upgrade without the current spawner and game manager.");
        }

        SetObject(spawner, "difficultySettings", difficultySettings);
        SetObjectArray(spawner, "obstacles", obstacleData);
        SetFloat(spawner, "spawnX", 11.5f);
        ApplySpawnerPlayabilityTuning(spawner);

        SetObject(manager, "difficultySettings", difficultySettings);
        ApplyGameManagerDifficultyFallbacks(manager);

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Applied Hobo Rush 95+ rubric data upgrade without rebuilding the scene.");
    }

    public static void ApplyRubricDataUpgradeBatch()
    {
        try
        {
            ApplyRubricDataUpgrade();
            ValidateGameScene();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    [MenuItem("Tools/Hobo Rush/Apply UI Polish")]
    public static void ApplyUiPolish()
    {
        ImportPixelUiSprites();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        DestroySceneObject("Canvas");
        DestroySceneObject("UI Manager");
        DestroySceneObject("EventSystem");

        RunnerUIManager uiManager = CreateUi();
        RunnerGameManager gameManager = FindSceneObject<RunnerGameManager>();
        RunnerObstacleSpawner spawner = FindSceneObject<RunnerObstacleSpawner>();
        RunnerPlayerController player = FindSceneObject<RunnerPlayerController>();
        if (gameManager == null || spawner == null || player == null)
        {
            throw new InvalidOperationException("Cannot apply UI polish because one or more gameplay systems are missing.");
        }

        SetObject(gameManager, "uiManager", uiManager);
        SetObject(gameManager, "obstacleSpawner", spawner);
        SetObject(gameManager, "player", player);

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Applied polished score HUD and game-over UI.");
    }

    public static void ApplyUiPolishBatch()
    {
        try
        {
            ApplyUiPolish();
            ValidateGameScene();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    [MenuItem("Tools/Hobo Rush/Restore Original Duck")]
    public static void RestoreOriginalDuck()
    {
        ImportSpriteFolder(RunnerCharacterFolder, 48f);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        RunnerPlayerController player = FindSceneObject<RunnerPlayerController>();
        if (player == null)
        {
            throw new InvalidOperationException("Runner player is missing from the scene.");
        }

        Sprite[] duckFrames = { RunnerCharacter("HelmetlessRunner_Duck") };
        SetSpriteArray(player, "duckFrames", duckFrames);
        SetObject(player, "deadSprite", RunnerCharacter("HelmetlessRunner_Duck"));
        SetVector2(player, "duckColliderSize", new Vector2(0.8f, 0.58f));
        SetVector2(player, "duckColliderOffset", new Vector2(0.02f, -0.2f));

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Restored the original runner duck pose.");
    }

    public static void RestoreOriginalDuckBatch()
    {
        try
        {
            RestoreOriginalDuck();
            ValidateGameScene();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void ConfigureProjectIdentity()
    {
        PlayerSettings.companyName = "StudentGames";
        PlayerSettings.productName = "Hobo Rush";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.studentgames.hoborush");
    }

    private static void EnsureFolders()
    {
        EnsureFolder(RootFolder);
        EnsureFolder(PrefabFolder);
        EnsureFolder(PixelUiFolder);
        EnsureFolder(DataFolder);
        EnsureFolder(ObstacleDataFolder);
        EnsureFolder(DifficultyDataFolder);
        EnsureFolder(SceneFolder);
        EnsureFolder(AudioFolder);
        EnsureFolder(FontFolder);
    }

    private static void EnsureKenneyAssetsPresent()
    {
        if (!Directory.Exists(Path.GetFullPath(KenneyFolder)) || !File.Exists(Path.GetFullPath(TileFolder + "/tile_0000.png")))
        {
            throw new InvalidOperationException("Kenney Pixel Platformer assets are missing from " + KenneyFolder + ".");
        }
    }

    private static void ImportKenneySprites()
    {
        AssetDatabase.Refresh();
        ImportSpriteFolder(TileFolder, 18f);
        ImportSpriteFolder(CharacterFolder, 24f);
        ImportSpriteFolder(BackgroundFolder, 18f);
        ImportSpriteFolder(RunnerCharacterFolder, 48f);
        ImportPixelUiSprites();
        AssetDatabase.Refresh();
    }

    private static void ImportPixelUiSprites()
    {
        if (!Directory.Exists(Path.GetFullPath(PixelUiFolder)))
        {
            return;
        }

        string[] files = Directory.GetFiles(Path.GetFullPath(PixelUiFolder), "*.png", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < files.Length; i++)
        {
            string path = ToAssetPath(files[i]);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spriteBorder = new Vector4(16f, 16f, 16f, 16f);
            importer.SaveAndReimport();
        }
    }

    private static void ImportSpriteFolder(string folder, float pixelsPerUnit)
    {
        if (!Directory.Exists(Path.GetFullPath(folder)))
        {
            return;
        }

        string[] files = Directory.GetFiles(Path.GetFullPath(folder), "*.png", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < files.Length; i++)
        {
            string path = ToAssetPath(files[i]);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }

    private static void DeleteLegacyAssets()
    {
        string[] legacyPaths = Array.Empty<string>();

        for (int i = 0; i < legacyPaths.Length; i++)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(legacyPaths[i]) != null || AssetDatabase.IsValidFolder(legacyPaths[i]))
            {
                AssetDatabase.DeleteAsset(legacyPaths[i]);
            }
        }
    }

    private static void DestroySceneObject(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        if (target != null)
        {
            UnityEngine.Object.DestroyImmediate(target);
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent ?? "Assets", name);
    }

    private static int EnsureLayer(string layerName)
    {
        int existingLayer = LayerMask.NameToLayer(layerName);
        if (existingLayer >= 0)
        {
            return existingLayer;
        }

        UnityEngine.Object tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
        SerializedObject tagManager = new SerializedObject(tagManagerAsset);
        SerializedProperty layers = tagManager.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = layerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }
        }

        throw new InvalidOperationException("No empty Unity layer slot is available for " + layerName + ".");
    }

    private static Sprite Tile(int index)
    {
        return LoadSprite(TileFolder + "/tile_" + index.ToString("0000") + ".png");
    }

    private static Sprite Character(int index)
    {
        return LoadSprite(CharacterFolder + "/tile_" + index.ToString("0000") + ".png");
    }

    private static Sprite RunnerCharacter(string name)
    {
        return LoadSprite(RunnerCharacterFolder + "/" + name + ".png");
    }

    private static Sprite PixelUi(string name)
    {
        return LoadSprite(PixelUiFolder + "/" + name + ".png");
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            throw new InvalidOperationException("Missing sprite at " + path);
        }

        return sprite;
    }

    private static AudioClip LoadAudio(string name)
    {
        string path = AudioFolder + "/" + name + ".ogg";
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null)
        {
            throw new InvalidOperationException("Missing audio clip at " + path);
        }

        return clip;
    }

    private static Sprite CreateSquareSprite()
    {
        string path = RootFolder + "/Art/SolidPixel.png";
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        File.WriteAllBytes(Path.GetFullPath(path), texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 1f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        return LoadSprite(path);
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0.05f, -10f);
        camera.orthographic = true;
        camera.orthographicSize = 4.35f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.39f, 0.73f, 0.96f, 1f);
    }

    private static void CreateBackground(Transform parent, Sprite square)
    {
        CreateSpriteObject("Clean Blue Sky", square, new Color(0.39f, 0.73f, 0.96f, 1f), new Vector2(0f, 0.6f), new Vector2(46f, 9.5f), -40, parent);
        CreateSpriteObject("Soft Horizon", square, new Color(0.71f, 0.9f, 0.95f, 1f), new Vector2(0f, -1.45f), new Vector2(46f, 2.8f), -38, parent);

        Transform cloudBands = new GameObject("Scrolling Cloud Bands").transform;
        cloudBands.SetParent(parent, false);
        Transform[] cloudSegments = new Transform[4];
        for (int i = 0; i < cloudSegments.Length; i++)
        {
            Transform segment = new GameObject("Cloud Band Segment " + (i + 1)).transform;
            segment.SetParent(cloudBands, false);
            segment.position = new Vector3(-24f + i * SegmentWidth, 0f, 0f);
            BuildCloudBandSegment(segment, square);
            cloudSegments[i] = segment;
        }

        RunnerParallaxScroller cloudScroller = cloudBands.gameObject.AddComponent<RunnerParallaxScroller>();
        SetObjectArray(cloudScroller, "segments", cloudSegments);
        SetFloat(cloudScroller, "segmentWidth", SegmentWidth);
        SetFloat(cloudScroller, "speedMultiplier", 0.14f);
        SetFloat(cloudScroller, "resetX", -30f);

        Transform distantBlocks = new GameObject("Scrolling Distant Backdrop").transform;
        distantBlocks.SetParent(parent, false);
        Transform[] backdropSegments = new Transform[4];
        for (int i = 0; i < backdropSegments.Length; i++)
        {
            Transform segment = new GameObject("Backdrop Segment " + (i + 1)).transform;
            segment.SetParent(distantBlocks, false);
            segment.position = new Vector3(-24f + i * SegmentWidth, 0f, 0f);
            BuildDistantBackdropSegment(segment, square);
            backdropSegments[i] = segment;
        }

        RunnerParallaxScroller backdropScroller = distantBlocks.gameObject.AddComponent<RunnerParallaxScroller>();
        SetObjectArray(backdropScroller, "segments", backdropSegments);
        SetFloat(backdropScroller, "segmentWidth", SegmentWidth);
        SetFloat(backdropScroller, "speedMultiplier", 0.28f);
        SetFloat(backdropScroller, "resetX", -30f);
    }

    private static void BuildCloudBandSegment(Transform parent, Sprite square)
    {
        Color cloud = new Color(0.85f, 0.96f, 0.98f, 1f);
        Color shadow = new Color(0.68f, 0.88f, 0.93f, 1f);

        CreateSpriteObject("Cloud Long 1", square, cloud, new Vector2(-5.6f, 1.45f), new Vector2(3.2f, 0.28f), -30, parent);
        CreateSpriteObject("Cloud Cap 1", square, cloud, new Vector2(-4.35f, 1.65f), new Vector2(1.4f, 0.28f), -30, parent);
        CreateSpriteObject("Cloud Shadow 1", square, shadow, new Vector2(-5.45f, 1.29f), new Vector2(2.6f, 0.12f), -29, parent);

        CreateSpriteObject("Cloud Long 2", square, cloud, new Vector2(1.8f, 1.2f), new Vector2(3.8f, 0.24f), -30, parent);
        CreateSpriteObject("Cloud Cap 2", square, cloud, new Vector2(0.75f, 1.4f), new Vector2(1.25f, 0.24f), -30, parent);
        CreateSpriteObject("Cloud Shadow 2", square, shadow, new Vector2(2.05f, 1.06f), new Vector2(2.9f, 0.1f), -29, parent);

        CreateSpriteObject("Cloud Small", square, cloud, new Vector2(6.5f, 1.72f), new Vector2(1.8f, 0.22f), -30, parent);
    }

    private static void BuildDistantBackdropSegment(Transform parent, Sprite square)
    {
        Color mist = new Color(0.64f, 0.86f, 0.83f, 1f);
        Color mid = new Color(0.42f, 0.72f, 0.62f, 1f);
        Color dark = new Color(0.22f, 0.48f, 0.45f, 1f);

        CreateSpriteObject("Distant Mist Band", square, mist, new Vector2(0f, -1.27f), new Vector2(SegmentWidth + 0.25f, 0.42f), -28, parent);
        CreateSpriteObject("Distant Green Band", square, mid, new Vector2(0f, -1.62f), new Vector2(SegmentWidth + 0.25f, 0.38f), -27, parent);
        CreateSpriteObject("Distant Base Line", square, dark, new Vector2(0f, -1.86f), new Vector2(SegmentWidth + 0.25f, 0.12f), -26, parent);

        BuildBackdropForest(parent, square);
    }

    private static void BuildBackdropForest(Transform parent, Sprite square)
    {
        CreateDistantTree(parent, square, "Distant Tree A", -6.75f, -1.82f, 1.08f, 0.82f, false);
        CreateDistantTree(parent, square, "Distant Tree B", -4.15f, -1.84f, 0.74f, 0.62f, true);
        CreateDistantTree(parent, square, "Distant Tree C", -1.35f, -1.83f, 1.24f, 0.95f, false);
        CreateDistantTree(parent, square, "Distant Tree D", 2.25f, -1.84f, 0.86f, 0.72f, true);
        CreateDistantTree(parent, square, "Distant Tree E", 5.25f, -1.83f, 1.12f, 0.88f, false);
        CreateDistantShrub(parent, square, "Distant Shrub A", -2.95f, -1.84f, 0.66f);
        CreateDistantShrub(parent, square, "Distant Shrub B", 6.85f, -1.84f, 0.54f);
    }

    private static void CreateDistantTree(Transform parent, Sprite square, string name, float x, float baseY, float height, float width, bool pale)
    {
        Color trunk = pale ? new Color(0.23f, 0.42f, 0.36f, 1f) : new Color(0.18f, 0.35f, 0.31f, 1f);
        Color leafDark = pale ? new Color(0.35f, 0.66f, 0.49f, 1f) : new Color(0.25f, 0.55f, 0.43f, 1f);
        Color leafLight = pale ? new Color(0.49f, 0.78f, 0.55f, 1f) : new Color(0.39f, 0.7f, 0.5f, 1f);

        Transform tree = new GameObject(name).transform;
        tree.SetParent(parent, false);
        CreateSpriteObject("Trunk", square, trunk, new Vector2(x, baseY + height * 0.28f), new Vector2(width * 0.16f, height * 0.68f), -25, tree);
        CreateSpriteObject("Canopy Core", square, leafDark, new Vector2(x, baseY + height * 0.86f), new Vector2(width, height * 0.42f), -24, tree);
        CreateSpriteObject("Canopy Top", square, leafLight, new Vector2(x - width * 0.04f, baseY + height * 1.12f), new Vector2(width * 0.62f, height * 0.34f), -24, tree);
        CreateSpriteObject("Canopy Left", square, leafDark, new Vector2(x - width * 0.35f, baseY + height * 0.74f), new Vector2(width * 0.44f, height * 0.32f), -24, tree);
        CreateSpriteObject("Canopy Right", square, leafLight, new Vector2(x + width * 0.34f, baseY + height * 0.78f), new Vector2(width * 0.42f, height * 0.3f), -24, tree);
        CreateSpriteObject("Ground Shadow", square, new Color(0.16f, 0.39f, 0.34f, 1f), new Vector2(x, baseY + 0.03f), new Vector2(width * 0.82f, 0.08f), -25, tree);
    }

    private static void CreateDistantShrub(Transform parent, Sprite square, string name, float x, float baseY, float width)
    {
        Transform shrub = new GameObject(name).transform;
        shrub.SetParent(parent, false);
        CreateSpriteObject("Shrub Base", square, new Color(0.22f, 0.54f, 0.42f, 1f), new Vector2(x, baseY + 0.16f), new Vector2(width, 0.28f), -24, shrub);
        CreateSpriteObject("Shrub Highlight", square, new Color(0.42f, 0.74f, 0.5f, 1f), new Vector2(x - width * 0.12f, baseY + 0.32f), new Vector2(width * 0.58f, 0.18f), -24, shrub);
    }

    private static void CreateGround(Transform parent, Sprite square, Sprite dirtScatter, int groundLayer)
    {
        GameObject groundColliderObject = new GameObject("Ground Collider - Top Aligned");
        groundColliderObject.layer = groundLayer;
        groundColliderObject.transform.SetParent(parent, false);
        groundColliderObject.transform.position = new Vector3(0f, GroundTopY - 0.18f, 0f);
        BoxCollider2D groundCollider = groundColliderObject.AddComponent<BoxCollider2D>();
        groundCollider.size = new Vector2(48f, 0.36f);

        Transform groundRoot = new GameObject("Scrolling Tile Road").transform;
        groundRoot.SetParent(parent, false);
        Transform[] segments = new Transform[4];
        for (int i = 0; i < segments.Length; i++)
        {
            Transform segment = new GameObject("Road Segment " + (i + 1)).transform;
            segment.SetParent(groundRoot, false);
            segment.position = new Vector3(-24f + i * SegmentWidth, 0f, 0f);
            BuildGroundSegment(segment, square, dirtScatter);
            segments[i] = segment;
        }

        RunnerParallaxScroller scroller = groundRoot.gameObject.AddComponent<RunnerParallaxScroller>();
        SetObjectArray(scroller, "segments", segments);
        SetFloat(scroller, "segmentWidth", SegmentWidth);
        SetFloat(scroller, "speedMultiplier", 1f);
        SetFloat(scroller, "resetX", -30f);

        CreateSpriteObject("Ground Lower Shadow", square, new Color(0.43f, 0.25f, 0.2f, 1f), new Vector2(0f, GroundTopY - 2.03f), new Vector2(48f, 0.12f), -2, parent);
    }

    private static void BuildGroundSegment(Transform parent, Sprite square, Sprite dirtScatter)
    {
        CreateSpriteObject("Grass Surface", square, new Color(0.37f, 0.78f, 0.36f, 1f), new Vector2(0f, GroundTopY - 0.47f), new Vector2(SegmentWidth + 0.35f, 0.94f), 0, parent);
        CreateSpriteObject("Grass Highlight", square, new Color(0.54f, 0.9f, 0.43f, 1f), new Vector2(0f, GroundTopY - 0.1f), new Vector2(SegmentWidth + 0.35f, 0.18f), 1, parent);
        CreateSpriteObject("Grass Root Edge", square, new Color(0.1f, 0.61f, 0.45f, 1f), new Vector2(0f, GroundTopY - 0.88f), new Vector2(SegmentWidth + 0.35f, 0.14f), 2, parent);
        CreateSpriteObject("Platform Underside", square, new Color(0.09f, 0.25f, 0.25f, 1f), new Vector2(0f, GroundTopY - 1.01f), new Vector2(SegmentWidth + 0.35f, 0.12f), 1, parent);
        CreateSpriteObject("Continuous Dirt Fill", square, new Color(0.72f, 0.43f, 0.29f, 1f), new Vector2(0f, GroundTopY - 1.55f), new Vector2(SegmentWidth + 0.35f, 1.32f), -3, parent);
        CreateSpriteObject("Dirt Top Shadow", square, new Color(0.43f, 0.25f, 0.2f, 1f), new Vector2(0f, GroundTopY - 0.96f), new Vector2(SegmentWidth + 0.35f, 0.1f), -1, parent);

        CreateSpriteObject("Dirt Fleck Scatter", dirtScatter, Color.white, new Vector2(0f, GroundTopY - 1.53f), new Vector2(0.34f, 0.35f), -1, parent);
    }

    private static RunnerPlayerController CreatePlayer(Sprite[] runFrames, Sprite[] duckSprites, Sprite jumpSprite, Sprite deadSprite, int playerLayer, int groundLayer)
    {
        GameObject player = new GameObject("Helmetless Runner");
        player.layer = playerLayer;
        player.transform.position = new Vector3(-5.25f, GroundTopY + 0.5f, 0f);

        SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
        renderer.sprite = runFrames[0];
        renderer.sortingOrder = 20;

        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3.8f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.sharedMaterial = CreateFrictionlessMaterial();

        BoxCollider2D bodyCollider = player.AddComponent<BoxCollider2D>();
        bodyCollider.size = new Vector2(0.7f, 0.82f);
        bodyCollider.offset = new Vector2(0f, -0.08f);

        Transform groundCheck = new GameObject("Ground Check").transform;
        groundCheck.SetParent(player.transform, false);
        groundCheck.localPosition = new Vector3(0f, -0.53f, 0f);

        RunnerPlayerController controller = player.AddComponent<RunnerPlayerController>();
        SetObject(controller, "rb", rb);
        SetObject(controller, "bodyCollider", bodyCollider);
        SetObject(controller, "spriteRenderer", renderer);
        SetObject(controller, "groundCheck", groundCheck);
        SetLayerMask(controller, "groundLayer", 1 << groundLayer);
        SetSpriteArray(controller, "runFrames", runFrames);
        SetSpriteArray(controller, "duckFrames", duckSprites);
        SetObject(controller, "jumpSprite", jumpSprite);
        SetObject(controller, "deadSprite", deadSprite);
        SetFloat(controller, "jumpVelocity", 13.4f);
        SetFloat(controller, "jumpBufferTime", 0.16f);
        SetFloat(controller, "jumpCutMultiplier", 0.58f);
        SetFloat(controller, "coyoteTime", 0.12f);
        SetFloat(controller, "groundCheckRadius", 0.16f);
        SetFloat(controller, "groundProbeDistance", 0.13f);
        SetVector2(controller, "standingColliderSize", new Vector2(0.7f, 0.82f));
        SetVector2(controller, "standingColliderOffset", new Vector2(0f, -0.08f));
        SetVector2(controller, "duckColliderSize", new Vector2(0.8f, 0.58f));
        SetVector2(controller, "duckColliderOffset", new Vector2(0.02f, -0.2f));
        return controller;
    }

    private static PhysicsMaterial2D CreateFrictionlessMaterial()
    {
        string path = RootFolder + "/RunnerFrictionless.physicsMaterial2D";
        PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
        if (material == null)
        {
            material = new PhysicsMaterial2D("RunnerFrictionless");
            AssetDatabase.CreateAsset(material, path);
        }

        material.friction = 0f;
        material.bounciness = 0f;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject CreateObstaclePrefab(string name, Sprite sprite, Vector2 colliderSize, Vector2 colliderOffset, Vector3 scale, int obstacleLayer, Sprite[] animationFrames = null)
    {
        string prefabPath = PrefabFolder + "/" + name + ".prefab";
        AssetDatabase.DeleteAsset(prefabPath);

        GameObject obstacle = new GameObject(name);
        obstacle.layer = obstacleLayer;
        obstacle.transform.localScale = scale;
        SpriteRenderer renderer = obstacle.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 18;

        BoxCollider2D collider = obstacle.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = colliderSize;
        collider.offset = colliderOffset;

        RunnerObstacle runnerObstacle = obstacle.AddComponent<RunnerObstacle>();
        if (animationFrames != null && animationFrames.Length > 1)
        {
            SetObject(runnerObstacle, "spriteRenderer", renderer);
            SetSpriteArray(runnerObstacle, "animationFrames", animationFrames);
            SetFloat(runnerObstacle, "animationFrameRate", 11f);
        }
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obstacle, prefabPath);
        UnityEngine.Object.DestroyImmediate(obstacle);
        return prefab;
    }

    private static RunnerDifficultySettings EnsureDifficultySettingsAsset()
    {
        EnsureFolder(DifficultyDataFolder);
        string path = DifficultyDataFolder + "/RunnerDifficultySettings.asset";
        RunnerDifficultySettings settings = AssetDatabase.LoadAssetAtPath<RunnerDifficultySettings>(path);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<RunnerDifficultySettings>();
            AssetDatabase.CreateAsset(settings, path);
        }

        SerializedObject serialized = new SerializedObject(settings);
        serialized.FindProperty("startingSpeed").floatValue = 8.2f;
        serialized.FindProperty("maxSpeed").floatValue = 23.5f;
        serialized.FindProperty("speedIncreasePerSecond").floatValue = 0.052f;
        serialized.FindProperty("scoreSpeedStep").intValue = 100;
        serialized.FindProperty("speedIncreasePerScoreStep").floatValue = 0.12f;
        serialized.FindProperty("scorePerMeter").floatValue = 8f;
        serialized.FindProperty("firstSpawnDelay").floatValue = 1.2f;
        serialized.FindProperty("minSpawnInterval").floatValue = 0.72f;
        serialized.FindProperty("maxSpawnInterval").floatValue = 1.42f;
        serialized.FindProperty("intervalSpeedPressure").floatValue = 0.03f;
        serialized.FindProperty("globalMinimumGap").floatValue = 0.62f;
        serialized.FindProperty("birdHeightOffset").floatValue = -0.15f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings);
        return settings;
    }

    private static ObstacleData[] EnsureObstacleDataAssets(GameObject spikePrefab, GameObject spikeRowPrefab, GameObject cratePrefab, GameObject batPrefab)
    {
        if (spikePrefab == null || spikeRowPrefab == null || cratePrefab == null || batPrefab == null)
        {
            throw new InvalidOperationException("Cannot create obstacle data because one or more obstacle prefabs are missing.");
        }

        EnsureFolder(ObstacleDataFolder);
        ObstacleSetup[] setups =
        {
            new ObstacleSetup("Spike Cluster", spikePrefab, GroundTopY + 0.6f, 0f, 1.1f, 0f, 0.64f, false, 7),
            new ObstacleSetup("Spike Row", spikeRowPrefab, GroundTopY + 0.6f, 0f, 0.85f, 7.8f, 0.68f, false, 6),
            new ObstacleSetup("Supply Crate", cratePrefab, GroundTopY + 0.54f, 0f, 0.75f, 9.6f, 0.84f, false, 4),
            new ObstacleSetup("Low Bat", batPrefab, GroundTopY + 1.16f, 0f, 0.55f, 8.8f, 0.88f, true, 4),
            new ObstacleSetup("High Bat", batPrefab, GroundTopY + 1.62f, 0f, 0.42f, 11.8f, 0.62f, true, 3)
        };

        ObstacleData[] data = new ObstacleData[setups.Length];
        for (int i = 0; i < setups.Length; i++)
        {
            data[i] = EnsureObstacleDataAsset(setups[i]);
        }

        return data;
    }

    private static ObstacleData EnsureObstacleDataAsset(ObstacleSetup setup)
    {
        string path = ObstacleDataFolder + "/" + setup.AssetName + ".asset";
        ObstacleData data = AssetDatabase.LoadAssetAtPath<ObstacleData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<ObstacleData>();
            AssetDatabase.CreateAsset(data, path);
        }

        SerializedObject serialized = new SerializedObject(data);
        serialized.FindProperty("label").stringValue = setup.Label;
        serialized.FindProperty("prefab").objectReferenceValue = setup.Prefab;
        serialized.FindProperty("yPosition").floatValue = setup.YPosition;
        serialized.FindProperty("spawnYOffset").floatValue = setup.SpawnYOffset;
        serialized.FindProperty("weight").floatValue = setup.Weight;
        serialized.FindProperty("minGameSpeed").floatValue = setup.MinGameSpeed;
        serialized.FindProperty("recoveryGap").floatValue = setup.RecoveryGap;
        serialized.FindProperty("isBird").boolValue = setup.IsBird;
        serialized.FindProperty("prewarmCount").intValue = setup.PrewarmCount;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
        return data;
    }

    private static RunnerObstacleSpawner CreateSpawner(Transform obstacleRoot, ObstacleData[] obstacleData, RunnerDifficultySettings difficultySettings)
    {
        GameObject spawnerObject = new GameObject("Obstacle Spawner");
        RunnerObstacleSpawner spawner = spawnerObject.AddComponent<RunnerObstacleSpawner>();
        SetObject(spawner, "obstacleParent", obstacleRoot);
        SetObject(spawner, "difficultySettings", difficultySettings);
        SetFloat(spawner, "spawnX", 11.5f);
        ApplySpawnerPlayabilityTuning(spawner);
        SetObjectArray(spawner, "obstacles", obstacleData);

        return spawner;
    }

    private static RunnerUIManager CreateUi()
    {
        GameObject canvasObject = new GameObject("Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Font font = ProjectFont();
        Color ink = new Color(0.02f, 0.03f, 0.04f, 1f);
        Sprite panelSprite = PixelUi("9-Slice_space");
        Sprite yellowButton = PixelUi("9-Slice_Colored_yellow");
        Sprite yellowButtonPressed = PixelUi("9-Slice_Colored_yellow_pressed");
        Sprite redButton = PixelUi("9-Slice_Colored_red");
        Sprite redButtonPressed = PixelUi("9-Slice_Colored_red_pressed");

        GameObject scorePanel = CreateSlicedPanel("Score HUD Panel", canvasObject.transform, panelSprite, Color.white, Vector2.zero, new Vector2(360f, 142f));
        Image scorePanelImage = scorePanel.GetComponent<Image>();
        if (scorePanelImage != null)
        {
            scorePanelImage.enabled = false;
            scorePanelImage.raycastTarget = false;
        }

        SetRect(scorePanel.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -14f), new Vector2(360f, 136f));

        Text scoreLabel = CreateText("Score Label", scorePanel.transform, "SCORE", 18, FontStyle.Bold, TextAnchor.UpperLeft, ink);
        SetRect(scoreLabel.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -16f), new Vector2(150f, 24f));

        Text scoreText = CreateText("Score Text", scorePanel.transform, "0", 44, FontStyle.Bold, TextAnchor.UpperLeft, ink);
        SetRect(scoreText.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -42f), new Vector2(270f, 50f));

        Text bestText = CreateText("Best Text", scorePanel.transform, "BEST: 0", 16, FontStyle.Bold, TextAnchor.UpperLeft, ink);
        SetRect(bestText.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -96f), new Vector2(320f, 30f));

        Text speedText = CreateText("Speed Text", canvasObject.transform, string.Empty, 24, FontStyle.Bold, TextAnchor.UpperLeft, ink);
        SetRect(speedText.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, -30f), new Vector2(300f, 36f));
        speedText.gameObject.SetActive(false);

        Text hintText = CreateText("Hint Text", canvasObject.transform, string.Empty, 24, FontStyle.Bold, TextAnchor.LowerCenter, new Color(0.08f, 0.12f, 0.17f, 0.76f));
        SetRect(hintText.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(760f, 42f));
        hintText.gameObject.SetActive(false);

        GameObject gameOverPanel = CreatePanel("Game Over Overlay", canvasObject.transform, new Color(0.03f, 0.05f, 0.07f, 0.54f), Vector2.zero, Vector2.zero);
        SetStretch(gameOverPanel.GetComponent<RectTransform>());

        GameObject gameOverCard = CreateSlicedPanel("Game Over Card", gameOverPanel.transform, panelSprite, Color.white, new Vector2(0f, 16f), new Vector2(760f, 456f));

        Text title = CreateText("Title", gameOverCard.transform, "RUN ENDED", 50, FontStyle.Bold, TextAnchor.MiddleCenter, ink);
        SetCenteredRect(title.GetComponent<RectTransform>(), new Vector2(0f, 140f), new Vector2(640f, 70f));
        Text finalScore = CreateText("Final Score", gameOverCard.transform, "SCORE 0\nBEST: 0", 30, FontStyle.Bold, TextAnchor.MiddleCenter, ink);
        SetCenteredRect(finalScore.GetComponent<RectTransform>(), new Vector2(0f, 34f), new Vector2(620f, 110f));
        Button restart = CreatePixelButton("Restart Button", gameOverCard.transform, font, "RESTART", new Vector2(-142f, -136f), new Vector2(248f, 70f), yellowButton, yellowButtonPressed, ink);
        Button exit = CreatePixelButton("Exit Button", gameOverCard.transform, font, "EXIT", new Vector2(142f, -136f), new Vector2(248f, 70f), redButton, redButtonPressed, ink);
        gameOverPanel.SetActive(false);

        EditorUi editorUi = CreateEditorUi(canvasObject.transform, font);

        GameObject managerObject = new GameObject("UI Manager");
        RunnerUIManager manager = managerObject.AddComponent<RunnerUIManager>();
        SetObject(manager, "scoreText", scoreText);
        SetObject(manager, "highScoreText", bestText);
        SetObject(manager, "speedText", speedText);
        SetObject(manager, "hintText", hintText);
        SetObject(manager, "hudPanel", scorePanel);
        SetObject(manager, "gameOverPanel", gameOverPanel);
        SetObject(manager, "finalScoreText", finalScore);
        SetObject(manager, "restartButton", restart);
        SetObject(manager, "exitButton", exit);
        SetObject(manager, "editorPanel", editorUi.Panel);
        SetObject(manager, "speedSlider", editorUi.SpeedSlider);
        SetObject(manager, "spawnSlider", editorUi.SpawnSlider);
        SetObject(manager, "birdsToggle", editorUi.BirdsToggle);
        SetObject(manager, "speedSliderText", editorUi.SpeedText);
        SetObject(manager, "spawnSliderText", editorUi.SpawnText);
        SetObject(manager, "spawnGroundHazardButton", editorUi.SpawnGroundButton);
        SetObject(manager, "spawnBirdButton", editorUi.SpawnBirdButton);
        SetObject(manager, "closeEditorButton", editorUi.CloseButton);

        CreateEventSystem();
        return manager;
    }

    private static EditorUi CreateEditorUi(Transform parent, Font font)
    {
        GameObject panel = CreateSlicedPanel("In Game Editor Panel", parent, PixelUi("9-Slice_space"), Color.white, new Vector2(34f, -128f), new Vector2(420f, 338f));
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);

        Text title = CreateText("Editor Title", panel.transform, "RUN TUNER", 28, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
        SetRect(title.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -18f), new Vector2(240f, 38f));

        Text speedText = CreateText("Speed Label", panel.transform, "Speed x1.00", 20, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.84f, 0.94f, 1f, 1f));
        SetRect(speedText.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -70f), new Vector2(220f, 28f));
        Slider speedSlider = CreateSlider("Speed Slider", panel.transform, new Vector2(22f, -106f), 0.55f, 1.65f, 1f);

        Text spawnText = CreateText("Spawn Label", panel.transform, "Spawn gap x1.00", 20, FontStyle.Bold, TextAnchor.UpperLeft, new Color(0.84f, 0.94f, 1f, 1f));
        SetRect(spawnText.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -142f), new Vector2(260f, 28f));
        Slider spawnSlider = CreateSlider("Spawn Slider", panel.transform, new Vector2(22f, -178f), 0.55f, 1.8f, 1f);

        Toggle birdsToggle = CreateToggle("Bats Toggle", panel.transform, font, "Bats", new Vector2(22f, -222f));
        Button spawnGround = CreatePixelButton("Spawn Hazard Button", panel.transform, font, "SPIKES", new Vector2(-96f, -280f), new Vector2(160f, 46f), PixelUi("9-Slice_Colored_yellow"), PixelUi("9-Slice_Colored_yellow_pressed"), new Color(0.08f, 0.12f, 0.17f, 1f));
        Button spawnBird = CreatePixelButton("Spawn Bat Button", panel.transform, font, "BAT", new Vector2(92f, -280f), new Vector2(160f, 46f), PixelUi("9-Slice_Colored_blue"), PixelUi("9-Slice_Colored_blue_pressed"), new Color(0.08f, 0.12f, 0.17f, 1f));
        Button close = CreatePixelButton("Close Editor Button", panel.transform, font, "CLOSE", new Vector2(148f, -38f), new Vector2(128f, 38f), PixelUi("9-Slice_Colored_grey"), PixelUi("9-Slice_Colored_grey_pressed"), Color.white);

        panel.SetActive(false);
        return new EditorUi(panel, speedSlider, spawnSlider, birdsToggle, speedText, spawnText, spawnGround, spawnBird, close);
    }

    private static void CreateGameManager(RunnerUIManager uiManager, RunnerObstacleSpawner spawner, RunnerPlayerController player, RunnerDifficultySettings difficultySettings)
    {
        GameObject managerObject = new GameObject("Game Manager");
        RunnerGameManager manager = managerObject.AddComponent<RunnerGameManager>();
        manager.Configure(uiManager, spawner, player);
        SetObject(manager, "difficultySettings", difficultySettings);
        ApplyGameManagerDifficultyFallbacks(manager);
    }

    private static void ApplySpawnerPlayabilityTuning(RunnerObstacleSpawner spawner)
    {
        SetFloat(spawner, "firstSpawnDelay", 1.2f);
        SetFloat(spawner, "minSpawnInterval", 0.72f);
        SetFloat(spawner, "maxSpawnInterval", 1.42f);
        SetFloat(spawner, "intervalSpeedPressure", 0.03f);
        SetFloat(spawner, "globalMinimumGap", 0.62f);
        SetFloat(spawner, "birdHeightOffset", -0.15f);

        EditorUtility.SetDirty(spawner);
    }

    private static void ApplyGameManagerDifficultyFallbacks(RunnerGameManager manager)
    {
        SetFloat(manager, "startingSpeed", 8.2f);
        SetFloat(manager, "maxSpeed", 23.5f);
        SetFloat(manager, "speedIncreasePerSecond", 0.052f);
        SetInt(manager, "scoreSpeedStep", 100);
        SetFloat(manager, "speedIncreasePerScoreStep", 0.12f);
        SetFloat(manager, "scorePerMeter", 8f);
    }

    private static void ValidatePlayableGaps(RunnerPlayerController player, RunnerObstacleSpawner spawner, RunnerGameManager manager)
    {
        if (spawner == null || manager == null)
        {
            throw new InvalidOperationException("Cannot validate obstacle gaps without the spawner and game manager.");
        }

        RunnerDifficultySettings difficultySettings = GetSerializedObject<RunnerDifficultySettings>(manager, "difficultySettings");
        float spawnX = GetSerializedFloat(spawner, "spawnX");
        float maxSpeed = difficultySettings != null ? difficultySettings.MaxSpeed : GetSerializedFloat(manager, "maxSpeed");
        float timeFromSpawnToPlayer = (spawnX - player.transform.position.x) / Mathf.Max(0.01f, maxSpeed);
        if (timeFromSpawnToPlayer < 0.62f)
        {
            throw new InvalidOperationException("Obstacles reach the player too quickly at max speed. Increase spawnX or lower maxSpeed.");
        }

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        float jumpVelocity = GetSerializedFloat(player, "jumpVelocity");
        float gravity = Mathf.Abs(Physics2D.gravity.y * (rb != null ? rb.gravityScale : 1f));
        float jumpHeight = jumpVelocity * jumpVelocity / (2f * Mathf.Max(0.01f, gravity));
        if (jumpHeight < 1.45f)
        {
            throw new InvalidOperationException("Jump height is too low for the taller ground obstacles.");
        }

        float globalGap = difficultySettings != null ? difficultySettings.GlobalMinimumGap : GetSerializedFloat(spawner, "globalMinimumGap");
        if (globalGap < 0.6f)
        {
            throw new InvalidOperationException("Global obstacle gap is too low for reliable late-game reaction time.");
        }

        SerializedObject serializedSpawner = new SerializedObject(spawner);
        SerializedProperty obstacles = serializedSpawner.FindProperty("obstacles");
        for (int i = 0; i < obstacles.arraySize; i++)
        {
            ObstacleData data = obstacles.GetArrayElementAtIndex(i).objectReferenceValue as ObstacleData;
            if (data == null)
            {
                throw new InvalidOperationException("Obstacle data slot " + i + " is not assigned.");
            }

            string label = data.Label;
            float recoveryGap = data.RecoveryGap;
            float requiredGap = GetMinimumRecoveryGapForLabel(label);
            if (recoveryGap < requiredGap)
            {
                throw new InvalidOperationException(label + " recovery gap is too low. Required at least " + requiredGap.ToString("0.00") + " seconds.");
            }
        }

        Debug.Log("Playability gap check: max-speed warning time " + timeFromSpawnToPlayer.ToString("0.00") + "s, jump height " + jumpHeight.ToString("0.00") + "m, safe recovery gaps enabled.");
    }

    private static void ValidateDataDrivenSetup(RunnerObstacleSpawner spawner, RunnerGameManager manager)
    {
        if (spawner == null || manager == null)
        {
            throw new InvalidOperationException("Cannot validate data-driven setup without spawner and game manager.");
        }

        RunnerDifficultySettings managerDifficulty = GetSerializedObject<RunnerDifficultySettings>(manager, "difficultySettings");
        RunnerDifficultySettings spawnerDifficulty = GetSerializedObject<RunnerDifficultySettings>(spawner, "difficultySettings");
        if (managerDifficulty == null || spawnerDifficulty == null || managerDifficulty != spawnerDifficulty)
        {
            throw new InvalidOperationException("Game Manager and Obstacle Spawner must share the same RunnerDifficultySettings asset.");
        }

        if (managerDifficulty.ScoreSpeedStep != 100 || managerDifficulty.SpeedIncreasePerScoreStep <= 0f)
        {
            throw new InvalidOperationException("Difficulty settings must keep score-step speed scaling active.");
        }

        SerializedObject serializedSpawner = new SerializedObject(spawner);
        SerializedProperty obstacleArray = serializedSpawner.FindProperty("obstacles");
        if (obstacleArray == null || obstacleArray.arraySize < 5)
        {
            throw new InvalidOperationException("Obstacle spawner must reference the five required ObstacleData assets.");
        }

        bool hasGround = false;
        bool hasBird = false;
        for (int i = 0; i < obstacleArray.arraySize; i++)
        {
            ObstacleData data = obstacleArray.GetArrayElementAtIndex(i).objectReferenceValue as ObstacleData;
            if (data == null || data.Prefab == null)
            {
                throw new InvalidOperationException("Obstacle data slot " + i + " is missing its data asset or prefab.");
            }

            if (data.PrewarmCount <= 0 || data.Weight <= 0f)
            {
                throw new InvalidOperationException(data.Label + " must have a positive pool prewarm count and spawn weight.");
            }

            if (data.Prefab.GetComponent<RunnerObstacle>() == null || data.Prefab.GetComponent<Collider2D>() == null)
            {
                throw new InvalidOperationException(data.Label + " prefab must include RunnerObstacle and Collider2D components for pooled gameplay.");
            }

            hasBird |= data.IsBird;
            hasGround |= !data.IsBird;
        }

        if (!hasGround || !hasBird)
        {
            throw new InvalidOperationException("Obstacle data must include both ground hazards and bats.");
        }

        Debug.Log("Data-driven setup check: difficulty ScriptableObject assigned, obstacle ScriptableObjects assigned, pool prewarm data valid.");
    }

    private static float GetMinimumRecoveryGapForLabel(string label)
    {
        return label switch
        {
            "Supply Crate" => 0.8f,
            "Low Bat" => 0.82f,
            "Spike Row" => 0.66f,
            _ => 0.6f
        };
    }

    private static void CreateAudioManager()
    {
        GameObject audioObject = new GameObject("Audio Manager");
        RunnerAudioManager audioManager = audioObject.AddComponent<RunnerAudioManager>();
        SetFloat(audioManager, "masterVolume", 0.82f);
        SetFloat(audioManager, "footstepVolume", 0.24f);
        SetFloat(audioManager, "movementVolume", 0.58f);
        SetFloat(audioManager, "warningVolume", 0.34f);
        SetFloat(audioManager, "uiVolume", 0.52f);
        SetFloat(audioManager, "gameOverVolume", 0.78f);
        SetFloat(audioManager, "musicVolume", 0.1f);
        SetAudioArray(audioManager, "jumpClips", new[] { LoadAudio("Runner_Jump") });
        SetAudioArray(audioManager, "landClips", new[] { LoadAudio("Runner_Land_01"), LoadAudio("Runner_Land_02") });
        SetAudioArray(audioManager, "footstepClips", new[] { LoadAudio("Runner_Footstep_01"), LoadAudio("Runner_Footstep_02"), LoadAudio("Runner_Footstep_03") });
        SetAudioArray(audioManager, "slideClips", new[] { LoadAudio("Runner_Slide") });
        SetAudioArray(audioManager, "obstacleWarningClips", new[] { LoadAudio("Runner_ObstacleWarning") });
        SetAudioArray(audioManager, "birdWarningClips", new[] { LoadAudio("Runner_BatWarning") });
        SetAudioArray(audioManager, "uiClickClips", new[] { LoadAudio("Runner_UI_Click") });
        SetAudioArray(audioManager, "milestoneClips", new[] { LoadAudio("Runner_Milestone") });
        SetAudioArray(audioManager, "impactClips", new[] { LoadAudio("Runner_Impact") });
        SetAudioArray(audioManager, "gameOverClips", new[] { LoadAudio("Runner_GameOver") });
    }

    private static GameObject CreateSpriteObject(string name, Sprite sprite, Color color, Vector2 position, Vector2 scale, int sortingOrder, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = new Vector3(position.x, position.y, 0f);
        obj.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return obj;
    }

    private static Text CreateText(string name, Transform parent, string text, int size, FontStyle style, TextAnchor alignment, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Text label = obj.AddComponent<Text>();
        label.text = text;
        label.font = ProjectFont();
        label.fontSize = size;
        label.fontStyle = style;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    private static Font ProjectFont()
    {
        Font font = AssetDatabase.LoadAssetAtPath<Font>(UiFontPath);
        return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image image = panel.AddComponent<Image>();
        image.color = color;
        SetCenteredRect(panel.GetComponent<RectTransform>(), anchoredPosition, size);
        return panel;
    }

    private static GameObject CreateSlicedPanel(string name, Transform parent, Sprite sprite, Color color, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image image = panel.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 1f;
        image.color = color;
        SetCenteredRect(panel.GetComponent<RectTransform>(), anchoredPosition, size);
        return panel;
    }

    private static Button CreatePixelButton(string name, Transform parent, Font font, string label, Vector2 position, Vector2 size, Sprite normalSprite, Sprite pressedSprite, Color foreground)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.AddComponent<Image>();
        image.sprite = normalSprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 1f;
        image.color = Color.white;
        Button button = buttonObject.AddComponent<Button>();
        button.transition = Selectable.Transition.SpriteSwap;
        SpriteState state = button.spriteState;
        state.pressedSprite = pressedSprite;
        state.highlightedSprite = normalSprite;
        state.selectedSprite = normalSprite;
        button.spriteState = state;
        SetCenteredRect(buttonObject.GetComponent<RectTransform>(), position, size);

        Text text = CreateText("Label", buttonObject.transform, label, 22, FontStyle.Bold, TextAnchor.MiddleCenter, foreground);
        text.font = font;
        RectTransform textRect = text.GetComponent<RectTransform>();
        SetStretch(textRect);
        textRect.offsetMin = new Vector2(10f, 3f);
        textRect.offsetMax = new Vector2(-10f, -1f);
        return button;
    }

    private static Slider CreateSlider(string name, Transform parent, Vector2 position, float min, float max, float value)
    {
        GameObject sliderObject = new GameObject(name);
        sliderObject.transform.SetParent(parent, false);
        SetRect(sliderObject.AddComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), position, new Vector2(300f, 24f));
        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;

        GameObject background = new GameObject("Background");
        background.transform.SetParent(sliderObject.transform, false);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.24f, 0.32f, 0.42f, 1f);
        SetStretch(background.GetComponent<RectTransform>());

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        SetStretch(fillAreaRect);
        fillAreaRect.offsetMin = new Vector2(0f, 0f);
        fillAreaRect.offsetMax = new Vector2(0f, 0f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.46f, 0.86f, 1f, 1f);
        SetStretch(fill.GetComponent<RectTransform>());
        slider.fillRect = fill.GetComponent<RectTransform>();

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(sliderObject.transform, false);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(0.96f, 0.74f, 0.22f, 1f);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(24f, 34f);
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        return slider;
    }

    private static Toggle CreateToggle(string name, Transform parent, Font font, string label, Vector2 position)
    {
        GameObject toggleObject = new GameObject(name);
        toggleObject.transform.SetParent(parent, false);
        SetRect(toggleObject.AddComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), position, new Vector2(180f, 36f));
        Toggle toggle = toggleObject.AddComponent<Toggle>();
        toggle.isOn = true;

        GameObject background = new GameObject("Background");
        background.transform.SetParent(toggleObject.transform, false);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.24f, 0.32f, 0.42f, 1f);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(0f, 0.5f);
        backgroundRect.pivot = new Vector2(0f, 0.5f);
        backgroundRect.anchoredPosition = Vector2.zero;
        backgroundRect.sizeDelta = new Vector2(32f, 32f);

        GameObject check = new GameObject("Checkmark");
        check.transform.SetParent(background.transform, false);
        Image checkImage = check.AddComponent<Image>();
        checkImage.color = new Color(0.96f, 0.74f, 0.22f, 1f);
        SetCenteredRect(check.GetComponent<RectTransform>(), Vector2.zero, new Vector2(20f, 20f));
        toggle.graphic = checkImage;
        toggle.targetGraphic = backgroundImage;

        Text text = CreateText("Label", toggleObject.transform, label, 20, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        text.font = font;
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(42f, 0f);
        textRect.offsetMax = Vector2.zero;
        return toggle;
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static void SetCenteredRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static void SetStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetObject(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(fieldName).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetObjectArray(UnityEngine.Object target, string fieldName, Transform[] values)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty array = serialized.FindProperty(fieldName);
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetObjectArray(UnityEngine.Object target, string fieldName, UnityEngine.Object[] values)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty array = serialized.FindProperty(fieldName);
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetSpriteArray(UnityEngine.Object target, string fieldName, Sprite[] values)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty array = serialized.FindProperty(fieldName);
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetAudioArray(UnityEngine.Object target, string fieldName, AudioClip[] values)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty array = serialized.FindProperty(fieldName);
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetFloat(UnityEngine.Object target, string fieldName, float value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(fieldName).floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static float GetSerializedFloat(UnityEngine.Object target, string fieldName)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property == null)
        {
            throw new InvalidOperationException("Missing serialized float field " + fieldName + " on " + target.name + ".");
        }

        return property.floatValue;
    }

    private static T GetSerializedObject<T>(UnityEngine.Object target, string fieldName) where T : UnityEngine.Object
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property == null)
        {
            throw new InvalidOperationException("Missing serialized object field " + fieldName + " on " + target.name + ".");
        }

        return property.objectReferenceValue as T;
    }

    private static void SetInt(UnityEngine.Object target, string fieldName, int value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(fieldName).intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetVector2(UnityEngine.Object target, string fieldName, Vector2 value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(fieldName).vector2Value = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void SetLayerMask(UnityEngine.Object target, string fieldName, int value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(fieldName).intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static T FindSceneObject<T>() where T : UnityEngine.Object
    {
        return UnityEngine.Object.FindFirstObjectByType<T>();
    }

    private static string ToAssetPath(string fullPath)
    {
        string normalized = fullPath.Replace("\\", "/");
        string dataPath = Application.dataPath.Replace("\\", "/");
        if (!normalized.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Path is outside the Assets folder: " + fullPath);
        }

        return "Assets" + normalized.Substring(dataPath.Length);
    }

    private readonly struct ObstacleSetup
    {
        public ObstacleSetup(string label, GameObject prefab, float yPosition, float spawnYOffset, float weight, float minGameSpeed, float recoveryGap, bool isBird, int prewarmCount)
        {
            Label = label;
            Prefab = prefab;
            YPosition = yPosition;
            SpawnYOffset = spawnYOffset;
            Weight = weight;
            MinGameSpeed = minGameSpeed;
            RecoveryGap = recoveryGap;
            IsBird = isBird;
            PrewarmCount = prewarmCount;
        }

        public string Label { get; }
        public string AssetName => Label.Replace(" ", string.Empty);
        public GameObject Prefab { get; }
        public float YPosition { get; }
        public float SpawnYOffset { get; }
        public float Weight { get; }
        public float MinGameSpeed { get; }
        public float RecoveryGap { get; }
        public bool IsBird { get; }
        public int PrewarmCount { get; }
    }

    private readonly struct EditorUi
    {
        public EditorUi(GameObject panel, Slider speedSlider, Slider spawnSlider, Toggle birdsToggle, Text speedText, Text spawnText, Button spawnGroundButton, Button spawnBirdButton, Button closeButton)
        {
            Panel = panel;
            SpeedSlider = speedSlider;
            SpawnSlider = spawnSlider;
            BirdsToggle = birdsToggle;
            SpeedText = speedText;
            SpawnText = spawnText;
            SpawnGroundButton = spawnGroundButton;
            SpawnBirdButton = spawnBirdButton;
            CloseButton = closeButton;
        }

        public GameObject Panel { get; }
        public Slider SpeedSlider { get; }
        public Slider SpawnSlider { get; }
        public Toggle BirdsToggle { get; }
        public Text SpeedText { get; }
        public Text SpawnText { get; }
        public Button SpawnGroundButton { get; }
        public Button SpawnBirdButton { get; }
        public Button CloseButton { get; }
    }
}
