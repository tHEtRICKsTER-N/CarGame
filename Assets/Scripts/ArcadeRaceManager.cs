using System.Collections.Generic;
using UnityEngine;

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
    public float baseOpponentSpeedKmh = 86f;
    public float opponentSpeedStep = 5f;

    public ArcadeRaceState State { get; private set; }
    public bool RaceActive { get { return State == ArcadeRaceState.Racing; } }
    public float CountdownRemaining { get; private set; }
    public float RaceTime { get; private set; }
    public int LapsToWin { get { return lapsToWin; } }
    public IReadOnlyList<Vector3> Checkpoints { get { return checkpoints; } }
    public IReadOnlyList<ArcadeRaceParticipant> Participants { get { return participants; } }
    public ArcadeRaceParticipant PlayerParticipant { get; private set; }

    private readonly List<Vector3> checkpoints = new List<Vector3>();
    private readonly List<ArcadeRaceParticipant> participants = new List<ArcadeRaceParticipant>();
    private readonly List<ArcadeRaceParticipant> finishOrder = new List<ArcadeRaceParticipant>();
    private readonly string[] opponentNames = { "Nova", "Blaze", "Viper", "Comet", "Rogue" };
    private Rigidbody playerRigidbody;
    private Material checkpointMaterial;
    private Material routeMaterial;
    private Vector3 startForward;
    private Vector3 startRight;
    private bool initialized;

    private void Start()
    {
        if (playerCar == null)
        {
            playerCar = FindFirstObjectByType<PrometeoCarController>();
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

    private void InitializeRace()
    {
        initialized = true;
        State = ArcadeRaceState.Setup;
        playerRigidbody = playerCar.GetComponent<Rigidbody>();

        BuildDefaultTrack();
        CreateTrackVisuals();
        PositionPlayerAtStart();
        RegisterPlayer();
        SpawnOpponents();
        CreateHud();

        CountdownRemaining = countdownSeconds;
        State = ArcadeRaceState.Countdown;
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
        playerTransform.position = checkpoints[0] - (startForward * 6f) - (startRight * 3f);
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

    private void TickParticipants()
    {
        for (int i = 0; i < participants.Count; i++)
        {
            participants[i].TickProgress(checkpoints, checkpointRadius, lapsToWin, RaceTime);
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

    private void CreateTrackVisuals()
    {
        checkpointMaterial = CreateMaterial("Arcade Checkpoint Material", new Color(0f, 0.85f, 1f, 0.75f));
        routeMaterial = CreateMaterial("Arcade Route Material", new Color(1f, 0.82f, 0.15f, 1f));

        GameObject visualsRoot = new GameObject("Arcade Race Track");
        visualsRoot.transform.SetParent(transform, false);

        LineRenderer lineRenderer = visualsRoot.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true;
        lineRenderer.widthMultiplier = 0.35f;
        lineRenderer.positionCount = checkpoints.Count;
        lineRenderer.material = routeMaterial;
        lineRenderer.startColor = routeMaterial.color;
        lineRenderer.endColor = routeMaterial.color;

        for (int i = 0; i < checkpoints.Count; i++)
        {
            lineRenderer.SetPosition(i, checkpoints[i] + Vector3.up * 0.15f);
            CreateCheckpointMarker(visualsRoot.transform, checkpoints[i], i);
        }
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
