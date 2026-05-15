using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum ArcadeRaceState
{
    Setup,
    Countdown,
    Racing,
    Finished
}

[DefaultExecutionOrder(-150)]
public sealed class ArcadeRaceManager : MonoBehaviour
{
    [Header("Scene References")]
    public PrometeoCarController playerCar;
    public RaceTrackDefinition trackDefinition;

    [Header("Race")]
    [Range(1, 5)]
    public int lapsToWin = 2;
    [Range(1, 5)]
    public int opponentCount = 3;
    [Range(5f, 25f)]
    public float checkpointRadius = 14f;
    [Range(1f, 8f)]
    public float countdownSeconds = 3f;

    [Header("AI")]
    public RaceAIDifficulty aiDifficulty = RaceAIDifficulty.Medium;
    public float baseOpponentSpeedKmh = 86f;
    public float opponentSpeedStep = 5f;

    [Header("Debug")]
    public bool createTrackVisuals = true;

    public ArcadeRaceState State { get; private set; }
    public bool RaceActive { get { return State == ArcadeRaceState.Racing; } }
    public float CountdownRemaining { get; private set; }
    public float RaceTime { get; private set; }
    public int LapsToWin { get { return lapsToWin; } }
    public IReadOnlyList<Vector3> Checkpoints { get { return checkpoints; } }
    public IReadOnlyList<RaceRouteConnection> RouteConnections { get { return routeConnections; } }
    public IReadOnlyList<ArcadeRaceParticipant> Participants { get { return participants; } }
    public ArcadeRaceParticipant PlayerParticipant { get; private set; }
    public RaceAIDifficulty AIDifficulty { get { return aiDifficulty; } }
    public float CheckpointRadius { get { return checkpointRadius; } }
    public bool HasRouteGraph { get { return routeConnections.Count > 0; } }

    private readonly List<Vector3> checkpoints = new List<Vector3>();
    private readonly List<RaceRouteConnection> routeConnections = new List<RaceRouteConnection>();
    private readonly List<ArcadeRaceParticipant> participants = new List<ArcadeRaceParticipant>();
    private readonly List<ArcadeRaceParticipant> finishOrder = new List<ArcadeRaceParticipant>();
    private readonly string[] opponentNames = { "Nova", "Blaze", "Viper", "Comet", "Rogue" };
    private Rigidbody playerRigidbody;
    private Material checkpointMaterial;
    private Material routeMaterial;
    private StartFinishTrigger startFinishTrigger;
    private Vector3 startForward;
    private Vector3 startRight;
    private Vector3 playerSpawnPosition;
    private bool initialized;

    private void Start()
    {
        if (playerCar == null)
        {
            playerCar = FindFirstObjectByType<PrometeoCarController>();
        }

        if (trackDefinition == null)
        {
            trackDefinition = FindFirstObjectByType<RaceTrackDefinition>();
        }

        if (playerCar == null)
        {
            enabled = false;
            return;
        }

        InitializeRace();
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        if (State == ArcadeRaceState.Countdown)
        {
            CountdownRemaining -= Time.deltaTime;
            HoldPlayerAtStart();

            if (CountdownRemaining <= 0f)
            {
                State = ArcadeRaceState.Racing;
                RaceTime = 0f;
            }
        }
        else if (State == ArcadeRaceState.Racing)
        {
            RaceTime += Time.deltaTime;
            TickParticipants();
        }
        else if (State == ArcadeRaceState.Finished)
        {
            RaceTime += Time.deltaTime;
            TickParticipants();
        }
    }

    public void NotifyParticipantFinished(ArcadeRaceParticipant participant)
    {
        if (!finishOrder.Contains(participant))
        {
            finishOrder.Add(participant);
        }

        if (participant == PlayerParticipant)
        {
            State = ArcadeRaceState.Finished;
        }
    }

    public void HandleStartFinishCrossing(ArcadeRaceParticipant participant)
    {
        if (participant == null || !participants.Contains(participant))
        {
            return;
        }

        if (State != ArcadeRaceState.Racing && State != ArcadeRaceState.Finished)
        {
            return;
        }

        participant.TickProgress(checkpoints, checkpointRadius);
        participant.TryCompleteLap(lapsToWin, RaceTime);
    }

    public int GetRank(ArcadeRaceParticipant participant)
    {
        List<ArcadeRaceParticipant> standings = GetStandings();
        for (int i = 0; i < standings.Count; i++)
        {
            if (standings[i] == participant)
            {
                return i + 1;
            }
        }

        return standings.Count;
    }

