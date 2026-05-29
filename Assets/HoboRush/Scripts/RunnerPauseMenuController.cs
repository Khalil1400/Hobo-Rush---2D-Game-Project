using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RunnerPauseMenuController : MonoBehaviour
{
    private const string GameSceneName = "HoboRush";
    private const string LobbySceneName = "MainMenu";
    private static readonly Vector2 ReferenceResolution = new Vector2(1589f, 672f);
    private static readonly Vector2 PauseArtOffset = new Vector2(0f, -20f);

    [SerializeField] private float clickDelay = 0.12f;

    private GameObject pauseRoot;
    private bool isPaused;
    private bool busy;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreateForScene(scene);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapCurrentScene()
    {
        TryCreateForScene(SceneManager.GetActiveScene());
    }

    private static void TryCreateForScene(Scene scene)
    {
        if (scene.name != GameSceneName || FindFirstObjectByType<RunnerPauseMenuController>() != null)
        {
            return;
        }

        GameObject controllerObject = new GameObject("Pause Menu Controller");
        controllerObject.AddComponent<RunnerPauseMenuController>();
    }

    private void Awake()
    {
        BuildPauseMenu();
        SetPaused(false);
    }

    private void OnDestroy()
    {
        if (isPaused)
        {
            Time.timeScale = 1f;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame || busy)
        {
            return;
        }

        if (RunnerGameManager.Instance != null && RunnerGameManager.Instance.IsGameOver)
        {
            return;
        }

        SetPaused(!isPaused);
        RunnerAudioManager.Instance?.PlayUiClick();
    }

    public void RestartRun()
    {
        if (busy)
        {
            return;
        }

        StartCoroutine(ClickThenLoad(GameSceneName));
    }

    public void ExitToLobby()
    {
        if (busy)
        {
            return;
        }

        StartCoroutine(ClickThenLoad(LobbySceneName));
    }

    public void ExitGame()
    {
        if (busy)
        {
            return;
        }

        StartCoroutine(ClickThenExit());
    }

    private IEnumerator ClickThenLoad(string sceneName)
    {
        busy = true;
        RunnerAudioManager.Instance?.PlayUiClick();
        yield return new WaitForSecondsRealtime(clickDelay);
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator ClickThenExit()
    {
        busy = true;
        RunnerAudioManager.Instance?.PlayUiClick();
        yield return new WaitForSecondsRealtime(clickDelay);
        Time.timeScale = 1f;
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetPaused(bool paused)
    {
        isPaused = paused;
        if (pauseRoot != null)
        {
            pauseRoot.SetActive(paused);
        }

        Time.timeScale = paused ? 0f : 1f;
    }

    private void BuildPauseMenu()
    {
        GameObject canvasObject = new GameObject("Pause Menu Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        canvasObject.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        pauseRoot = new GameObject("Pause Menu Overlay");
        pauseRoot.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = pauseRoot.AddComponent<RectTransform>();
        Stretch(rootRect);

        Image backdrop = pauseRoot.AddComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.86f);
        backdrop.raycastTarget = true;

        GameObject artObject = new GameObject("Pause Menu Art");
        artObject.transform.SetParent(pauseRoot.transform, false);
        RectTransform artRect = artObject.AddComponent<RectTransform>();
        Center(artRect, PauseArtOffset, ReferenceResolution);

        Image image = artObject.AddComponent<Image>();
        image.sprite = LoadPauseSprite();
        image.preserveAspect = false;
        image.raycastTarget = true;
        image.color = Color.white;

        Button restart = CreateHitboxButton("Restart Hitbox", artObject.transform, 560f, 206f, 490f, 94f);
        Button lobby = CreateHitboxButton("Exit To Lobby Hitbox", artObject.transform, 560f, 319f, 490f, 92f);
        Button exit = CreateHitboxButton("Exit Game Hitbox", artObject.transform, 560f, 427f, 490f, 94f);

        restart.onClick.AddListener(RestartRun);
        lobby.onClick.AddListener(ExitToLobby);
        exit.onClick.AddListener(ExitGame);
        EnsureEventSystem();
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

    private Sprite LoadPauseSprite()
    {
        Texture2D texture = Resources.Load<Texture2D>("HoboRushMenu/HoboRush_Pause");
        if (texture == null)
        {
            Debug.LogError("Missing pause menu texture at Resources/HoboRushMenu/HoboRush_Pause");
            return null;
        }

        texture.filterMode = FilterMode.Point;
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
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

    private void Center(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
