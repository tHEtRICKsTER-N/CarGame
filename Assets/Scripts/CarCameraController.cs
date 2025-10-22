using UnityEngine;

public class CarCameraController : MonoBehaviour
{
    [Header("Camera Targets")]
    [Tooltip("The car's transform (main body)")]
    public Transform carTransform;

    [Tooltip("Optional: A specific look-at point on the car (leave empty to use car center)")]
    public Transform lookAtTarget;

    [Header("Camera Mode")]
    public CameraMode currentMode = CameraMode.FollowBehind;
    [Tooltip("Key to cycle through camera modes")]
    public KeyCode changeCameraKey = KeyCode.C;

    [Header("Follow Behind Settings")]
    [Range(2f, 15f)]
    public float followDistance = 6f;
    [Range(1f, 5f)]
    public float followHeight = 2f;
    [Range(1f, 20f)]
    public float followSmoothSpeed = 10f;
    [Range(1f, 20f)]
    public float rotationSmoothSpeed = 8f;

    [Header("Orbit Settings")]
    [Range(3f, 20f)]
    public float orbitDistance = 8f;
    [Range(0f, 5f)]
    public float orbitHeight = 2f;
    [Range(50f, 300f)]
    public float orbitSpeed = 100f;

    [Header("First Person Settings")]
    public Transform firstPersonPoint;
    [Range(1f, 20f)]
    public float firstPersonSmoothSpeed = 15f;

    [Header("Dynamic Camera Effects")]
    [Tooltip("Enable FOV changes based on speed")]
    public bool useDynamicFOV = true;
    [Range(50f, 70f)]
    public float baseFOV = 60f;
    [Range(70f, 90f)]
    public float maxFOV = 75f;
    [Range(20f, 150f)]
    public float fovSpeedThreshold = 100f;

    [Header("Camera Shake")]
    public bool useCameraShake = true;
    [Range(0f, 0.5f)]
    public float shakeIntensity = 0.1f;

    [Header("Collision Detection")]
    public bool avoidObstacles = true;
    public LayerMask obstacleLayer;
    [Range(0.1f, 2f)]
    public float cameraRadius = 0.3f;

    // Private variables
    private Camera cam;
    private Vector3 currentVelocity;
    private float currentRotationVelocity;
    private float orbitAngle = 0f;
    private Vector3 shakeOffset;
    private PrometeoCarController carController;
    private Vector3 lastCarPosition;
    private float currentFOV;

    public enum CameraMode
    {
        FollowBehind,
        Orbit,
        FirstPerson,
        Cinematic
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }

        if (carTransform != null)
        {
            carController = carTransform.GetComponent<PrometeoCarController>();
            lastCarPosition = carTransform.position;
        }

        currentFOV = baseFOV;
        if (cam != null)
        {
            cam.fieldOfView = currentFOV;
        }

