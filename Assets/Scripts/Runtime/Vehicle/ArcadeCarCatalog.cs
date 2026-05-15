using UnityEngine;

[CreateAssetMenu(fileName = "ArcadeCarCatalog", menuName = "Car Game/Arcade Car Catalog")]
public sealed class ArcadeCarCatalog : ScriptableObject
{
    public ArcadeCarOption[] cars = new ArcadeCarOption[0];

    public int Count { get { return cars != null ? cars.Length : 0; } }

    public ArcadeCarOption GetCar(int index)
    {
        if (cars == null || cars.Length == 0)
        {
            return null;
        }

        return cars[Mathf.Clamp(index, 0, cars.Length - 1)];
    }

    public string GetDisplayName(int index)
    {
        ArcadeCarOption car = GetCar(index);
        if (car == null || string.IsNullOrWhiteSpace(car.displayName))
        {
            return "Car " + (Mathf.Max(0, index) + 1);
        }

        return car.displayName;
    }
}

[System.Serializable]
public sealed class ArcadeCarOption
{
    public string displayName = "Car";
    public GameObject prefab;
    public Color uiColor = Color.white;
    public float visualScale = 1f;
    public Vector3 visualOffset;
    public Vector3 visualEulerAngles;
    public int maxSpeed = 95;
    [Range(1, 10)]
    public int accelerationMultiplier = 5;
    [Range(15, 45)]
    public int maxSteeringAngle = 32;
    public float mass = 1150f;
    public Vector3 centerOfMass = new Vector3(0f, -0.45f, 0.05f);
}
