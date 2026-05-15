using System.Collections.Generic;
using UnityEngine;

public enum RaceAIDifficulty
{
    Easy,
    Medium,
    Hard,
    EMPRESS
}

public enum RaceRouteConnectionKind
{
    Normal,
    Safe,
    Fast,
    Shortcut
}

[System.Serializable]
public sealed class RaceRouteConnection
{
    public int fromIndex;
    public int toIndex;
    public RaceRouteConnectionKind kind = RaceRouteConnectionKind.Normal;
    [Min(0.05f)]
    public float weight = 1f;

    public RaceRouteConnection()
    {
    }

    public RaceRouteConnection(int fromIndex, int toIndex, RaceRouteConnectionKind kind)
    {
        this.fromIndex = fromIndex;
        this.toIndex = toIndex;
        this.kind = kind;
        weight = 1f;
    }

    public RaceRouteConnection Clone()
    {
        return new RaceRouteConnection
        {
            fromIndex = fromIndex,
            toIndex = toIndex,
            kind = kind,
            weight = weight
        };
    }

    public bool IsValid(int checkpointCount)
    {
        return checkpointCount > 1
            && fromIndex >= 0
            && fromIndex < checkpointCount
            && toIndex >= 0
            && toIndex < checkpointCount
            && fromIndex != toIndex;
    }
}

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

    [Header("Auto Route Graph")]
    [Tooltip("Build the main route by connecting each checkpoint to the next checkpoint in order.")]
    public bool autoGenerateMainRouteConnections = true;
    [Tooltip("Only enable this if you want the editor/runtime preview to draw the final checkpoint back to the start. Lap completion still works through the start/finish trigger when this is off.")]
    public bool autoConnectFinalCheckpointToStart = false;
    [Tooltip("Infer forward shortcut/branch links from placed checkpoint positions.")]
    public bool autoBuildInferredRouteConnections = true;
    [Range(8f, 120f)]
    public float autoGraphMaxConnectionDistance = 48f;
    [Range(2, 20)]
    public int autoGraphMaxIndexSkip = 8;
    [Range(0f, 0.8f)]
    public float autoGraphMinimumShortcutSaving = 0.18f;
    public bool drawRouteConnections = true;

    [Header("Manual Route Overrides")]
    [Tooltip("Optional directed links for special branches. Most tracks should only need placed checkpoints and the auto graph.")]
    public RaceRouteConnection[] routeConnections;

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
            if (checkpoints[i] == null)
            {
                positions.Clear();
                return false;
            }

            positions.Add(checkpoints[i].position);
        }

        return positions.Count >= 2;
    }

    public bool TryGetRouteConnections(List<RaceRouteConnection> connections)
    {
        connections.Clear();

        if (checkpoints == null || checkpoints.Length < 2)
        {
            return false;
        }

        AppendAutoRouteConnections(connections);
        AppendInferredRouteConnections(connections);
        AppendManualRouteConnections(connections);
        return connections.Count > 0;
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

        PruneInvalidRouteConnections();
    }

    public void InsertCheckpoint(Transform checkpoint, int insertIndex)
    {
        List<Transform> ordered = new List<Transform>();
        if (checkpoints != null)
        {
            ordered.AddRange(checkpoints);
        }

        insertIndex = Mathf.Clamp(insertIndex, 0, ordered.Count);
        ordered.Insert(insertIndex, checkpoint);
        checkpoints = ordered.ToArray();
        ShiftRouteConnectionsForInsertedCheckpoint(insertIndex);
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
        int[] remap = new int[checkpoints.Length];
        for (int i = 0; i < checkpoints.Length; i++)
        {
            remap[i] = -1;
            if (checkpoints[i] != null)
            {
                remap[i] = compactedCheckpoints.Count;
                compactedCheckpoints.Add(checkpoints[i]);
            }
        }

        checkpoints = compactedCheckpoints.ToArray();
        RemapRouteConnections(remap);
    }

    public void SetRouteConnections(IList<RaceRouteConnection> connections)
    {
        if (connections == null)
        {
            routeConnections = new RaceRouteConnection[0];
            return;
        }

        List<RaceRouteConnection> cleanedConnections = new List<RaceRouteConnection>();
        for (int i = 0; i < connections.Count; i++)
        {
            RaceRouteConnection connection = connections[i];
            if (connection == null || !connection.IsValid(CheckpointCount))
            {
                continue;
            }

            AddConnection(cleanedConnections, connection.fromIndex, connection.toIndex, connection.kind, connection.weight);
        }

        routeConnections = cleanedConnections.ToArray();
    }

    public void AddRouteConnection(int fromIndex, int toIndex, RaceRouteConnectionKind kind)
    {
        if (fromIndex < 0 || fromIndex >= CheckpointCount || toIndex < 0 || toIndex >= CheckpointCount || fromIndex == toIndex)
        {
            return;
        }

        if (routeConnections == null)
        {
            routeConnections = new RaceRouteConnection[0];
        }

        List<RaceRouteConnection> connections = new List<RaceRouteConnection>(routeConnections.Length + 1);
        connections.AddRange(routeConnections);
        AddConnection(connections, fromIndex, toIndex, kind, 1f);
        routeConnections = connections.ToArray();
    }

    public void ClearRouteConnections()
    {
        routeConnections = new RaceRouteConnection[0];
    }

    public void PruneInvalidRouteConnections()
    {
        if (routeConnections == null || routeConnections.Length == 0)
        {
            return;
        }

        SetRouteConnections(routeConnections);
    }

    private void AppendAutoRouteConnections(List<RaceRouteConnection> connections)
    {
        if (!autoGenerateMainRouteConnections || checkpoints == null)
        {
            return;
        }

        for (int i = 0; i < checkpoints.Length - 1; i++)
        {
            AddConnection(connections, i, i + 1, RaceRouteConnectionKind.Normal, 1f);
        }

        if (autoConnectFinalCheckpointToStart && checkpoints.Length > 2)
        {
            AddConnection(connections, checkpoints.Length - 1, 0, RaceRouteConnectionKind.Normal, 1f);
        }
    }

    private void AppendInferredRouteConnections(List<RaceRouteConnection> connections)
    {
        if (!autoBuildInferredRouteConnections || checkpoints == null || checkpoints.Length < 4)
        {
            return;
        }

        int maxSkip = Mathf.Max(2, autoGraphMaxIndexSkip);
        float maxDistance = Mathf.Max(1f, autoGraphMaxConnectionDistance);

        for (int fromIndex = 0; fromIndex < checkpoints.Length - 2; fromIndex++)
        {
            if (checkpoints[fromIndex] == null)
            {
                continue;
            }

            int maxToIndex = Mathf.Min(checkpoints.Length - 1, fromIndex + maxSkip);
            for (int toIndex = fromIndex + 2; toIndex <= maxToIndex; toIndex++)
            {
                if (checkpoints[toIndex] == null || HasConnection(connections, fromIndex, toIndex))
                {
                    continue;
                }

                float directDistance = GetFlatDistance(fromIndex, toIndex);
                if (directDistance > maxDistance)
                {
                    continue;
                }

                float orderedDistance = GetOrderedDistance(fromIndex, toIndex);
                if (orderedDistance <= 0.01f || directDistance >= orderedDistance)
                {
                    continue;
                }

                float saving = 1f - (directDistance / orderedDistance);
                if (saving < autoGraphMinimumShortcutSaving)
                {
                    continue;
                }

                RaceRouteConnectionKind kind = RaceRouteConnectionKind.Normal;
                if (saving >= 0.38f || toIndex - fromIndex >= 5)
                {
                    kind = RaceRouteConnectionKind.Shortcut;
                }
                else if (saving >= 0.25f)
                {
                    kind = RaceRouteConnectionKind.Fast;
                }

                AddConnection(connections, fromIndex, toIndex, kind, Mathf.Clamp(directDistance / orderedDistance, 0.05f, 1f));
            }
        }
    }

    private void AppendManualRouteConnections(List<RaceRouteConnection> connections)
    {
        if (routeConnections == null)
        {
            return;
        }

        for (int i = 0; i < routeConnections.Length; i++)
        {
            RaceRouteConnection connection = routeConnections[i];
            if (connection == null || !connection.IsValid(CheckpointCount))
            {
                continue;
            }

            AddConnection(connections, connection.fromIndex, connection.toIndex, connection.kind, connection.weight);
        }
    }

    private void ShiftRouteConnectionsForInsertedCheckpoint(int insertIndex)
    {
        if (routeConnections == null)
        {
            return;
        }

        for (int i = 0; i < routeConnections.Length; i++)
        {
            if (routeConnections[i] == null)
            {
                continue;
            }

            if (routeConnections[i].fromIndex >= insertIndex)
            {
                routeConnections[i].fromIndex++;
            }

            if (routeConnections[i].toIndex >= insertIndex)
            {
                routeConnections[i].toIndex++;
            }
        }

        PruneInvalidRouteConnections();
    }

    private void RemapRouteConnections(IList<int> remap)
    {
        if (routeConnections == null || routeConnections.Length == 0)
        {
            return;
        }

        List<RaceRouteConnection> remappedConnections = new List<RaceRouteConnection>();
        for (int i = 0; i < routeConnections.Length; i++)
        {
            RaceRouteConnection connection = routeConnections[i];
            if (connection == null)
            {
                continue;
            }

            if (connection.fromIndex < 0 || connection.fromIndex >= remap.Count || connection.toIndex < 0 || connection.toIndex >= remap.Count)
            {
                continue;
            }

            int remappedFrom = remap[connection.fromIndex];
            int remappedTo = remap[connection.toIndex];
            if (remappedFrom < 0 || remappedTo < 0)
            {
                continue;
            }

            AddConnection(remappedConnections, remappedFrom, remappedTo, connection.kind, connection.weight);
        }

        routeConnections = remappedConnections.ToArray();
    }

    private float GetOrderedDistance(int fromIndex, int toIndex)
    {
        float distance = 0f;
        for (int i = fromIndex; i < toIndex; i++)
        {
            if (checkpoints[i] == null || checkpoints[i + 1] == null)
            {
                return 0f;
            }

            distance += Vector3.Distance(ProjectFlat(checkpoints[i].position), ProjectFlat(checkpoints[i + 1].position));
        }

        return distance;
    }

    private float GetFlatDistance(int fromIndex, int toIndex)
    {
        return Vector3.Distance(ProjectFlat(checkpoints[fromIndex].position), ProjectFlat(checkpoints[toIndex].position));
    }

    private static Vector3 ProjectFlat(Vector3 position)
    {
        return new Vector3(position.x, 0f, position.z);
    }

    private static bool HasConnection(IList<RaceRouteConnection> connections, int fromIndex, int toIndex)
    {
        for (int i = 0; i < connections.Count; i++)
        {
            RaceRouteConnection connection = connections[i];
            if (connection != null && connection.fromIndex == fromIndex && connection.toIndex == toIndex)
            {
                return true;
            }
        }

        return false;
    }

    private static void AddConnection(List<RaceRouteConnection> connections, int fromIndex, int toIndex, RaceRouteConnectionKind kind, float weight)
    {
        if (fromIndex == toIndex || fromIndex < 0 || toIndex < 0)
        {
            return;
        }

        for (int i = 0; i < connections.Count; i++)
        {
            RaceRouteConnection existing = connections[i];
            if (existing == null || existing.fromIndex != fromIndex || existing.toIndex != toIndex)
            {
                continue;
            }

            existing.kind = kind;
            existing.weight = Mathf.Max(0.05f, weight);
            return;
        }

        connections.Add(new RaceRouteConnection(fromIndex, toIndex, kind)
        {
            weight = Mathf.Max(0.05f, weight)
        });
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