    public List<ArcadeRaceParticipant> GetStandings()
    {
        List<ArcadeRaceParticipant> standings = new List<ArcadeRaceParticipant>(participants);
        standings.Sort(CompareParticipants);
        return standings;
    }

    public int GetNextProgressCheckpointIndex(int currentIndex)
    {
        if (checkpoints.Count < 2)
        {
            return 0;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, checkpoints.Count - 1);
        if (currentIndex >= checkpoints.Count - 1)
        {
            return 0;
        }

        int sequentialIndex = currentIndex + 1;
        if (!HasRouteGraph || HasForwardRouteConnection(currentIndex, sequentialIndex))
        {
            return sequentialIndex;
        }

        int bestIndex = sequentialIndex;
        int bestAdvance = int.MaxValue;
        for (int i = 0; i < routeConnections.Count; i++)
        {
            RaceRouteConnection connection = routeConnections[i];
            if (!IsForwardRouteEdge(connection, currentIndex))
            {
                continue;
            }

            int advance = GetProgressAdvance(currentIndex, connection.toIndex);
            if (advance > 0 && advance < bestAdvance)
            {
                bestAdvance = advance;
                bestIndex = connection.toIndex;
            }
        }

        return bestIndex;
    }

    public bool TryGetProgressCheckpointHit(Vector3 position, int currentIndex, float radius, out int hitIndex)
    {
        hitIndex = -1;

        if (checkpoints.Count < 2)
        {
            return false;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, checkpoints.Count - 1);
        if (currentIndex >= checkpoints.Count - 1)
        {
            return false;
        }

        float radiusSqr = radius * radius;
        int sequentialIndex = currentIndex + 1;
        int bestAdvance = -1;
        float bestDistanceSqr = float.MaxValue;

        EvaluateProgressHit(position, currentIndex, sequentialIndex, radiusSqr, ref hitIndex, ref bestAdvance, ref bestDistanceSqr);

        for (int i = 0; i < routeConnections.Count; i++)
        {
            RaceRouteConnection connection = routeConnections[i];
            if (!IsForwardRouteEdge(connection, currentIndex))
            {
                continue;
            }

            EvaluateProgressHit(position, currentIndex, connection.toIndex, radiusSqr, ref hitIndex, ref bestAdvance, ref bestDistanceSqr);
        }

        return hitIndex >= 0;
    }

    public int GetNextRoutePointIndex(int currentIndex, RaceAIDifficulty difficulty, float driverSeed)
    {
        if (checkpoints.Count < 2)
        {
            return 0;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, checkpoints.Count - 1);
        if (currentIndex >= checkpoints.Count - 1)
        {
            return 0;
        }

        if (!HasRouteGraph)
        {
            return currentIndex + 1;
        }

        int selectedIndex = currentIndex + 1;
        float bestScore = float.NegativeInfinity;
        bool foundCandidate = false;

        for (int i = 0; i < routeConnections.Count; i++)
        {
            RaceRouteConnection connection = routeConnections[i];
            if (!IsForwardRouteEdge(connection, currentIndex))
            {
                continue;
            }

            float score = GetRouteChoiceScore(connection, difficulty, driverSeed);
            if (score > bestScore)
            {
                foundCandidate = true;
                bestScore = score;
                selectedIndex = connection.toIndex;
            }
        }

        return foundCandidate ? selectedIndex : currentIndex + 1;
    }

    private void InitializeRace()
    {
        initialized = true;
        State = ArcadeRaceState.Setup;
        playerRigidbody = playerCar.GetComponent<Rigidbody>();

        BuildTrack();

        if (createTrackVisuals)
        {
            CreateTrackVisuals();
        }

        CreateStartFinishTrigger();
        PositionPlayerAtStart();
        RegisterPlayer();
        SpawnOpponents();
        CreateHud();

        CountdownRemaining = countdownSeconds;
        State = ArcadeRaceState.Countdown;
    }

    private void BuildTrack()
    {
        routeConnections.Clear();

        if (TryBuildFromTrackDefinition())
        {
            return;
        }

        if (TryBuildKnownSceneTrack())
        {
            return;
        }

        BuildDefaultTrack();
    }

