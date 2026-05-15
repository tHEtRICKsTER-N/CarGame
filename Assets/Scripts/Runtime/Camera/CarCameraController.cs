using UnityEngine;

[DisallowMultipleComponent]
public class CarCameraController : MonoBehaviour
{
    [Header("Camera Targets")]
    [Tooltip("The car's transform (main body). If empty, the first Prometeo car in the scene is used.")]
    public Transform carTransform;

    [Tooltip("Optional: a specific look-at point on the car. Leave empty for a stable forward look target.")]
    public Transform lookAtTarget;

    [Header("Camera Mode")]
    public CameraMode currentMode = CameraMode.FollowBehind;
    [Tooltip("Key to cycle through camera modes.")]
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

    [Header("Stutter Prevention")]
    [Tooltip("Enables Rigidbody interpolation on the target car so camera motion is smooth between physics ticks.")]
    public bool enableRigidbodyInterpolation = true;
    [Tooltip("Uses a flattened, smoothed car heading so bumps and body roll do not shake the chase camera.")]
    public bool ignoreCarPitchAndRoll = true;
    [Range(1f, 30f)]
    public float headingSmoothSpeed = 16f;
    [Range(1f, 30f)]
    public float lookTargetSmoothSpeed = 18f;

    [Header("Orbit Settings")]
    [Range(3f, 20f)]
    public float orbitDistance = 8f;
    [Range(0f, 5f)]
    public float orbitHeight = 2f;
    [Range(50f, 300f)]
    public float orbitSpeed = 100f;

    [Header("Dynamic Camera Effects")]
    [Tooltip("Enable FOV changes based on speed.")]
    public bool useDynamicFOV = true;
    [Range(50f, 70f)]
    public float baseFOV = 60f;
    [Range(70f, 90f)]
    public float maxFOV = 75f;
    [Range(20f, 150f)]
    public float fovSpeedThreshold = 100f;
    [Range(0f, 15f)]
    public float boostFOVKick = 7f;
    [Range(1f, 12f)]
    public float fovSmoothSpeed = 3.5f;

    [Header("Camera Shake")]
    public bool useCameraShake = false;
    [Range(0f, 0.5f)]
    public float shakeIntensity = 0.08f;
    [Range(0f, 0.5f)]
    public float boostShakeIntensity = 0.06f;

    [Header("Collision Detection")]
    public bool avoidObstacles = true;
    public LayerMask obstacleLayer;
    [Range(0.1f, 2f)]
    public float cameraRadius = 0.3f;

    private Camera cam;
    private Rigidbody carRigidbody;
    private PrometeoCarController carController;
    private ArcadeBoostController boostController;
    private Transform cachedCarTransform;
    private Vector3 currentVelocity;
    private Vector3 smoothedForward;
    private Vector3 smoothedLookTarget;
    private Vector3 baseCameraPosition;
    private Vector3 shakeOffset;
    private float orbitAngle;
    private float currentFOV;
    private bool targetStateInitialized;

