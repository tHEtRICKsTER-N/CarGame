using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RaceTrackDefinition))]
public sealed class RaceTrackDefinitionEditor : Editor
{
    private const float GroundRayHeight = 120f;
    private const float GroundRayDistance = 300f;

    private RaceTrackDefinition track;
    private int selectedCheckpointIndex = -1;

    private void OnEnable()
    {
        track = (RaceTrackDefinition)target;
        SceneView.duringSceneGui += DuringSceneGui;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGui;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("mapId"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"));

        EditorGUILayout.Space(8f);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("playerSpawn"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("forwardReference"));

        EditorGUILayout.Space(8f);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("startFinishLine"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("startFinishTriggerSize"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("startFinishTriggerCenter"));

        EditorGUILayout.Space(8f);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("checkpointRadius"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("drawOrderedPreviewLine"));

        EditorGUILayout.Space(8f);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("checkpoints"), true);

        serializedObject.ApplyModifiedProperties();

        selectedCheckpointIndex = GetSelectedCheckpointIndex();

        EditorGUILayout.Space(12f);
        DrawToolButtons();
    }

    private void DrawToolButtons()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Track Point Tools", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Selected", GetSelectedCheckpointLabel());

            string addButtonLabel = selectedCheckpointIndex >= 0
                ? "Add Checkpoint After " + GetCheckpointLabel(selectedCheckpointIndex)
                : "Add Checkpoint At Scene View";

            if (GUILayout.Button(addButtonLabel))
            {
                AddCheckpoint(GetNewCheckpointPosition(selectedCheckpointIndex), selectedCheckpointIndex);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Player Spawn"))
                {
                    AddPlayerSpawn(GetSceneViewPlacementPosition());
                }

                if (GUILayout.Button("Add Start/Finish Line"))
                {
                    AddStartFinishLine(GetDefaultStartFinishPosition());
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Use First Checkpoint As Spawn"))
                {
                    UseFirstCheckpointAsSpawn();
                }

                if (GUILayout.Button("Use Spawn As Start/Finish"))
                {
                    UseSpawnAsStartFinish();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh From Children"))
                {
                    RefreshFromChildren();
                }

                if (GUILayout.Button("Remove Empty Slots"))
                {
                    RemoveEmptySlots();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rename In Order"))
                {
                    RenameInOrder();
                }
            }

            if (GUILayout.Button("Project Checkpoints To Ground"))
            {
                ProjectCheckpointsToGround();
            }
        }
    }

    private void DuringSceneGui(SceneView sceneView)
    {
        if (track == null)
        {
            return;
        }

        selectedCheckpointIndex = GetSelectedCheckpointIndex();
        DrawCheckpointHandles();
        DrawTrackLine();
        DrawSpawnHandle();
        DrawStartFinishHandle();
    }

    private void DrawCheckpointHandles()
    {
        if (track.checkpoints == null)
        {
            return;
        }

        for (int i = 0; i < track.checkpoints.Length; i++)
        {
            Transform checkpoint = track.checkpoints[i];
            if (checkpoint == null)
            {
                continue;
            }

            bool isSelected = Selection.activeTransform == checkpoint;
            Handles.color = isSelected ? new Color(1f, 0.55f, 0f, 1f) : (i == 0 ? Color.green : new Color(0f, 0.85f, 1f, 1f));
            float handleSize = HandleUtility.GetHandleSize(checkpoint.position) * 0.28f;

            if (Handles.Button(checkpoint.position, Quaternion.identity, handleSize, handleSize, Handles.SphereHandleCap))
            {
                Selection.activeTransform = checkpoint;
                selectedCheckpointIndex = i;
                Repaint();
                SceneView.RepaintAll();
            }

            Handles.Label(checkpoint.position + Vector3.up * handleSize, i == 0 ? "START" : "CP " + i);

            EditorGUI.BeginChangeCheck();
            Vector3 newPosition = Handles.PositionHandle(checkpoint.position, checkpoint.rotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(checkpoint, "Move Track Checkpoint");
                checkpoint.position = newPosition;
                EditorUtility.SetDirty(checkpoint);
            }
        }
    }

    private void DrawTrackLine()
    {
        if (!track.drawOrderedPreviewLine)
        {
            return;
        }

        if (track.checkpoints == null || track.checkpoints.Length < 2)
        {
            return;
        }

        Handles.color = Color.yellow;
        for (int i = 0; i < track.checkpoints.Length - 1; i++)
        {
            Transform from = track.checkpoints[i];
            Transform to = track.checkpoints[i + 1];

            if (from == null || to == null)
            {
                continue;
            }

            Handles.DrawAAPolyLine(4f, from.position + Vector3.up, to.position + Vector3.up);
        }
    }

    private void DrawSpawnHandle()
    {
        if (track.playerSpawn == null)
        {
            return;
        }

        Handles.color = new Color(1f, 0.45f, 0f, 1f);
        Handles.ArrowHandleCap(0, track.playerSpawn.position, track.playerSpawn.rotation, HandleUtility.GetHandleSize(track.playerSpawn.position), EventType.Repaint);
        Handles.Label(track.playerSpawn.position + Vector3.up * 2f, "Player Spawn");

        EditorGUI.BeginChangeCheck();
        Vector3 newPosition = Handles.PositionHandle(track.playerSpawn.position, track.playerSpawn.rotation);
        Quaternion newRotation = Handles.RotationHandle(track.playerSpawn.rotation, track.playerSpawn.position);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(track.playerSpawn, "Move Player Spawn");
            track.playerSpawn.position = newPosition;
            track.playerSpawn.rotation = newRotation;
            EditorUtility.SetDirty(track.playerSpawn);
        }
    }

