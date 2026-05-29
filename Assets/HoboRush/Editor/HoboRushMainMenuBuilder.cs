using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;

public static class HoboRushMainMenuBuilder
{
    private const string ScenePath = "Assets/HoboRush/Scenes/MainMenu.unity";
    private const string GameScenePath = "Assets/HoboRush/Scenes/HoboRush.unity";
    private const string MenuSpritePath = "Assets/HoboRush/Art/MainMenu/HoboRush_MainMenu.png";
    private const string CreditsSpritePath = "Assets/HoboRush/Art/MainMenu/HoboRush_Credits.png";
    private const string OptionsSpritePath = "Assets/HoboRush/Art/MainMenu/HoboRush_Options.png";
    private const string FontPath = "Assets/HoboRush/Fonts/PressStart2P-Regular.ttf";
    private const string PixelUiFolder = "Assets/HoboRush/Art/KenneyPixelUI";
    private const string AudioFolder = "Assets/HoboRush/Audio";
    private const string MusicPath = "Assets/Resources/HoboRushAudio/Runner_Music.ogg";
    private static readonly Vector2 ReferenceResolution = new Vector2(1568f, 672f);
    private static readonly Vector2 BoardArtOffset = new Vector2(36f, 24f);
    private static readonly Vector2 OptionsArtOffset = new Vector2(501f, 0f);
    private const float OptionsContentX = 501f;
    private static readonly Color Ink = new Color(0.02f, 0.03f, 0.04f, 1f);

    [MenuItem("Tools/Hobo Rush/Build Main Menu Scene")]
    public static void BuildMainMenuScene()
    {
        ImportMenuTexture(MenuSpritePath);
        ImportMenuTexture(CreditsSpritePath);
        ImportMenuTexture(OptionsSpritePath);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MainMenu";

        CreateCamera();
        Canvas canvas = CreateCanvas();
        GameObject mainBackground = CreateFullScreenImage("Main Menu Art", canvas.transform, LoadSprite(MenuSpritePath), true);
        GameObject creditsRoot = CreateFullScreenImage("Credits Art", canvas.transform, LoadSprite(CreditsSpritePath), false);
        ApplyBoardArtPlacement(creditsRoot.GetComponent<RectTransform>());
        GameObject mainButtonRoot = new GameObject("Main Menu Button Hitboxes");
        mainButtonRoot.transform.SetParent(canvas.transform, false);

        Button playButton = CreateHitboxButton("Play Hitbox", mainButtonRoot.transform, 1140f, 104f, 304f, 96f);
        Button optionsButton = CreateHitboxButton("Options Hitbox", mainButtonRoot.transform, 1138f, 211f, 306f, 94f);
        Button creditsButton = CreateHitboxButton("Credits Hitbox", mainButtonRoot.transform, 1138f, 318f, 306f, 94f);
        Button exitButton = CreateHitboxButton("Exit Hitbox", mainButtonRoot.transform, 1148f, 428f, 286f, 88f);
        Button backCreditsButton = CreateHitboxButton("Credits Back Hitbox", creditsRoot.transform, 1328f, 516f, 150f, 74f);

        GameObject optionsRoot = CreateOptionsPanel(canvas.transform, LoadSprite(OptionsSpritePath), out Slider musicSlider, out Slider sfxSlider, out Text musicText, out Text sfxText, out Text bestText, out Button resetButton, out Button backOptionsButton);
        optionsRoot.SetActive(false);

        GameObject controllerObject = new GameObject("Main Menu Controller");
        RunnerMainMenuController controller = controllerObject.AddComponent<RunnerMainMenuController>();
        AudioSource clickSource = controllerObject.AddComponent<AudioSource>();
        AudioSource musicSource = controllerObject.AddComponent<AudioSource>();
        clickSource.playOnAwake = false;
        clickSource.loop = false;
        clickSource.spatialBlend = 0f;
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;

        ConfigureController(controller, mainButtonRoot, creditsRoot, optionsRoot, clickSource, musicSource, musicSlider, sfxSlider, musicText, sfxText, bestText);
        AddButtonListeners(controller, playButton, optionsButton, creditsButton, exitButton, backCreditsButton, backOptionsButton, resetButton, musicSlider, sfxSlider);
        CreateEventSystem();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Hobo Rush main menu scene built at " + ScenePath);

        _ = mainBackground;
    }

