using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AUtoARKitMapper : MonoBehaviour
{
    public SkinnedMeshRenderer face;

    public float riseSpeed = 2f;
    public float fallSpeed = 2f;

    private Coroutine transitionCoroutine;

    // ===== ARKit limits =====
    Dictionary<string, float> arkitLimits = new Dictionary<string, float>()
    {
        {"eyeBlinkLeft",    51f},
        {"eyeBlinkRight",   51f},
        {"browInnerUp",     100f},
        {"browOuterUpLeft", 40f},
        {"browOuterUpRight",40f},
        {"eyeSquintLeft",   100f},
        {"eyeSquintRight",  100f},
        {"browDownLeft",    31f},
        {"browDownRight",   31f},
        {"noseSneerLeft",   55f},
        {"noseSneerRight",  55f},
        {"mouthLeft",       54f},
        {"mouthRight",      54f},
        {"mouthRollUpper",  14f},
        {"mouthShrugLower", 35f},
        {"mouthClose",      10f}
    };

    // ===== FACS → ARKit =====
    Dictionary<string, string[]> facsToARKit = new Dictionary<string, string[]>()
    {
        {"AU45", new[]{"eyeBlinkLeft","eyeBlinkRight"}},
        {"M64",  new[]{"eyeLookDownLeft","eyeLookDownRight"}},
        {"M62",  new[]{"eyeLookInLeft","eyeLookOutRight"}},
        {"M61",  new[]{"eyeLookInRight","eyeLookOutLeft"}},
        {"M63",  new[]{"eyeLookUpLeft","eyeLookUpRight"}},
        {"AU7",  new[]{"eyeSquintLeft","eyeSquintRight"}},
        {"AU5",  new[]{"eyeWideLeft","eyeWideRight"}},

        {"AD29", new[]{"jawForward"}},
        {"AD30", new[]{"jawLeft","jawRight"}},
        {"AU26", new[]{"jawOpen"}},
        {"AU27", new[]{"jawOpen"}},

        {"AU8",  new[]{"mouthClose"}},
        {"AU22", new[]{"mouthFunnel"}},
        {"AU18", new[]{"mouthPucker"}},
        {"AD31", new[]{"mouthRight","mouthLeft"}},
        {"AU12", new[]{"mouthSmileLeft","mouthSmileRight"}},
        {"AU15", new[]{"mouthFrownLeft","mouthFrownRight"}},
        {"AU14", new[]{"mouthDimpleLeft","mouthDimpleRight"}},
        {"AU20", new[]{"mouthStretchLeft","mouthStretchRight"}},
        {"AU28", new[]{"mouthRollLower","mouthRollUpper"}},
        {"AU17", new[]{"mouthShrugLower","mouthShrugUpper"}},
        {"AU24", new[]{"mouthPressLeft","mouthPressRight"}},
        {"AU16", new[]{"mouthLowerDownLeft","mouthLowerDownRight"}},
        {"AU10", new[]{"mouthUpperUpLeft","mouthUpperUpRight"}},

        {"AU4",  new[]{"browDownLeft","browDownRight"}},
        {"AU1",  new[]{"browInnerUp"}},
        {"AU2",  new[]{"browOuterUpLeft","browOuterUpRight"}},
        {"AD34", new[]{"cheekPuff"}},
        {"AU6",  new[]{"cheekSquintLeft","cheekSquintRight"}},
        {"AU9",  new[]{"noseSneerLeft","noseSneerRight"}},
        {"AD19", new[]{"tongueOut"}}
    };

    List<string> facsList      = new List<string>();
    int          currentIndex  = 0;
    float        currentIntensity = 0f;

    // ===== AI mode =====
    private GeminiClient   geminiClient;
    private OpenAIClient   openAIClient;
    private GazeController gazeController;
    private bool aiMode = false;

    // =========================================================================
    // Model Switcher
    // =========================================================================
    public enum AIModel { Gemini, GPT }
    public AIModel currentModel = AIModel.Gemini;

    [Header("AI Settings")]
    public string testSituation = "The character is trying to hold back tears.";

    [Tooltip("When checked: uses explicit emotion prompt. When unchecked: uses situation-based prompt.")]
    public bool useExplicitPrompt = false;

    // =========================================================================
    // Gaze Co-articulation
    // =========================================================================
    [Header("Gaze Co-articulation")]
    public bool enableGaze = true;

    // =========================================================================
    // Temporal Emotion Arc
    // =========================================================================
    [Header("Temporal Arc")]
    public float macroOnset  = 0.30f;
    public float macroApex   = 1.00f;
    public float macroOffset = 0.30f;

    [Header("Apex Hold Scaling")]
    [Tooltip("Apex hold multiplied by this at avg AU intensity = 1.0.")]
    public float apexIntensityScale = 1.5f;

    // =========================================================================
    // Unilateral Expressions
    // =========================================================================
    [Header("Unilateral Expressions")]
    [Tooltip("When checked: _L/_R suffixes drive left/right blendshapes independently.")]
    public bool enableUnilateral = true;

    // =========================================================================
    // Evaluation callbacks
    // =========================================================================
    public System.Action onApexReached;
    public System.Action onArcComplete;

    void Start()
    {
        foreach (var key in facsToARKit.Keys)
            facsList.Add(key);

        geminiClient   = GetComponentInChildren<GeminiClient>();
        openAIClient   = GetComponentInChildren<OpenAIClient>();
        gazeController = GetComponent<GazeController>();

        if (gazeController == null)
            Debug.LogWarning("[AUtoARKitMapper] GazeController not found — gaze disabled.");
        if (openAIClient == null)
            Debug.LogWarning("[AUtoARKitMapper] OpenAIClient not found — GPT unavailable.");

        Debug.Log("Current AU: " + facsList[currentIndex]);
        Debug.Log("TAB = AI/Manual mode. In AI mode: RETURN = trigger, LEFT/RIGHT = switch model.");
        Debug.Log("Active model: " + currentModel);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            aiMode = !aiMode;
            Debug.Log("Mode: " + (aiMode ? "AI" : "Manual") + "  |  Model: " + currentModel);
        }

        if (aiMode)
        {
            if (Input.GetKeyDown(KeyCode.Return))
                StartCoroutine(TriggerAIAnimation());

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                currentModel = (currentModel == AIModel.Gemini) ? AIModel.GPT : AIModel.Gemini;
                Debug.Log("Switched model → " + currentModel);
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                currentModel = (currentModel == AIModel.Gemini) ? AIModel.GPT : AIModel.Gemini;
                Debug.Log("Switched model → " + currentModel);
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                currentIndex = (currentIndex + 1) % facsList.Count;
                Debug.Log("Current AU: " + facsList[currentIndex]);
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                currentIndex--;
                if (currentIndex < 0) currentIndex = facsList.Count - 1;
                Debug.Log("Current AU: " + facsList[currentIndex]);
            }

            if (Input.GetKey(KeyCode.Space))
                currentIntensity += riseSpeed * Time.deltaTime;
            else
                currentIntensity -= fallSpeed * Time.deltaTime;

            currentIntensity = Mathf.Clamp01(currentIntensity);
            ApplyFACS(facsList[currentIndex], currentIntensity);
        }

        if (Input.GetKeyDown(KeyCode.R))
            ResetFace();
    }

    // ===== AI trigger =====
    public IEnumerator TriggerAIAnimation()
    {
        Debug.Log($"[{currentModel}] Calling for: " + testSituation);

        System.Action<AnimationData> handler = (animData) =>
        {
            if (animData == null)
            {
                Debug.LogError($"[{currentModel}] returned null — check API key and Console");
                return;
            }

            ApplyAnimationData(animData);

            Debug.Log($"[{currentModel}] applied " + animData.aus.Length + " AUs  |  "
                + "Unilateral: " + (enableUnilateral ? "ON" : "OFF")
                + "  |  Gaze: " + animData.gazeTarget);
        };

        // ── Select correct prompt based on gaze/unilateral/explicit state ─────
        bool gazeOn     = enableGaze && gazeController != null;
        bool unilateralOn = enableUnilateral;
        bool explicitOn = useExplicitPrompt;

        string selectedPrompt;
        string promptName;

        if (!gazeOn && !unilateralOn)
        {
            selectedPrompt = explicitOn ? GeminiClient.Prompt_Explicit_UnilatOFF_GazeOFF
                                        : GeminiClient.Prompt_Situation_UnilatOFF_GazeOFF;
            promptName     = explicitOn ? "P5 (Explicit, NoGaze, NoUnilat)" : "P1 (Situation, NoGaze, NoUnilat)";
        }
        else if (!gazeOn && unilateralOn)
        {
            selectedPrompt = explicitOn ? GeminiClient.Prompt_Explicit_UnilatON_GazeOFF
                                        : GeminiClient.Prompt_Situation_UnilatON_GazeOFF;
            promptName     = explicitOn ? "P6 (Explicit, NoGaze, Unilat)" : "P2 (Situation, NoGaze, Unilat)";
        }
        else if (gazeOn && !unilateralOn)
        {
            selectedPrompt = explicitOn ? GeminiClient.Prompt_Explicit_UnilatOFF_GazeON
                                        : GeminiClient.Prompt_Situation_UnilatOFF_GazeON;
            promptName     = explicitOn ? "P7 (Explicit, Gaze, NoUnilat)" : "P3 (Situation, Gaze, NoUnilat)";
        }
        else
        {
            selectedPrompt = explicitOn ? GeminiClient.Prompt_Explicit_UnilatON_GazeON
                                        : GeminiClient.Prompt_Situation_UnilatON_GazeON;
            promptName     = explicitOn ? "P8 (Explicit, Gaze, Unilat)" : "P4 (Situation, Gaze, Unilat)";
        }

        if (geminiClient != null) geminiClient.activePrompt = selectedPrompt;
        if (openAIClient != null) openAIClient.activePrompt = selectedPrompt;

        Debug.Log($"[Prompt] Using: {promptName}");

        // ── Route to selected model ───────────────────────────────────────────
        if (currentModel == AIModel.Gemini)
            yield return StartCoroutine(geminiClient.GetAnimation(testSituation, handler));
        else
        {
            if (openAIClient != null)
                yield return StartCoroutine(openAIClient.GetAnimation(testSituation, handler));
            else
                Debug.LogError("[GPT] OpenAIClient not found.");
        }
    }

    // ===== Apply AI data =====
    public void ApplyAnimationData(AnimationData data)
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(PlayEmotionArc(data));
    }

    // =========================================================================
    // PlayEmotionArc
    // =========================================================================
    IEnumerator PlayEmotionArc(AnimationData data)
    {
        int     count         = face.sharedMesh.blendShapeCount;
        float[] startWeights  = new float[count];
        float[] targetWeights = new float[count];
        float[] zeroWeights   = new float[count];

        for (int i = 0; i < count; i++)
            startWeights[i] = face.GetBlendShapeWeight(i);

        float intensitySum   = 0f;
        int   intensityCount = 0;

        foreach (var au in data.aus)
        {
            string auCode = au.au;
            string side   = "both";

            if (auCode.EndsWith("_L"))
            {
                side   = "L";
                auCode = auCode.Substring(0, auCode.Length - 2);
            }
            else if (auCode.EndsWith("_R"))
            {
                side   = "R";
                auCode = auCode.Substring(0, auCode.Length - 2);
            }

            if (!enableUnilateral) side = "both";

            if (!facsToARKit.ContainsKey(auCode))
            {
                Debug.LogWarning("AU not in mapping: " + au.au);
                continue;
            }

            foreach (string shape in facsToARKit[auCode])
            {
                if (enableUnilateral && side != "both")
                {
                    bool isLeft  = shape.EndsWith("Left")  || shape.EndsWith("left");
                    bool isRight = shape.EndsWith("Right") || shape.EndsWith("right");
                    if (isLeft  && side == "R") continue;
                    if (isRight && side == "L") continue;
                }

                int idx = face.sharedMesh.GetBlendShapeIndex(shape);
                if (idx < 0) continue;

                float weight = au.intensity * 100f;
                if (arkitLimits.ContainsKey(shape))
                    weight = Mathf.Min(weight, arkitLimits[shape]);

                targetWeights[idx] = weight;
                Debug.Log($"AI: {au.au} → {shape} ({weight:F1})" +
                          (side != "both" ? $" [{side} side only]" : ""));
            }

            intensitySum += au.intensity;
            intensityCount++;
        }

        float avgIntensity   = intensityCount > 0 ? intensitySum / intensityCount : 0.5f;
        float onsetDuration  = (data.onset  > 0f) ? data.onset  : macroOnset;
        float offsetDuration = (data.offset > 0f) ? data.offset : macroOffset;
        float apexDuration   = macroApex * Mathf.Lerp(1f, apexIntensityScale, avgIntensity);

        Debug.Log($"[Arc] onset={onsetDuration:F2}s  apex={apexDuration:F2}s  " +
                  $"offset={offsetDuration:F2}s  avgIntensity={avgIntensity:F2}");

        // Phase 1 — Onset
        yield return StartCoroutine(ArcLerpWeights(startWeights, targetWeights, onsetDuration, ArcEaseInQuad));
        ArcSetWeights(targetWeights);
        onApexReached?.Invoke();

        // Gaze fires at apex
        if (enableGaze && gazeController != null)
        {
            gazeController.ApplyGaze(data.gazeTarget, data.gazeHold);
            Debug.Log($"[Arc] Gaze fired → {data.gazeTarget}");
        }

        // Phase 2 — Apex hold
        yield return new WaitForSeconds(apexDuration);

        // Phase 3 — Offset — gaze returns with face
        if (enableGaze && gazeController != null)
            gazeController.ReturnToNeutral();

        yield return StartCoroutine(ArcLerpWeights(targetWeights, zeroWeights, offsetDuration, ArcEaseOutCubic));
        ArcSetWeights(zeroWeights);

        onArcComplete?.Invoke();
        Debug.Log("[Arc] Complete — face and gaze returned to neutral");
    }

    IEnumerator ArcLerpWeights(float[] from, float[] to, float duration,
                                System.Func<float, float> easing)
    {
        if (duration <= 0f) { ArcSetWeights(to); yield break; }

        float elapsed = 0f;
        int   count   = face.sharedMesh.blendShapeCount;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float easedT = easing(Mathf.Clamp01(elapsed / duration));
            for (int i = 0; i < count; i++)
                face.SetBlendShapeWeight(i, Mathf.Lerp(from[i], to[i], easedT));
            yield return null;
        }
    }

    void ArcSetWeights(float[] weights)
    {
        for (int i = 0; i < weights.Length; i++)
            face.SetBlendShapeWeight(i, weights[i]);
    }

    static float ArcEaseInQuad(float t)   => t * t;
    static float ArcEaseOutCubic(float t) { float f = t - 1f; return f * f * f + 1f; }

    void ApplyFACS(string facsCode, float intensity)
    {
        foreach (string shape in facsToARKit[facsCode])
        {
            int index = face.sharedMesh.GetBlendShapeIndex(shape);
            if (index >= 0)
            {
                float weight = intensity * 100f;
                if (arkitLimits.ContainsKey(shape))
                    weight = Mathf.Min(weight, arkitLimits[shape]);
                face.SetBlendShapeWeight(index, weight);
                if (intensity > 0.01f)
                    Debug.Log(facsCode + " → " + shape + " (" + weight + ")");
            }
        }
    }

    void ResetFace()
    {
        for (int i = 0; i < face.sharedMesh.blendShapeCount; i++)
            face.SetBlendShapeWeight(i, 0);
        currentIntensity = 0f;
        Debug.Log("Face Reset");
    }
}