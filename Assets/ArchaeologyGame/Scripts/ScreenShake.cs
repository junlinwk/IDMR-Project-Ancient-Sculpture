using System.Collections;
using UnityEngine;

/// <summary>
/// Very small transform shake for feedback.
/// Attach this to the camera rig root or a camera pivot, not directly to the tracked HMD transform if possible.
/// </summary>
public class ScreenShake : MonoBehaviour
{
    [SerializeField] private Transform target;

    private Vector3 originalLocalPosition;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        if (target == null)
        {
            target = transform;
        }

        originalLocalPosition = target.localPosition;
    }

    public void Shake(float intensity, float duration)
    {
        if (target == null || intensity <= 0f || duration <= 0f)
        {
            return;
        }

        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }

        shakeCoroutine = StartCoroutine(ShakeRoutine(intensity, duration));
    }

    private IEnumerator ShakeRoutine(float intensity, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            Vector2 offset = Random.insideUnitCircle * intensity;
            target.localPosition = originalLocalPosition + new Vector3(offset.x, offset.y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.localPosition = originalLocalPosition;
        shakeCoroutine = null;
    }
}
