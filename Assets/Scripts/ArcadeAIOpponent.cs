using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class ArcadeAIOpponent : MonoBehaviour
{
    [Header("Driving")]
    public float desiredSpeedKmh = 88f;
    public float cautiousTurnSpeedKmh = 50f;
    public float steeringResponse = 6f;
    public float brakeStrength = 420f;
    public float waypointLookAhead = 10f;

    private ArcadeRaceManager raceManager;
    private PrometeoCarController car;
    private ArcadeRaceParticipant participant;
    private Rigidbody carRigidbody;
    private float currentSteering;
    private bool configured;

    public void Configure(ArcadeRaceManager manager, PrometeoCarController sourceCar, ArcadeRaceParticipant raceParticipant, float speedOffset)
    {
        raceManager = manager;
        car = sourceCar;
        participant = raceParticipant;
        desiredSpeedKmh += speedOffset;
        configured = true;
    }

    private void Awake()
    {
        carRigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (!configured || raceManager == null || car == null)
        {
            return;
        }

        if (!raceManager.RaceActive || participant.HasFinished)
        {
            ApplyBrake(brakeStrength * 0.5f);
            AnimateWheelMeshes();
            return;
        }

        IReadOnlyList<Vector3> checkpoints = raceManager.Checkpoints;
        Vector3 target = checkpoints[participant.NextCheckpointIndex];
        Vector3 targetDirection = target - transform.position;
        targetDirection.y = 0f;

        if (targetDirection.magnitude < waypointLookAhead && checkpoints.Count > 1)
        {
            int lookAheadIndex = participant.NextCheckpointIndex + 1;
            if (lookAheadIndex >= checkpoints.Count)
            {
                lookAheadIndex = 0;
            }

            target = Vector3.Lerp(target, checkpoints[lookAheadIndex], 0.45f);
        }

        DriveToward(target);
        AnimateWheelMeshes();
    }

    private void DriveToward(Vector3 target)
    {
        Vector3 localTarget = transform.InverseTransformPoint(target);
        float steerInput = Mathf.Clamp(localTarget.x / Mathf.Max(Mathf.Abs(localTarget.z), 8f), -1f, 1f);

        if (localTarget.z < -4f)
        {
            steerInput = Mathf.Sign(localTarget.x);
        }

        currentSteering = Mathf.Lerp(currentSteering, steerInput, steeringResponse * Time.fixedDeltaTime);

        float steeringAngle = currentSteering * car.maxSteeringAngle;
        car.frontLeftCollider.steerAngle = steeringAngle;
        car.frontRightCollider.steerAngle = steeringAngle;

        float speedKmh = carRigidbody.linearVelocity.magnitude * 3.6f;
        float turnSlowdown = Mathf.Lerp(desiredSpeedKmh, cautiousTurnSpeedKmh, Mathf.Abs(currentSteering));
        float targetSpeed = localTarget.z < 0f ? 20f : turnSlowdown;

        if (speedKmh < targetSpeed)
        {
            ApplyMotorTorque((car.accelerationMultiplier * 55f) * Mathf.Sign(Mathf.Max(localTarget.z, 0.2f)));
            ApplyBrake(0f);
        }
        else
        {
            ApplyMotorTorque(0f);
            ApplyBrake(Mathf.Lerp(0f, brakeStrength, Mathf.InverseLerp(targetSpeed, targetSpeed + 25f, speedKmh)));
        }
    }

    private void ApplyMotorTorque(float torque)
    {
        car.frontLeftCollider.motorTorque = torque;
        car.frontRightCollider.motorTorque = torque;
        car.rearLeftCollider.motorTorque = torque;
        car.rearRightCollider.motorTorque = torque;
    }

    private void ApplyBrake(float brakeTorque)
    {
        car.frontLeftCollider.brakeTorque = brakeTorque;
        car.frontRightCollider.brakeTorque = brakeTorque;
        car.rearLeftCollider.brakeTorque = brakeTorque;
        car.rearRightCollider.brakeTorque = brakeTorque;

        if (brakeTorque > 0f)
        {
            ApplyMotorTorque(0f);
        }
    }

    private void AnimateWheelMeshes()
    {
        UpdateWheel(car.frontLeftCollider, car.frontLeftMesh);
        UpdateWheel(car.frontRightCollider, car.frontRightMesh);
        UpdateWheel(car.rearLeftCollider, car.rearLeftMesh);
        UpdateWheel(car.rearRightCollider, car.rearRightMesh);
    }

    private static void UpdateWheel(WheelCollider wheelCollider, GameObject wheelMesh)
    {
        if (wheelCollider == null || wheelMesh == null)
        {
            return;
        }

        Vector3 position;
        Quaternion rotation;
        wheelCollider.GetWorldPose(out position, out rotation);
        wheelMesh.transform.position = position;
        wheelMesh.transform.rotation = rotation;
    }
}
