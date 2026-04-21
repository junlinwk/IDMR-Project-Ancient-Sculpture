using UnityEngine;

/// <summary>
/// Weak-point collision box for a small stone.
/// Attach this to the weak-point child object under the stone.
///
/// States:
/// 0 hits  -> green
/// 1 hit   -> yellow
/// 2 hits  -> red
/// 3 hits  -> enable gravity on the parent stone and hide this weak-point box
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class WeakPointCollisionBox : MonoBehaviour
{
    [Header("Hit Settings")]
    [SerializeField] private int hitsBeforeDrop = 3;
    [SerializeField] private string pickaxeTag = "Pickaxe";
    [Range(0f, 1f)]
    [SerializeField] private float hitSuccessChance = 1.0f;
    [SerializeField] private float hitHapticAmplitude = 0.45f;
    [SerializeField] private float hitHapticDuration = 0.08f;

    [Header("Colors")]
    [SerializeField] private Color initialColor = Color.green;
    [SerializeField] private Color hitOneColor = Color.yellow;
    [SerializeField] private Color hitTwoColor = Color.red;

    [Header("Drop Settings")]
    [SerializeField] private bool hideParentRenderersOnDrop = false;
    [SerializeField] private float floorY = 0f;
    [SerializeField] private float landingVolume = 1f;
    [SerializeField] private bool logWeakPointHits = true;

    private BoxCollider weakPointCollider;
    private Renderer[] targetRenderers;
    private FeedbackManager feedback;
    private Transform parentStone;
    private int hitCount;
    private bool dropped;

    private void Awake()
    {
        weakPointCollider = GetComponent<BoxCollider>();
        weakPointCollider.isTrigger = true;

        targetRenderers = GetComponentsInChildren<Renderer>(true);
        RockFragment rockFragment = GetComponentInParent<RockFragment>();
        if (rockFragment != null)
        {
            parentStone = rockFragment.transform;
        }
        else if (transform.parent != null)
        {
            parentStone = transform.parent;
        }
        else
        {
            parentStone = transform;
        }
        feedback = Object.FindFirstObjectByType<FeedbackManager>();
    }

    private void Start()
    {
        ApplyStateVisual();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (dropped)
        {
            return;
        }

        if (!IsPickaxe(other))
        {
            return;
        }

        if (logWeakPointHits)
        {
            Debug.Log($"{nameof(WeakPointCollisionBox)} hit on {gameObject.name} by {other.name}.");
        }

        PlayHitFeedback(other);

        if (Random.value > hitSuccessChance)
        {
            if (logWeakPointHits)
            {
                Debug.Log($"{nameof(WeakPointCollisionBox)} miss on {gameObject.name} by {other.name}.");
            }

            return;
        }

        hitCount++;

        if (hitCount >= hitsBeforeDrop)
        {
            DropStone();
            return;
        }

        ApplyStateVisual();
    }

    private void PlayHitFeedback(Collider other)
    {
        Vector3 impactPoint = transform.position;
        if (other != null)
        {
            impactPoint = other.ClosestPoint(transform.position);
        }

        Pickaxe pickaxe = other != null ? other.GetComponentInParent<Pickaxe>() : null;
        if (pickaxe != null)
        {
            pickaxe.PlayStrikeFeedback(impactPoint, hitHapticAmplitude, hitHapticDuration);
            return;
        }

        if (feedback != null)
        {
            feedback.PlayPickaxeStrikeSound(impactPoint);
        }
    }

    private bool IsPickaxe(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (other.CompareTag(pickaxeTag))
        {
            return true;
        }

        return other.GetComponentInParent<Pickaxe>() != null;
    }

    private void ApplyStateVisual()
    {
        Color color = initialColor;

        if (hitCount == 1)
        {
            color = hitOneColor;
        }
        else if (hitCount >= 2)
        {
            color = hitTwoColor;
        }

        SetRendererColor(color);
    }

    private void SetRendererColor(Color color)
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            return;
        }

        MaterialPropertyBlock block = new MaterialPropertyBlock();

        foreach (Renderer renderer in targetRenderers)
        {
            if (renderer == null)
            {
                continue;
            }

            renderer.GetPropertyBlock(block);
            block.SetColor("_Color", color);
            block.SetColor("_BaseColor", color);
            renderer.SetPropertyBlock(block);
        }
    }

    private void DropStone()
    {
        dropped = true;

        if (logWeakPointHits)
        {
            Debug.Log($"{nameof(WeakPointCollisionBox)} dropping stone on {gameObject.name}.");
        }

        if (feedback != null)
        {
            feedback.PlayRockDropFeedback(transform.position);
        }

        EnsureLandingFeedback();

        weakPointCollider.enabled = false;

        if (hideParentRenderersOnDrop && targetRenderers != null)
        {
            foreach (Renderer renderer in targetRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }
        }
    }

    private void EnsureLandingFeedback()
    {
        if (parentStone == null)
        {
            return;
        }

        RockLandingFeedback landingFeedback = parentStone.GetComponent<RockLandingFeedback>();
        if (landingFeedback == null)
        {
            landingFeedback = parentStone.gameObject.AddComponent<RockLandingFeedback>();
        }

        landingFeedback.Arm(floorY, landingVolume);
    }

    private void Reset()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            boxCollider.isTrigger = true;
        }
    }

    private void OnValidate()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            boxCollider.isTrigger = true;
        }

        hitSuccessChance = Mathf.Clamp01(hitSuccessChance);
    }
}
