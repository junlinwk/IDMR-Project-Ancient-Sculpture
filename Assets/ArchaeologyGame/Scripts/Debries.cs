using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Debris scattering system.
/// Attach to a parent GameObject with multiple child Rigidbodies.
/// When ScatterDebries() is called, applies outward force to all children and destroys them after lifeTime.
/// </summary>
public class Debries : MonoBehaviour
{
    [SerializeField] private float explosionForce = 1f;
    [SerializeField] private float lifeTime = 2f;

    private Rigidbody[] rbs;

    private void Awake()
    {
        rbs = GetComponentsInChildren<Rigidbody>();
    }

    /// <summary>
    /// Scatter all child Rigidbodies outward from the center.
    /// </summary>
    public void ScatterDebries()
    {
        foreach (var rb in rbs)
        {
            Vector3 explosionDirection = (rb.position - transform.position).normalized;

            // Apply force to the Rigidbody
            rb.AddForce(explosionDirection * explosionForce, ForceMode.Impulse);
            rb.useGravity = true;
            Destroy(rb.gameObject, lifeTime);
        }
    }
}
