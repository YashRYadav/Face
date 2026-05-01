using UnityEngine;
using System.Collections;
using UniVRM10;

/// <summary>
/// GazeController — manages eye gaze direction based on parsed LLM output.
/// Uses VRM yaw/pitch system to drive gaze, then patches right eye pitch
/// to match left eye for symmetry (fixes VRM range map imbalance).
///
/// Called by AUtoARKitMapper.PlayEmotionArc() at apex phase.
/// Gaze returns to neutral when AUtoARKitMapper calls ReturnToNeutral()
/// at the start of offset phase — so face and gaze fade together.
/// </summary>
public class GazeController : MonoBehaviour
{
    [Header("Gaze Angles (degrees)")]
    [Tooltip("How many degrees eyes rotate left/right for lateral gaze")]
    public float yawAngle   = 25f;

    [Tooltip("How many degrees eyes rotate up/down for vertical gaze")]
    public float pitchAngle = 20f;

    [Tooltip("How smoothly gaze transitions — lerp speed per second")]
    public float gazeSmoothing = 6f;

    private float currentYaw   = 0f;
    private float currentPitch = 0f;
    private float targetYaw    = 0f;
    private float targetPitch  = 0f;

    private Vrm10Instance vrmInstance;
    private bool          initialized = false;

    private Transform leftEyeBone;
    private Transform rightEyeBone;
    private bool      syncEyes = false;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    void Start()
    {
        vrmInstance = GetComponent<Vrm10Instance>();

        if (vrmInstance == null)
        {
            Debug.LogError("[GazeController] Vrm10Instance not found on " + gameObject.name);
            enabled = false;
            return;
        }

        StartCoroutine(InitDelayed());
    }

    IEnumerator InitDelayed()
    {
        yield return null;
        yield return null;

        vrmInstance.LookAtTargetType = VRM10ObjectLookAt.LookAtTargetTypes.YawPitchValue;
        vrmInstance.Runtime.LookAt.SetYawPitchManually(0f, 0f);

        var animator = GetComponent<Animator>();
        if (animator != null)
        {
            leftEyeBone  = animator.GetBoneTransform(HumanBodyBones.LeftEye);
            rightEyeBone = animator.GetBoneTransform(HumanBodyBones.RightEye);
            syncEyes     = (leftEyeBone != null && rightEyeBone != null);
        }

        initialized = true;
        Debug.Log("[GazeController] Initialized.");
    }

    void LateUpdate()
    {
        if (!initialized) return;

        currentYaw   = Mathf.Lerp(currentYaw,   targetYaw,   Time.deltaTime * gazeSmoothing);
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * gazeSmoothing);

        vrmInstance.Runtime.LookAt.SetYawPitchManually(currentYaw, currentPitch);

        if (syncEyes)
        {
            Vector3 leftRot  = leftEyeBone.localEulerAngles;
            Vector3 rightRot = rightEyeBone.localEulerAngles;
            rightEyeBone.localEulerAngles = new Vector3(leftRot.x, rightRot.y, rightRot.z);
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Called by AUtoARKitMapper at apex — sets gaze direction.
    /// </summary>
    public void ApplyGaze(string gazeDirection, float holdDuration)
    {
        if (!initialized) return;
        (targetYaw, targetPitch) = GetYawPitch(gazeDirection);
    }

    /// <summary>
    /// Called by AUtoARKitMapper at start of offset phase —
    /// gaze returns to neutral together with face fading.
    /// </summary>
    public void ReturnToNeutral()
    {
        targetYaw   = 0f;
        targetPitch = 0f;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    (float yaw, float pitch) GetYawPitch(string gazeDirection)
    {
        return gazeDirection switch
        {
            "forward"    => (  0f,               0f         ),
            "up"         => (  0f,              -pitchAngle ),
            "down"       => (  0f,               pitchAngle ),
            "left"       => ( -yawAngle,         0f         ),
            "right"      => (  yawAngle,         0f         ),
            "away_left"  => ( -yawAngle * 1.5f,  0f         ),
            "away_right" => (  yawAngle * 1.5f,  0f         ),
            "down_left"  => ( -yawAngle,         pitchAngle ),
            "down_right" => (  yawAngle,         pitchAngle ),
            _            => (  0f,               0f         )
        };
    }
}