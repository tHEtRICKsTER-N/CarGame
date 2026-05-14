using System.Collections.Generic;
using UnityEngine;

public sealed class ArcadeRaceParticipant : MonoBehaviour
{
    public string displayName = "Driver";
    public bool isPlayer;

    public int CompletedLaps { get; private set; }
    public int NextCheckpointIndex { get; private set; }
    public float ProgressScore { get; private set; }
    public float FinishTime { get; private set; }
    public bool HasFinished { get; private set; }

    private ArcadeRaceManager raceManager;

    public void Initialize(ArcadeRaceManager manager, string participantName, bool player)
    {
        raceManager = manager;
        displayName = participantName;
        isPlayer = player;
        CompletedLaps = 0;
        NextCheckpointIndex = 1;
        ProgressScore = 0f;
        FinishTime = 0f;
        HasFinished = false;
    }

    public void TickProgress(IReadOnlyList<Vector3> checkpoints, float checkpointRadius, int lapsToWin, float raceTime)
    {
        if (HasFinished || checkpoints == null || checkpoints.Count < 2)
        {
            return;
        }

        float radiusSqr = checkpointRadius * checkpointRadius;
        int safety = checkpoints.Count;

        while (safety > 0 && (transform.position - checkpoints[NextCheckpointIndex]).sqrMagnitude <= radiusSqr)
        {
            AdvanceCheckpoint(checkpoints.Count, lapsToWin, raceTime);
            safety--;

            if (HasFinished)
            {
                break;
            }
        }

        ProgressScore = CalculateProgressScore(checkpoints);
    }

    public float DistanceToNextCheckpoint(IReadOnlyList<Vector3> checkpoints)
    {
        if (checkpoints == null || checkpoints.Count == 0)
        {
            return 0f;
        }

        return Vector3.Distance(transform.position, checkpoints[NextCheckpointIndex]);
    }

    private void AdvanceCheckpoint(int checkpointCount, int lapsToWin, float raceTime)
    {
        int checkpointHit = NextCheckpointIndex;
        NextCheckpointIndex++;

        if (NextCheckpointIndex >= checkpointCount)
        {
            NextCheckpointIndex = 0;
        }

        if (checkpointHit == 0)
        {
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
        }
    }

    private float CalculateProgressScore(IReadOnlyList<Vector3> checkpoints)
    {
        int checkpointCount = checkpoints.Count;
        int previousCheckpoint = NextCheckpointIndex - 1;
        if (previousCheckpoint < 0)
        {
            previousCheckpoint = checkpointCount - 1;
        }

        Vector3 from = checkpoints[previousCheckpoint];
        Vector3 to = checkpoints[NextCheckpointIndex];
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
