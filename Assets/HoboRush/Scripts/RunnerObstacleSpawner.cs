using System.Collections.Generic;
using UnityEngine;

public class RunnerObstacleSpawner : MonoBehaviour
{
    [SerializeField] private ObstacleData[] obstacles;
    [SerializeField] private RunnerDifficultySettings difficultySettings;
    [SerializeField] private Transform obstacleParent;
    [SerializeField] private float spawnX = 12f;
    [SerializeField] private float firstSpawnDelay = 1.2f;
    [SerializeField] private float minSpawnInterval = 0.72f;
    [SerializeField] private float maxSpawnInterval = 1.45f;
    [SerializeField] private float intervalSpeedPressure = 0.034f;
    [SerializeField] private float globalMinimumGap = 0.62f;
    [SerializeField] private float birdHeightOffset = -0.15f;

    private readonly Dictionary<ObstacleData, Queue<RunnerObstacle>> pools = new Dictionary<ObstacleData, Queue<RunnerObstacle>>();
    private readonly Dictionary<RunnerObstacle, ObstacleData> poolOwners = new Dictionary<RunnerObstacle, ObstacleData>();
    private float spawnTimer;
    private float editorIntervalMultiplier = 1f;
    private bool birdsEnabled = true;
    private ObstacleData lastSpawnedObstacle;

    public float EditorIntervalMultiplier => editorIntervalMultiplier;
    public bool BirdsEnabled => birdsEnabled;

    private void Awake()
    {
        if (obstacleParent == null)
        {
            obstacleParent = transform;
        }

        PrewarmPools();
    }

    private void Start()
    {
        spawnTimer = ResolveFirstSpawnDelay();
    }

    private void Update()
    {
        if (RunnerGameManager.Instance != null && RunnerGameManager.Instance.IsGameOver)
        {
            return;
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnRandomObstacle();
            ResetTimer();
        }
    }

    public void SetEditorIntervalMultiplier(float multiplier)
    {
        editorIntervalMultiplier = Mathf.Clamp(multiplier, 0.45f, 1.8f);
    }

    public void SetBirdsEnabled(bool enabled)
    {
        birdsEnabled = enabled;
    }

    public void SpawnRandomObstacle()
    {
        ObstacleData selected = PickObstacle();
        SpawnDefinition(selected);
    }

    public void SpawnGroundHazard()
    {
        SpawnSpecific(false);
    }

    public void SpawnBird()
    {
        SpawnSpecific(true);
    }

    public void ReturnObstacle(RunnerObstacle obstacle)
    {
        if (obstacle == null)
        {
            return;
        }

        if (!poolOwners.TryGetValue(obstacle, out ObstacleData owner) || owner == null)
        {
            obstacle.gameObject.SetActive(false);
            return;
        }

        obstacle.gameObject.SetActive(false);
        obstacle.transform.SetParent(obstacleParent, false);

        if (!pools.TryGetValue(owner, out Queue<RunnerObstacle> pool))
        {
            pool = new Queue<RunnerObstacle>();
            pools.Add(owner, pool);
        }

        pool.Enqueue(obstacle);
    }

    private void SpawnSpecific(bool bird)
    {
        if (obstacles == null)
        {
            return;
        }

        for (int i = 0; i < obstacles.Length; i++)
        {
            ObstacleData definition = obstacles[i];
            if (definition != null && definition.IsBird == bird && definition.Prefab != null)
            {
                if (SpawnDefinition(definition))
                {
                    ResetTimer();
                }

                return;
            }
        }
    }

    private bool SpawnDefinition(ObstacleData definition)
    {
        if (definition == null || definition.Prefab == null)
        {
            return false;
        }

        RunnerObstacle obstacle = GetObstacle(definition);
        if (obstacle == null)
        {
            return false;
        }

        float yPosition = definition.YPosition + definition.SpawnYOffset + (definition.IsBird ? ResolveBirdHeightOffset() : 0f);
        obstacle.transform.SetParent(obstacleParent, false);
        obstacle.transform.SetPositionAndRotation(new Vector3(spawnX, yPosition, 0f), Quaternion.identity);
        obstacle.name = definition.Label;
        obstacle.PrepareForSpawn(this);
        obstacle.gameObject.SetActive(true);

        lastSpawnedObstacle = definition;
        RunnerAudioManager.Instance?.PlayObstacleWarning(definition.IsBird);
        return true;
    }

