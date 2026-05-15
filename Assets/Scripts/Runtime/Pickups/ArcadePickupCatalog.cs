using UnityEngine;

[CreateAssetMenu(fileName = "ArcadePickupCatalog", menuName = "Car Game/Arcade Pickup Catalog")]
public sealed class ArcadePickupCatalog : ScriptableObject
{
    public Sprite boostSprite;
    public Sprite timerSprite;
    public GameObject defaultCoinPrefab;
    public GameObject screenHyperdriveVfxPrefab;
    public GameObject carNitroVfxPrefab;

    public static ArcadePickupCatalog LoadDefault(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        return Resources.Load<ArcadePickupCatalog>(resourcePath);
    }
}