    private void DrawStartFinishHandle()
    {
        if (track.startFinishLine == null)
        {
            return;
        }

        Handles.color = new Color(1f, 0.9f, 0.05f, 1f);
        Matrix4x4 oldMatrix = Handles.matrix;
        Handles.matrix = Matrix4x4.TRS(track.startFinishLine.position, track.startFinishLine.rotation, Vector3.one);
        Handles.DrawWireCube(track.startFinishTriggerCenter, track.startFinishTriggerSize);
        Handles.matrix = oldMatrix;

        Handles.ArrowHandleCap(0, track.startFinishLine.position, track.startFinishLine.rotation, HandleUtility.GetHandleSize(track.startFinishLine.position), EventType.Repaint);
        Handles.Label(track.startFinishLine.position + Vector3.up * 2.5f, "Start / Finish");

        EditorGUI.BeginChangeCheck();
        Vector3 newPosition = Handles.PositionHandle(track.startFinishLine.position, track.startFinishLine.rotation);
        Quaternion newRotation = Handles.RotationHandle(track.startFinishLine.rotation, track.startFinishLine.position);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(track.startFinishLine, "Move Start Finish Line");
            track.startFinishLine.position = newPosition;
            track.startFinishLine.rotation = newRotation;
            EditorUtility.SetDirty(track.startFinishLine);
        }
    }

    private void AddCheckpoint(Vector3 position, int insertAfterIndex)
    {
        Undo.RegisterCompleteObjectUndo(track, "Add Track Checkpoint");

        int insertIndex = insertAfterIndex >= 0
            ? Mathf.Clamp(insertAfterIndex + 1, 0, track.CheckpointCount)
            : track.CheckpointCount;
        GameObject checkpointObject = new GameObject(GetCheckpointName(insertIndex));
        Undo.RegisterCreatedObjectUndo(checkpointObject, "Add Track Checkpoint");
        checkpointObject.transform.SetParent(track.transform);
        checkpointObject.transform.position = position;

        List<Transform> ordered = new List<Transform>();
        if (track.checkpoints != null)
        {
            ordered.AddRange(track.checkpoints);
        }

        ordered.Insert(insertIndex, checkpointObject.transform);
        track.SetCheckpoints(ordered);
        RenameInOrder();
        EditorUtility.SetDirty(track);
        Selection.activeTransform = checkpointObject.transform;
        selectedCheckpointIndex = insertIndex;
    }

    private void AddPlayerSpawn(Vector3 position)
    {
        Undo.RegisterCompleteObjectUndo(track, "Add Player Spawn");

        GameObject spawnObject = new GameObject("PlayerSpawn");
        Undo.RegisterCreatedObjectUndo(spawnObject, "Add Player Spawn");
        spawnObject.transform.SetParent(track.transform);
        spawnObject.transform.position = position;

        if (track.checkpoints != null && track.checkpoints.Length > 1 && track.checkpoints[0] != null)
        {
            Vector3 forward = track.checkpoints[1].position - track.checkpoints[0].position;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.01f)
            {
                spawnObject.transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            }
        }

        track.playerSpawn = spawnObject.transform;
        track.forwardReference = spawnObject.transform;
        EditorUtility.SetDirty(track);
        Selection.activeTransform = spawnObject.transform;
    }

    private void AddStartFinishLine(Vector3 position)
    {
        Undo.RegisterCompleteObjectUndo(track, "Add Start Finish Line");

        GameObject startFinishObject = new GameObject("StartFinishLine");
        Undo.RegisterCreatedObjectUndo(startFinishObject, "Add Start Finish Line");
        startFinishObject.transform.SetParent(track.transform);
        startFinishObject.transform.position = position;
        startFinishObject.transform.rotation = GetDefaultStartRotation();

        track.startFinishLine = startFinishObject.transform;
        EditorUtility.SetDirty(track);
        Selection.activeTransform = startFinishObject.transform;
    }

    private void UseFirstCheckpointAsSpawn()
    {
        if (track.checkpoints == null || track.checkpoints.Length == 0 || track.checkpoints[0] == null)
        {
            return;
        }

        Undo.RegisterCompleteObjectUndo(track, "Use First Checkpoint As Spawn");
        track.playerSpawn = track.checkpoints[0];
        track.forwardReference = track.checkpoints[0];
        EditorUtility.SetDirty(track);
    }

    private void UseSpawnAsStartFinish()
    {
        if (track.playerSpawn == null)
        {
            return;
        }

        Undo.RegisterCompleteObjectUndo(track, "Use Spawn As Start Finish");
        track.startFinishLine = track.playerSpawn;
        EditorUtility.SetDirty(track);
    }

    private void RefreshFromChildren()
    {
        Undo.RegisterCompleteObjectUndo(track, "Refresh Track Checkpoints");
        track.RefreshCheckpointsFromChildren();
        EditorUtility.SetDirty(track);
    }

    private void RemoveEmptySlots()
    {
        Undo.RegisterCompleteObjectUndo(track, "Remove Empty Checkpoint Slots");
        track.RemoveEmptyCheckpointSlots();
        EditorUtility.SetDirty(track);
    }

    private void RenameInOrder()
    {
        if (track.checkpoints == null)
        {
            return;
        }

        for (int i = 0; i < track.checkpoints.Length; i++)
        {
            if (track.checkpoints[i] == null)
            {
                continue;
            }

            Undo.RecordObject(track.checkpoints[i].gameObject, "Rename Track Checkpoint");
            track.checkpoints[i].name = GetCheckpointName(i);
            EditorUtility.SetDirty(track.checkpoints[i].gameObject);
        }
    }

    private void ProjectCheckpointsToGround()
    {
        if (track.checkpoints == null)
        {
            return;
        }

        for (int i = 0; i < track.checkpoints.Length; i++)
        {
            Transform checkpoint = track.checkpoints[i];
            if (checkpoint == null)
            {
                continue;
            }

            Vector3 rayOrigin = checkpoint.position + Vector3.up * GroundRayHeight;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, GroundRayDistance))
            {
                Undo.RecordObject(checkpoint, "Project Checkpoint To Ground");
                checkpoint.position = hit.point;
                EditorUtility.SetDirty(checkpoint);
            }
        }
    }

    private Vector3 GetSceneViewPlacementPosition()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null || sceneView.camera == null)
        {
            return track.transform.position;
        }

        Ray ray = new Ray(sceneView.camera.transform.position, sceneView.camera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, GroundRayDistance))
        {
            return hit.point;
        }

        return sceneView.pivot;
    }

    private Vector3 GetDefaultStartFinishPosition()
    {
        if (track.playerSpawn != null)
        {
            return track.playerSpawn.position;
        }

        if (track.checkpoints != null && track.checkpoints.Length > 0 && track.checkpoints[0] != null)
        {
            return track.checkpoints[0].position;
        }

        return GetSceneViewPlacementPosition();
    }

    private Quaternion GetDefaultStartRotation()
    {
        if (track.playerSpawn != null)
        {
            return track.playerSpawn.rotation;
        }

        if (track.forwardReference != null)
        {
            return track.forwardReference.rotation;
        }

        if (track.checkpoints != null && track.checkpoints.Length > 1 && track.checkpoints[0] != null && track.checkpoints[1] != null)
        {
            Vector3 forward = track.checkpoints[1].position - track.checkpoints[0].position;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.01f)
            {
                return Quaternion.LookRotation(forward.normalized, Vector3.up);
            }
        }

        return Quaternion.identity;
    }

    private Vector3 GetNewCheckpointPosition(int insertAfterIndex)
    {
        if (insertAfterIndex < 0 || track.checkpoints == null || insertAfterIndex >= track.checkpoints.Length || track.checkpoints[insertAfterIndex] == null)
        {
            return GetSceneViewPlacementPosition();
        }

        Transform selectedCheckpoint = track.checkpoints[insertAfterIndex];
        Transform nextCheckpoint = null;

        if (track.checkpoints.Length > 1 && insertAfterIndex < track.checkpoints.Length - 1)
        {
            int nextIndex = insertAfterIndex + 1;
            nextCheckpoint = track.checkpoints[nextIndex];
        }

        if (nextCheckpoint != null && nextCheckpoint != selectedCheckpoint)
        {
            return Vector3.Lerp(selectedCheckpoint.position, nextCheckpoint.position, 0.5f);
        }

        return selectedCheckpoint.position + selectedCheckpoint.forward * 8f;
    }

    private int GetSelectedCheckpointIndex()
    {
        if (track == null || track.checkpoints == null || Selection.activeTransform == null)
        {
            return -1;
        }

        for (int i = 0; i < track.checkpoints.Length; i++)
        {
            if (track.checkpoints[i] == Selection.activeTransform)
            {
                return i;
            }
        }

        return -1;
    }

    private string GetSelectedCheckpointLabel()
    {
        return selectedCheckpointIndex >= 0 ? GetCheckpointLabel(selectedCheckpointIndex) : "None";
    }

    private static string GetCheckpointLabel(int index)
    {
        return index == 0 ? "START" : "CP " + index;
    }

    private static string GetCheckpointName(int index)
    {
        return "Checkpoint_" + index.ToString("00");
    }
}
