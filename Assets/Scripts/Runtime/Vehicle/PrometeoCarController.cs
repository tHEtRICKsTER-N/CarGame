using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class PrometeoCarController : MonoBehaviour
{
    [Header("Driving")]
    public int maxSpeed = 95;
    [Range(1, 10)]
    public int accelerationMultiplier = 5;
    public int brakeForce = 520;
    public int handbrakeForce = 850;
    [Range(15, 45)]
    public int maxSteeringAngle = 32;
    [Range(0.1f, 8f)]
    public float steeringReturnSpeed = 4.5f;

    [Header("Compatibility")]
    public bool useUI;
    public bool useSounds;
    public bool useTouchControls;

    [Header("Wheel Colliders")]
    public WheelCollider frontLeftCollider;
    public WheelCollider frontRightCollider;
    public WheelCollider rearLeftCollider;
    public WheelCollider rearRightCollider;

    [Header("Wheel Mesh Targets")]
    public GameObject frontLeftMesh;
    public GameObject frontRightMesh;
    public GameObject rearLeftMesh;
    public GameObject rearRightMesh;

    [Header("Input")]
    public KeyCode handbrakeKey = KeyCode.Space;

    public float carSpeed;
    public bool isDrifting;
    public bool isTractionLocked;

    private Rigidbody carRigidbody;
    private float horizontalInput;
    private float verticalInput;
    private float currentSteeringAngle;

    private void Awake()
    {
        carRigidbody = GetComponent<Rigidbody>();
        if (carRigidbody != null)
        {
            carRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            carRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
    }

    private void Update()
    {
        ReadInput();
        UpdateSpeedAndTractionState();
        AnimateWheelMeshes();
    }

    private void FixedUpdate()
    {
        if (!HasWheelRig())
        {
            return;
        }

        ApplySteering();
        ApplyDrive();
    }

    private void ReadInput()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            horizontalInput = Mathf.Min(horizontalInput, -1f);
        }
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            horizontalInput = Mathf.Max(horizontalInput, 1f);
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            verticalInput = Mathf.Max(verticalInput, 1f);
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            verticalInput = Mathf.Min(verticalInput, -1f);
        }
    }

    private void ApplySteering()
    {
        float targetSteeringAngle = horizontalInput * maxSteeringAngle;
        currentSteeringAngle = Mathf.Lerp(currentSteeringAngle, targetSteeringAngle, steeringReturnSpeed * Time.fixedDeltaTime);
        frontLeftCollider.steerAngle = currentSteeringAngle;
        frontRightCollider.steerAngle = currentSteeringAngle;
    }

    private void ApplyDrive()
    {
        float speedKmh = GetSpeedKmh();
        float forwardSpeed = carRigidbody != null ? Vector3.Dot(carRigidbody.linearVelocity, transform.forward) : 0f;
        bool handbrake = Input.GetKey(handbrakeKey);
        bool brakingForDirectionChange = verticalInput < -0.1f && forwardSpeed > 1.2f;
        bool reversing = verticalInput < -0.1f && forwardSpeed <= 1.2f;
        bool overForwardLimit = verticalInput > 0.1f && speedKmh >= maxSpeed;
        bool overReverseLimit = reversing && speedKmh >= 32f;

        float motorTorque = 0f;
        if (!handbrake && !brakingForDirectionChange && !overForwardLimit && !overReverseLimit)
        {
            motorTorque = verticalInput * accelerationMultiplier * 185f;
        }

        SetMotorTorque(motorTorque);

        float brakeTorque = 0f;
        if (handbrake)
        {
            brakeTorque = handbrakeForce;
        }
        else if (brakingForDirectionChange)
        {
            brakeTorque = brakeForce;
        }

        SetBrakeTorque(brakeTorque);
    }

    private void SetMotorTorque(float torque)
    {
        frontLeftCollider.motorTorque = torque;
        frontRightCollider.motorTorque = torque;
        rearLeftCollider.motorTorque = torque;
        rearRightCollider.motorTorque = torque;
    }

    private void SetBrakeTorque(float torque)
    {
        frontLeftCollider.brakeTorque = torque;
        frontRightCollider.brakeTorque = torque;
        rearLeftCollider.brakeTorque = torque;
        rearRightCollider.brakeTorque = torque;
    }

    private void UpdateSpeedAndTractionState()
    {
        carSpeed = GetSpeedKmh();
        isDrifting = false;
        isTractionLocked = false;

        UpdateWheelTraction(frontLeftCollider);
        UpdateWheelTraction(frontRightCollider);
        UpdateWheelTraction(rearLeftCollider);
        UpdateWheelTraction(rearRightCollider);
    }

    private void UpdateWheelTraction(WheelCollider wheelCollider)
    {
        if (wheelCollider == null || !wheelCollider.GetGroundHit(out WheelHit hit))
        {
            return;
        }

        float sidewaysSlip = Mathf.Abs(hit.sidewaysSlip);
        float forwardSlip = Mathf.Abs(hit.forwardSlip);
        isDrifting |= sidewaysSlip > 0.42f;
        isTractionLocked |= forwardSlip > 0.58f || sidewaysSlip > 0.72f;
    }

    private void AnimateWheelMeshes()
    {
        UpdateWheel(frontLeftCollider, frontLeftMesh);
        UpdateWheel(frontRightCollider, frontRightMesh);
        UpdateWheel(rearLeftCollider, rearLeftMesh);
        UpdateWheel(rearRightCollider, rearRightMesh);
    }

    private float GetSpeedKmh()
    {
        if (carRigidbody == null)
        {
            return 0f;
        }

        return carRigidbody.linearVelocity.magnitude * 3.6f;
    }

    private bool HasWheelRig()
    {
        return frontLeftCollider != null
            && frontRightCollider != null
            && rearLeftCollider != null
            && rearRightCollider != null;
    }

    private static void UpdateWheel(WheelCollider wheelCollider, GameObject wheelMesh)
    {
        if (wheelCollider == null || wheelMesh == null)
        {
            return;
        }

        wheelCollider.GetWorldPose(out Vector3 position, out Quaternion rotation);
        wheelMesh.transform.SetPositionAndRotation(position, rotation);
    }
}
