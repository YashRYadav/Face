using UnityEngine;
using System.Collections;
using System.IO;

/// <summary>
/// EvaluationRunner — Automated Dataset Collection
///
/// 560 screenshots: 7 emotions × 4 conditions × 4 combos × 5 iterations
///
/// Press E in Play mode to start.
/// </summary>
public class EvaluationRunner : MonoBehaviour
{
    [Header("References")]
    public AUtoARKitMapper faceMapper;
    public GazeController  gazeController;
    public GeminiClient    geminiClient;
    public OpenAIClient    openAIClient;

    [Header("Timing")]
    public float screenshotDelay    = 1.0f;
    public float betweenRunDelay    = 0.5f;
    public float betweenComboDelay  = 0.5f;
    public float arcTimeoutSeconds  = 30f;

    private bool   isRunning = false;
    private string basePath;

    // ── Emotion data ──────────────────────────────────────────────────────────

    string[] emotions = new[]
    {
        "anger", "contempt", "disgust", "fear", "happiness", "sadness", "surprise"
    };

    string[] situationPrompts = new[]
    {
        "The character sees someone kicking a stray animal on the street",
        "The character listens to someone they consider inferior trying to give them advice",
        "The character opens a container and finds it full of foul smelling rotting food",
        "The character hears footsteps behind them in a dark empty alley at night",
        "The character is reunited with their childhood best friend after ten years apart",
        "The character kneels beside a grave and looks down at the ground in grief",
        "The character opens the door and everyone they know jumps out yelling surprise"
    };

    string[] explicitPrompts = new[]
    {
        "Express the emotion: anger",
        "Express the emotion: contempt",
        "Express the emotion: disgust",
        "Express the emotion: fear",
        "Express the emotion: happiness",
        "Express the emotion: sadness",
        "Express the emotion: surprise"
    };

    // ── Condition definitions ─────────────────────────────────────────────────

    struct Condition
    {
        public string folderName;
        public string situationPromptText;
        public string explicitPromptText;
        public bool   gazeOn;
        public bool   unilateralOn;
    }

    Condition[] conditions;

    // ── Combo definitions ─────────────────────────────────────────────────────

    struct Combo
    {
        public string folderName;
        public string modelLabel;
        public string promptLabel;
        public AUtoARKitMapper.AIModel model;
        public bool   useSituation;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Start()
    {
        if (faceMapper    == null) faceMapper    = GetComponent<AUtoARKitMapper>();
        if (gazeController== null) gazeController= GetComponent<GazeController>();
        if (geminiClient  == null) geminiClient  = GetComponentInChildren<GeminiClient>();
        if (openAIClient  == null) openAIClient  = GetComponentInChildren<OpenAIClient>();

        conditions = new[]
        {
            new Condition
            {
                folderName          = "NoGaze_NoAsym",
                situationPromptText = GeminiClient.Prompt_Situation_UnilatOFF_GazeOFF,
                explicitPromptText  = GeminiClient.Prompt_Explicit_UnilatOFF_GazeOFF,
                gazeOn = false, unilateralOn = false
            },
            new Condition
            {
                folderName          = "Gaze_NoAsym",
                situationPromptText = GeminiClient.Prompt_Situation_UnilatOFF_GazeON,
                explicitPromptText  = GeminiClient.Prompt_Explicit_UnilatOFF_GazeON,
                gazeOn = true, unilateralOn = false
            },
            new Condition
            {
                folderName          = "NoGaze_Asym",
                situationPromptText = GeminiClient.Prompt_Situation_UnilatON_GazeOFF,
                explicitPromptText  = GeminiClient.Prompt_Explicit_UnilatON_GazeOFF,
                gazeOn = false, unilateralOn = true
            },
            new Condition
            {
                folderName          = "Gaze_Asym",
                situationPromptText = GeminiClient.Prompt_Situation_UnilatON_GazeON,
                explicitPromptText  = GeminiClient.Prompt_Explicit_UnilatON_GazeON,
                gazeOn = true, unilateralOn = true
            }
        };

        basePath = System.Environment.GetFolderPath(
                       System.Environment.SpecialFolder.Desktop)
                   + "/Evaluation_Run";

        Debug.Log("[Eval] Ready. Press E to start.");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && !isRunning)
            StartCoroutine(RunEvaluation());
    }

    // ── Main loop ─────────────────────────────────────────────────────────────

