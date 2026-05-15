using UnityEngine;

public static class ArcadeVehicleFactory
{
    private const string VisualRootName = "SelectedCarVisual";
    private const string WheelColliderRootName = "Arcade WheelColliders";
    private const string WheelMeshRootName = "Arcade WheelTargets";

    public static PrometeoCarController ConfigureVehicle(GameObject rootObject, ArcadeCarOption carOption)
    {
        if (rootObject == null)
        {
            rootObject = new GameObject("PlayerCar");
        }

        rootObject.name = "PlayerCar";
        Rigidbody rigidbody = EnsureRigidbody(rootObject, carOption);
        PrometeoCarController controller = EnsureController(rootObject, carOption);

        Transform visualRoot = ReplaceVisual(rootObject.transform, carOption);
        Bounds localBounds = CalculateLocalBounds(rootObject.transform, visualRoot);
        if (localBounds.size.sqrMagnitude < 0.01f)
        {
            localBounds = new Bounds(new Vector3(0f, 0.65f, 0f), new Vector3(1.8f, 1.25f, 4.2f));
        }

        ConfigureBodyCollider(rootObject, localBounds);
        ConfigureWheelRig(rootObject.transform, controller, localBounds);

        rigidbody.centerOfMass = carOption != null ? carOption.centerOfMass : new Vector3(0f, -0.45f, 0.05f);
        return controller;
    }

    private static Rigidbody EnsureRigidbody(GameObject rootObject, ArcadeCarOption carOption)
    {
        Rigidbody rigidbody = rootObject.GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = rootObject.AddComponent<Rigidbody>();
        }

