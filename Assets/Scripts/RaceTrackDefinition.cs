using System.Collections.Generic;
using UnityEngine;

public sealed class RaceTrackDefinition : MonoBehaviour
{
    [Header("Map")]
    public string mapId = "map";
    public string displayName = "Race Track";

    [Header("Start")]
    public Transform playerSpawn;
    public Transform forwardReference;

    [Header("Start / Finish")]
    public Transform startFinishLine;
    public Vector3 startFinishTriggerSize = new Vector3(14f, 5f, 4f);
    public Vector3 startFinishTriggerCenter = new Vector3(0f, 2.5f, 0f);

    [Header("Path")]
    [Range(5f, 35f)]
    public float checkpointRadius = 16f;
    [Tooltip("Editor-only helper. Keep this off when placing branched graph points because it draws inferred order links.")]
    public bool drawOrderedPreviewLine = false;
    public Transform[] checkpoints;

    public int CheckpointCount
    {
        get { return checkpoints != null ? checkpoints.Length : 0; }
    }

    public bool TryGetCheckpointPositions(List<Vector3> positions)
    {
        positions.Clear();

        if (checkpoints == null)
        {
            return false;
        }

        for (int i = 0; i < checkpoints.Length; i++)
        {
            if (checkpoints[i] != null)
            {
                positions.Add(checkpoints[i].position);
            }
        }

        return positions.Count >= 2;
    }

    public Vector3 GetStartPosition(Vector3 fallback)
    {
        return playerSpawn != null ? playerSpawn.position : fallback;
    }

    public Vector3 GetStartForward(Vector3 fallback)
    {
        Transform source = forwardReference != null ? forwardReference : startFinishLine != null ? startFinishLine : playerSpawn;
        if (source == null)
        {
            return fallback;
        }

        Vector3 forward = Vector3.ProjectOnPlane(source.forward, Vector3.up);
        return forward.sqrMagnitude > 0.01f ? forward.normalized : fallback;
    }

    public Vector3 GetStartFinishPosition(Vector3 fallback)
    {
        if (startFinishLine != null)
        {
            return startFinishLine.position;
        }

        if (playerSpawn != null)
        {
            return playerSpawn.position;
        }

        return fallback;
    }

    public Quaternion GetStartFinishRotation(Vector3 fallbackForward)
    {
        Transform source = startFinishLine != null ? startFinishLine : forwardReference != null ? forwardReference : playerSpawn;
        if (source != null)
        {
            return source.rotation;
        }

        Vector3 forward = Vector3.ProjectOnPlane(fallbackForward, Vector3.up);
        if (forward.sqrMagnitude < 0.01f)
        {
            forward = Vector3.forward;
        }

        return Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    public void SetCheckpoints(IList<Transform> orderedCheckpoints)
    {
        if (orderedCheckpoints == null)
        {
            checkpoints = new Transform[0];
            return;
        }

        checkpoints = new Transform[orderedCheckpoints.Count];
        for (int i = 0; i < orderedCheckpoints.Count; i++)
        {
            checkpoints[i] = orderedCheckpoints[i];
        }
    }

    public void RefreshCheckpointsFromChildren()
    {
        List<Transform> foundCheckpoints = new List<Transform>();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("Checkpoint_"))
            {
                foundCheckpoints.Add(child);
            }
        }

        foundCheckpoints.Sort(CompareCheckpointNames);
        SetCheckpoints(foundCheckpoints);
    }

    public void RemoveEmptyCheckpointSlots()
    {
        if (checkpoints == null)
        {
            return;
        }

        List<Transform> compactedCheckpoints = new List<Transform>();
        for (int i = 0; i < checkpoints.Length; i++)
        {
            if (checkpoints[i] != null)
            {
                compactedCheckpoints.Add(checkpoints[i]);
            }
        }

        SetCheckpoints(compactedCheckpoints);
    }

    private static int CompareCheckpointNames(Transform left, Transform right)
    {
        return ExtractCheckpointNumber(left.name).CompareTo(ExtractCheckpointNumber(right.name));
    }

    private static int ExtractCheckpointNumber(string checkpointName)
    {
        int underscoreIndex = checkpointName.LastIndexOf('_');
        if (underscoreIndex < 0 || underscoreIndex >= checkpointName.Length - 1)
        {
            return int.MaxValue;
        }

        int number;
        return int.TryParse(checkpointName.Substring(underscoreIndex + 1), out number) ? number : int.MaxValue;
    }
}
