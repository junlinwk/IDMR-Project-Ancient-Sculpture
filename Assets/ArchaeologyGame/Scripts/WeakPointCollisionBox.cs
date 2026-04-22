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
    [Tooltip("Hits required to break the rock through the weak point, per upgrade level. " +
             "Index 0 = Lv0, 1 = Lv1, etc. If empty, falls back to Hits Before Drop.")]
    [SerializeField] private int[] hitsPerLevelWeak = new int[] { 3, 2, 1, 1 };
    [Tooltip("Fallback hit count used when Hits Per Level Weak is empty. Ignored otherwise.")]
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
    private RockFragment parentRockFragment;
    private int hitCount;
    private bool dropped;
    private int currentThreshold;

    private void Awake()
    {
        weakPointCollider = GetComponent<BoxCollider>();
        weakPointCollider.isTrigger = true;

        targetRenderers = GetComponentsInChildren<Renderer>(true);
        parentRockFragment = GetComponentInParent<RockFragment>();
        if (parentRockFragment != null)
        {
            parentStone = parentRockFragment.transform;
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

        currentThreshold = GetWeakHitsForLevel(0);
    }

    private void Start()
    {
        // Subscribe to upgrade events so the weak-point threshold shrinks along
        // with the pickaxe's power.
        if (ArchaeologyGameManager.Instance != null)
        {
            ArchaeologyGameManager.Instance.OnUpgradeLevelChanged.AddListener(HandleUpgrade);
            currentThreshold = GetWeakHitsForLevel(ArchaeologyGameManager.Instance.GetUpgradeLevel());
        }

        ApplyStateVisual();
    }

    private void OnDestroy()
    {
        if (ArchaeologyGameManager.Instance != null)
        {
            ArchaeologyGameManager.Instance.OnUpgradeLevelChanged.RemoveListener(HandleUpgrade);
        }
    }

    private void HandleUpgrade(int level)
    {
        currentThreshold = GetWeakHitsForLevel(level);
    }

    private int GetWeakHitsForLevel(int level)
    {
        if (hitsPerLevelWeak != null && hitsPerLevelWeak.Length > 0)
        {
            int idx = Mathf.Clamp(level, 0, hitsPerLevelWeak.Length - 1);
            return Mathf.Max(1, hitsPerLevelWeak[idx]);
        }
        return Mathf.Max(1, hitsBeforeDrop);
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

        if (logWeakPointHits)
        {
            Debug.Log($"{nameof(WeakPointCollisionBox)} hit count: {hitCount}/{currentThreshold} on {gameObject.name}.");
        }

        if (hitCount >= currentThreshold)
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
        weakPointCollider.enabled = false;

        if (logWeakPointHits)
        {
            Debug.Log($"{nameof(WeakPointCollisionBox)} threshold reached on {gameObject.name}. Routing to RockFragment.");
        }

        // Preferred path: delegate to RockFragment so the rock goes through the
        // shared destruction pipeline — ore spawn, upgrade counter, manager
        // notification. This unifies the weak-point route with the regular
        // body-hit route.
        if (parentRockFragment != null)
        {
            parentRockFragment.TriggerDestruction();
            return;
        }

        // Fallback (no RockFragment on parent): keep the legacy fall-and-land
        // animation so at least the stone visibly drops.
        if (feedback != null)
        {
            feedback.PlayRockDropFeedback(transform.position);
        }

        EnsureLandingFeedback();

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