    private void ResetTimer()
    {
        float speed = RunnerGameManager.Instance != null ? RunnerGameManager.Instance.CurrentSpeed : ResolveStartingSpeedFallback();
        float pressure = Mathf.Max(0f, speed - ResolveStartingSpeedFallback()) * ResolveIntervalSpeedPressure();
        float recoveryGap = lastSpawnedObstacle != null ? lastSpawnedObstacle.RecoveryGap : ResolveGlobalMinimumGap();
        float min = Mathf.Max(ResolveGlobalMinimumGap(), recoveryGap, ResolveMinSpawnInterval() - pressure);
        float max = Mathf.Max(min + 0.18f, ResolveMaxSpawnInterval() - pressure);
        spawnTimer = Random.Range(min, max) * editorIntervalMultiplier;
    }

    private ObstacleData PickObstacle()
    {
        if (obstacles == null)
        {
            return null;
        }

        float speed = RunnerGameManager.Instance != null ? RunnerGameManager.Instance.CurrentSpeed : ResolveStartingSpeedFallback();
        float totalWeight = 0f;

        for (int i = 0; i < obstacles.Length; i++)
        {
            ObstacleData definition = obstacles[i];
            if (!CanUseDefinition(definition, speed))
            {
                continue;
            }

            totalWeight += Mathf.Max(0f, definition.Weight);
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float roll = Random.value * totalWeight;
        for (int i = 0; i < obstacles.Length; i++)
        {
            ObstacleData definition = obstacles[i];
            if (!CanUseDefinition(definition, speed))
            {
                continue;
            }

            roll -= Mathf.Max(0f, definition.Weight);
            if (roll <= 0f)
            {
                return definition;
            }
        }

        return null;
    }

    private bool CanUseDefinition(ObstacleData definition, float speed)
    {
        if (definition == null || definition.Prefab == null)
        {
            return false;
        }

        if (definition.IsBird && !birdsEnabled)
        {
            return false;
        }

        return speed >= definition.MinGameSpeed;
    }

    private void PrewarmPools()
    {
        if (obstacles == null)
        {
            return;
        }

        for (int i = 0; i < obstacles.Length; i++)
        {
            ObstacleData data = obstacles[i];
            if (data == null || data.Prefab == null || pools.ContainsKey(data))
            {
                continue;
            }

            Queue<RunnerObstacle> pool = new Queue<RunnerObstacle>();
            pools.Add(data, pool);
            for (int j = 0; j < data.PrewarmCount; j++)
            {
                RunnerObstacle obstacle = CreatePoolInstance(data);
                obstacle.gameObject.SetActive(false);
                pool.Enqueue(obstacle);
            }
        }
    }

    private RunnerObstacle GetObstacle(ObstacleData data)
    {
        if (!pools.TryGetValue(data, out Queue<RunnerObstacle> pool))
        {
            pool = new Queue<RunnerObstacle>();
            pools.Add(data, pool);
        }

        while (pool.Count > 0)
        {
            RunnerObstacle obstacle = pool.Dequeue();
            if (obstacle != null)
            {
                return obstacle;
            }
        }

        return CreatePoolInstance(data);
    }

    private RunnerObstacle CreatePoolInstance(ObstacleData data)
    {
        GameObject instance = Instantiate(data.Prefab, obstacleParent);
        instance.name = data.Label;
        RunnerObstacle obstacle = instance.GetComponent<RunnerObstacle>();
        if (obstacle == null)
        {
            obstacle = instance.AddComponent<RunnerObstacle>();
        }

        poolOwners[obstacle] = data;
        obstacle.PrepareForSpawn(this);
        return obstacle;
    }

    private float ResolveFirstSpawnDelay()
    {
        return difficultySettings != null ? difficultySettings.FirstSpawnDelay : firstSpawnDelay;
    }

    private float ResolveMinSpawnInterval()
    {
        return difficultySettings != null ? difficultySettings.MinSpawnInterval : minSpawnInterval;
    }

    private float ResolveMaxSpawnInterval()
    {
        return difficultySettings != null ? difficultySettings.MaxSpawnInterval : maxSpawnInterval;
    }

    private float ResolveIntervalSpeedPressure()
    {
        return difficultySettings != null ? difficultySettings.IntervalSpeedPressure : intervalSpeedPressure;
    }

    private float ResolveGlobalMinimumGap()
    {
        return difficultySettings != null ? difficultySettings.GlobalMinimumGap : globalMinimumGap;
    }

    private float ResolveBirdHeightOffset()
    {
        return difficultySettings != null ? difficultySettings.BirdHeightOffset : birdHeightOffset;
    }

    private float ResolveStartingSpeedFallback()
    {
        return difficultySettings != null ? difficultySettings.StartingSpeed : 7f;
    }
}