    IEnumerator RunEvaluation()
    {
        isRunning = true;
        int totalDone = 0;

        Debug.Log("[Eval] ===== EVALUATION STARTED =====");

        Combo[] combos = new[]
        {
            new Combo { folderName="Gemini_Situation", modelLabel="gemini", promptLabel="situation",
                        model=AUtoARKitMapper.AIModel.Gemini, useSituation=true  },
            new Combo { folderName="Gemini_Explicit",  modelLabel="gemini", promptLabel="explicit",
                        model=AUtoARKitMapper.AIModel.Gemini, useSituation=false },
            new Combo { folderName="GPT_Situation",    modelLabel="gpt",    promptLabel="situation",
                        model=AUtoARKitMapper.AIModel.GPT,    useSituation=true  },
            new Combo { folderName="GPT_Explicit",     modelLabel="gpt",    promptLabel="explicit",
                        model=AUtoARKitMapper.AIModel.GPT,    useSituation=false }
        };

        foreach (var condition in conditions)
        {
            Debug.Log($"[Eval] ══ Condition: {condition.folderName} ══");

            faceMapper.enableGaze      = condition.gazeOn;
            faceMapper.enableUnilateral = condition.unilateralOn;

            yield return new WaitForSeconds(betweenComboDelay);

            for (int eIdx = 0; eIdx < emotions.Length; eIdx++)
            {
                string emotion = emotions[eIdx];
                Debug.Log($"[Eval] Emotion: {emotion}");

                foreach (var combo in combos)
                {
                    Debug.Log($"[Eval] Combo: {combo.folderName}");

                    faceMapper.currentModel = combo.model;

                    faceMapper.testSituation = combo.useSituation
                        ? situationPrompts[eIdx]
                        : explicitPrompts[eIdx];

                    string activePrompt = combo.useSituation
                        ? condition.situationPromptText
                        : condition.explicitPromptText;

                    if (geminiClient != null) geminiClient.activePrompt = activePrompt;
                    if (openAIClient != null) openAIClient.activePrompt = activePrompt;

                    Debug.Log($"[Eval] Using prompt type: {combo.folderName} | {condition.folderName}");

                    string folderPath = Path.Combine(
                        basePath,
                        Capitalize(emotion),
                        condition.folderName,
                        combo.folderName
                    );
                    Directory.CreateDirectory(folderPath);

                    for (int iter = 1; iter <= 5; iter++)
                    {
                        Debug.Log($"[Eval] {Capitalize(emotion)} / {condition.folderName} / {combo.folderName} / Run {iter}");

                        bool screenshotTaken = false;
                        bool arcDone         = false;

                        faceMapper.onApexReached = () =>
                        {
                            if (!screenshotTaken)
                                StartCoroutine(TakeScreenshot(
                                    folderPath,
                                    combo.modelLabel,
                                    combo.promptLabel,
                                    condition.folderName,
                                    emotion,
                                    iter,
                                    () => screenshotTaken = true
                                ));
                        };

                        faceMapper.onArcComplete = () => { arcDone = true; };

                        yield return StartCoroutine(faceMapper.TriggerAIAnimation());

                        float waited = 0f;
                        while (!arcDone && waited < arcTimeoutSeconds)
                        {
                            waited += Time.deltaTime;
                            yield return null;
                        }

                        if (!arcDone)
                            Debug.LogWarning($"[Eval] Arc timeout for {emotion} run {iter}");
                        if (!screenshotTaken)
                            Debug.LogWarning($"[Eval] Screenshot not taken for {emotion} run {iter}");

                        faceMapper.onApexReached = null;
                        faceMapper.onArcComplete = null;

                        yield return new WaitForSeconds(betweenRunDelay);

                        totalDone++;
                        Debug.Log($"[Eval] Progress: {totalDone}/560");
                    }

                    yield return new WaitForSeconds(betweenComboDelay);
                }
            }
        }

        // Restore defaults
        faceMapper.enableGaze       = true;
        faceMapper.enableUnilateral = true;

        Debug.Log("[Eval] ===== EVALUATION COMPLETE =====");
        Debug.Log($"[Eval] {totalDone} images saved to: {basePath}");
        isRunning = false;
    }

    // ── Screenshot ────────────────────────────────────────────────────────────

    IEnumerator TakeScreenshot(string folderPath, string modelLabel, string promptLabel,
                                string conditionLabel, string emotion, int iteration,
                                System.Action onDone)
    {
        yield return new WaitForSeconds(screenshotDelay);
        yield return new WaitForEndOfFrame();

        string filename = $"{modelLabel}_{promptLabel}_{conditionLabel}_{emotion}_{iteration}.png";
        string fullPath = Path.Combine(folderPath, filename);

        ScreenCapture.CaptureScreenshot(fullPath);
        Debug.Log($"[Eval] Saved: {filename}");
        onDone?.Invoke();
    }

    string Capitalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpper(s[0]) + s.Substring(1);
    }
}