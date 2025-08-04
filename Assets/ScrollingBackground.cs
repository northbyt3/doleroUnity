using System.Collections;
using UnityEngine;

public class ScrollingBackground : MonoBehaviour
{
    public float speed = 0.1f;
    [SerializeField] private Renderer bgRenderer;

    private Vector2 currentDirection = Vector2.right;
    private Vector2 targetDirection = Vector2.right;
    private float directionLerpTime = 1f;
    private float directionLerpProgress = 1f;

    void Start()
    {
        StartCoroutine(ChangeDirectionRoutine());
    }

    void Update()
    {
        // Smoothly interpolate direction
        if (directionLerpProgress < 1f)
        {
            directionLerpProgress += Time.deltaTime / directionLerpTime;
            currentDirection = Vector2.Lerp(currentDirection, targetDirection, directionLerpProgress);
        }

        bgRenderer.material.mainTextureOffset += currentDirection.normalized * speed * Time.deltaTime;
    }

    IEnumerator ChangeDirectionRoutine()
    {
        while (true)
        {
            // Wait for a random time between 2–5 seconds
            float waitTime = Random.Range(2f, 5f);
            yield return new WaitForSeconds(waitTime);

            // Choose a new random direction (e.g., left/right/up/down or diagonal)
            float angle = Random.Range(0f, 360f);
            targetDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            // Reset interpolation
            directionLerpProgress = 0f;
            directionLerpTime = Random.Range(0.5f, 2f); // Smooth transition time
        }
    }
}
