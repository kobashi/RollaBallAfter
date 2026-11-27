using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages layout and phase-shifted Lissajous/wave animations for a group of child objects.
/// Supports waving floors (grid), moving stairs (line/step), and chained rotating blades.
/// </summary>
public class WaveGroupAnimator : MonoBehaviour
{
    public enum LayoutMode
    {
        Grid,
        Line,
        Circle
    }

    [System.Serializable]
    public struct MemberEntry
    {
        public Transform target;
        public int indexX;
        public int indexZ;
        public Vector3 baseLocalPosition;
        public Vector3 baseLocalEuler;
        public Vector3 baseLocalScale;
    }

    [Header("Group Layout & Generation")]
    public LayoutMode layoutMode = LayoutMode.Grid;
    [Min(1)] public int countX = 8;
    [Min(1)] public int countZ = 8;
    public Vector3 spacing = new Vector3(1.1f, 0f, 1.1f);
    public float circleRadius = 4f;
    public GameObject targetPrefab;
    public PrimitiveType primitiveType = PrimitiveType.Cube;
    public Vector3 childScale = Vector3.one;
    public Material childMaterial;
    public bool addKinematicRigidbody = true;

    [Header("Global Timing & Phase Shift")]
    [Range(-5f, 5f)]
    public float speed = 0.5f;
    public float basePhaseDeg = 0f;
    public float phaseStepXDeg = 25f;
    public float phaseStepZDeg = 25f;

    [Header("Position Wave")]
    public bool applyPosition = true;
    public Vector3 posAmplitude = new Vector3(0f, 0.8f, 0f);
    public Vector3 posFrequency = new Vector3(1f, 1f, 1f);
    public Vector3 posAxisPhaseDeg = new Vector3(0f, 0f, 0f);

    [Header("Rotation Wave (Euler Degrees)")]
    public bool applyRotation = false;
    public Vector3 rotAmplitude = new Vector3(0f, 0f, 45f);
    public Vector3 rotFrequency = new Vector3(1f, 1f, 1f);
    public Vector3 rotAxisPhaseDeg = Vector3.zero;
    public bool continuousSpin = false;
    public Vector3 spinSpeedDegPerSec = new Vector3(0f, 0f, 90f);

    [Header("Scale Wave")]
    public bool applyScale = false;
    public Vector3 scaleAmplitude = Vector3.zero;
    public Vector3 scaleFrequency = new Vector3(1f, 1f, 1f);
    public Vector3 scaleAxisPhaseDeg = Vector3.zero;

    [Header("Scene View Visualization")]
    public bool showSceneGizmos = true;
    public Color gizmoColor = new Color(0.2f, 0.85f, 1f, 0.8f);

    [SerializeField]
    private List<MemberEntry> members = new List<MemberEntry>();

    public IReadOnlyList<MemberEntry> Members => members;

    void Start()
    {
        if (members.Count == 0 && transform.childCount > 0)
        {
            CollectExistingChildren();
        }
    }

    void Update()
    {
        ApplyAnimationAtTime(Time.time);
    }

    public void ApplyAnimationAtTime(float timeSec)
    {
        float t = timeSec * speed;

        for (int i = 0; i < members.Count; i++)
        {
            MemberEntry m = members[i];
            if (m.target == null) continue;

            float phaseOffset = basePhaseDeg + m.indexX * phaseStepXDeg + m.indexZ * phaseStepZDeg;

            if (applyPosition)
            {
                Vector3 offset = EvaluateWave(t, posAmplitude, posFrequency, posAxisPhaseDeg, phaseOffset);
                m.target.localPosition = m.baseLocalPosition + offset;
            }

            if (applyRotation)
            {
                if (continuousSpin)
                {
                    Vector3 spinOffset = spinSpeedDegPerSec * (timeSec * speed) + Vector3.one * phaseOffset;
                    // Apply per-axis spin only on axes with non-zero spinSpeed
                    Vector3 rot = m.baseLocalEuler;
                    if (Mathf.Abs(spinSpeedDegPerSec.x) > 0.001f) rot.x += spinSpeedDegPerSec.x * timeSec * speed + phaseOffset;
                    if (Mathf.Abs(spinSpeedDegPerSec.y) > 0.001f) rot.y += spinSpeedDegPerSec.y * timeSec * speed + phaseOffset;
                    if (Mathf.Abs(spinSpeedDegPerSec.z) > 0.001f) rot.z += spinSpeedDegPerSec.z * timeSec * speed + phaseOffset;
                    m.target.localEulerAngles = rot;
                }
                else
                {
                    Vector3 rotOffset = EvaluateWave(t, rotAmplitude, rotFrequency, rotAxisPhaseDeg, phaseOffset);
                    m.target.localEulerAngles = m.baseLocalEuler + rotOffset;
                }
            }

            if (applyScale)
            {
                Vector3 scaleOffset = EvaluateWave(t, scaleAmplitude, scaleFrequency, scaleAxisPhaseDeg, phaseOffset);
                m.target.localScale = m.baseLocalScale + scaleOffset;
            }
        }
    }

