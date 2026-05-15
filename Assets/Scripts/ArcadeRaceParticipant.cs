using System.Collections.Generic;
using UnityEngine;

public sealed class ArcadeRaceParticipant : MonoBehaviour
{
    public string displayName = "Driver";
    public bool isPlayer;

    public int CompletedLaps { get; private set; }
    public int CurrentCheckpointIndex { get; private set; }
    public int NextCheckpointIndex { get; private set; }
    public float ProgressScore { get; private set; }
    public float FinishTime { get; private set; }
    public bool HasFinished { get; private set; }
    public bool CanCompleteLap { get { return NextCheckpointIndex == 0 && !HasFinished; } }

    private ArcadeRaceManager raceManager;

    public void Initialize(ArcadeRaceManager manager, string participantName, bool player)
    {
        raceManager = manager;
        displayName = participantName;
        isPlayer = player;
        CompletedLaps = 0;
        CurrentCheckpointIndex = 0;
        NextCheckpointIndex = raceManager != null ? raceManager.GetNextProgressCheckpointIndex(CurrentCheckpointIndex) : 1;
        ProgressScore = 0f;
        FinishTime = 0f;
        HasFinished = false;
    }

    public void TickProgress(IReadOnlyList<Vector3> checkpoints, float checkpointRadius)
    {
        if (HasFinished || checkpoints == null || checkpoints.Count < 2)
        {
            return;
        }

        if (NextCheckpointIndex == 0)
        {
            ProgressScore = CalculateProgressScore(checkpoints);
            return;
        }

        if (raceManager != null && raceManager.HasRouteGraph)
        {
            TickGraphProgress(checkpoints, checkpointRadius);
            return;
        }

        float radiusSqr = checkpointRadius * checkpointRadius;
        int safety = checkpoints.Count;

        while (safety > 0 && (transform.position - checkpoints[NextCheckpointIndex]).sqrMagnitude <= radiusSqr)
        {
            AdvanceCheckpoint(checkpoints.Count);
            CurrentCheckpointIndex = NextCheckpointIndex == 0 ? checkpoints.Count - 1 : Mathf.Max(0, NextCheckpointIndex - 1);
            if (NextCheckpointIndex == 0)
            {
                break;
            }

            safety--;
        }

        ProgressScore = CalculateProgressScore(checkpoints);
    }

    public bool TryCompleteLap(int lapsToWin, float raceTime)
    {
        if (!CanCompleteLap)
        {
            return false;
        }

        CompletedLaps++;

        if (CompletedLaps >= lapsToWin)
        {
            HasFinished = true;
            FinishTime = raceTime;

            if (raceManager != null)
            {
                raceManager.NotifyParticipantFinished(this);
            }
        }
        else
        {
            CurrentCheckpointIndex = 0;
            NextCheckpointIndex = raceManager != null ? raceManager.GetNextProgressCheckpointIndex(CurrentCheckpointIndex) : 1;
        }

        return true;
    }

    public float DistanceToNextCheckpoint(IReadOnlyList<Vector3> checkpoints)
    {
        if (checkpoints == null || checkpoints.Count == 0)
        {
            return 0f;
        }

        return Vector3.Distance(transform.position, checkpoints[NextCheckpointIndex]);
    }

    private void AdvanceCheckpoint(int checkpointCount)
    {
        NextCheckpointIndex++;

        if (NextCheckpointIndex >= checkpointCount)
        {
            NextCheckpointIndex = 0;
        }
    }

    private void TickGraphProgress(IReadOnlyList<Vector3> checkpoints, float checkpointRadius)
    {
        int safety = checkpoints.Count;
        while (safety > 0 && raceManager.TryGetProgressCheckpointHit(transform.position, CurrentCheckpointIndex, checkpointRadius, out int hitIndex))
        {
            CurrentCheckpointIndex = hitIndex;
            NextCheckpointIndex = raceManager.GetNextProgressCheckpointIndex(CurrentCheckpointIndex);
            if (NextCheckpointIndex == 0)
            {
                break;
            }

            safety--;
        }

        ProgressScore = CalculateProgressScore(checkpoints);
    }

    private float CalculateProgressScore(IReadOnlyList<Vector3> checkpoints)
    {
        int checkpointCount = checkpoints.Count;
        int previousCheckpoint = Mathf.Clamp(CurrentCheckpointIndex, 0, checkpointCount - 1);
        int nextCheckpoint = Mathf.Clamp(NextCheckpointIndex, 0, checkpointCount - 1);

        Vector3 from = checkpoints[previousCheckpoint];
        Vector3 to = checkpoints[nextCheckpoint];
        Vector3 segment = to - from;
        float segmentLength = segment.magnitude;
        float segmentProgress = 0f;

        if (segmentLength > 0.01f)
        {
            segmentProgress = Vector3.Dot(transform.position - from, segment / segmentLength) / segmentLength;
            segmentProgress = Mathf.Clamp01(segmentProgress);
        }

        return (CompletedLaps * checkpointCount) + previousCheckpoint + segmentProgress;
    }
}
