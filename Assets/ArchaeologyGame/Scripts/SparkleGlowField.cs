using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SparkleGlowField : MonoBehaviour
{
    [Header("Glow Settings")]
    [SerializeField] private Color glowColor = new Color(0.2f, 1f, 0.35f, 1f);
    [SerializeField] private int lightCount = 8;
    [SerializeField] private float lightIntensity = 4f;
    [SerializeField] private float lightRange = 2.5f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 0.45f;
    [SerializeField] private float bobAmplitude = 0.12f;
    [SerializeField] private float bobSpeed = 2.1f;
    [SerializeField] private float retargetInterval = 1.25f;

    private readonly List<FloatingGlowLight> lights = new List<FloatingGlowLight>();
    private Collider sparkleCollider;

    private void Awake()
    {
        sparkleCollider = GetComponent<Collider>();
    }

    private void Start()
    {
        CreateLights();
    }

    private void OnDestroy()
    {
        for (int i = 0; i < lights.Count; i++)
        {
            FloatingGlowLight light = lights[i];
            if (light != null && light.Transform != null)
            {
                Destroy(light.Transform.gameObject);
            }
        }

        lights.Clear();
    }

    private void Update()
    {
        if (sparkleCollider == null)
        {
            return;
        }

        float time = Time.time;
        for (int i = 0; i < lights.Count; i++)
        {
            FloatingGlowLight light = lights[i];
            if (light == null || light.Transform == null)
            {
                continue;
            }

            if (light.NextRetargetTime <= time)
            {
                light.TargetPosition = GetRandomPointInsideCollider();
                light.NextRetargetTime = time + retargetInterval * Random.Range(0.85f, 1.15f);
            }

            Vector3 currentPosition = light.Transform.position;
            Vector3 targetPosition = light.TargetPosition + Vector3.up * Mathf.Sin(time * bobSpeed + light.Seed) * bobAmplitude;
            light.Transform.position = Vector3.MoveTowards(currentPosition, targetPosition, moveSpeed * Time.deltaTime);
        }
    }

    private void CreateLights()
    {
        int count = Mathf.Max(1, lightCount);
        for (int i = 0; i < count; i++)
        {
            GameObject lightObject = new GameObject($"SparkleGlowLight_{i + 1}");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.position = GetRandomPointInsideCollider();

            Light glowLight = lightObject.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.color = glowColor;
            glowLight.intensity = lightIntensity;
            glowLight.range = lightRange;
            glowLight.shadows = LightShadows.None;

            lights.Add(new FloatingGlowLight
            {
                Transform = lightObject.transform,
                TargetPosition = lightObject.transform.position,
                Seed = Random.Range(0f, Mathf.PI * 2f),
                NextRetargetTime = Time.time + Random.Range(0f, retargetInterval)
            });
        }
    }

    private Vector3 GetRandomPointInsideCollider()
    {
        if (sparkleCollider is BoxCollider boxCollider)
        {
            return GetRandomPointInsideBox(boxCollider);
        }

        if (sparkleCollider is SphereCollider sphereCollider)
        {
            return GetRandomPointInsideSphere(sphereCollider);
        }

        Bounds bounds = sparkleCollider.bounds;
        for (int attempt = 0; attempt < 32; attempt++)
        {
            Vector3 candidate = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z));

            if (bounds.Contains(candidate))
            {
                return candidate;
            }
        }

        return bounds.center;
    }

    private Vector3 GetRandomPointInsideBox(BoxCollider boxCollider)
    {
        Vector3 localCenter = boxCollider.center;
        Vector3 localHalfSize = boxCollider.size * 0.5f;

        Vector3 localPoint = new Vector3(
            Random.Range(-localHalfSize.x, localHalfSize.x),
            Random.Range(-localHalfSize.y, localHalfSize.y),
            Random.Range(-localHalfSize.z, localHalfSize.z));

        return boxCollider.transform.TransformPoint(localCenter + localPoint);
    }

    private Vector3 GetRandomPointInsideSphere(SphereCollider sphereCollider)
    {
        Vector3 localCenter = sphereCollider.center;
        Vector3 localPoint = Random.insideUnitSphere * sphereCollider.radius;
        return sphereCollider.transform.TransformPoint(localCenter + localPoint);
    }

    private class FloatingGlowLight
    {
        public Transform Transform;
        public Vector3 TargetPosition;
        public float Seed;
        public float NextRetargetTime;
    }
}
