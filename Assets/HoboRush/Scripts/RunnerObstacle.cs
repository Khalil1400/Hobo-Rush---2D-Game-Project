using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RunnerObstacle : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] animationFrames;
    [SerializeField] private float animationFrameRate = 10f;
    [SerializeField] private float speedMultiplier = 1f;
    [SerializeField] private float destroyX = -14f;
    [SerializeField] private bool deadly = true;

    private float animationTimer;
    private int animationIndex;
    private RunnerObstacleSpawner owningSpawner;
    private Sprite startingSprite;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            startingSprite = spriteRenderer.sprite;
        }
    }

    private void Update()
    {
        if (RunnerGameManager.Instance != null && RunnerGameManager.Instance.IsGameOver)
        {
            return;
        }

        UpdateAnimation();

        float speed = RunnerGameManager.Instance != null ? RunnerGameManager.Instance.CurrentSpeed : 7f;
        transform.position += Vector3.left * (speed * speedMultiplier * Time.deltaTime);

        if (transform.position.x <= destroyX)
        {
            ReturnToPool();
        }
    }

    public void PrepareForSpawn(RunnerObstacleSpawner spawner)
    {
        owningSpawner = spawner;
        animationTimer = 0f;
        animationIndex = 0;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            if (animationFrames != null && animationFrames.Length > 0)
            {
                spriteRenderer.sprite = animationFrames[0];
            }
            else if (startingSprite != null)
            {
                spriteRenderer.sprite = startingSprite;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!deadly)
        {
            return;
        }

        if (other.GetComponentInParent<RunnerPlayerController>() != null && RunnerGameManager.Instance != null)
        {
            RunnerGameManager.Instance.GameOver();
        }
    }

    private void UpdateAnimation()
    {
        if (spriteRenderer == null || animationFrames == null || animationFrames.Length <= 1)
        {
            return;
        }

        animationTimer += Time.deltaTime;
        if (animationTimer < 1f / Mathf.Max(1f, animationFrameRate))
        {
            return;
        }

        animationTimer = 0f;
        animationIndex = (animationIndex + 1) % animationFrames.Length;
        spriteRenderer.sprite = animationFrames[animationIndex];
    }

    private void ReturnToPool()
    {
        if (owningSpawner != null)
        {
            owningSpawner.ReturnObstacle(this);
            return;
        }

        gameObject.SetActive(false);
    }
}
