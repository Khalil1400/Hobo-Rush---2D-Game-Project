using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RunnerMainMenuController : MonoBehaviour
{
    private const string MusicVolumeKey = "HoboRushMusicVolume";
    private const string SfxVolumeKey = "HoboRushSfxVolume";
    private const string HighScoreKey = "HoboRushHighScore";

    [SerializeField] private string gameSceneName = "HoboRush";
    [SerializeField] private GameObject mainButtonRoot;
    [SerializeField] private GameObject creditsRoot;
    [SerializeField] private GameObject optionsRoot;
    [SerializeField] private AudioSource clickSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Text musicValueText;
    [SerializeField] private Text sfxValueText;
    [SerializeField] private Text bestScoreText;
    [SerializeField, Range(0.02f, 0.3f)] private float clickDelay = 0.12f;

    private bool busy;
    private Font runtimeFont;
    private static readonly Vector2 ReferenceResolution = new Vector2(1568f, 672f);
    private static readonly Vector2 BoardArtOffset = new Vector2(36f, 24f);
    private static readonly Vector2 OptionsArtOffset = new Vector2(501f, 0f);
    private const float OptionsContentX = 501f;
    private static readonly Color Ink = new Color(0.02f, 0.03f, 0.04f, 1f);

    private void Awake()
    {
        if (mainButtonRoot == null || creditsRoot == null || optionsRoot == null)
        {
            BuildRuntimeMenu();
        }

        NormalizeAudioListeners();
        ShowMainImmediate();
        InitializeSliders();
        StartMenuMusic();
        UpdateBestScoreText();
    }

    public void PlayGame()
    {
        if (busy)
        {
            return;
        }

        StartCoroutine(ClickThenLoadGame());
    }

    public void ShowOptions()
    {
        PlayClick();
        if (mainButtonRoot != null)
        {
            mainButtonRoot.SetActive(false);
        }

        if (creditsRoot != null)
        {
            creditsRoot.SetActive(false);
        }

        if (optionsRoot != null)
        {
            optionsRoot.SetActive(true);
        }

        UpdateBestScoreText();
    }

    public void ShowCredits()
    {
        PlayClick();
        if (mainButtonRoot != null)
        {
            mainButtonRoot.SetActive(false);
        }

        if (optionsRoot != null)
        {
            optionsRoot.SetActive(false);
        }

        if (creditsRoot != null)
        {
            creditsRoot.SetActive(true);
        }
    }

    public void ShowMain()
    {
        PlayClick();
        ShowMainImmediate();
    }

    public void ExitGame()
    {
        if (busy)
        {
            return;
        }

        StartCoroutine(ClickThenExit());
    }

    public void SetMusicVolume(float value)
    {
        float clamped = Mathf.Clamp(value, 0f, 0.2f);
        PlayerPrefs.SetFloat(MusicVolumeKey, clamped);
        PlayerPrefs.Save();

        if (musicSource != null)
        {
            musicSource.volume = clamped;
        }

        if (RunnerAudioManager.Instance != null)
        {
            RunnerAudioManager.Instance.SetMusicVolume(clamped);
        }

        if (musicValueText != null)
        {
            musicValueText.text = "MUSIC " + Mathf.RoundToInt(clamped * 100f) + "%";
        }
    }

    public void SetSfxVolume(float value)
    {
        float clamped = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxVolumeKey, clamped);
        PlayerPrefs.Save();

        if (RunnerAudioManager.Instance != null)
        {
            RunnerAudioManager.Instance.SetSfxVolume(clamped);
        }

        if (sfxValueText != null)
        {
            sfxValueText.text = "SFX " + Mathf.RoundToInt(clamped * 100f) + "%";
        }
    }

    public void ResetBestScore()
    {
        PlayClick();
        PlayerPrefs.DeleteKey(HighScoreKey);
        PlayerPrefs.Save();
        UpdateBestScoreText();
    }

    private IEnumerator ClickThenLoadGame()
    {
        busy = true;
        PlayClick();
        yield return new WaitForSecondsRealtime(clickDelay);
        SceneManager.LoadScene(gameSceneName);
    }

    private IEnumerator ClickThenExit()
    {
        busy = true;
        PlayClick();
        yield return new WaitForSecondsRealtime(clickDelay);
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void PlayClick()
    {
        if (RunnerAudioManager.Instance != null)
        {
            RunnerAudioManager.Instance.PlayUiClick();
            return;
        }

        if (clickSource == null || clickClip == null)
        {
            return;
        }

        float sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        clickSource.PlayOneShot(clickClip, Mathf.Clamp01(sfxVolume));
    }

    private void ShowMainImmediate()
    {
        if (mainButtonRoot != null)
        {
            mainButtonRoot.SetActive(true);
        }

        if (creditsRoot != null)
        {
            creditsRoot.SetActive(false);
        }

        if (optionsRoot != null)
        {
            optionsRoot.SetActive(false);
        }
    }

    private void InitializeSliders()
    {
        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(MusicVolumeKey, 0.1f));
            SetMusicVolume(musicSlider.value);
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));
            SetSfxVolume(sfxSlider.value);
        }
    }

    private void StartMenuMusic()
    {
        if (RunnerAudioManager.Instance != null)
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Stop();
            }

            return;
        }

        if (musicSource == null)
        {
            return;
        }

        if (musicClip != null)
        {
            musicSource.clip = musicClip;
        }

        if (musicSource.clip == null)
        {
            return;
        }

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.volume = PlayerPrefs.GetFloat(MusicVolumeKey, 0.1f);
        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    private void BuildRuntimeMenu()
    {
        CreateMenuCamera();
        Canvas canvas = CreateMenuCanvas();
        Sprite mainSprite = LoadResourceSprite("HoboRushMenu/HoboRush_MainMenu");
        Sprite creditsSprite = LoadResourceSprite("HoboRushMenu/HoboRush_Credits");
        Sprite optionsSprite = LoadResourceSprite("HoboRushMenu/HoboRush_Options");

        CreateFullScreenImage("Main Menu Art", canvas.transform, mainSprite, true);
        creditsRoot = CreateFullScreenImage("Credits Art", canvas.transform, creditsSprite, false);
        ApplyBoardArtPlacement(creditsRoot.GetComponent<RectTransform>());

        mainButtonRoot = new GameObject("Main Menu Button Hitboxes");
        mainButtonRoot.transform.SetParent(canvas.transform, false);
        Button playButton = CreateHitboxButton("Play Hitbox", mainButtonRoot.transform, 1140f, 104f, 304f, 96f);
        Button optionsButton = CreateHitboxButton("Options Hitbox", mainButtonRoot.transform, 1138f, 211f, 306f, 94f);
        Button creditsButton = CreateHitboxButton("Credits Hitbox", mainButtonRoot.transform, 1138f, 318f, 306f, 94f);
        Button exitButton = CreateHitboxButton("Exit Hitbox", mainButtonRoot.transform, 1148f, 428f, 286f, 88f);
        Button creditsBackButton = CreateHitboxButton("Credits Back Hitbox", creditsRoot.transform, 1328f, 516f, 150f, 74f);

        optionsRoot = CreateOptionsPanel(canvas.transform, optionsSprite, out musicSlider, out sfxSlider, out musicValueText, out sfxValueText, out bestScoreText, out Button resetButton, out Button optionsBackButton);

        clickSource = gameObject.AddComponent<AudioSource>();
        clickSource.playOnAwake = false;
        clickSource.spatialBlend = 0f;
        clickClip = Resources.Load<AudioClip>("HoboRushAudio/Runner_UI_Click");

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicClip = Resources.Load<AudioClip>("HoboRushAudio/Runner_Music");

        playButton.onClick.AddListener(PlayGame);
        optionsButton.onClick.AddListener(ShowOptions);
        creditsButton.onClick.AddListener(ShowCredits);
        exitButton.onClick.AddListener(ExitGame);
        creditsBackButton.onClick.AddListener(ShowMain);
        optionsBackButton.onClick.AddListener(ShowMain);
        resetButton.onClick.AddListener(ResetBestScore);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSfxVolume);
        EnsureEventSystem();
    }

    private void CreateMenuCamera()
    {
        if (Camera.main != null)
        {
            if (FindFirstObjectByType<AudioListener>() == null)
            {
                Camera.main.gameObject.AddComponent<AudioListener>();
            }

            return;
        }

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.33f, 0.72f, 0.92f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        cameraObject.AddComponent<AudioListener>();
    }

    private void NormalizeAudioListeners()
    {
        AudioListener[] activeListeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (activeListeners.Length == 0)
        {
            CreateMenuCamera();
            activeListeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        }

        if (activeListeners.Length <= 1)
        {
            return;
        }

        AudioListener preferred = Camera.main != null ? Camera.main.GetComponent<AudioListener>() : activeListeners[0];
        if (preferred != null)
        {
            preferred.enabled = true;
        }

        if (preferred == null)
        {
            preferred = activeListeners[0];
        }

        for (int i = 0; i < activeListeners.Length; i++)
        {
            activeListeners[i].enabled = activeListeners[i] == preferred;
        }
    }

    private Canvas CreateMenuCanvas()
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

    private GameObject CreateFullScreenImage(string objectName, Transform parent, Sprite sprite, bool active)
    {
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = false;
        image.raycastTarget = false;
        Stretch(image.rectTransform);
        obj.SetActive(active);
        return obj;
    }

    private GameObject CreateOptionsPanel(Transform parent, Sprite optionsSprite, out Slider music, out Slider sfx, out Text musicText, out Text sfxText, out Text bestText, out Button resetButton, out Button backButton)
    {
        GameObject root = new GameObject("Options Panel");
        root.transform.SetParent(parent, false);
        Stretch(root.AddComponent<RectTransform>());

        GameObject art = CreateFullScreenImage("Options Art", root.transform, optionsSprite, true);
        ApplyOptionsArtPlacement(art.GetComponent<RectTransform>());

        musicText = CreateText("Music Value", root.transform, "MUSIC 10%", 22, TextAnchor.MiddleCenter, Color.white);
        Center(musicText.rectTransform, new Vector2(OptionsContentX, 92f), new Vector2(430f, 36f));
        music = CreateSlider("Music Slider", root.transform, new Vector2(OptionsContentX, 54f), 0f, 0.2f, 0.1f);

        sfxText = CreateText("SFX Value", root.transform, "SFX 100%", 22, TextAnchor.MiddleCenter, Color.white);
        Center(sfxText.rectTransform, new Vector2(OptionsContentX, 0f), new Vector2(430f, 36f));
        sfx = CreateSlider("SFX Slider", root.transform, new Vector2(OptionsContentX, -38f), 0f, 1f, 1f);

        bestText = CreateText("Best Score Text", root.transform, "BEST SCORE 0", 18, TextAnchor.MiddleCenter, Color.white);
        Center(bestText.rectTransform, new Vector2(OptionsContentX, -98f), new Vector2(430f, 34f));

        resetButton = CreateVisibleButton("Reset Best Button", root.transform, "RESET", new Vector2(OptionsContentX - 92f, -136f), new Vector2(170f, 42f), new Color(1f, 0.78f, 0.06f, 1f));
        backButton = CreateVisibleButton("Options Back Button", root.transform, "BACK", new Vector2(OptionsContentX + 116f, -136f), new Vector2(126f, 42f), new Color(1f, 0.78f, 0.06f, 1f));
        root.SetActive(false);
        return root;
    }

    private Button CreateHitboxButton(string objectName, Transform parent, float left, float top, float width, float height)
    {
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.001f);
        image.raycastTarget = true;

        Button button = obj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0.001f);
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.08f);
        colors.pressedColor = new Color(0f, 0f, 0f, 0.08f);
        colors.selectedColor = colors.normalColor;
        colors.disabledColor = new Color(1f, 1f, 1f, 0f);
        button.colors = colors;

        float x = left + width * 0.5f - ReferenceResolution.x * 0.5f;
        float y = ReferenceResolution.y * 0.5f - (top + height * 0.5f);
        Center(obj.GetComponent<RectTransform>(), new Vector2(x, y), new Vector2(width, height));
        return button;
    }

    private Button CreateVisibleButton(string objectName, Transform parent, string label, Vector2 position, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(parent, false);
        Image shadow = obj.AddComponent<Image>();
        shadow.color = new Color(0.16f, 0.09f, 0.04f, 0.85f);
        shadow.raycastTarget = false;
        Center(obj.GetComponent<RectTransform>(), position + new Vector2(0f, -4f), size + new Vector2(8f, 8f));

        GameObject face = new GameObject("Face");
        face.transform.SetParent(obj.transform, false);
        Image image = face.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        Button button = face.AddComponent<Button>();
        button.targetGraphic = image;
        Center(image.rectTransform, new Vector2(0f, 4f), size);

        Text text = CreateText("Label", face.transform, label, 16, TextAnchor.MiddleCenter, Ink);
        Stretch(text.rectTransform);
        return button;
    }

    private Slider CreateSlider(string objectName, Transform parent, Vector2 position, float min, float max, float value)
    {
        GameObject sliderObject = new GameObject(objectName);
        sliderObject.transform.SetParent(parent, false);
        Center(sliderObject.AddComponent<RectTransform>(), position, new Vector2(350f, 28f));

        GameObject background = new GameObject("Background");
        background.transform.SetParent(sliderObject.transform, false);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.16f, 0.09f, 0.05f, 0.96f);
        Stretch(background.GetComponent<RectTransform>());

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        Stretch(fillAreaRect);
        fillAreaRect.offsetMin = new Vector2(5f, 5f);
        fillAreaRect.offsetMax = new Vector2(-5f, -5f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(1f, 0.79f, 0.09f, 1f);
        Stretch(fill.GetComponent<RectTransform>());

        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObject.transform, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        Stretch(handleAreaRect);
        handleAreaRect.offsetMin = new Vector2(10f, 0f);
        handleAreaRect.offsetMax = new Vector2(-10f, 0f);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(1f, 0.84f, 0.13f, 1f);
        Center(handle.GetComponent<RectTransform>(), Vector2.zero, new Vector2(34f, 38f));

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

    private Text CreateText(string objectName, Transform parent, string value, int size, TextAnchor alignment, Color color)
    {
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(parent, false);
        Text text = obj.AddComponent<Text>();
        text.text = value;
        text.font = RuntimeFont();
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private Sprite LoadResourceSprite(string path)
    {
        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture == null)
        {
            Debug.LogError("Missing main menu texture in Resources: " + path);
            return null;
        }

        texture.filterMode = FilterMode.Point;
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Font RuntimeFont()
    {
        if (runtimeFont == null)
        {
            runtimeFont = Resources.Load<Font>("HoboRushFonts/PressStart2P-Regular");
        }

        if (runtimeFont == null)
        {
            runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        return runtimeFont;
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void ApplyBoardArtPlacement(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        Stretch(rect);
        rect.offsetMin = BoardArtOffset;
        rect.offsetMax = BoardArtOffset;
    }

    private void ApplyOptionsArtPlacement(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        Stretch(rect);
        rect.offsetMin = OptionsArtOffset;
        rect.offsetMax = OptionsArtOffset;
    }

    private void Center(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void UpdateBestScoreText()
    {
        if (bestScoreText != null)
        {
            bestScoreText.text = "BEST SCORE " + PlayerPrefs.GetInt(HighScoreKey, 0);
        }
    }
}