    public enum CameraMode
    {
        FollowBehind,
        Orbit,
        Cinematic
    }

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }

        CacheTargetReferences();

        currentFOV = baseFOV;
        if (cam != null)
        {
            cam.fieldOfView = currentFOV;
        }

        ResetCamera();
    }

    private void LateUpdate()
    {
        CacheTargetReferences();

        if (carTransform == null)
        {
            return;
        }

        if (Input.GetKeyDown(changeCameraKey))
        {
            CycleCameraMode();
        }

        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
        UpdateSmoothedTargetState(deltaTime);

        switch (currentMode)
        {
            case CameraMode.FollowBehind:
                UpdateFollowBehindCamera(deltaTime);
                break;
            case CameraMode.Orbit:
                UpdateOrbitCamera(deltaTime);
                break;
            case CameraMode.Cinematic:
                UpdateCinematicCamera(deltaTime);
                break;
        }

        if (useDynamicFOV && cam != null)
        {
            UpdateDynamicFOV(deltaTime);
        }

        ApplyFinalPosition(deltaTime);
    }

    private void CacheTargetReferences()
    {
        if (carTransform == null)
        {
            PrometeoCarController foundCar = FindFirstObjectByType<PrometeoCarController>();
            if (foundCar != null)
            {
                carTransform = foundCar.transform;
            }
        }

        if (carTransform == null || carTransform == cachedCarTransform)
        {
            return;
        }

        cachedCarTransform = carTransform;
        carRigidbody = carTransform.GetComponent<Rigidbody>();
        carController = carTransform.GetComponent<PrometeoCarController>();
        boostController = carTransform.GetComponent<ArcadeBoostController>();
        targetStateInitialized = false;

        if (enableRigidbodyInterpolation && carRigidbody != null)
        {
            carRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    private void UpdateSmoothedTargetState(float deltaTime)
    {
        Vector3 rawForward = GetStableCarForward();
        Vector3 rawLookTarget = GetRawLookTarget(rawForward);

        if (!targetStateInitialized)
        {
            smoothedForward = rawForward;
            smoothedLookTarget = rawLookTarget;
            baseCameraPosition = transform.position;
            targetStateInitialized = true;
            return;
        }

        smoothedForward = Vector3.Slerp(smoothedForward, rawForward, DampedLerp(headingSmoothSpeed, deltaTime));
        smoothedLookTarget = Vector3.Lerp(smoothedLookTarget, rawLookTarget, DampedLerp(lookTargetSmoothSpeed, deltaTime));
    }

    private void UpdateFollowBehindCamera(float deltaTime)
    {
        Vector3 desiredPosition = GetCarPosition() - (smoothedForward * followDistance) + (Vector3.up * followHeight);

        if (avoidObstacles)
        {
            desiredPosition = CheckCameraCollision(smoothedLookTarget, desiredPosition);
        }

        baseCameraPosition = Vector3.SmoothDamp(
            baseCameraPosition,
            desiredPosition,
            ref currentVelocity,
            SmoothTimeFromSpeed(followSmoothSpeed),
            Mathf.Infinity,
            deltaTime);

        RotateToward(smoothedLookTarget, rotationSmoothSpeed, deltaTime);
    }

    private void UpdateOrbitCamera(float deltaTime)
    {
        orbitAngle += orbitSpeed * deltaTime;
        if (orbitAngle > 360f)
        {
            orbitAngle -= 360f;
        }

        float radians = orbitAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Sin(radians) * orbitDistance, orbitHeight, Mathf.Cos(radians) * orbitDistance);
        Vector3 desiredPosition = GetCarPosition() + offset;

        baseCameraPosition = Vector3.Lerp(baseCameraPosition, desiredPosition, DampedLerp(followSmoothSpeed, deltaTime));
        RotateToward(smoothedLookTarget, rotationSmoothSpeed, deltaTime);
    }

    private void UpdateCinematicCamera(float deltaTime)
    {
        Vector3 right = Vector3.Cross(Vector3.up, smoothedForward).normalized;
        Vector3 desiredPosition = GetCarPosition()
            - (smoothedForward * followDistance * 0.55f)
            + (right * followDistance * 0.7f)
            + (Vector3.up * followHeight);

        baseCameraPosition = Vector3.SmoothDamp(
            baseCameraPosition,
            desiredPosition,
            ref currentVelocity,
            SmoothTimeFromSpeed(followSmoothSpeed * 0.75f),
            Mathf.Infinity,
            deltaTime);

        RotateToward(smoothedLookTarget, rotationSmoothSpeed * 0.85f, deltaTime);
    }

    private void UpdateDynamicFOV(float deltaTime)
    {
        float speed = 0f;
        if (carController != null)
        {
            speed = Mathf.Abs(carController.carSpeed);
        }
        else if (carRigidbody != null)
        {
            speed = carRigidbody.linearVelocity.magnitude * 3.6f;
        }

        float targetFOV = Mathf.Lerp(baseFOV, maxFOV, Mathf.Clamp01(speed / fovSpeedThreshold));
        if (boostController != null && boostController.IsBoosting)
        {
            targetFOV += boostFOVKick;
        }

        currentFOV = Mathf.Lerp(currentFOV, targetFOV, DampedLerp(fovSmoothSpeed, deltaTime));
        cam.fieldOfView = currentFOV;
    }

    private void ApplyFinalPosition(float deltaTime)
    {
        Vector3 finalPosition = baseCameraPosition;

        if (useCameraShake)
        {
            finalPosition += GetSmoothShake(deltaTime);
        }
        else
        {
            shakeOffset = Vector3.Lerp(shakeOffset, Vector3.zero, DampedLerp(12f, deltaTime));
        }

        transform.position = finalPosition;
    }

    private Vector3 GetSmoothShake(float deltaTime)
    {
        float speed = 0f;
        bool isDrifting = false;

        if (carController != null)
        {
            speed = Mathf.Abs(carController.carSpeed);
            isDrifting = carController.isDrifting;
        }
        else if (carRigidbody != null)
        {
            speed = carRigidbody.linearVelocity.magnitude * 3.6f;
        }

        float shakeAmount = Mathf.Clamp01(speed / 120f) * shakeIntensity;
        if (isDrifting)
        {
            shakeAmount *= 1.75f;
        }

        if (boostController != null && boostController.IsBoosting)
        {
            shakeAmount += boostShakeIntensity;
        }

        float noiseTime = Time.time * 18f;
        Vector3 targetShake = new Vector3(
            (Mathf.PerlinNoise(noiseTime, 0.13f) - 0.5f) * 2f,
            (Mathf.PerlinNoise(0.37f, noiseTime) - 0.5f) * 2f,
            0f) * shakeAmount;

        shakeOffset = Vector3.Lerp(shakeOffset, targetShake, DampedLerp(18f, deltaTime));
        return (transform.right * shakeOffset.x) + (transform.up * shakeOffset.y);
    }

    private Vector3 CheckCameraCollision(Vector3 lookTarget, Vector3 desiredPosition)
    {
        Vector3 direction = desiredPosition - lookTarget;
        float distance = direction.magnitude;

        if (distance <= cameraRadius)
        {
            return desiredPosition;
        }

        RaycastHit hit;
        if (Physics.SphereCast(lookTarget, cameraRadius, direction / distance, out hit, distance, obstacleLayer))
        {
            return lookTarget + (direction / distance) * Mathf.Max(hit.distance - cameraRadius, 0.25f);
        }

        return desiredPosition;
    }

    private Vector3 GetStableCarForward()
    {
        Vector3 forward = carTransform.forward;
        if (ignoreCarPitchAndRoll)
        {
            forward = Vector3.ProjectOnPlane(forward, Vector3.up);
        }

        if (forward.sqrMagnitude < 0.001f)
        {
            return smoothedForward.sqrMagnitude > 0.001f ? smoothedForward.normalized : Vector3.forward;
        }

        return forward.normalized;
    }

    private Vector3 GetRawLookTarget(Vector3 stableForward)
    {
        if (lookAtTarget != null)
        {
            return lookAtTarget.position;
        }

        return GetCarPosition() + (Vector3.up * 1.25f) + (stableForward * 1.4f);
    }

    private Vector3 GetCarPosition()
    {
        return carTransform != null ? carTransform.position : transform.position;
    }

    private void RotateToward(Vector3 target, float speed, float deltaTime)
    {
        Vector3 direction = target - baseCameraPosition;
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, DampedLerp(speed, deltaTime));
    }

    private void CycleCameraMode()
    {
        int nextMode = ((int)currentMode + 1) % System.Enum.GetValues(typeof(CameraMode)).Length;
        SetCameraMode((CameraMode)nextMode);
        Debug.Log("Camera Mode: " + currentMode);
    }

    public void SetCameraMode(CameraMode mode)
    {
        currentMode = mode;
        currentVelocity = Vector3.zero;
        shakeOffset = Vector3.zero;

        if (currentMode == CameraMode.Orbit)
        {
            orbitAngle = 0f;
        }
    }

    public void ResetCamera()
    {
        CacheTargetReferences();

        if (carTransform == null)
        {
            return;
        }

        Vector3 stableForward = GetStableCarForward();
        smoothedForward = stableForward;
        smoothedLookTarget = GetRawLookTarget(stableForward);
        baseCameraPosition = GetCarPosition() - (stableForward * followDistance) + (Vector3.up * followHeight);
        currentVelocity = Vector3.zero;
        shakeOffset = Vector3.zero;
        targetStateInitialized = true;

        transform.position = baseCameraPosition;
        transform.LookAt(smoothedLookTarget, Vector3.up);
    }

    private static float SmoothTimeFromSpeed(float speed)
    {
        return 1f / Mathf.Max(0.01f, speed);
    }

    private static float DampedLerp(float speed, float deltaTime)
    {
        return 1f - Mathf.Exp(-Mathf.Max(0f, speed) * deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        if (carTransform == null)
        {
            return;
        }

        Vector3 stableForward = Application.isPlaying && smoothedForward.sqrMagnitude > 0.001f
            ? smoothedForward
            : Vector3.ProjectOnPlane(carTransform.forward, Vector3.up).normalized;

        if (stableForward.sqrMagnitude < 0.001f)
        {
            stableForward = carTransform.forward;
        }

        Gizmos.color = Color.blue;
        Vector3 followPos = carTransform.position - (stableForward * followDistance) + (Vector3.up * followHeight);
        Gizmos.DrawWireSphere(followPos, 0.3f);

        Gizmos.color = Color.yellow;
        float segments = 32f;
        Vector3 lastPoint = carTransform.position + new Vector3(orbitDistance, orbitHeight, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = (i / segments) * 360f * Mathf.Deg2Rad;
            Vector3 point = carTransform.position + new Vector3(Mathf.Sin(angle) * orbitDistance, orbitHeight, Mathf.Cos(angle) * orbitDistance);
            Gizmos.DrawLine(lastPoint, point);
            lastPoint = point;
        }

    }
}
