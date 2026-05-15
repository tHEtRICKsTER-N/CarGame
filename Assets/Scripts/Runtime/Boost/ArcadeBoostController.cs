using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class ArcadeBoostController : MonoBehaviour
{
    [Header("Meter")]
    public float maxBoost = 100f;
    public float startingBoost = 35f;
    public float boostDrainPerSecond = 34f;

    [Header("Input")]
    public KeyCode boostKey = KeyCode.LeftShift;
    public KeyCode alternateBoostKey = KeyCode.E;

    [Header("Driving")]
    public float boostAcceleration = 11f;
    public float boostedMaxSpeedMultiplier = 1.35f;
    public int boostedAccelerationBonus = 2;
    public float maxBoostSpeedKmh = 150f;

    public float CurrentBoost { get; private set; }
    public bool IsBoosting { get; private set; }
    public float NormalizedBoost { get { return maxBoost > 0.01f ? Mathf.Clamp01(CurrentBoost / maxBoost) : 0f; } }
    public bool IsFull { get { return CurrentBoost >= maxBoost - 0.01f; } }

    private ArcadeRaceManager raceManager;
    private PrometeoCarController car;
    private Rigidbody carRigidbody;
    private ArcadeBoostVfx boostVfx;
    private int baseMaxSpeed;
    private int baseAccelerationMultiplier;
    private bool baseValuesCaptured;

    public void Initialize(ArcadeRaceManager manager, PrometeoCarController playerCar)
    {
        raceManager = manager;
        car = playerCar != null ? playerCar : GetComponent<PrometeoCarController>();
        carRigidbody = GetComponent<Rigidbody>();
        CaptureBaseValues();
        CurrentBoost = Mathf.Clamp(CurrentBoost > 0f ? CurrentBoost : startingBoost, 0f, maxBoost);
        EnsureBoostVfx();
    }

    public float AddBoost(float amount)
    {
        if (amount <= 0f || IsFull)
        {
            return 0f;
        }

        float before = CurrentBoost;
        CurrentBoost = Mathf.Clamp(CurrentBoost + amount, 0f, maxBoost);
        return CurrentBoost - before;
    }

    private void Awake()
    {
        carRigidbody = GetComponent<Rigidbody>();
        car = GetComponent<PrometeoCarController>();
        CaptureBaseValues();
        CurrentBoost = Mathf.Clamp(startingBoost, 0f, maxBoost);
        EnsureBoostVfx();
    }

    private void Update()
    {
        bool wantsBoost = Input.GetKey(boostKey) || Input.GetKey(alternateBoostKey);
        bool canBoost = raceManager == null || raceManager.RaceActive;
        IsBoosting = canBoost && wantsBoost && CurrentBoost > 0.01f;

        if (IsBoosting)
        {
            CurrentBoost = Mathf.Max(0f, CurrentBoost - boostDrainPerSecond * Time.deltaTime);
        }

        ApplyBoostedCarSettings(IsBoosting);
    }

    private void FixedUpdate()
    {
        if (!IsBoosting || carRigidbody == null)
        {
            return;
        }

        Vector3 horizontalVelocity = carRigidbody.linearVelocity;
        horizontalVelocity.y = 0f;
        float speedKmh = horizontalVelocity.magnitude * 3.6f;
        float forwardSpeed = Vector3.Dot(carRigidbody.linearVelocity, transform.forward);

        if (speedKmh < maxBoostSpeedKmh && forwardSpeed > -1f)
        {
            carRigidbody.AddForce(transform.forward * boostAcceleration, ForceMode.Acceleration);
        }
    }

    private void OnDisable()
    {
        ApplyBoostedCarSettings(false);
        IsBoosting = false;
    }

    private void OnDestroy()
    {
        if (boostVfx != null)
        {
            Destroy(boostVfx);
        }
    }

    private void CaptureBaseValues()
    {
        if (baseValuesCaptured || car == null)
        {
            return;
        }

        baseMaxSpeed = car.maxSpeed;
        baseAccelerationMultiplier = car.accelerationMultiplier;
        baseValuesCaptured = true;
    }

    private void ApplyBoostedCarSettings(bool boosted)
    {
        if (car == null)
        {
            return;
        }

        CaptureBaseValues();

        if (boosted)
        {
            car.maxSpeed = Mathf.RoundToInt(baseMaxSpeed * boostedMaxSpeedMultiplier);
            car.accelerationMultiplier = Mathf.Clamp(baseAccelerationMultiplier + boostedAccelerationBonus, 1, 10);
        }
        else
        {
            car.maxSpeed = baseMaxSpeed;
            car.accelerationMultiplier = baseAccelerationMultiplier;
        }
    }

    private void EnsureBoostVfx()
    {
        if (boostVfx == null)
        {
            boostVfx = GetComponent<ArcadeBoostVfx>();
        }

        if (boostVfx == null)
        {
            boostVfx = gameObject.AddComponent<ArcadeBoostVfx>();
        }

        boostVfx.Initialize(this);
    }
}
