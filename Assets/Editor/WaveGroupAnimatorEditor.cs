using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WaveGroupAnimator))]
public class WaveGroupAnimatorEditor : Editor
{
    private bool autoPlayPreview = true;
    private bool liveScenePreview = false;
    private float previewTime = 0f;
    private double lastEditorTime;
    private Vector2 previewOrbit = new Vector2(20f, -30f);

    void OnEnable()
    {
        lastEditorTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += OnEditorUpdate;
    }

    void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        if (liveScenePreview && !Application.isPlaying)
        {
            WaveGroupAnimator anim = target as WaveGroupAnimator;
            if (anim != null)
            {
                anim.ApplyAnimationAtTime(0f);
            }
        }
    }

    private void OnEditorUpdate()
    {
        if (Application.isPlaying || !autoPlayPreview) return;

        double now = EditorApplication.timeSinceStartup;
        float dt = (float)(now - lastEditorTime);
        lastEditorTime = now;
        previewTime += dt;

        WaveGroupAnimator anim = target as WaveGroupAnimator;
        if (anim != null && liveScenePreview)
        {
            anim.ApplyAnimationAtTime(previewTime);
            SceneView.RepaintAll();
        }

        Repaint();
    }

    public override void OnInspectorGUI()
    {
        WaveGroupAnimator anim = (WaveGroupAnimator)target;

        // 1. Quick Presets
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Quick Teaching Presets", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Wave Floor\n(Grid)", GUILayout.Height(36)))
        {
            ApplyPresetWaveFloor(anim);
        }
        if (GUILayout.Button("Moving Stairs\n(Line)", GUILayout.Height(36)))
        {
            ApplyPresetStairs(anim);
        }
        if (GUILayout.Button("Spinning Blades\n(Rotation)", GUILayout.Height(36)))
        {
            ApplyPresetSpinningBlades(anim);
        }
        if (GUILayout.Button("Helix Wave\n(3D Lissajous)", GUILayout.Height(36)))
        {
            ApplyPresetHelix(anim);
        }
        EditorGUILayout.EndHorizontal();

        // 2. Group Management Buttons
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Group Object Management", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate / Rebuild", GUILayout.Height(28)))
        {
            Undo.RegisterFullObjectHierarchyUndo(anim.gameObject, "Rebuild Wave Group");
            anim.RebuildChildren();
            EditorUtility.SetDirty(anim);
        }
        if (GUILayout.Button("Collect Children", GUILayout.Height(28)))
        {
            Undo.RecordObject(anim, "Collect Wave Group Children");
            anim.CollectExistingChildren();
            EditorUtility.SetDirty(anim);
        }
        if (GUILayout.Button("Clear", GUILayout.Height(28), GUILayout.Width(64)))
        {
            Undo.RegisterFullObjectHierarchyUndo(anim.gameObject, "Clear Wave Group");
            anim.ClearChildren();
            EditorUtility.SetDirty(anim);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox($"Grouped Members: {anim.Members.Count}", MessageType.None);

        // 3. Default Inspector
        EditorGUILayout.Space(4);
        DrawDefaultInspector();

        // 4. Mini Preview Viewer
        EditorGUILayout.Space(8);
        DrawPreviewViewer(anim);
    }

    private void DrawPreviewViewer(WaveGroupAnimator anim)
    {
        EditorGUILayout.LabelField("Phase & Trajectory Mini Viewer", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        autoPlayPreview = GUILayout.Toggle(autoPlayPreview, "Auto Play", "Button", GUILayout.Width(80));
        bool newLive = GUILayout.Toggle(liveScenePreview, "Live Scene Preview", "Button", GUILayout.Width(135));
        if (newLive != liveScenePreview)
        {
            liveScenePreview = newLive;
            if (!liveScenePreview && !Application.isPlaying)
            {
                anim.ApplyAnimationAtTime(0f);
                SceneView.RepaintAll();
            }
        }
        if (GUILayout.Button("Reset t=0", GUILayout.Width(75)))
        {
            previewTime = 0f;
            if (!Application.isPlaying)
            {
                anim.ApplyAnimationAtTime(0f);
                SceneView.RepaintAll();
            }
        }
        EditorGUILayout.EndHorizontal();

        if (!autoPlayPreview)
        {
            float newT = EditorGUILayout.Slider("Preview Time", previewTime % 10f, 0f, 10f);
            if (!Mathf.Approximately(newT, previewTime % 10f))
            {
                previewTime = newT;
                if (liveScenePreview && !Application.isPlaying)
                {
                    anim.ApplyAnimationAtTime(previewTime);
                    SceneView.RepaintAll();
                }
            }
        }

        Rect rect = GUILayoutUtility.GetRect(100f, 200f, GUILayout.ExpandWidth(true));
        HandlePreviewDrag(rect);

        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(rect, new Color(0.11f, 0.12f, 0.15f, 1f));
            DrawPreviewContent(rect, anim);
        }

        EditorGUILayout.LabelField("Drag in preview to orbit 3D view", EditorStyles.miniLabel);
    }

    private void HandlePreviewDrag(Rect rect)
    {
        Event e = Event.current;
        if (rect.Contains(e.mousePosition) && e.type == EventType.MouseDrag && e.button == 0)
        {
            previewOrbit.y += e.delta.x * 0.6f;
            previewOrbit.x = Mathf.Clamp(previewOrbit.x + e.delta.y * 0.6f, -85f, 85f);
            e.Use();
            Repaint();
        }
    }

    private void DrawPreviewContent(Rect rect, WaveGroupAnimator anim)
    {
        GUI.BeginClip(rect);
        Rect localRect = new Rect(0, 0, rect.width, rect.height);
        Vector2 center = localRect.center;
        float scale = Mathf.Min(rect.width, rect.height) * 0.045f;

        Quaternion rot = Quaternion.Euler(previewOrbit.x, previewOrbit.y, 0f);
        var members = anim.Members;

        float tSample = Application.isPlaying ? Time.time : previewTime;
        float t = tSample * anim.speed;

        if (members != null && members.Count > 0)
        {
            Vector3 prevScreen = Vector3.zero;
            for (int i = 0; i < members.Count; i++)
            {
                var m = members[i];
                float phaseOffset = anim.basePhaseDeg + m.indexX * anim.phaseStepXDeg + m.indexZ * anim.phaseStepZDeg;

                Vector3 pos = m.baseLocalPosition;
                if (anim.applyPosition)
                {
                    pos += WaveGroupAnimator.EvaluateWave(t, anim.posAmplitude, anim.posFrequency, anim.posAxisPhaseDeg, phaseOffset);
                }

                Vector3 p = rot * pos;
                Vector2 pt = new Vector2(center.x + p.x * scale, center.y - p.y * scale);

                float norm = (float)i / Mathf.Max(1, members.Count - 1);
                Color c = Color.Lerp(new Color(0.2f, 0.7f, 1f), new Color(1f, 0.45f, 0.25f), norm);

                if (i > 0 && anim.layoutMode != WaveGroupAnimator.LayoutMode.Grid)
                {
                    Handles.color = new Color(c.r, c.g, c.b, 0.45f);
                    Handles.DrawLine(prevScreen, pt);
                }
                prevScreen = pt;

                // Draw blade orientation indicator if rotation is active
                if (anim.applyRotation)
                {
                    Vector3 euler = m.baseLocalEuler;
                    if (anim.continuousSpin)
                    {
                        if (Mathf.Abs(anim.spinSpeedDegPerSec.x) > 0.001f) euler.x += anim.spinSpeedDegPerSec.x * tSample * anim.speed + phaseOffset;
                        if (Mathf.Abs(anim.spinSpeedDegPerSec.y) > 0.001f) euler.y += anim.spinSpeedDegPerSec.y * tSample * anim.speed + phaseOffset;
                        if (Mathf.Abs(anim.spinSpeedDegPerSec.z) > 0.001f) euler.z += anim.spinSpeedDegPerSec.z * tSample * anim.speed + phaseOffset;
                    }
                    else
                    {
                        euler += WaveGroupAnimator.EvaluateWave(t, anim.rotAmplitude, anim.rotFrequency, anim.rotAxisPhaseDeg, phaseOffset);
                    }

                    Vector3 dir = rot * (Quaternion.Euler(euler) * Vector3.right) * 1.1f;
                    Vector2 d2 = new Vector2(dir.x * scale, -dir.y * scale);
                    Handles.color = c;
                    Handles.DrawLine(pt - d2, pt + d2);
                }

                Handles.color = c;
                Handles.DrawSolidDisc(pt, Vector3.forward, 3.5f);
            }
        }
        else
        {
            GUI.Label(new Rect(10, localRect.height * 0.45f, localRect.width - 20, 30),
                "No members yet. Click 'Generate / Rebuild' or a Preset.", EditorStyles.centeredGreyMiniLabel);
        }

        GUI.EndClip();
    }

    void OnSceneGUI()
    {
        WaveGroupAnimator anim = (WaveGroupAnimator)target;
        if (!anim.showSceneGizmos || anim.Members == null || anim.Members.Count == 0) return;

        Handles.color = anim.gizmoColor;
        var members = anim.Members;
        for (int i = 0; i < members.Count - 1; i++)
        {
            if (members[i].target == null || members[i + 1].target == null) continue;
            if (anim.layoutMode == WaveGroupAnimator.LayoutMode.Grid && members[i].indexZ != members[i + 1].indexZ) continue;
            Handles.DrawLine(members[i].target.position, members[i + 1].target.position);
        }
    }

    private void ApplyPresetWaveFloor(WaveGroupAnimator anim)
    {
        Undo.RegisterFullObjectHierarchyUndo(anim.gameObject, "Apply Wave Floor Preset");
        anim.layoutMode = WaveGroupAnimator.LayoutMode.Grid;
        anim.countX = 8;
        anim.countZ = 8;
        anim.spacing = new Vector3(1.05f, 0f, 1.05f);
        anim.primitiveType = PrimitiveType.Cube;
        anim.childScale = new Vector3(1f, 2f, 1f);
        anim.speed = 0.5f;
        anim.phaseStepXDeg = 25f;
        anim.phaseStepZDeg = 25f;
        anim.applyPosition = true;
        anim.posAmplitude = new Vector3(0f, 0.8f, 0f);
        anim.posFrequency = Vector3.one;
        anim.posAxisPhaseDeg = Vector3.zero;
        anim.applyRotation = false;
        anim.applyScale = false;
        anim.RebuildChildren();
        EditorUtility.SetDirty(anim);
    }

    private void ApplyPresetStairs(WaveGroupAnimator anim)
    {
        Undo.RegisterFullObjectHierarchyUndo(anim.gameObject, "Apply Moving Stairs Preset");
        anim.layoutMode = WaveGroupAnimator.LayoutMode.Line;
        anim.countX = 10;
        anim.countZ = 1;
        anim.spacing = new Vector3(0f, 0.35f, 1.1f);
        anim.primitiveType = PrimitiveType.Cube;
        anim.childScale = new Vector3(2.5f, 0.4f, 1f);
        anim.speed = 0.5f;
        anim.phaseStepXDeg = 36f;
        anim.phaseStepZDeg = 0f;
        anim.applyPosition = true;
        anim.posAmplitude = new Vector3(0f, 0.5f, 0f);
        anim.posFrequency = Vector3.one;
        anim.posAxisPhaseDeg = Vector3.zero;
        anim.applyRotation = false;
        anim.applyScale = false;
        anim.RebuildChildren();
        EditorUtility.SetDirty(anim);
    }

    private void ApplyPresetSpinningBlades(WaveGroupAnimator anim)
    {
        Undo.RegisterFullObjectHierarchyUndo(anim.gameObject, "Apply Spinning Blades Preset");
        anim.layoutMode = WaveGroupAnimator.LayoutMode.Line;
        anim.countX = 8;
        anim.countZ = 1;
        anim.spacing = new Vector3(0f, 0f, 1.5f);
        anim.primitiveType = PrimitiveType.Cube;
        anim.childScale = new Vector3(3.2f, 0.25f, 0.4f);
        anim.speed = 1.0f;
        anim.phaseStepXDeg = 22.5f;
        anim.phaseStepZDeg = 0f;
        anim.applyPosition = false;
        anim.applyRotation = true;
        anim.continuousSpin = true;
        anim.spinSpeedDegPerSec = new Vector3(0f, 90f, 0f);
        anim.applyScale = false;
        anim.RebuildChildren();
        EditorUtility.SetDirty(anim);
    }

    private void ApplyPresetHelix(WaveGroupAnimator anim)
    {
        Undo.RegisterFullObjectHierarchyUndo(anim.gameObject, "Apply Helix Wave Preset");
        anim.layoutMode = WaveGroupAnimator.LayoutMode.Line;
        anim.countX = 14;
        anim.countZ = 1;
        anim.spacing = new Vector3(0f, 0f, 0.8f);
        anim.primitiveType = PrimitiveType.Sphere;
        anim.childScale = new Vector3(0.6f, 0.6f, 0.6f);
        anim.speed = 0.6f;
        anim.phaseStepXDeg = 25f;
        anim.phaseStepZDeg = 0f;
        anim.applyPosition = true;
        anim.posAmplitude = new Vector3(1.5f, 1.5f, 0f);
        anim.posFrequency = Vector3.one;
        anim.posAxisPhaseDeg = new Vector3(0f, 90f, 0f);
        anim.applyRotation = false;
        anim.applyScale = false;
        anim.RebuildChildren();
        EditorUtility.SetDirty(anim);
    }
}
