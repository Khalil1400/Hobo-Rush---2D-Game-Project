using UnityEngine;
using UnityEngine.SceneManagement;

public class RunnerGameManager : MonoBehaviour
{
    [SerializeField] private RunnerUIManager uiManager;
    [SerializeField] private RunnerObstacleSpawner obstacleSpawner;
    [SerializeField] private RunnerPlayerController player;
    [SerializeField] private RunnerDifficultySettings difficultySettings;
    [SerializeField] private float startingSpeed = 8f;
    [SerializeField] private float maxSpeed = 22f;
    [SerializeField] private float speedIncreasePerSecond = 0.045f;
    [SerializeField] private int scoreSpeedStep = 100;
    [SerializeField] private float speedIncreasePerScoreStep = 0.12f;
    [SerializeField] private float scorePerMeter = 8f;

    private float runTime;
    private float distance;
    private float baseSpeed;
    private float editorSpeedMultiplier = 1f;
    private int score;
    private int highScore;
    private int nextMilestoneScore = 500;
    private bool isGameOver;

    public static RunnerGameManager Instance { get; private set; }
    public bool IsGameOver => isGameOver;
    public float RunTime => runTime;
    public float Distance => distance;
    public float CurrentSpeed => baseSpeed * editorSpeedMultiplier;
    public int Score => score;
    public int HighScore => highScore;
    public RunnerDifficultySettings DifficultySettings => difficultySettings;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Time.timeScale = 1f;
        highScore = PlayerPrefs.GetInt("HoboRushHighScore", 0);
        baseSpeed = ResolveStartingSpeed();
    }

    private void Start()
    {
        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<RunnerUIManager>();
        }

        if (obstacleSpawner == null)
        {
            obstacleSpawner = FindFirstObjectByType<RunnerObstacleSpawner>();
        }

        if (player == null)
        {
            player = FindFirstObjectByType<RunnerPlayerController>();
        }

        if (uiManager != null)
        {
            uiManager.Initialize(this, obstacleSpawner);
        }
    }

    private void Update()
    {
        if (isGameOver)
        {
            return;
        }

        runTime += Time.deltaTime;
        int resolvedScoreStep = ResolveScoreSpeedStep();
        float resolvedScoreSpeedIncrease = Mathf.Max(0f, ResolveSpeedIncreasePerScoreStep());
        int scoreSpeedLevel = score / resolvedScoreStep;
        baseSpeed = Mathf.Min(ResolveMaxSpeed(), ResolveStartingSpeed() + runTime * ResolveSpeedIncreasePerSecond() + scoreSpeedLevel * resolvedScoreSpeedIncrease);
        distance += CurrentSpeed * Time.deltaTime;
        score = Mathf.FloorToInt(distance * ResolveScorePerMeter());

        if (score >= nextMilestoneScore)
        {
            RunnerAudioManager.Instance?.PlayMilestone();
            nextMilestoneScore += 500;
        }
    }

    public void SetEditorSpeedMultiplier(float multiplier)
    {
        editorSpeedMultiplier = Mathf.Clamp(multiplier, 0.55f, 1.65f);
    }

    public void GameOver()
    {
        if (isGameOver)
        {
            return;
        }

        isGameOver = true;
        highScore = Mathf.Max(highScore, score);
        PlayerPrefs.SetInt("HoboRushHighScore", highScore);
        PlayerPrefs.Save();
        RunnerAudioManager.Instance?.PlayGameOver();

        if (player != null)
        {
            player.SetDead();
        }

        if (uiManager != null)
        {
            uiManager.ShowGameOver(score, highScore, runTime);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Configure(RunnerUIManager ui, RunnerObstacleSpawner spawner, RunnerPlayerController playerController)
    {
        uiManager = ui;
        obstacleSpawner = spawner;
        player = playerController;
    }

    private float ResolveStartingSpeed()
    {
        return difficultySettings != null ? difficultySettings.StartingSpeed : startingSpeed;
    }

    private float ResolveMaxSpeed()
    {
        return difficultySettings != null ? difficultySettings.MaxSpeed : maxSpeed;
    }

    private float ResolveSpeedIncreasePerSecond()
    {
        return difficultySettings != null ? difficultySettings.SpeedIncreasePerSecond : speedIncreasePerSecond;
    }

    private int ResolveScoreSpeedStep()
    {
        return difficultySettings != null ? difficultySettings.ScoreSpeedStep : Mathf.Max(1, scoreSpeedStep);
    }

    private float ResolveSpeedIncreasePerScoreStep()
    {
        return difficultySettings != null ? difficultySettings.SpeedIncreasePerScoreStep : speedIncreasePerScoreStep;
    }

    private float ResolveScorePerMeter()
    {
        return difficultySettings != null ? difficultySettings.ScorePerMeter : scorePerMeter;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
