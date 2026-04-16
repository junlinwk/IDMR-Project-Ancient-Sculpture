using UnityEngine;

/// <summary>
/// Moves the rock downward without Rigidbody, plays a landing sound once it crosses a Y threshold,
/// then destroys the rock after it falls a bit lower.
/// Attach to the rock root object, or let WeakPointCollisionBox add it automatically.
/// </summary>
public class RockLandingFeedback : MonoBehaviour
{
    [SerializeField] private float fallSpeed = 2.5f;
    [SerializeField] private float destroyBelowYOffset = 1f;
    [SerializeField] private float landingVolume = 1f;
    [SerializeField] private bool logLanding = true;

    private FeedbackManager feedback;
    private float landingYThreshold;
    private float destroyYThreshold;
    private bool isArmed;
    private bool hasPlayed;

    private void Awake()
    {
        feedback = Object.FindFirstObjectByType<FeedbackManager>();
    }

    private void Update()
    {
        if (!isArmed)
        {
            return;
        }

        float previousY = transform.position.y;
        FallStep();
        float currentY = transform.position.y;

        if (!hasPlayed && previousY > landingYThreshold && currentY <= landingYThreshold)
        {
            hasPlayed = true;

            if (logLanding)
            {
                Debug.Log($"{nameof(RockLandingFeedback)}: {gameObject.name} crossed Y threshold {landingYThreshold:0.###}.");
            }

            if (feedback != null)
            {
                feedback.PlayRockLandFeedback(transform.position, volume: landingVolume);
            }
        }

        if (hasPlayed && currentY <= destroyYThreshold)
        {
            if (logLanding)
            {
                Debug.Log($"{nameof(RockLandingFeedback)}: destroying {gameObject.name} below Y {destroyYThreshold:0.###}.");
            }

            Destroy(gameObject);
        }
    }

    private void FallStep()
    {
        transform.position += Vector3.down * (fallSpeed * Time.deltaTime);
    }

    public void Arm(float yThreshold, float volume = 1f)
    {
        landingYThreshold = yThreshold;
        destroyYThreshold = yThreshold - destroyBelowYOffset;
        landingVolume = volume;
        isArmed = true;
        hasPlayed = false;
    }
}
