using System.Reflection;
using UnityEngine;

/// <summary>
/// Auto-configures OVRGrabber on the controller anchors so OVRGrabbable objects can be picked up
/// without requiring manual scene setup.
/// </summary>
public static class OvrGrabberBootstrap
{
    // Radius of the sphere that detects grabbable objects near the controller.
    // 0.12 is the OVR default (tight). 0.25 is more forgiving for XR simulator
    // users who have to aim with WASD + mouse instead of a real tracked hand.
    private const float GrabVolumeRadius = 0.25f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ConfigureGrabbers()
    {
        ConfigureControllerAnchor("LeftControllerAnchor", OVRInput.Controller.LTouch);
        ConfigureControllerAnchor("RightControllerAnchor", OVRInput.Controller.RTouch);
    }

    private static void ConfigureControllerAnchor(string anchorName, OVRInput.Controller controller)
    {
        GameObject anchor = GameObject.Find(anchorName);
        if (anchor == null)
        {
            return;
        }

        Rigidbody rb = anchor.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = anchor.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

        OVRGrabber grabber = anchor.GetComponent<OVRGrabber>();
        if (grabber == null)
        {
            grabber = anchor.AddComponent<OVRGrabber>();
        }

        Transform gripPoint = FindOrCreateChild(anchor.transform, "OVRGripPoint");
        Transform grabVolumeRoot = FindOrCreateChild(anchor.transform, "OVRGrabVolume");
        SphereCollider grabVolume = grabVolumeRoot.GetComponent<SphereCollider>();
        if (grabVolume == null)
        {
            grabVolume = grabVolumeRoot.gameObject.AddComponent<SphereCollider>();
        }
        grabVolume.isTrigger = true;
        grabVolume.radius = GrabVolumeRadius;
        grabVolume.center = Vector3.zero;

        SetPrivateField(grabber, "m_controller", controller);
        SetPrivateField(grabber, "m_gripTransform", gripPoint);
        SetPrivateField(grabber, "m_grabVolumes", new Collider[] { grabVolume });
        SetPrivateField(grabber, "m_parentTransform", anchor.transform);

        OVRManager manager = Object.FindFirstObjectByType<OVRManager>();
        if (manager != null)
        {
            SetPrivateField(grabber, "m_player", manager.gameObject);
        }

        Debug.Log($"OvrGrabberBootstrap: configured {anchorName} for {controller}.");
    }

    private static Transform FindOrCreateChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            return child;
        }

        GameObject childObject = new GameObject(childName);
        childObject.transform.SetParent(parent, false);
        childObject.transform.localPosition = Vector3.zero;
        childObject.transform.localRotation = Quaternion.identity;
        childObject.transform.localScale = Vector3.one;
        return childObject.transform;
    }

    private static void SetPrivateField<T>(T target, string fieldName, object value) where T : Object
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }
}
