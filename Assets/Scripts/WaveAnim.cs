using UnityEngine;

/// <summary>
/// Single-object Lissajous / wave animation script (Local transform based).
/// Intended as a teaching reference alongside WaveGroupAnimator.
/// </summary>
public class WaveAnim : MonoBehaviour
{
    [Header("Position")]
    public bool applyPosition = true;
    public Vector3 amplitude = new Vector3(0f, 1f, 0f);
    public Vector3 frequency = new Vector3(1f, 1f, 1f);
    public Vector3 phaseDeg = Vector3.zero;

    [Header("Rotation (Euler Degrees)")]
    public bool applyRotation = false;
    public Vector3 rotAmplitude = Vector3.zero;
    public Vector3 rotFrequency = new Vector3(1f, 1f, 1f);
    public Vector3 rotPhaseDeg = Vector3.zero;

    [Header("Scale")]
    public bool applyScale = false;
    public Vector3 scaleAmplitude = Vector3.zero;
    public Vector3 scaleFrequency = new Vector3(1f, 1f, 1f);
    public Vector3 scalePhaseDeg = Vector3.zero;

    [Header("Speed")]
    [Range(-5f, 5f)]
    public float speed = 1f;

    private Vector3 initialLocalPos;
    private Vector3 initialLocalEuler;
    private Vector3 initialLocalScale;

    void Start()
    {
        initialLocalPos = transform.localPosition;
        initialLocalEuler = transform.localEulerAngles;
        initialLocalScale = transform.localScale;
    }

    void Update()
    {
        float t = Time.time * speed;

        if (applyPosition)
        {
            transform.localPosition = initialLocalPos + EvaluateWave(t, amplitude, frequency, phaseDeg);
        }

        if (applyRotation)
        {
            transform.localEulerAngles = initialLocalEuler + EvaluateWave(t, rotAmplitude, rotFrequency, rotPhaseDeg);
        }

        if (applyScale)
        {
            transform.localScale = initialLocalScale + EvaluateWave(t, scaleAmplitude, scaleFrequency, scalePhaseDeg);
        }
    }

    public static Vector3 EvaluateWave(float t, Vector3 amp, Vector3 freq, Vector3 phase)
    {
        return new Vector3(
            amp.x * Mathf.Sin(t * freq.x * Mathf.PI * 2f + phase.x * Mathf.Deg2Rad),
            amp.y * Mathf.Sin(t * freq.y * Mathf.PI * 2f + phase.y * Mathf.Deg2Rad),
            amp.z * Mathf.Sin(t * freq.z * Mathf.PI * 2f + phase.z * Mathf.Deg2Rad)
        );
    }
}
