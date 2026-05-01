How to run the project? :-

Project setup
1. Clone the repository.
2. Open project in Unity.
3. In Project panel, right click → Create → Config → API Keys.
4. Click the created asset, fill in API keys for Gemini and OpenAI in Inspector (an "APIKeys asset is created in this step").
5. Select the character game object in hierarchy.
6. Drag the APIKeys asset into the "Api Keys" field on both GeminiClient" and "OpenAIClient" components in the Inspector.
7. Expand 'Male_AvatarARkit' in hierarchy, drag 'Face' child object into the "Face" field on the "AUtoARKitMapper" component in Inspector.

Running the system

1. Press Play.
2. Starts as manual mode, where blendshapes can be manually deformed by pressing spacebar key. Press Tab to switch to AI mode.
3. Type a situation in the Test Situation field in Inspector.
4. Press Enter to trigger expression.
5. Use Left/Right arrows to switch between Gemini and GPT model.
6. Toggle Enable Gaze, Enable Unilateral, Use Explicit Prompt in Inspector to change prompt condition.

Prompts used for animation generation are listed in 'GeminiClient.cs', 'EvaluationRunner.cs' contains the situation and explicit prompts passed to animation generation LLM for each of the 7 emotions during evaluation, 'evaluate\_images\_gemini2.5pro.py' and 'evaluate\_images\_gemini3flash.py' contain the prompt used by vision LLM to classify the images into one of the seven emotions.

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

- 5. 'OpenAIClient.cs': Mirrors 'GeminiClient.cs' for GPT-5-mini via the OpenAI Chat Completions API. Uses the same eight prompt strings defined in GeminiClient, with `activePrompt` set externally by 'AUtoBlendshape.cs' or 'EvaluationRunner.cs'. API key loaded from APIKeys ScriptableObject and passed as a Bearer token in the Authorization header.`ParseResponse()` handles two possible OpenAI response formats — standard Chat Completions (`choices[0].message.content`) and the Responses API format (`output[].content[].text`) — falling back between them to ensure content is extracted regardless of which format is returned. Extracted text is passed to `FreeFormParser.Parse()`.

- 6. 'FreeFormParser.cs': Static parser that extracts AnimationData from free-form LLM text responses. Handles both natural language and JSON-like output formats without enforcing a strict schema.
AU extraction uses two regex patterns — Pattern 1 matches AU codes with optional _L/_R suffixes followed by intensity values in any common format (colon, equals, brackets, space). Pattern 2 handles JSON-style "au": "AU12_R", "intensity": 0.8 objects. Both patterns validate against a whitelist of valid AU codes and deduplicate using a seen HashSet.
Gaze extraction searches the full response text for valid direction keywords, checking longer compound directions (away_left, down_right etc.) before shorter ones to avoid partial matches. Also handles JSON-style "gaze_target": "down" as a fallback. Defaults to "forward" if no gaze direction is found.

- 7. 'EvaluationRunner.cs': Script used for automated dataset collection. Press E in Play mode to start. Iterates over all 560 combinations — 7 emotions × 4 conditions × 4 model-prompt combos × 5 iterations — triggering an LLM animation call for each and capturing a screenshot at the apex of the expression arc via the onApexReached callback. Screenshots are saved to Desktop/Evaluation_Run/ with filenames encoding model, prompt type, condition, emotion, and iteration number for easy parsing during analysis.
For each condition, sets enableGaze and enableUnilateral flags on 'AUtoBlendshape.cs' and assigns the correct prompt string to both 'GeminiClient.cs' and 'OpenAIClient.cs'. Includes an arc timeout (default 30s) to prevent the loop from stalling if an API call fails. Restores default flag values on completion. The 'Evaluation_Run' folder and all subfolders are created automatically on the Desktop when EvaluationRunner starts. No manual folder setup is required.
------------------------------------------------------------------------------------------------------------------------------------------------

Codes used for evaluation are located in "Evaluation" folder. The summary of each of the files is as follows:

- 1. 'evaluate\_images\_gemini2.5pro.py'

Automated LLM-as-judge classifier. Walks the `Evaluation_Run` folder recursively, finds all PNG images, and classifies each using Gemini 2.5 Pro. For each image, sends the classification prompt asking the model to identify the Ekman emotion and provide a confidence score (1-5). Parses the filename to extract intended emotion, model, prompt type, condition, and iteration. Compares predicted emotion against intended emotion and assigns a binary correct/incorrect label. Results are saved incrementally to a CSV file after each image to prevent data loss on interruption. A configurable delay between API calls prevents rate limiting. Was used three times in evaluation to generate three csv files.

- 2. 'evaluate\_images\_gemini3flash.py'

Identical in structure to `evaluate_images_gemini2.5pro.py` but uses Gemini 3 Flash Preview as the classifier. API delay is set to 0 since Gemini 3 Flash has higher rate limits. Output is saved to a separately named CSV file. Was used three times in evaluation to generate three csv files.

- 3. 'generate\_table.py'

Took the 6 evaluation CSV files (3 runs each for Gemini 2.5 Pro and Gemini 3 Flash) and generates accuracy tables averaged across runs. Maps each prompt type and condition combination to P1-P8 labels, computes mean accuracy per emotion per prompt condition, and adds row and column averages. Outputs two tables — one per classifier — each saved as both a CSV and a color-coded PNG figure to `Desktop/Evaluation_Run/Tables/`.

- 4. 'misclassification\_analysis.py'

Generates confusion matrices for Gemini 2.5 Pro and Gemini 3 Flash classifiers. Loads and averages all 3 runs per classifier, counts how many times each intended emotion was classified as each predicted emotion, and converts counts to row percentages. Prints a terminal summary showing correct classification rates and top misclassifications per emotion. Saves a heatmap confusion matrix PNG for each classifier to `Desktop/Evaluation_Run/Misclassification/`.

- 5. **user\_study\_confusion.py**

Generates confusion matrices from the user study summary CSV. Reads pre-computed classification percentages per emotion pair and condition, builds a 7×7 confusion matrix for each condition (baseline and full features), and saves a heatmap PNG for each to `Desktop/Evaluation_Run/Misclassification/`. Also prints a terminal summary of top misclassifications per emotion per condition.
-------------------------------------------------------------------------------------------------------------------------------------------------------