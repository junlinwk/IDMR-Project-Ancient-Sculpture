using UnityEngine;

public class SparkleMovement : MonoBehaviour
{
    [Header("Float Motion")]
    [SerializeField] private float bobAmplitude = 0.15f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float orbitRadius = 0.25f;
    [SerializeField] private float orbitSpeed = 1.4f;

    private Vector3 startPosition;

    private void Awake()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        float time = Time.time;
        float x = Mathf.Cos(time * orbitSpeed) * orbitRadius;
        float z = Mathf.Sin(time * orbitSpeed) * orbitRadius;
        float y = Mathf.Sin(time * bobSpeed) * bobAmplitude;

        transform.position = startPosition + new Vector3(x, y, z);
    }
}
