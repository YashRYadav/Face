This is a research project on AI-driven facial animation system, where a situation is provided to the system, the AI assesses the emotion in the situation, and animates the 3d character's facial expression according to the context. It supports context-appropriate gaze movement, and unilateral expression control (as it is important to represent 'contempt' emotion, as stated in Ekman's Theory.


How to run the project? :-

Project setup
1. Clone the repository.
2. Open project in Unity.
3. In Project panel, right click → Create → Config → API Keys.
4. Click the created asset, fill in API key for Gemini in Inspector (an "APIKeys asset is created in this step").
5. Select the character game object in hierarchy.
6. Drag the APIKey asset into the "Api Keys" field on "GeminiClient" component in the Inspector.
7. Expand 'Male_AvatarARkit' in hierarchy, drag 'Face' child object into the "Face" field on the "AUtoARKitMapper" component in Inspector.

Running the system

1. Press Play.
2. Starts as manual mode, where blendshapes can be manually deformed by pressing spacebar key. Press Tab to switch to AI mode.
3. Type a situation in the Test Situation field in Inspector.
4. Press Enter to trigger expression.
5. Use Left/Right arrows to switch between Gemini and GPT model.
6. Toggle Enable Gaze, Enable Unilateral, Use Explicit Prompt in Inspector to change prompt condition.

Prompts used for animation generation are listed in 'GeminiClient.cs', 'EvaluationRunner.cs' contains the situation prompts passed to animation generation LLM for each of the 7 emotions during evaluation.

Results are stored in 'Results' folder.

------------------------------------------------------------------------------------------------------------------------------------------------
The C# scripts used for the creation of project are located in the folder Assets > Scripts. The roles of those scripts are:

- 1. 'AUtoBlendshape.cs': Core system controller. Contains the FACS AU to ARKit blendshape mapping and per-blendshape weight limits to prevent mesh deformation. Supports two runtime modes toggled by Tab — manual mode (arrow keys cycle AUs, Space raises intensity, R resets face) and AI mode (Enter triggers LLM call, arrow keys switch between Gemini and GPT).
In AI mode, automatically selects one of eight prompt variants (P1-P8) based on three Inspector toggles: Enable Gaze, Enable Unilateral, and Use Explicit Prompt. On receiving AnimationData, plays a three-phase temporal arc — ease-in onset, apex hold (dynamically scaled by average AU intensity), and ease-out offset. Fires emotion-congruent gaze at apex via 'GazeController.cs' and returns to neutral at offset start. Supports unilateral expression control by routing _L/_R suffixed AUs to left or right blendshapes independently. Exposes onApexReached and onArcComplete callbacks for automated screenshot capture by 'EvaluationRunner.cs' file.

- 2. 'AnimationData.cs': Data container classes passed between components. `ActionUnit` stores a single AU code and its intensity value. `AnimationData` holds the full parsed LLM output — an array of ActionUnits, onset and offset durations (default -1f, signals fallback to Inspector values), and gaze target direction (default "forward").

- 3. 'GazeController.cs': Manages emotion-congruent eye gaze using the UniVRM10 LookAt system. Initializes with a two-frame delay to ensure VRM runtime is ready, then sets LookAt mode to manual YawPitch control. In LateUpdate, smoothly lerps current yaw and pitch toward target values and applies them to the VRM instance. Also patches the right eye pitch to match the left eye each frame to correct a VRM range map asymmetry.
Called by 'AUtoBlendshape.cs' at apex via ApplyGaze() which converts a gaze direction string (forward, down, up, left, right, away_left, away_right, down_left, down_right) to yaw and pitch angles using a parameterized lookup. Gaze returns to neutral via ReturnToNeutral() at the start of offset phase so face and eyes fade together. Yaw, pitch angles and smoothing speed are configurable in Inspector.

- 4. 'GeminiClient.cs': Handles API communication with Gemini 2.5 Flash. Contains all eight static prompt strings (P1-P8) covering every combination of situation/explicit input, unilateral ON/OFF, and gaze ON/OFF. The active prompt is set externally by AUtoARKitMapper or EvaluationRunner via `activePrompt`, defaulting to P1 if none is set.
`GetAnimation()` sends the situation string and active prompt to the Gemini API as a system instruction with user content, then passes the raw text response to `FreeFormParser.Parse()` which extracts AUs, laterality suffixes, and gaze direction. API key is loaded from the APIKeys ScriptableObject at runtime.

- 5. 'FreeFormParser.cs': Static parser that extracts AnimationData from free-form LLM text responses. Handles both natural language and JSON-like output formats without enforcing a strict schema.
AU extraction uses two regex patterns — Pattern 1 matches AU codes with optional _L/_R suffixes followed by intensity values in any common format (colon, equals, brackets, space). Pattern 2 handles JSON-style "au": "AU12_R", "intensity": 0.8 objects. Both patterns validate against a whitelist of valid AU codes and deduplicate using a seen HashSet.
Gaze extraction searches the full response text for valid direction keywords, checking longer compound directions (away_left, down_right etc.) before shorter ones to avoid partial matches. Also handles JSON-style "gaze_target": "down" as a fallback. Defaults to "forward" if no gaze direction is found.

------------------------------------------------------------------------------------------------------------------------------------------------

Codes used for evaluation are located in "Evaluation" folder. The summary of each of the files is as follows:

- 1. 'user\_study\_confusion.py'

Generates confusion matrices from the user study summary summary CSV. Reads pre-computed classification percentages per emotion pair and condition, builds a 7×7 confusion matrix for each condition (baseline and full features), and saves a heatmap PNG for each to `Desktop/Evaluation_Run/Misclassification/`. Also prints a terminal summary of top misclassifications per emotion per condition.
_________________________________________________
These were the situation prompts provided to the LLM for facial animation of expressions which were evaluated by user study of 45 participants:
        "The character sees someone kicking a stray animal on the street" - anger, 
        "The character listens to someone they consider inferior trying to give them advice" - contempt,
        "The character opens a container and finds it full of foul smelling rotting food" - disgust,
        "The character hears footsteps behind them in a dark empty alley at night" - fear,
        "The character is reunited with their childhood best friend after ten years apart" - happiness,
        "The character kneels beside a grave and looks down at the ground in grief" - sadness,
        "The character opens the door and everyone they know jumps out yelling surprise" - surprise.
__________________________________________________
The results received coincide with previous psychological research, and this research shows the potential of LLMs for 3d animation.
