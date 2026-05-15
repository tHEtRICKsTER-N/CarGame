using UnityEngine;

[DefaultExecutionOrder(-200)]
public sealed class ArcadeRaceBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRaceManager()
    {
        if (FindFirstObjectByType<ArcadeRaceManager>() != null)
        {
            return;
        }

        PrometeoCarController playerCar = FindFirstObjectByType<PrometeoCarController>();
        RaceTrackDefinition trackDefinition = FindFirstObjectByType<RaceTrackDefinition>();
        if (playerCar == null && trackDefinition == null)
        {
            return;
        }

        GameObject managerObject = new GameObject("Arcade Race Manager");
        ArcadeRaceManager manager = managerObject.AddComponent<ArcadeRaceManager>();
        manager.playerCar = playerCar;
        manager.trackDefinition = trackDefinition;
    }
}
