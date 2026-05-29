using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RunnerUIManager : MonoBehaviour
{
    [SerializeField] private Text scoreText;
    [SerializeField] private Text highScoreText;
    [SerializeField] private Text speedText;
    [SerializeField] private Text hintText;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Text finalScoreText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private GameObject editorPanel;
    [SerializeField] private Slider speedSlider;
    [SerializeField] private Slider spawnSlider;
    [SerializeField] private Toggle birdsToggle;
    [SerializeField] private Text speedSliderText;
    [SerializeField] private Text spawnSliderText;
    [SerializeField] private Button spawnGroundHazardButton;
    [SerializeField] private Button spawnBirdButton;
    [SerializeField] private Button closeEditorButton;

    private RunnerGameManager gameManager;
    private RunnerObstacleSpawner spawner;
    private Text scoreLabelText;
    private bool uiReferencesResolved;
    private GameObject endRunMenuRoot;
    private bool endRunButtonBusy;
    private static readonly Vector2 EndRunReferenceResolution = new Vector2(1584f, 672f);
    private static readonly Vector2 EndRunArtOffset = new Vector2(0f, -4f);

    private void Awake()
    {
        ApplyRuntimeUiLayout();
        BuildEndRunMenu();

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ExitGame);
        }

        if (spawnGroundHazardButton != null)
        {
            spawnGroundHazardButton.onClick.AddListener(SpawnGroundHazard);
        }

        if (spawnBirdButton != null)
        {
            spawnBirdButton.onClick.AddListener(SpawnBird);
        }

        if (closeEditorButton != null)
        {
            closeEditorButton.onClick.AddListener(ToggleEditorPanel);
        }

        if (speedSlider != null)
        {
            speedSlider.onValueChanged.AddListener(SetSpeedMultiplier);
        }

        if (spawnSlider != null)
        {
            spawnSlider.onValueChanged.AddListener(SetSpawnMultiplier);
        }

        if (birdsToggle != null)
        {
            birdsToggle.onValueChanged.AddListener(SetBirdsEnabled);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (endRunMenuRoot != null)
        {
            endRunMenuRoot.SetActive(false);
        }

        if (hudPanel != null)
        {
            hudPanel.SetActive(true);
        }

        if (editorPanel != null)
        {
            editorPanel.SetActive(false);
        }

        if (speedText != null)
        {
            speedText.gameObject.SetActive(false);
        }

        if (hintText != null)
        {
            hintText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (IsEditorTogglePressed())
        {
            ToggleEditorPanel();
        }

        ApplyHudTextLayout();
        UpdateHud();
    }

    public void Initialize(RunnerGameManager manager, RunnerObstacleSpawner obstacleSpawner)
    {
        gameManager = manager;
        spawner = obstacleSpawner;

        if (speedSlider != null)
        {
            SetSpeedMultiplier(speedSlider.value);
        }

        if (spawnSlider != null)
        {
            SetSpawnMultiplier(spawnSlider.value);
        }

        if (birdsToggle != null)
        {
            SetBirdsEnabled(birdsToggle.isOn);
        }

        UpdateHud();
    }

    public void ShowGameOver(int score, int highScore, float runTime)
    {
        ApplyRuntimeUiLayout();
        BuildEndRunMenu();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (hudPanel != null)
        {
            hudPanel.SetActive(false);
        }

        if (finalScoreText != null)
        {
            finalScoreText.text = "SCORE " + score + "\nBEST: " + highScore;
        }

        if (endRunMenuRoot != null)
        {
            endRunMenuRoot.SetActive(true);
        }
    }

    private void UpdateHud()
    {
        if (gameManager == null)
        {
            gameManager = RunnerGameManager.Instance;
        }

        if (gameManager == null)
        {
            return;
        }

        if (scoreText != null)
        {
            scoreText.text = gameManager.Score.ToString();
        }

        if (highScoreText != null)
        {
            highScoreText.text = "BEST: " + Mathf.Max(gameManager.HighScore, gameManager.Score);
        }

        // The main HUD intentionally stays minimal: score, best score, and game-over stats only.
    }

    private void ApplyRuntimeUiLayout()
    {
        ResolveUiReferences();

        if (hudPanel == null)
        {
            GameObject foundHud = FindNamedObject("Score HUD Panel");
            if (foundHud != null)
            {
                hudPanel = foundHud;
            }
        }

        DisableImage("Score HUD Inlay");
        DisableImage("Score HUD Panel");
        DisableImage("Game Over Stats Inlay");
        ApplyHudPanelLayout();
        ApplyHudTextLayout();
        ApplyGameOverTextLayout();

        GameObject finalTime = FindNamedObject("Final Time");
        if (finalTime != null)
        {
            finalTime.SetActive(false);
        }
    }

    private void ApplyHudTextLayout()
    {
        ResolveUiReferences();

        if (scoreLabelText != null)
        {
            scoreLabelText.alignment = TextAnchor.UpperLeft;
            scoreLabelText.fontSize = 18;
            PrepareHudText(scoreLabelText);
            SetRect(scoreLabelText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -16f), new Vector2(150f, 24f));
        }

        if (scoreText != null)
        {
            scoreText.alignment = TextAnchor.UpperLeft;
            scoreText.fontSize = 44;
            PrepareHudText(scoreText);
            SetRect(scoreText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -42f), new Vector2(270f, 50f));
        }

        if (highScoreText != null)
        {
            highScoreText.alignment = TextAnchor.UpperLeft;
            highScoreText.fontSize = 16;
            PrepareHudText(highScoreText);
            SetRect(highScoreText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -96f), new Vector2(320f, 30f));
        }
    }

    private void ApplyHudPanelLayout()
    {
        if (hudPanel == null)
        {
            return;
        }

        RectTransform rect = hudPanel.GetComponent<RectTransform>();
        SetRect(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -14f), new Vector2(360f, 136f));
    }

    private void ApplyGameOverTextLayout()
    {
        if (finalScoreText == null)
        {
            return;
        }

        finalScoreText.alignment = TextAnchor.MiddleCenter;
        finalScoreText.fontSize = 30;
        finalScoreText.lineSpacing = 1.15f;
        finalScoreText.color = Color.black;
        finalScoreText.horizontalOverflow = HorizontalWrapMode.Overflow;
        finalScoreText.verticalOverflow = VerticalWrapMode.Overflow;
        SetRect(finalScoreText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 34f), new Vector2(620f, 110f));
    }

    private void ResolveUiReferences()
    {
        if (uiReferencesResolved &&
            scoreLabelText != null &&
            scoreText != null &&
            highScoreText != null &&
            finalScoreText != null)
        {
            return;
        }

        Text foundScoreLabel = FindText("Score Label");
        if (foundScoreLabel != null)
        {
            scoreLabelText = foundScoreLabel;
        }

        Text foundScore = FindText("Score Text");
        if (foundScore != null)
        {
            scoreText = foundScore;
        }

        Text foundBest = FindText("Best Text");
        if (foundBest != null)
        {
            highScoreText = foundBest;
        }

        Text foundFinalScore = FindText("Final Score");
        if (foundFinalScore != null)
        {
            finalScoreText = foundFinalScore;
        }

        uiReferencesResolved = scoreLabelText != null &&
            scoreText != null &&
            highScoreText != null &&
            finalScoreText != null;
    }

    private void PrepareHudText(Text text)
    {
        text.color = Color.black;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.resizeTextForBestFit = false;
    }

    private void BuildEndRunMenu()
    {
        if (endRunMenuRoot != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("End Run Menu Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6000;
        canvasObject.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = EndRunReferenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        endRunMenuRoot = new GameObject("End Run Menu Overlay");
        endRunMenuRoot.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = endRunMenuRoot.AddComponent<RectTransform>();
        Stretch(rootRect);

        Image backdrop = endRunMenuRoot.AddComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.86f);
        backdrop.raycastTarget = true;

        GameObject artObject = new GameObject("End Run Menu Art");
        artObject.transform.SetParent(endRunMenuRoot.transform, false);
        RectTransform artRect = artObject.AddComponent<RectTransform>();
        SetRect(artRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), EndRunArtOffset, EndRunReferenceResolution);

        Image image = artObject.AddComponent<Image>();
        image.sprite = LoadMenuSprite("HoboRushMenu/HoboRush_EndRun");
        image.preserveAspect = false;
        image.raycastTarget = true;
        image.color = Color.white;

        Button restart = CreateEndRunHitbox("End Run Restart Hitbox", artObject.transform, 552f, 220f, 498f, 96f);
        Button lobby = CreateEndRunHitbox("End Run Lobby Hitbox", artObject.transform, 552f, 337f, 498f, 92f);
        Button exit = CreateEndRunHitbox("End Run Exit Hitbox", artObject.transform, 552f, 449f, 498f, 96f);

        restart.onClick.AddListener(RestartGame);
        lobby.onClick.AddListener(ExitToLobby);
        exit.onClick.AddListener(ExitGame);
        endRunMenuRoot.SetActive(false);
    }

    private Button CreateEndRunHitbox(string objectName, Transform parent, float left, float top, float width, float height)
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

        float x = left + width * 0.5f - EndRunReferenceResolution.x * 0.5f;
        float y = EndRunReferenceResolution.y * 0.5f - (top + height * 0.5f);
        SetRect(obj.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(width, height));
        return button;
    }

    private Sprite LoadMenuSprite(string resourcePath)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            Debug.LogError("Missing menu texture in Resources: " + resourcePath);
            return null;
        }

        texture.filterMode = FilterMode.Point;
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private void DisableImage(string objectName)
    {
        GameObject target = FindNamedObject(objectName);
        if (target == null)
        {
            return;
        }

        Image image = target.GetComponent<Image>();
        if (image != null)
        {
            image.enabled = false;
        }
    }

    private GameObject FindNamedObject(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name == objectName)
            {
                return transforms[i].gameObject;
            }
        }

        return null;
    }

    private Text FindText(string objectName)
    {
        GameObject target = FindNamedObject(objectName);
        return target != null ? target.GetComponent<Text>() : null;
    }

    private void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private void Stretch(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void ToggleEditorPanel()
    {
        if (editorPanel == null)
        {
            return;
        }

        RunnerAudioManager.Instance?.PlayUiClick();
        editorPanel.SetActive(!editorPanel.activeSelf);
    }

    private void SetSpeedMultiplier(float value)
    {
        if (gameManager == null)
        {
            gameManager = RunnerGameManager.Instance;
        }

        if (gameManager != null)
        {
            gameManager.SetEditorSpeedMultiplier(value);
        }

        if (speedSliderText != null)
        {
            speedSliderText.text = "Speed x" + value.ToString("0.00");
        }
    }

    private void SetSpawnMultiplier(float value)
    {
        if (spawner == null)
        {
            spawner = FindFirstObjectByType<RunnerObstacleSpawner>();
        }

        if (spawner != null)
        {
            spawner.SetEditorIntervalMultiplier(value);
        }

        if (spawnSliderText != null)
        {
            spawnSliderText.text = "Spawn gap x" + value.ToString("0.00");
        }
    }

    private void SetBirdsEnabled(bool enabled)
    {
        if (spawner == null)
        {
            spawner = FindFirstObjectByType<RunnerObstacleSpawner>();
        }

        if (spawner != null)
        {
            spawner.SetBirdsEnabled(enabled);
        }
    }

    private void SpawnGroundHazard()
    {
        RunnerAudioManager.Instance?.PlayUiClick();
        if (spawner != null)
        {
            spawner.SpawnGroundHazard();
        }
    }

    private void SpawnBird()
    {
        RunnerAudioManager.Instance?.PlayUiClick();
        if (spawner != null)
        {
            spawner.SpawnBird();
        }
    }

    private void RestartGame()
    {
        if (endRunButtonBusy)
        {
            return;
        }

        RunnerAudioManager.Instance?.PlayUiClick();
        if (RunnerGameManager.Instance != null)
        {
            RunnerGameManager.Instance.RestartGame();
        }
    }

    private void ExitToLobby()
    {
        if (endRunButtonBusy)
        {
            return;
        }

        StartCoroutine(ClickThenLoadLobby());
    }

    private void ExitGame()
    {
        if (endRunButtonBusy)
        {
            return;
        }

        endRunButtonBusy = true;
        RunnerAudioManager.Instance?.PlayUiClick();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private System.Collections.IEnumerator ClickThenLoadLobby()
    {
        endRunButtonBusy = true;
        RunnerAudioManager.Instance?.PlayUiClick();
        yield return new WaitForSecondsRealtime(0.12f);
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private bool IsEditorTogglePressed()
    {
        bool pressed = false;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            pressed |= keyboard.eKey.wasPressedThisFrame;
        }
        return pressed;
    }
}