    private bool TryBuildFromTrackDefinition()
    {
        if (trackDefinition == null || !trackDefinition.TryGetCheckpointPositions(checkpoints))
        {
            return false;
        }

        checkpointRadius = trackDefinition.checkpointRadius;
        trackDefinition.TryGetRouteConnections(routeConnections);
        playerSpawnPosition = trackDefinition.GetStartPosition(checkpoints[0]);
        startForward = trackDefinition.GetStartForward(GetDirectionToNextCheckpoint());
        startRight = Vector3.Cross(Vector3.up, startForward).normalized;
        return true;
    }

    private bool TryBuildKnownSceneTrack()
    {
        if (SceneManager.GetActiveScene().name != "complete_track_demo")
        {
            return false;
        }

        BuildCartoonOvalTrack();
        return true;
    }

    private void BuildCartoonOvalTrack()
    {
        checkpoints.Clear();

        startForward = Vector3.right;
        startRight = Vector3.Cross(Vector3.up, startForward).normalized;
        checkpointRadius = 16f;

        checkpoints.Add(new Vector3(-54f, 0f, -28f));
        checkpoints.Add(new Vector3(4f, 0f, -28f));
        checkpoints.Add(new Vector3(58f, 0f, -25f));
        checkpoints.Add(new Vector3(83f, 0f, 0f));
        checkpoints.Add(new Vector3(58f, 0f, 25f));
        checkpoints.Add(new Vector3(0f, 0f, 31f));
        checkpoints.Add(new Vector3(-58f, 0f, 25f));
        checkpoints.Add(new Vector3(-83f, 0f, 0f));
        checkpoints.Add(new Vector3(-58f, 0f, -25f));

        playerSpawnPosition = checkpoints[0] - (startForward * 6f) - (startRight * 3f);
    }

    private void BuildDefaultTrack()
    {
        checkpoints.Clear();

        Vector3 origin = playerCar.transform.position;
        startForward = Vector3.ProjectOnPlane(playerCar.transform.forward, Vector3.up).normalized;
        if (startForward.sqrMagnitude < 0.01f)
        {
            startForward = Vector3.forward;
        }

        startRight = Vector3.Cross(Vector3.up, startForward).normalized;
        playerSpawnPosition = origin - (startForward * 6f) - (startRight * 3f);

        checkpoints.Add(origin);
        checkpoints.Add(origin + (startForward * 72f));
        checkpoints.Add(origin + (startForward * 88f) + (startRight * 38f));
        checkpoints.Add(origin + (startForward * 14f) + (startRight * 48f));
        checkpoints.Add(origin - (startForward * 58f) + (startRight * 34f));
        checkpoints.Add(origin - (startForward * 76f) - (startRight * 34f));
        checkpoints.Add(origin - (startForward * 4f) - (startRight * 48f));
        checkpoints.Add(origin + (startForward * 82f) - (startRight * 36f));
    }

