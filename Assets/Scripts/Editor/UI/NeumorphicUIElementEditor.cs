using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(NeumorphicUIElement))]
[CanEditMultipleObjects]
public sealed class NeumorphicUIElementEditor : Editor
{
    private const string DefaultMaterialPath = "Assets/Materials/UI/DarkNeumorphicUI.mat";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "Dark neumorphic UI surface. Use Raised for normal panels/buttons, Inset for selected wells, and Preview Pressed to tune the button sink animation.",
            MessageType.Info);

        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Assign Default Material"))
            {
                AssignDefaultMaterial();
            }

            if (GUILayout.Button("Apply To Graphic"))
            {
                ApplyElements();
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Raised Panel"))
            {
                ApplyPreset(NeumorphicPreset.RaisedPanel);
            }

            if (GUILayout.Button("Button"))
            {
                ApplyPreset(NeumorphicPreset.Button);
            }

            if (GUILayout.Button("Inset Well"))
            {
                ApplyPreset(NeumorphicPreset.InsetWell);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Small Radius"))
            {
                ApplyCornerPreset(14f);
            }

            if (GUILayout.Button("Medium Radius"))
            {
                ApplyCornerPreset(22f);
            }

            if (GUILayout.Button("Pill Radius"))
            {
                ApplyPillRadius();
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Preview Released"))
            {
                SetPreviewPress(0f);
            }

            if (GUILayout.Button("Preview Pressed"))
            {
                SetPreviewPress(1f);
            }
        }
    }

    private void AssignDefaultMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(DefaultMaterialPath);
        if (material == null)
        {
            Debug.LogWarning("Default neumorphic material was not found at " + DefaultMaterialPath);
            return;
        }

        ForEachTarget(element =>
        {
            Undo.RecordObject(element, "Assign Neumorphic Material");
            element.sourceMaterial = material;
            element.createMaterialInstance = true;

            Graphic graphic = element.GetComponent<Graphic>();
            if (graphic != null)
            {
                Undo.RecordObject(graphic, "Assign Neumorphic Material");
                graphic.material = material;
                graphic.raycastTarget = true;
            }

            element.Apply();
            EditorUtility.SetDirty(element);
        });
    }

    private void ApplyElements()
    {
        ForEachTarget(element =>
        {
            Undo.RecordObject(element, "Apply Neumorphic UI Element");
            element.Apply();
            EditorUtility.SetDirty(element);
        });
    }

    private void SetPreviewPress(float value)
    {
        ForEachTarget(element =>
        {
            Undo.RecordObject(element, "Preview Neumorphic Press");
            element.previewPressAmount = value;
            element.Apply();
            EditorUtility.SetDirty(element);
        });
    }

    private void ApplyPreset(NeumorphicPreset preset)
    {
        ForEachTarget(element =>
        {
            Undo.RecordObject(element, "Apply Neumorphic Preset");

            switch (preset)
            {
                case NeumorphicPreset.Button:
                    element.mode = NeumorphicUIElement.SurfaceMode.Raised;
                    element.radiusMode = NeumorphicUIElement.RadiusMode.Pixels;
                    element.cornerRadius = 0.18f;
                    element.cornerRadiusPixels = 22f;
                    element.shapePadding = 0.08f;
                    element.shapePaddingPixels = 10f;
                    element.bevelSize = 0.1f;
                    element.bevelSizePixels = 14f;
                    element.shadowOffset = new Vector2(0.028f, -0.028f);
                    element.darkShadowOpacity = 0.58f;
                    element.lightShadowOpacity = 0.2f;
                    element.enablePressAnimation = true;
                    element.onlyAnimateSelectables = false;
                    element.pressedAmount = 1f;
                    element.pressInSpeed = 28f;
                    element.pressOutSpeed = 18f;
                    element.pressedShadowFade = 0.82f;
                    element.pressedDarken = 0.08f;
                    break;
                case NeumorphicPreset.InsetWell:
                    element.mode = NeumorphicUIElement.SurfaceMode.Inset;
                    element.radiusMode = NeumorphicUIElement.RadiusMode.Pixels;
                    element.cornerRadius = 0.16f;
                    element.cornerRadiusPixels = 18f;
                    element.shapePadding = 0.07f;
                    element.shapePaddingPixels = 8f;
                    element.bevelSize = 0.085f;
                    element.bevelSizePixels = 12f;
                    element.shadowOffset = new Vector2(0.02f, -0.02f);
                    element.darkShadowOpacity = 0.42f;
                    element.lightShadowOpacity = 0.12f;
                    element.enablePressAnimation = false;
                    element.previewPressAmount = 0f;
                    break;
                default:
                    element.mode = NeumorphicUIElement.SurfaceMode.Raised;
                    element.radiusMode = NeumorphicUIElement.RadiusMode.Pixels;
                    element.cornerRadius = 0.16f;
                    element.cornerRadiusPixels = 22f;
                    element.shapePadding = 0.075f;
                    element.shapePaddingPixels = 10f;
                    element.bevelSize = 0.095f;
                    element.bevelSizePixels = 14f;
                    element.shadowOffset = new Vector2(0.028f, -0.028f);
                    element.darkShadowOpacity = 0.55f;
                    element.lightShadowOpacity = 0.18f;
                    element.enablePressAnimation = false;
                    element.previewPressAmount = 0f;
                    break;
            }

            element.Apply();
            EditorUtility.SetDirty(element);
        });
    }

    private void ApplyCornerPreset(float radiusPixels)
    {
        ForEachTarget(element =>
        {
            Undo.RecordObject(element, "Apply Neumorphic Corner Preset");
            element.radiusMode = NeumorphicUIElement.RadiusMode.Pixels;
            element.cornerRadiusPixels = radiusPixels;
            element.Apply();
            EditorUtility.SetDirty(element);
        });
    }

    private void ApplyPillRadius()
    {
        ForEachTarget(element =>
        {
            Undo.RecordObject(element, "Apply Neumorphic Pill Radius");
            element.SetPillCornerRadius();
            EditorUtility.SetDirty(element);
        });
    }

    private void ForEachTarget(System.Action<NeumorphicUIElement> action)
    {
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] is NeumorphicUIElement element)
            {
                action(element);
            }
        }
    }

    private enum NeumorphicPreset
    {
        RaisedPanel,
        Button,
        InsetWell
    }
}
