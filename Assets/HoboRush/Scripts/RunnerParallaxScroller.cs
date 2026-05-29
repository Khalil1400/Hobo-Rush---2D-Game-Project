using UnityEngine;

public class RunnerParallaxScroller : MonoBehaviour
{
    [SerializeField] private Transform[] segments;
    [SerializeField] private float segmentWidth = 12f;
    [SerializeField] private float speedMultiplier = 1f;
    [SerializeField] private float resetX = -18f;

    private void Update()
    {
        if (RunnerGameManager.Instance != null && RunnerGameManager.Instance.IsGameOver)
        {
            return;
        }

        float speed = RunnerGameManager.Instance != null ? RunnerGameManager.Instance.CurrentSpeed : 7f;
        float delta = speed * speedMultiplier * Time.deltaTime;

        for (int i = 0; i < segments.Length; i++)
        {
            Transform segment = segments[i];
            if (segment == null)
            {
                continue;
            }

            segment.position += Vector3.left * delta;
            if (segment.position.x <= resetX)
            {
                segment.position += Vector3.right * (segmentWidth * segments.Length);
            }
        }
    }
}
