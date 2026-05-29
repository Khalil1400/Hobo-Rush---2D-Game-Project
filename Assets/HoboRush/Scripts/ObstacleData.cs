using UnityEngine;

[CreateAssetMenu(menuName = "Hobo Rush/Obstacle Data", fileName = "ObstacleData")]
public class ObstacleData : ScriptableObject
{
    [SerializeField] private string label = "Obstacle";
    [SerializeField] private GameObject prefab;
    [SerializeField] private float yPosition = -2.03f;
    [SerializeField] private float spawnYOffset;
    [SerializeField] private float weight = 1f;
    [SerializeField] private float minGameSpeed;
    [SerializeField] private float recoveryGap = 0.62f;
    [SerializeField] private bool isBird;
    [SerializeField] private int prewarmCount = 4;

    public string Label => label;
    public GameObject Prefab => prefab;
    public float YPosition => yPosition;
    public float SpawnYOffset => spawnYOffset;
    public float Weight => weight;
    public float MinGameSpeed => minGameSpeed;
    public float RecoveryGap => recoveryGap;
    public bool IsBird => isBird;
    public int PrewarmCount => Mathf.Max(1, prewarmCount);
}
