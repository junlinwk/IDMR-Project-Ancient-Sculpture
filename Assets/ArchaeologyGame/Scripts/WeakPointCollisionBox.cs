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

    [Header("Colors")]
    [SerializeField] private Color initialColor = Color.green;
    [SerializeField] private Color hitOneColor = Color.yellow;
    [SerializeField] private Color hitTwoColor = Color.red;

    [Header("Drop Settings")]
    [SerializeField] private bool forceParentGravityOnDrop = true;

    private BoxCollider weakPointCollider;
    private Renderer[] targetRenderers;
    private Rigidbody parentRigidbody;
    private int hitCount;
    private bool dropped;

    private void Awake()
    {
        weakPointCollider = GetComponent<BoxCollider>();
        weakPointCollider.isTrigger = true;

        targetRenderers = GetComponentsInChildren<Renderer>(true);
        parentRigidbody = GetComponentInParent<Rigidbody>();
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

        hitCount++;

        if (hitCount >= hitsBeforeDrop)
        {
            DropStone();
            return;
        }

        ApplyStateVisual();
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

        if (forceParentGravityOnDrop)
        {
            Rigidbody rb = GetOrCreateParentRigidbody();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.constraints = RigidbodyConstraints.None;
            }
        }

        weakPointCollider.enabled = false;

        if (targetRenderers != null)
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

    private Rigidbody GetOrCreateParentRigidbody()
    {
        if (parentRigidbody != null)
        {
            return parentRigidbody;
        }

        Transform parent = transform.parent;
        if (parent != null)
        {
            parentRigidbody = parent.GetComponent<Rigidbody>();
            if (parentRigidbody == null)
            {
                parentRigidbody = parent.gameObject.AddComponent<Rigidbody>();
            }

            return parentRigidbody;
        }

        parentRigidbody = GetComponent<Rigidbody>();
        if (parentRigidbody == null)
        {
            parentRigidbody = gameObject.AddComponent<Rigidbody>();
        }

        return parentRigidbody;
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
    }
}
