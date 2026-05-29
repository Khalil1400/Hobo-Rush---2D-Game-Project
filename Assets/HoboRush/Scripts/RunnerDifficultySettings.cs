using UnityEngine;

[CreateAssetMenu(menuName = "Hobo Rush/Difficulty Settings", fileName = "RunnerDifficultySettings")]
public class RunnerDifficultySettings : ScriptableObject
{
    [Header("Speed")]
    [SerializeField] private float startingSpeed = 8.2f;
    [SerializeField] private float maxSpeed = 23.5f;
    [SerializeField] private float speedIncreasePerSecond = 0.052f;
    [SerializeField] private int scoreSpeedStep = 100;
    [SerializeField] private float speedIncreasePerScoreStep = 0.12f;
    [SerializeField] private float scorePerMeter = 8f;

    [Header("Spawning")]
    [SerializeField] private float firstSpawnDelay = 1.2f;
    [SerializeField] private float minSpawnInterval = 0.72f;
    [SerializeField] private float maxSpawnInterval = 1.42f;
    [SerializeField] private float intervalSpeedPressure = 0.03f;
    [SerializeField] private float globalMinimumGap = 0.62f;
    [SerializeField] private float birdHeightOffset = -0.15f;

    public float StartingSpeed => startingSpeed;
    public float MaxSpeed => maxSpeed;
    public float SpeedIncreasePerSecond => speedIncreasePerSecond;
    public int ScoreSpeedStep => Mathf.Max(1, scoreSpeedStep);
    public float SpeedIncreasePerScoreStep => speedIncreasePerScoreStep;
    public float ScorePerMeter => scorePerMeter;
    public float FirstSpawnDelay => firstSpawnDelay;
    public float MinSpawnInterval => minSpawnInterval;
    public float MaxSpawnInterval => maxSpawnInterval;
    public float IntervalSpeedPressure => intervalSpeedPressure;
    public float GlobalMinimumGap => globalMinimumGap;
    public float BirdHeightOffset => birdHeightOffset;
}
