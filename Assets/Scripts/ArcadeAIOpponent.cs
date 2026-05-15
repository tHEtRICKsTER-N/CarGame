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

    [Header("Awareness")]
    public bool useRaycastAwareness = true;
    public LayerMask awarenessMask = Physics.DefaultRaycastLayers;
    public float sensorHeight = 1.1f;
    public float forwardSensorLength = 24f;
    public float angledSensorLength = 18f;
    public float sideSensorLength = 9f;
    [Range(10f, 60f)]
    public float sensorAngle = 32f;
    public bool drawAwarenessDebug;

    private ArcadeRaceManager raceManager;
    private PrometeoCarController car;
    private ArcadeRaceParticipant participant;
    private Rigidbody carRigidbody;
    private float currentSteering;
    private RaceAIDifficulty difficulty = RaceAIDifficulty.Medium;
    private int currentRouteIndex;
    private int targetRouteIndex = 1;
    private float driverSeed;
    private float sensorCaution = 1f;
    private float cornerCaution = 1f;
    private float accelerationConfidence = 1f;
    private float avoidanceStrength = 1f;
    private float sensorSpeedFloor = 0.42f;
    private float rubberbandSpeedMultiplier = 1f;
    private bool configured;
    private readonly RaycastHit[] sensorHits = new RaycastHit[8];

    private struct DrivingAwareness
    {
        public float obstaclePressure;
        public float avoidanceSteer;
        public float routeTurnSeverity;
        public float speedFactor;
        public float brakePressure;
    }

    public void Configure(ArcadeRaceManager manager, PrometeoCarController sourceCar, ArcadeRaceParticipant raceParticipant, float speedOffset)
    {
        raceManager = manager;
        car = sourceCar;
        participant = raceParticipant;
        difficulty = manager != null ? manager.AIDifficulty : RaceAIDifficulty.Medium;
        driverSeed = Mathf.Abs(transform.GetInstanceID() * 0.0137f);
        currentRouteIndex = 0;
        targetRouteIndex = manager != null ? manager.GetNextRoutePointIndex(currentRouteIndex, difficulty, driverSeed) : 1;
        ApplyDifficultyProfile(speedOffset);
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
        Vector3 target = raceManager.HasRouteGraph ? GetGraphTarget(checkpoints) : GetOrderedTarget(checkpoints);

        DriveToward(target, checkpoints);
        AnimateWheelMeshes();
    }

    private Vector3 GetGraphTarget(IReadOnlyList<Vector3> checkpoints)
    {
        UpdateGraphRouteTarget(checkpoints);

        Vector3 target = checkpoints[targetRouteIndex];
        Vector3 targetDirection = target - transform.position;
        targetDirection.y = 0f;

        if (targetRouteIndex != 0 && targetDirection.magnitude < waypointLookAhead && checkpoints.Count > 1)
        {
            int lookAheadIndex = raceManager.GetNextRoutePointIndex(targetRouteIndex, difficulty, driverSeed + 0.37f);
            if (lookAheadIndex >= 0 && lookAheadIndex < checkpoints.Count)
            {
                target = Vector3.Lerp(target, checkpoints[lookAheadIndex], 0.45f);
            }
        }

        return target;
    }

    private Vector3 GetOrderedTarget(IReadOnlyList<Vector3> checkpoints)
    {
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

        return target;
    }

    private void UpdateGraphRouteTarget(IReadOnlyList<Vector3> checkpoints)
    {
        if (targetRouteIndex < 0 || targetRouteIndex >= checkpoints.Count)
        {
            currentRouteIndex = Mathf.Clamp(participant.CurrentCheckpointIndex, 0, checkpoints.Count - 1);
            targetRouteIndex = raceManager.GetNextRoutePointIndex(currentRouteIndex, difficulty, driverSeed);
            return;
        }

        float radius = Mathf.Max(6f, raceManager.CheckpointRadius * 0.65f);
        Vector3 targetOffset = checkpoints[targetRouteIndex] - transform.position;
        targetOffset.y = 0f;

        if (targetOffset.sqrMagnitude > radius * radius)
        {
            return;
        }

        currentRouteIndex = targetRouteIndex;
        targetRouteIndex = raceManager.GetNextRoutePointIndex(currentRouteIndex, difficulty, driverSeed);
    }

    private void DriveToward(Vector3 target, IReadOnlyList<Vector3> checkpoints)
    {
        DrivingAwareness awareness = SenseDrivingAwareness(target, checkpoints);
        Vector3 localTarget = transform.InverseTransformPoint(target);
        float steerInput = Mathf.Clamp(localTarget.x / Mathf.Max(Mathf.Abs(localTarget.z), 8f), -1f, 1f);

        if (localTarget.z < -4f)
        {
            steerInput = Mathf.Sign(localTarget.x);
        }

        steerInput = Mathf.Clamp(steerInput + awareness.avoidanceSteer * avoidanceStrength, -1f, 1f);
        currentSteering = Mathf.Lerp(currentSteering, steerInput, steeringResponse * Time.fixedDeltaTime);

        float steeringAngle = currentSteering * car.maxSteeringAngle;
        car.frontLeftCollider.steerAngle = steeringAngle;
        car.frontRightCollider.steerAngle = steeringAngle;

        float speedKmh = carRigidbody.linearVelocity.magnitude * 3.6f;
        float turnSeverity = Mathf.Clamp01(Mathf.Max(Mathf.Abs(currentSteering), awareness.routeTurnSeverity) * cornerCaution);
        float turnSlowdown = Mathf.Lerp(desiredSpeedKmh, cautiousTurnSpeedKmh, turnSeverity);
        rubberbandSpeedMultiplier = Mathf.Lerp(rubberbandSpeedMultiplier, raceManager.GetAIRubberbandSpeedMultiplier(participant), 2.8f * Time.fixedDeltaTime);
        float targetSpeed = localTarget.z < 0f ? 18f : turnSlowdown * awareness.speedFactor * rubberbandSpeedMultiplier;
        float speedBrake = Mathf.Lerp(0f, brakeStrength, Mathf.InverseLerp(targetSpeed, targetSpeed + 25f, speedKmh));
        float awarenessBrake = awareness.brakePressure * brakeStrength;

        if (speedKmh < targetSpeed && awareness.brakePressure < 0.55f)
        {
            float rubberbandTorque = Mathf.Lerp(0.88f, 1.18f, Mathf.InverseLerp(0.85f, 1.28f, rubberbandSpeedMultiplier));
            float torqueScale = Mathf.Lerp(1f, 0.35f, awareness.obstaclePressure) * accelerationConfidence * rubberbandTorque;
            ApplyMotorTorque((car.accelerationMultiplier * 55f) * Mathf.Sign(Mathf.Max(localTarget.z, 0.2f)) * torqueScale);
            ApplyBrake(0f);
        }
        else
        {
            ApplyMotorTorque(0f);
            ApplyBrake(Mathf.Max(speedBrake, awarenessBrake));
        }
    }

    private DrivingAwareness SenseDrivingAwareness(Vector3 target, IReadOnlyList<Vector3> checkpoints)
    {
        DrivingAwareness awareness = new DrivingAwareness
        {
            speedFactor = 1f,
            routeTurnSeverity = GetRouteTurnSeverity(target, checkpoints)
        };

        if (!useRaycastAwareness)
        {
            return awareness;
        }

        Vector3 origin = transform.position + Vector3.up * sensorHeight + transform.forward * 0.65f;
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f)
        {
            forward = transform.forward;
        }

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 leftForward = Quaternion.AngleAxis(-sensorAngle, Vector3.up) * forward;
        Vector3 rightForward = Quaternion.AngleAxis(sensorAngle, Vector3.up) * forward;

        float frontPressure = GetSensorPressure(origin, forward, forwardSensorLength);
        float leftFrontPressure = GetSensorPressure(origin, leftForward, angledSensorLength);
        float rightFrontPressure = GetSensorPressure(origin, rightForward, angledSensorLength);
        float leftSidePressure = GetSensorPressure(origin, -right, sideSensorLength) * 0.55f;
        float rightSidePressure = GetSensorPressure(origin, right, sideSensorLength) * 0.55f;

        float leftPressure = Mathf.Max(leftFrontPressure, leftSidePressure);
        float rightPressure = Mathf.Max(rightFrontPressure, rightSidePressure);

        awareness.obstaclePressure = Mathf.Clamp01(Mathf.Max(frontPressure, Mathf.Max(leftPressure, rightPressure) * 0.75f));
        awareness.avoidanceSteer = Mathf.Clamp(leftPressure - rightPressure, -1f, 1f);

        if (frontPressure > 0.42f && Mathf.Abs(awareness.avoidanceSteer) < 0.12f)
        {
            awareness.avoidanceSteer = leftPressure <= rightPressure ? -0.35f : 0.35f;
        }

        float sensorSlowdown = Mathf.Clamp01(awareness.obstaclePressure * sensorCaution);
        awareness.speedFactor = Mathf.Lerp(1f, sensorSpeedFloor, sensorSlowdown);
        awareness.brakePressure = Mathf.Clamp01(Mathf.Max(sensorSlowdown - 0.18f, awareness.routeTurnSeverity * cornerCaution - 0.58f));
        return awareness;
    }

    private float GetSensorPressure(Vector3 origin, Vector3 direction, float length)
    {
        if (length <= 0.01f || !TrySensorCast(origin, direction, length, out RaycastHit hit))
        {
            DrawSensor(origin, direction, length, false);
            return 0f;
        }

        DrawSensor(origin, direction, hit.distance, true);
        return 1f - Mathf.Clamp01(hit.distance / length);
    }

    private bool TrySensorCast(Vector3 origin, Vector3 direction, float length, out RaycastHit closestHit)
    {
        closestHit = default;
        int hitCount = Physics.RaycastNonAlloc(origin, direction, sensorHits, length, awarenessMask, QueryTriggerInteraction.Ignore);
        float closestDistance = length;
        bool foundHit = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = sensorHits[i];
            if (hit.collider == null || IsOwnCollider(hit.collider))
            {
                continue;
            }

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestHit = hit;
                foundHit = true;
            }
        }

        return foundHit;
    }

    private bool IsOwnCollider(Collider collider)
    {
        if (collider.attachedRigidbody != null && collider.attachedRigidbody == carRigidbody)
        {
            return true;
        }

        Transform hitTransform = collider.transform;
        return hitTransform == transform || hitTransform.IsChildOf(transform);
    }

    private float GetRouteTurnSeverity(Vector3 target, IReadOnlyList<Vector3> checkpoints)
    {
        int nextIndex = GetNextLookAheadRouteIndex(checkpoints);
        if (nextIndex < 0 || nextIndex >= checkpoints.Count)
        {
            return 0f;
        }

        Vector3 toTarget = Vector3.ProjectOnPlane(target - transform.position, Vector3.up);
        Vector3 targetToNext = Vector3.ProjectOnPlane(checkpoints[nextIndex] - target, Vector3.up);
        if (toTarget.sqrMagnitude < 0.1f || targetToNext.sqrMagnitude < 0.1f)
        {
            return 0f;
        }

        float turnAngle = Mathf.Abs(Vector3.SignedAngle(toTarget.normalized, targetToNext.normalized, Vector3.up));
        float bendSeverity = Mathf.InverseLerp(18f, 95f, turnAngle);
        float turnProximity = 1f - Mathf.InverseLerp(waypointLookAhead * 1.25f, waypointLookAhead * 3.5f, toTarget.magnitude);
        return Mathf.Clamp01(bendSeverity * turnProximity);
    }

    private int GetNextLookAheadRouteIndex(IReadOnlyList<Vector3> checkpoints)
    {
        if (checkpoints == null || checkpoints.Count < 3)
        {
            return -1;
        }

        if (raceManager.HasRouteGraph)
        {
            if (targetRouteIndex <= 0 || targetRouteIndex >= checkpoints.Count)
            {
                return -1;
            }

            int nextIndex = raceManager.GetNextRoutePointIndex(targetRouteIndex, difficulty, driverSeed + 0.73f);
            return nextIndex == 0 || nextIndex == targetRouteIndex ? -1 : nextIndex;
        }

        int orderedNextIndex = participant.NextCheckpointIndex + 1;
        if (orderedNextIndex >= checkpoints.Count)
        {
            orderedNextIndex = 0;
        }

        return orderedNextIndex == 0 ? -1 : orderedNextIndex;
    }

    private void DrawSensor(Vector3 origin, Vector3 direction, float length, bool hit)
    {
        if (!drawAwarenessDebug)
        {
            return;
        }

        Debug.DrawRay(origin, direction.normalized * length, hit ? Color.red : Color.green);
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

    private void ApplyDifficultyProfile(float speedOffset)
    {
        sensorCaution = 1f;
        cornerCaution = 1f;
        accelerationConfidence = 1f;
        avoidanceStrength = 1f;
        sensorSpeedFloor = 0.42f;

        switch (difficulty)
        {
            case RaceAIDifficulty.Easy:
                desiredSpeedKmh = desiredSpeedKmh * 0.88f + speedOffset * 0.45f;
                cautiousTurnSpeedKmh *= 0.88f;
                steeringResponse *= 0.82f;
                brakeStrength *= 1.12f;
                waypointLookAhead *= 1.15f;
                sensorCaution = 1.2f;
                cornerCaution = 1.22f;
                accelerationConfidence = 0.82f;
                avoidanceStrength = 1.25f;
                sensorSpeedFloor = 0.34f;
                break;
            case RaceAIDifficulty.Hard:
                desiredSpeedKmh = desiredSpeedKmh * 1.08f + speedOffset;
                cautiousTurnSpeedKmh *= 1.06f;
                steeringResponse *= 1.12f;
                brakeStrength *= 0.92f;
                waypointLookAhead *= 0.9f;
                sensorCaution = 0.86f;
                cornerCaution = 0.88f;
                accelerationConfidence = 1.08f;
                avoidanceStrength = 0.95f;
                sensorSpeedFloor = 0.5f;
                break;
            case RaceAIDifficulty.EMPRESS:
                desiredSpeedKmh = desiredSpeedKmh * 1.18f + speedOffset * 1.2f;
                cautiousTurnSpeedKmh *= 1.14f;
                steeringResponse *= 1.22f;
                brakeStrength *= 0.84f;
                waypointLookAhead *= 0.78f;
                sensorCaution = 0.72f;
                cornerCaution = 0.74f;
                accelerationConfidence = 1.16f;
                avoidanceStrength = 0.82f;
                sensorSpeedFloor = 0.58f;
                break;
            default:
                desiredSpeedKmh += speedOffset;
                break;
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