        // If no first person point is set, create one
        if (firstPersonPoint == null && carTransform != null)
        {
            GameObject fpPoint = new GameObject("FirstPersonPoint");
            fpPoint.transform.parent = carTransform;
            fpPoint.transform.localPosition = new Vector3(0f, 1f, 0.5f);
            firstPersonPoint = fpPoint.transform;
        }
    }

    void LateUpdate()
    {
        if (carTransform == null) return;

        // Change camera mode
        if (Input.GetKeyDown(changeCameraKey))
        {
            CycleCameraMode();
        }

        // Get look at target
        Vector3 lookTarget = lookAtTarget != null ? lookAtTarget.position : carTransform.position;

        // Update camera based on current mode
        switch (currentMode)
        {
            case CameraMode.FollowBehind:
                UpdateFollowBehindCamera(lookTarget);
                break;
            case CameraMode.Orbit:
                UpdateOrbitCamera(lookTarget);
                break;
            case CameraMode.FirstPerson:
                UpdateFirstPersonCamera();
                break;
            case CameraMode.Cinematic:
                UpdateCinematicCamera(lookTarget);
                break;
        }

        // Apply dynamic FOV
        if (useDynamicFOV && cam != null)
        {
            UpdateDynamicFOV();
        }

        // Apply camera shake
        if (useCameraShake && currentMode != CameraMode.FirstPerson)
        {
            ApplyCameraShake();
        }
    }

    void UpdateFollowBehindCamera(Vector3 lookTarget)
    {
        // Calculate desired position behind the car
        Vector3 desiredPosition = carTransform.position - (carTransform.forward * followDistance) + (Vector3.up * followHeight);

        // Check for obstacles
        if (avoidObstacles)
        {
            desiredPosition = CheckCameraCollision(lookTarget, desiredPosition);
        }

        // Smooth follow
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, 1f / followSmoothSpeed);

        // Smooth rotation to look at target
        Vector3 direction = lookTarget - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
    }

    void UpdateOrbitCamera(Vector3 lookTarget)
    {
        // Increase orbit angle over time
        orbitAngle += orbitSpeed * Time.deltaTime;
        if (orbitAngle > 360f) orbitAngle -= 360f;

        // Calculate orbit position
        float radians = orbitAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Sin(radians) * orbitDistance, orbitHeight, Mathf.Cos(radians) * orbitDistance);
        Vector3 desiredPosition = carTransform.position + offset;

        // Move camera
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSmoothSpeed * Time.deltaTime);

        // Look at target
        transform.LookAt(lookTarget);
    }

    void UpdateFirstPersonCamera()
    {
        if (firstPersonPoint != null)
        {
            // Smooth follow for first person
            transform.position = Vector3.Lerp(transform.position, firstPersonPoint.position, firstPersonSmoothSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, firstPersonPoint.rotation, firstPersonSmoothSpeed * Time.deltaTime);
        }
    }

    void UpdateCinematicCamera(Vector3 lookTarget)
    {
        // Side-angle follow
        Vector3 sideOffset = carTransform.right * followDistance * 0.7f;
        Vector3 desiredPosition = carTransform.position - (carTransform.forward * followDistance * 0.5f) + sideOffset + (Vector3.up * followHeight);

        // Smooth movement
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, 1f / (followSmoothSpeed * 0.7f));

        // Look at target
        Vector3 direction = lookTarget - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * 0.8f * Time.deltaTime);
    }

    void UpdateDynamicFOV()
    {
        float speed = 0f;
        if (carController != null)
        {
            speed = Mathf.Abs(carController.carSpeed);
        }

        // Calculate target FOV based on speed
        float targetFOV = Mathf.Lerp(baseFOV, maxFOV, speed / fovSpeedThreshold);
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * 2f);
        cam.fieldOfView = currentFOV;
    }

    void ApplyCameraShake()
    {
        float speed = 0f;
        bool isDrifting = false;

        if (carController != null)
        {
            speed = Mathf.Abs(carController.carSpeed);
            isDrifting = carController.isDrifting;
        }

        // Calculate shake based on speed and drifting
        float shakeAmount = (speed / 100f) * shakeIntensity;
        if (isDrifting)
        {
            shakeAmount *= 2f;
        }

        // Generate random shake offset
        shakeOffset = new Vector3(
            Random.Range(-shakeAmount, shakeAmount),
            Random.Range(-shakeAmount, shakeAmount),
            0f
        );

        transform.position += shakeOffset;
    }

    Vector3 CheckCameraCollision(Vector3 lookTarget, Vector3 desiredPosition)
    {
        Vector3 direction = desiredPosition - lookTarget;
        float distance = direction.magnitude;

        RaycastHit hit;
        if (Physics.SphereCast(lookTarget, cameraRadius, direction.normalized, out hit, distance, obstacleLayer))
        {
            // Move camera closer to avoid obstacle
            return lookTarget + direction.normalized * (hit.distance - cameraRadius);
        }

        return desiredPosition;
    }

    void CycleCameraMode()
    {
        int nextMode = ((int)currentMode + 1) % System.Enum.GetValues(typeof(CameraMode)).Length;
        currentMode = (CameraMode)nextMode;

        // Reset orbit angle when switching to orbit mode
        if (currentMode == CameraMode.Orbit)
        {
            orbitAngle = 0f;
        }

        Debug.Log("Camera Mode: " + currentMode);
    }

    // Public method to set camera mode from other scripts
    public void SetCameraMode(CameraMode mode)
    {
        currentMode = mode;
        if (currentMode == CameraMode.Orbit)
        {
            orbitAngle = 0f;
        }
    }

    // Public method to reset camera position
    public void ResetCamera()
    {
        if (carTransform != null)
        {
            Vector3 resetPosition = carTransform.position - (carTransform.forward * followDistance) + (Vector3.up * followHeight);
            transform.position = resetPosition;
            transform.LookAt(carTransform);
        }
    }

    // Visualize camera settings in editor
    void OnDrawGizmosSelected()
    {
        if (carTransform == null) return;

        // Draw follow behind position
        Gizmos.color = Color.blue;
        Vector3 followPos = carTransform.position - (carTransform.forward * followDistance) + (Vector3.up * followHeight);
        Gizmos.DrawWireSphere(followPos, 0.3f);

        // Draw orbit path
        Gizmos.color = Color.yellow;
        float segments = 32;
        Vector3 lastPoint = carTransform.position + new Vector3(orbitDistance, orbitHeight, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = (i / segments) * 360f * Mathf.Deg2Rad;
            Vector3 point = carTransform.position + new Vector3(Mathf.Sin(angle) * orbitDistance, orbitHeight, Mathf.Cos(angle) * orbitDistance);
            Gizmos.DrawLine(lastPoint, point);
            lastPoint = point;
        }

        // Draw first person point
        if (firstPersonPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(firstPersonPoint.position, 0.2f);
        }
    }
}