    public static Vector3 EvaluateWave(float t, Vector3 amp, Vector3 freq, Vector3 axisPhaseDeg, float groupPhaseDeg)
    {
        return new Vector3(
            amp.x * Mathf.Sin(t * freq.x * Mathf.PI * 2f + (axisPhaseDeg.x + groupPhaseDeg) * Mathf.Deg2Rad),
            amp.y * Mathf.Sin(t * freq.y * Mathf.PI * 2f + (axisPhaseDeg.y + groupPhaseDeg) * Mathf.Deg2Rad),
            amp.z * Mathf.Sin(t * freq.z * Mathf.PI * 2f + (axisPhaseDeg.z + groupPhaseDeg) * Mathf.Deg2Rad)
        );
    }

    public void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
        members.Clear();
    }

    public void CollectExistingChildren()
    {
        members.Clear();
        int childCount = transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            int ix = (layoutMode == LayoutMode.Grid && countX > 0) ? (i % countX) : i;
            int iz = (layoutMode == LayoutMode.Grid && countX > 0) ? (i / countX) : 0;

            members.Add(new MemberEntry
            {
                target = child,
                indexX = ix,
                indexZ = iz,
                baseLocalPosition = child.localPosition,
                baseLocalEuler = child.localEulerAngles,
                baseLocalScale = child.localScale
            });
        }
    }

    public void RebuildChildren()
    {
        ClearChildren();

        int nx = Mathf.Max(1, countX);
        int nz = layoutMode == LayoutMode.Grid ? Mathf.Max(1, countZ) : 1;

        for (int iz = 0; iz < nz; iz++)
        {
            for (int ix = 0; ix < nx; ix++)
            {
                Vector3 localPos = ComputeLayoutPosition(ix, iz, nx, nz);
                GameObject go = CreateChildObject(ix, iz);
                go.transform.SetParent(transform, false);
                go.transform.localPosition = localPos;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = childScale;

                members.Add(new MemberEntry
                {
                    target = go.transform,
                    indexX = ix,
                    indexZ = iz,
                    baseLocalPosition = localPos,
                    baseLocalEuler = Vector3.zero,
                    baseLocalScale = childScale
                });
            }
        }
    }

    private Vector3 ComputeLayoutPosition(int ix, int iz, int nx, int nz)
    {
        switch (layoutMode)
        {
            case LayoutMode.Grid:
            {
                float offsetX = (ix - (nx - 1) * 0.5f) * spacing.x;
                float offsetY = (ix + iz) * spacing.y;
                float offsetZ = (iz - (nz - 1) * 0.5f) * spacing.z;
                return new Vector3(offsetX, offsetY, offsetZ);
            }
            case LayoutMode.Line:
            {
                float offsetX = (ix - (nx - 1) * 0.5f) * spacing.x;
                float offsetY = ix * spacing.y;
                float offsetZ = (ix - (nx - 1) * 0.5f) * spacing.z;
                return new Vector3(offsetX, offsetY, offsetZ);
            }
            case LayoutMode.Circle:
            {
                float angle = (Mathf.PI * 2f / nx) * ix;
                return new Vector3(Mathf.Cos(angle) * circleRadius, ix * spacing.y, Mathf.Sin(angle) * circleRadius);
            }
            default:
                return Vector3.zero;
        }
    }

    private GameObject CreateChildObject(int ix, int iz)
    {
        GameObject go;
        if (targetPrefab != null)
        {
            go = Instantiate(targetPrefab);
        }
        else
        {
            go = GameObject.CreatePrimitive(primitiveType);
        }

        go.name = $"WaveItem_{ix}_{iz}";

        if (childMaterial != null)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = childMaterial;
        }

        if (addKinematicRigidbody)
        {
            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        return go;
    }

    void OnDrawGizmos()
    {
        if (!showSceneGizmos || members == null || members.Count == 0) return;

        Gizmos.color = gizmoColor;
        for (int i = 0; i < members.Count; i++)
        {
            MemberEntry m = members[i];
            if (m.target == null) continue;

            if (applyPosition && posAmplitude.sqrMagnitude > 0.0001f)
            {
                Vector3 centerWorld = transform.TransformPoint(m.baseLocalPosition);
                Vector3 ampWorld = transform.TransformVector(posAmplitude);
                Gizmos.DrawLine(centerWorld - ampWorld, centerWorld + ampWorld);
            }
        }
    }
}