    public static void BuildMainMenuSceneBatch()
    {
        try
        {
            BuildMainMenuScene();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.33f, 0.72f, 0.92f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        cameraObject.AddComponent<AudioListener>();
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Main Menu Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static GameObject CreateFullScreenImage(string name, Transform parent, Sprite sprite, bool active)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = false;
        image.raycastTarget = false;
        SetStretch(obj.GetComponent<RectTransform>());
        obj.SetActive(active);
        return obj;
    }

    private static GameObject CreateOptionsPanel(Transform parent, Sprite optionsSprite, out Slider musicSlider, out Slider sfxSlider, out Text musicText, out Text sfxText, out Text bestText, out Button resetButton, out Button backButton)
    {
        GameObject root = new GameObject("Options Panel");
        root.transform.SetParent(parent, false);
        SetStretch(root.AddComponent<RectTransform>());

        GameObject art = CreateFullScreenImage("Options Art", root.transform, optionsSprite, true);
        ApplyOptionsArtPlacement(art.GetComponent<RectTransform>());

        musicText = CreateText("Music Value", root.transform, "MUSIC 10%", 22, TextAnchor.MiddleCenter, Color.white);
        SetCenteredRect(musicText.rectTransform, new Vector2(OptionsContentX, 92f), new Vector2(430f, 36f));
        musicSlider = CreateSlider("Music Slider", root.transform, new Vector2(OptionsContentX, 54f), 0f, 0.2f, 0.1f);

        sfxText = CreateText("SFX Value", root.transform, "SFX 100%", 22, TextAnchor.MiddleCenter, Color.white);
        SetCenteredRect(sfxText.rectTransform, new Vector2(OptionsContentX, 0f), new Vector2(430f, 36f));
        sfxSlider = CreateSlider("SFX Slider", root.transform, new Vector2(OptionsContentX, -38f), 0f, 1f, 1f);

        bestText = CreateText("Best Score Text", root.transform, "BEST SCORE 0", 18, TextAnchor.MiddleCenter, Color.white);
        SetCenteredRect(bestText.rectTransform, new Vector2(OptionsContentX, -98f), new Vector2(430f, 34f));

        resetButton = CreatePixelButton("Reset Best Button", root.transform, "RESET", new Vector2(OptionsContentX - 92f, -136f), new Vector2(170f, 42f), false);
        backButton = CreatePixelButton("Options Back Button", root.transform, "BACK", new Vector2(OptionsContentX + 116f, -136f), new Vector2(126f, 42f), false);
        return root;
    }

    private static Button CreateHitboxButton(string name, Transform parent, float left, float top, float width, float height)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.001f);
        image.raycastTarget = true;

