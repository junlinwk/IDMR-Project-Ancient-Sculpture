using UnityEngine;

/// <summary>
/// Iron ore pickup logic.
/// When ore comes near the player, it's automatically collected.
/// </summary>
public class IronOre : MonoBehaviour
{
    [Header("Pickup Settings")]
    [SerializeField] private float pickupRange = 1.0f;
    [SerializeField] private float moveSpeed = 10f;

    private Transform playerTarget;
    private bool isBeingPickedUp = false;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Find the player's camera rig
        OVRCameraRig cameraRig = FindObjectOfType<OVRCameraRig>();
        if (cameraRig != null)
        {
            playerTarget = cameraRig.centerEyeAnchor;
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        // Direct collision with player trigger
        if (collision.CompareTag("Player"))
        {
            PickUp();
        }
    }

    private void Update()
    {
        // If player exists and ore is within pickup range, move toward player
        if (playerTarget != null && !isBeingPickedUp)
        {
            float distance = Vector3.Distance(transform.position, playerTarget.position);
            if (distance < pickupRange)
            {
                // Move toward player
                Vector3 direction = (playerTarget.position - transform.position).normalized;
                if (rb != null)
                {
                    rb.linearVelocity = direction * moveSpeed;
                }
                else
                {
                    transform.position += direction * moveSpeed * Time.deltaTime;
                }

                // When very close, pick up
                if (distance < 0.3f)
                {
                    PickUp();
                }
            }
        }
    }

    private void PickUp()
    {
        if (isBeingPickedUp)
        {
            return;
        }

        isBeingPickedUp = true;

        // Register ore collection
        if (ArchaeologyGameManager.Instance != null)
        {
            ArchaeologyGameManager.Instance.AddOre(1);
        }

        // Destroy this ore
        Destroy(gameObject);
    }
}