        rigidbody.mass = carOption != null ? Mathf.Max(600f, carOption.mass) : 1150f;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        return rigidbody;
    }

    private static PrometeoCarController EnsureController(GameObject rootObject, ArcadeCarOption carOption)
    {
        PrometeoCarController controller = rootObject.GetComponent<PrometeoCarController>();
        if (controller == null)
        {
            controller = rootObject.AddComponent<PrometeoCarController>();
        }

        if (carOption != null)
        {
            controller.maxSpeed = Mathf.Max(35, carOption.maxSpeed);
            controller.accelerationMultiplier = Mathf.Clamp(carOption.accelerationMultiplier, 1, 10);
            controller.maxSteeringAngle = Mathf.Clamp(carOption.maxSteeringAngle, 15, 45);
        }

        controller.useUI = false;
        controller.useSounds = false;
        controller.useTouchControls = false;
        controller.enabled = true;
        return controller;
    }

    private static Transform ReplaceVisual(Transform root, ArcadeCarOption carOption)
    {
        Transform oldVisual = root.Find(VisualRootName);
        if (oldVisual != null)
        {
            Object.Destroy(oldVisual.gameObject);
        }

        GameObject visualRootObject = new GameObject(VisualRootName);
        Transform visualRoot = visualRootObject.transform;
        visualRoot.SetParent(root, false);

        if (carOption != null && carOption.prefab != null)
        {
            GameObject model = Object.Instantiate(carOption.prefab, visualRoot);
            model.name = carOption.displayName;
            model.transform.localPosition = carOption.visualOffset;
            model.transform.localRotation = Quaternion.Euler(carOption.visualEulerAngles);
            model.transform.localScale = Vector3.one * Mathf.Max(0.01f, carOption.visualScale);

            Bounds initialBounds = CalculateLocalBounds(root, visualRoot);
            if (initialBounds.size.sqrMagnitude > 0.01f)
            {
                float groundClearance = 0.08f;
                model.transform.localPosition += Vector3.up * (groundClearance - initialBounds.min.y);
            }
        }
        else
        {
            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fallback.name = "Fallback Car Visual";
            fallback.transform.SetParent(visualRoot, false);
            fallback.transform.localScale = new Vector3(1.8f, 0.8f, 3.8f);
            fallback.transform.localPosition = new Vector3(0f, 0.55f, 0f);

            Renderer renderer = fallback.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                if (shader != null)
                {
                    renderer.sharedMaterial = new Material(shader) { color = new Color(0.95f, 0.25f, 0.1f) };
                }
            }

            Collider collider = fallback.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }
        }

        return visualRoot;
    }

    private static void ConfigureBodyCollider(GameObject rootObject, Bounds localBounds)
    {
        BoxCollider bodyCollider = rootObject.GetComponent<BoxCollider>();
        if (bodyCollider == null)
        {
            bodyCollider = rootObject.AddComponent<BoxCollider>();
        }

        Vector3 size = localBounds.size;
        bodyCollider.center = new Vector3(localBounds.center.x, localBounds.center.y + size.y * 0.03f, localBounds.center.z);
        bodyCollider.size = new Vector3(Mathf.Max(1.2f, size.x * 0.88f), Mathf.Max(0.7f, size.y * 0.78f), Mathf.Max(2.2f, size.z * 0.9f));
    }

    private static void ConfigureWheelRig(Transform root, PrometeoCarController controller, Bounds localBounds)
    {
        Transform colliderRoot = FindOrCreateChild(root, WheelColliderRootName);
        Transform meshRoot = FindOrCreateChild(root, WheelMeshRootName);

        float width = Mathf.Max(1.6f, localBounds.size.x);
        float length = Mathf.Max(3.2f, localBounds.size.z);
        float radius = Mathf.Clamp(Mathf.Min(width, length) * 0.17f, 0.28f, 0.5f);
        float track = width * 0.42f;
        float frontZ = localBounds.center.z + length * 0.32f;
        float rearZ = localBounds.center.z - length * 0.32f;
        float wheelY = localBounds.min.y + radius + 0.1f;

        controller.frontLeftCollider = ConfigureWheelCollider(colliderRoot, "Front Left WheelCollider", new Vector3(-track, wheelY, frontZ), radius, true);
        controller.frontRightCollider = ConfigureWheelCollider(colliderRoot, "Front Right WheelCollider", new Vector3(track, wheelY, frontZ), radius, true);
        controller.rearLeftCollider = ConfigureWheelCollider(colliderRoot, "Rear Left WheelCollider", new Vector3(-track, wheelY, rearZ), radius, false);
        controller.rearRightCollider = ConfigureWheelCollider(colliderRoot, "Rear Right WheelCollider", new Vector3(track, wheelY, rearZ), radius, false);

        controller.frontLeftMesh = ConfigureWheelTarget(meshRoot, "Front Left Wheel Target", controller.frontLeftCollider.transform);
        controller.frontRightMesh = ConfigureWheelTarget(meshRoot, "Front Right Wheel Target", controller.frontRightCollider.transform);
        controller.rearLeftMesh = ConfigureWheelTarget(meshRoot, "Rear Left Wheel Target", controller.rearLeftCollider.transform);
        controller.rearRightMesh = ConfigureWheelTarget(meshRoot, "Rear Right Wheel Target", controller.rearRightCollider.transform);
    }

    private static WheelCollider ConfigureWheelCollider(Transform parent, string name, Vector3 localPosition, float radius, bool isFront)
    {
        Transform wheelTransform = FindOrCreateChild(parent, name);
        wheelTransform.localPosition = localPosition;
        wheelTransform.localRotation = Quaternion.identity;

        WheelCollider wheelCollider = wheelTransform.GetComponent<WheelCollider>();
        if (wheelCollider == null)
        {
            wheelCollider = wheelTransform.gameObject.AddComponent<WheelCollider>();
        }

        wheelCollider.radius = radius;
        wheelCollider.mass = isFront ? 24f : 26f;
        wheelCollider.suspensionDistance = 0.28f;
        wheelCollider.forceAppPointDistance = 0.08f;

        JointSpring spring = wheelCollider.suspensionSpring;
        spring.spring = 33000f;
        spring.damper = 5200f;
        spring.targetPosition = 0.48f;
        wheelCollider.suspensionSpring = spring;

        WheelFrictionCurve forwardFriction = wheelCollider.forwardFriction;
        forwardFriction.stiffness = isFront ? 1.25f : 1.32f;
        wheelCollider.forwardFriction = forwardFriction;

        WheelFrictionCurve sidewaysFriction = wheelCollider.sidewaysFriction;
        sidewaysFriction.stiffness = isFront ? 1.15f : 1.05f;
        wheelCollider.sidewaysFriction = sidewaysFriction;

        return wheelCollider;
    }

    private static GameObject ConfigureWheelTarget(Transform parent, string name, Transform source)
    {
        Transform target = FindOrCreateChild(parent, name);
        target.localPosition = source.localPosition;
        target.localRotation = source.localRotation;
        return target.gameObject;
    }

    private static Transform FindOrCreateChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            return child;
        }

        GameObject childObject = new GameObject(name);
        child = childObject.transform;
        child.SetParent(parent, false);
        return child;
    }

    private static Bounds CalculateLocalBounds(Transform root, Transform visualRoot)
    {
        if (visualRoot == null)
        {
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            Bounds rendererBounds = renderers[i].bounds;
            Vector3 min = root.InverseTransformPoint(rendererBounds.min);
            Vector3 max = root.InverseTransformPoint(rendererBounds.max);
            Bounds localRendererBounds = new Bounds((min + max) * 0.5f, new Vector3(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y), Mathf.Abs(max.z - min.z)));

            if (!hasBounds)
            {
                bounds = localRendererBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(localRendererBounds);
            }
        }

        return bounds;
    }
}