        Button button = obj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0.001f);
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.08f);
        colors.pressedColor = new Color(0f, 0f, 0f, 0.08f);
        colors.selectedColor = new Color(1f, 1f, 1f, 0.001f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0f);
        button.colors = colors;

        RectTransform rect = obj.GetComponent<RectTransform>();
        float x = left + width * 0.5f - ReferenceResolution.x * 0.5f;
        float y = ReferenceResolution.y * 0.5f - (top + height * 0.5f);
        SetCenteredRect(rect, new Vector2(x, y), new Vector2(width, height));
        return button;
    }

    private static Button CreatePixelButton(string name, Transform parent, string label, Vector2 position, Vector2 size, bool red)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.sprite = LoadSprite(PixelUiFolder + (red ? "/9-Slice_Colored_red.png" : "/9-Slice_Colored_yellow.png"));
        image.type = Image.Type.Sliced;
        image.raycastTarget = true;

        Button button = obj.AddComponent<Button>();
        SpriteState state = button.spriteState;
        state.highlightedSprite = image.sprite;
        state.selectedSprite = image.sprite;
        state.pressedSprite = LoadSprite(PixelUiFolder + (red ? "/9-Slice_Colored_red_pressed.png" : "/9-Slice_Colored_yellow_pressed.png"));
        button.spriteState = state;
        button.transition = Selectable.Transition.SpriteSwap;

        SetCenteredRect(obj.GetComponent<RectTransform>(), position, size);

        Text text = CreateText("Label", obj.transform, label, 16, TextAnchor.MiddleCenter, Ink);
        SetStretch(text.rectTransform);
        return button;
    }

    private static Slider CreateSlider(string name, Transform parent, Vector2 position, float min, float max, float value)
    {
        GameObject sliderObject = new GameObject(name);
        sliderObject.transform.SetParent(parent, false);
        SetCenteredRect(sliderObject.AddComponent<RectTransform>(), position, new Vector2(350f, 28f));

        GameObject background = new GameObject("Background");
        background.transform.SetParent(sliderObject.transform, false);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.16f, 0.09f, 0.05f, 0.96f);
        SetStretch(background.GetComponent<RectTransform>());

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        SetStretch(fillAreaRect);
        fillAreaRect.offsetMin = new Vector2(5f, 5f);
        fillAreaRect.offsetMax = new Vector2(-5f, -5f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(1f, 0.79f, 0.09f, 1f);
        SetStretch(fill.GetComponent<RectTransform>());

        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObject.transform, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        SetStretch(handleAreaRect);
        handleAreaRect.offsetMin = new Vector2(10f, 0f);
        handleAreaRect.offsetMax = new Vector2(-10f, 0f);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.sprite = LoadSprite(PixelUiFolder + "/9-Slice_Colored_yellow.png");
        handleImage.type = Image.Type.Sliced;
        SetCenteredRect(handle.GetComponent<RectTransform>(), Vector2.zero, new Vector2(34f, 38f));

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handle.GetComponent<RectTransform>();
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private static Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Text text = obj.AddComponent<Text>();
        text.text = value;
        text.font = ProjectFont();
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static void ConfigureController(RunnerMainMenuController controller, GameObject mainButtons, GameObject creditsRoot, GameObject optionsRoot, AudioSource clickSource, AudioSource musicSource, Slider musicSlider, Slider sfxSlider, Text musicText, Text sfxText, Text bestText)
    {
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("gameSceneName").stringValue = "HoboRush";
        serialized.FindProperty("mainButtonRoot").objectReferenceValue = mainButtons;
        serialized.FindProperty("creditsRoot").objectReferenceValue = creditsRoot;
        serialized.FindProperty("optionsRoot").objectReferenceValue = optionsRoot;
        serialized.FindProperty("clickSource").objectReferenceValue = clickSource;
        serialized.FindProperty("musicSource").objectReferenceValue = musicSource;
        serialized.FindProperty("clickClip").objectReferenceValue = LoadAudio("Runner_UI_Click");
        serialized.FindProperty("musicClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);
        serialized.FindProperty("musicSlider").objectReferenceValue = musicSlider;
        serialized.FindProperty("sfxSlider").objectReferenceValue = sfxSlider;
        serialized.FindProperty("musicValueText").objectReferenceValue = musicText;
        serialized.FindProperty("sfxValueText").objectReferenceValue = sfxText;
        serialized.FindProperty("bestScoreText").objectReferenceValue = bestText;
        serialized.FindProperty("clickDelay").floatValue = 0.12f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddButtonListeners(RunnerMainMenuController controller, Button play, Button options, Button credits, Button exit, Button creditsBack, Button optionsBack, Button resetBest, Slider musicSlider, Slider sfxSlider)
    {
        UnityEventTools.AddPersistentListener(play.onClick, controller.PlayGame);
        UnityEventTools.AddPersistentListener(options.onClick, controller.ShowOptions);
        UnityEventTools.AddPersistentListener(credits.onClick, controller.ShowCredits);
        UnityEventTools.AddPersistentListener(exit.onClick, controller.ExitGame);
        UnityEventTools.AddPersistentListener(creditsBack.onClick, controller.ShowMain);
        UnityEventTools.AddPersistentListener(optionsBack.onClick, controller.ShowMain);
        UnityEventTools.AddPersistentListener(resetBest.onClick, controller.ResetBestScore);
        UnityEventTools.AddPersistentListener(musicSlider.onValueChanged, controller.SetMusicVolume);
        UnityEventTools.AddPersistentListener(sfxSlider.onValueChanged, controller.SetSfxVolume);
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }

    private static void ImportMenuTexture(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException("Missing menu texture at " + path);
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
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

    private static Font ProjectFont()
    {
        Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static void SetStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void ApplyBoardArtPlacement(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        SetStretch(rect);
        rect.offsetMin = BoardArtOffset;
        rect.offsetMax = BoardArtOffset;
    }

    private static void ApplyOptionsArtPlacement(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        SetStretch(rect);
        rect.offsetMin = OptionsArtOffset;
        rect.offsetMax = OptionsArtOffset;
    }

    private static void SetCenteredRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