    private void PositionPlayerAtStart()
    {
        Transform playerTransform = playerCar.transform;
        playerTransform.position = playerSpawnPosition;
        playerTransform.rotation = Quaternion.LookRotation(startForward, Vector3.up);

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }
    }

    private void RegisterPlayer()
    {
        PlayerParticipant = playerCar.GetComponent<ArcadeRaceParticipant>();
        if (PlayerParticipant == null)
        {
            PlayerParticipant = playerCar.gameObject.AddComponent<ArcadeRaceParticipant>();
        }

        PlayerParticipant.Initialize(this, "You", true);
        participants.Add(PlayerParticipant);
    }

    private void SpawnOpponents()
    {
        for (int i = 0; i < opponentCount; i++)
        {
            Vector3 spawnPosition = GetGridPosition(i + 1);
            GameObject opponentObject = Instantiate(playerCar.gameObject, spawnPosition, Quaternion.LookRotation(startForward, Vector3.up));
            opponentObject.name = opponentNames[i % opponentNames.Length];

            PrometeoCarController opponentCar = opponentObject.GetComponent<PrometeoCarController>();
            if (opponentCar != null)
            {
                opponentCar.useUI = false;
                opponentCar.useSounds = false;
                opponentCar.useTouchControls = false;
                opponentCar.enabled = false;
            }

            DisableOpponentAudio(opponentObject);
            TintOpponent(opponentObject, i);

            ArcadeRaceParticipant participant = opponentObject.GetComponent<ArcadeRaceParticipant>();
            if (participant == null)
            {
                participant = opponentObject.AddComponent<ArcadeRaceParticipant>();
            }

            participant.Initialize(this, opponentObject.name, false);
            participants.Add(participant);

            ArcadeAIOpponent ai = opponentObject.AddComponent<ArcadeAIOpponent>();
            ai.desiredSpeedKmh = baseOpponentSpeedKmh;
            ai.Configure(this, opponentCar, participant, i * opponentSpeedStep);
        }
    }

    private Vector3 GetGridPosition(int index)
    {
        int lane = index % 2 == 0 ? -1 : 1;
        int row = (index + 1) / 2;
        return checkpoints[0] - (startForward * (8f + row * 7f)) + (startRight * lane * 5f);
    }

    private Vector3 GetDirectionToNextCheckpoint()
    {
        if (checkpoints.Count < 2)
        {
            return playerCar != null ? playerCar.transform.forward : Vector3.forward;
        }

        Vector3 direction = Vector3.ProjectOnPlane(checkpoints[1] - checkpoints[0], Vector3.up);
        return direction.sqrMagnitude > 0.01f ? direction.normalized : Vector3.forward;
    }

    private void EvaluateProgressHit(Vector3 position, int currentIndex, int candidateIndex, float radiusSqr, ref int hitIndex, ref int bestAdvance, ref float bestDistanceSqr)
    {
        if (candidateIndex <= currentIndex || candidateIndex >= checkpoints.Count)
        {
            return;
        }

        float distanceSqr = (position - checkpoints[candidateIndex]).sqrMagnitude;
        if (distanceSqr > radiusSqr)
        {
            return;
        }

        int advance = GetProgressAdvance(currentIndex, candidateIndex);
        if (advance > bestAdvance || (advance == bestAdvance && distanceSqr < bestDistanceSqr))
        {
            bestAdvance = advance;
            bestDistanceSqr = distanceSqr;
            hitIndex = candidateIndex;
        }
    }

    private bool IsForwardRouteEdge(RaceRouteConnection connection, int currentIndex)
    {
        if (connection == null || !connection.IsValid(checkpoints.Count) || connection.fromIndex != currentIndex)
        {
            return false;
        }

        if (connection.toIndex == 0)
        {
            return currentIndex >= checkpoints.Count - 1;
        }

        return connection.toIndex > currentIndex;
    }

    private bool HasForwardRouteConnection(int fromIndex, int toIndex)
    {
        for (int i = 0; i < routeConnections.Count; i++)
        {
            RaceRouteConnection connection = routeConnections[i];
            if (connection != null && connection.fromIndex == fromIndex && connection.toIndex == toIndex)
            {
                return true;
            }
        }

        return false;
    }

    private int GetProgressAdvance(int fromIndex, int toIndex)
    {
        if (toIndex > fromIndex)
        {
            return toIndex - fromIndex;
        }

        if (toIndex == 0 && fromIndex >= checkpoints.Count - 1)
        {
            return 1;
        }

        return -1;
    }

    private float GetRouteChoiceScore(RaceRouteConnection connection, RaceAIDifficulty difficulty, float driverSeed)
    {
        int advance = Mathf.Max(1, GetProgressAdvance(connection.fromIndex, connection.toIndex));
        float preference = GetDifficultyPreference(connection.kind, difficulty);
        float shortcutPressure = 0f;

        switch (difficulty)
        {
            case RaceAIDifficulty.Easy:
                shortcutPressure = -0.45f * Mathf.Max(0, advance - 1);
                break;
            case RaceAIDifficulty.Medium:
                shortcutPressure = 0.08f * Mathf.Max(0, advance - 1);
                break;
            case RaceAIDifficulty.Hard:
                shortcutPressure = 0.24f * Mathf.Max(0, advance - 1);
                break;
            case RaceAIDifficulty.EMPRESS:
                shortcutPressure = 0.42f * Mathf.Max(0, advance - 1);
                break;
        }

        float distancePenalty = GetFlatDistance(connection.fromIndex, connection.toIndex) * 0.004f;
        float deterministicNoise = Mathf.Sin((driverSeed + 1f) * 12.9898f + connection.toIndex * 78.233f) * 0.08f;
        return preference + shortcutPressure - distancePenalty - connection.weight * 0.15f + deterministicNoise;
    }

    private static float GetDifficultyPreference(RaceRouteConnectionKind kind, RaceAIDifficulty difficulty)
    {
        switch (difficulty)
        {
            case RaceAIDifficulty.Easy:
                switch (kind)
                {
                    case RaceRouteConnectionKind.Safe:
                        return 4.2f;
                    case RaceRouteConnectionKind.Fast:
                        return 1.8f;
                    case RaceRouteConnectionKind.Shortcut:
                        return 0.5f;
                    default:
                        return 3.2f;
                }
            case RaceAIDifficulty.Hard:
                switch (kind)
                {
                    case RaceRouteConnectionKind.Safe:
                        return 1.7f;
                    case RaceRouteConnectionKind.Fast:
                        return 3.4f;
                    case RaceRouteConnectionKind.Shortcut:
                        return 3.8f;
                    default:
                        return 2.4f;
                }
            case RaceAIDifficulty.EMPRESS:
                switch (kind)
                {
                    case RaceRouteConnectionKind.Safe:
                        return 1.2f;
                    case RaceRouteConnectionKind.Fast:
                        return 3.9f;
                    case RaceRouteConnectionKind.Shortcut:
                        return 4.8f;
                    default:
                        return 2.1f;
                }
            default:
                switch (kind)
                {
                    case RaceRouteConnectionKind.Safe:
                        return 3f;
                    case RaceRouteConnectionKind.Fast:
                        return 2.7f;
                    case RaceRouteConnectionKind.Shortcut:
                        return 2f;
                    default:
                        return 3f;
                }
        }
    }

    private float GetFlatDistance(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= checkpoints.Count || toIndex < 0 || toIndex >= checkpoints.Count)
        {
            return 0f;
        }

        Vector3 from = checkpoints[fromIndex];
        Vector3 to = checkpoints[toIndex];
        from.y = 0f;
        to.y = 0f;
        return Vector3.Distance(from, to);
    }

    private void TickParticipants()
    {
        for (int i = 0; i < participants.Count; i++)
        {
            participants[i].TickProgress(checkpoints, checkpointRadius);
        }
    }

    private void HoldPlayerAtStart()
    {
        if (playerRigidbody == null)
        {
            return;
        }

        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;
    }

    private void CreateHud()
    {
        GameObject hudObject = new GameObject("Arcade Race HUD");
        ArcadeRaceHud hud = hudObject.AddComponent<ArcadeRaceHud>();
        hud.Initialize(this);
    }

    private void CreateStartFinishTrigger()
    {
        if (checkpoints.Count == 0)
        {
            return;
        }

        Vector3 triggerPosition = trackDefinition != null
            ? trackDefinition.GetStartFinishPosition(checkpoints[0])
            : checkpoints[0];
        Quaternion triggerRotation = trackDefinition != null
            ? trackDefinition.GetStartFinishRotation(startForward)
            : Quaternion.LookRotation(startForward, Vector3.up);

        GameObject triggerObject = new GameObject("Start Finish Trigger");
        triggerObject.transform.SetParent(transform, false);
        triggerObject.transform.SetPositionAndRotation(triggerPosition, triggerRotation);

        BoxCollider boxCollider = triggerObject.AddComponent<BoxCollider>();
        boxCollider.isTrigger = true;
        boxCollider.size = trackDefinition != null ? trackDefinition.startFinishTriggerSize : new Vector3(14f, 5f, 4f);
        boxCollider.center = trackDefinition != null ? trackDefinition.startFinishTriggerCenter : new Vector3(0f, 2.5f, 0f);

        startFinishTrigger = triggerObject.AddComponent<StartFinishTrigger>();
        startFinishTrigger.raceManager = this;
    }

    private void CreateTrackVisuals()
    {
        checkpointMaterial = CreateMaterial("Arcade Checkpoint Material", new Color(0f, 0.85f, 1f, 0.75f));
        routeMaterial = CreateMaterial("Arcade Route Material", new Color(1f, 0.82f, 0.15f, 1f));

        GameObject visualsRoot = new GameObject("Arcade Race Track");
        visualsRoot.transform.SetParent(transform, false);

        for (int i = 0; i < checkpoints.Count; i++)
        {
            CreateCheckpointMarker(visualsRoot.transform, checkpoints[i], i);
        }

        if (HasRouteGraph)
        {
            for (int i = 0; i < routeConnections.Count; i++)
            {
                RaceRouteConnection connection = routeConnections[i];
                if (connection == null || !connection.IsValid(checkpoints.Count))
                {
                    continue;
                }

                CreateRouteLine(
                    visualsRoot.transform,
                    "Route " + connection.fromIndex + " -> " + connection.toIndex,
                    checkpoints[connection.fromIndex],
                    checkpoints[connection.toIndex],
                    GetConnectionColor(connection.kind),
                    connection.kind == RaceRouteConnectionKind.Normal ? 0.22f : 0.36f);
            }
        }
        else
        {
            LineRenderer lineRenderer = visualsRoot.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = false;
            lineRenderer.widthMultiplier = 0.35f;
            lineRenderer.positionCount = checkpoints.Count;
            lineRenderer.material = routeMaterial;
            lineRenderer.startColor = routeMaterial.color;
            lineRenderer.endColor = routeMaterial.color;

            for (int i = 0; i < checkpoints.Count; i++)
            {
                lineRenderer.SetPosition(i, checkpoints[i] + Vector3.up * 0.15f);
            }
        }
    }

    private void CreateRouteLine(Transform parent, string lineName, Vector3 from, Vector3 to, Color color, float width)
    {
        GameObject lineObject = new GameObject(lineName);
        lineObject.transform.SetParent(parent, false);

        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false;
        lineRenderer.widthMultiplier = width;
        lineRenderer.positionCount = 2;
        lineRenderer.material = routeMaterial;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.SetPosition(0, from + Vector3.up * 0.18f);
        lineRenderer.SetPosition(1, to + Vector3.up * 0.18f);
    }

    private void CreateCheckpointMarker(Transform parent, Vector3 position, int index)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = index == 0 ? "Start Gate" : "Checkpoint " + index;
        marker.transform.SetParent(parent, false);
        marker.transform.position = position + Vector3.up * 0.08f;
        marker.transform.localScale = new Vector3(checkpointRadius * 0.12f, 0.04f, checkpointRadius * 0.12f);

        Collider markerCollider = marker.GetComponent<Collider>();
        if (markerCollider != null)
        {
            markerCollider.enabled = false;
        }

        Renderer renderer = marker.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = checkpointMaterial;
        }

        GameObject labelObject = new GameObject("Checkpoint Label");
        labelObject.transform.SetParent(marker.transform, false);
        labelObject.transform.localPosition = Vector3.up * 3f;
        labelObject.transform.localRotation = Quaternion.identity;

        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.text = index == 0 ? "START" : index.ToString();
        label.characterSize = 1.8f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = Color.white;
    }

    private static Material CreateMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.name = name;
        material.color = color;
        return material;
    }

    private static Color GetConnectionColor(RaceRouteConnectionKind kind)
    {
        switch (kind)
        {
            case RaceRouteConnectionKind.Safe:
                return new Color(0.25f, 0.95f, 0.45f, 0.9f);
            case RaceRouteConnectionKind.Fast:
                return new Color(0.15f, 0.65f, 1f, 0.9f);
            case RaceRouteConnectionKind.Shortcut:
                return new Color(1f, 0.82f, 0.05f, 1f);
            default:
                return routeMaterialColor;
        }
    }

    private static readonly Color routeMaterialColor = new Color(1f, 1f, 1f, 0.5f);

    private static void DisableOpponentAudio(GameObject opponentObject)
    {
        AudioSource[] audioSources = opponentObject.GetComponentsInChildren<AudioSource>(true);
        for (int i = 0; i < audioSources.Length; i++)
        {
            audioSources[i].Stop();
            audioSources[i].enabled = false;
        }
    }

    private static void TintOpponent(GameObject opponentObject, int index)
    {
        Color[] colors =
        {
            new Color(0.95f, 0.15f, 0.12f),
            new Color(0.12f, 0.45f, 1f),
            new Color(0.1f, 0.85f, 0.38f),
            new Color(1f, 0.55f, 0.05f)
        };

        Renderer[] renderers = opponentObject.GetComponentsInChildren<Renderer>(true);
        Color color = colors[index % colors.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].sharedMaterial == null)
            {
                continue;
            }

            Material material = new Material(renderers[i].sharedMaterial);
            material.color = color;
            renderers[i].sharedMaterial = material;
        }
    }

    private static int CompareParticipants(ArcadeRaceParticipant left, ArcadeRaceParticipant right)
    {
        if (left.HasFinished && right.HasFinished)
        {
            return left.FinishTime.CompareTo(right.FinishTime);
        }

        if (left.HasFinished)
        {
            return -1;
        }

        if (right.HasFinished)
        {
            return 1;
        }

        return right.ProgressScore.CompareTo(left.ProgressScore);
    }
}
