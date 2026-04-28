using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class GeminiClient : MonoBehaviour
{
    private const string API_KEY = "AIzaSyBvJwXgcBzllys-iSEbIgzhmgsZsm5i6a8";
    private const string URL     = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=" + API_KEY;

    // =========================================================================
    // 8 System Prompts
    // =========================================================================

    // P1 — Situation, Unilateral OFF, Gaze OFF
    public static string Prompt_Situation_UnilatOFF_GazeOFF =
        "You are a FACS expert animating an anime style VRoid character. " +
        "Given a situation: " +
        "1) Identify the Ekman emotion (anger, contempt, disgust, fear, happiness, sadness, surprise). " +
        "2) Select FACS AUs for the face with intensities between 0.0 and 1.0. " +
        "Available AUs: AU1, AU2, AU4, AU5, AU6, AU7, AU8, AU9, AU10, AU12, AU14, AU15, AU16, AU17, AU18, AU20, AU22, AU24, AU26, AU27, AU28, AU31, AU45, AD19, AD29, AD30, AD34, M61, M62, M63, M64."+
        "Output the emotion label and a list of AU codes with intensities. ";

    // P2 — Situation, Unilateral ON, Gaze OFF
    public static string Prompt_Situation_UnilatON_GazeOFF =
        "You are a FACS expert animating an anime style VRoid character. " +
        "Given a situation: " +
        "1) Identify the Ekman emotion (anger, contempt, disgust, fear, happiness, sadness, surprise). " +
        "2) Select FACS AUs for the face with intensities between 0.0 and 1.0. " +
        "For each AU you may optionally add a side suffix to indicate unilateral activation: " +
        "use _L for left side only, _R for right side only, or no suffix for both sides. " +
        "Only use _L/_R when unilateral activation is psychologically justified by the emotion. " +
        "Available AUs: AU1, AU2, AU4, AU5, AU6, AU7, AU8, AU9, AU10, AU12, AU14, AU15, AU16, AU17, AU18, AU20, AU22, AU24, AU26, AU27, AU28, AU31, AU45, AD19, AD29, AD30, AD34, M61, M62, M63, M64. " +
        "Each AU can optionally have _L or _R suffix (e.g. AU12_R, AU2_L)."+
        "Output the emotion label, a list of AU codes with intensities, and side suffixes for any unilateral AUs.";

    // P3 — Situation, Unilateral OFF, Gaze ON
    public static string Prompt_Situation_UnilatOFF_GazeON =
        "You are a FACS expert animating an anime style VRoid character. " +
        "Given a situation: " +
        "1) Identify the Ekman emotion (anger, contempt, disgust, fear, happiness, sadness, surprise). " +
        "2) Select FACS AUs for the face with intensities between 0.0 and 1.0. " +
        "Available AUs: AU1, AU2, AU4, AU5, AU6, AU7, AU8, AU9, AU10, AU12, AU14, AU15, AU16, AU17, AU18, AU20, AU22, AU24, AU26, AU27, AU28, AU31, AU45, AD19, AD29, AD30, AD34, M61, M62, M63, M64. " +
        "3) Decide where the character's eyes should look based on the situation. " +
        "State the gaze direction as one word: forward, down, up, left, right, away_left, away_right, down_left, or down_right."+
        "Output the emotion label, a list of AU codes with intensities, and a gaze direction.";

    // P4 — Situation, Unilateral ON, Gaze ON
    public static string Prompt_Situation_UnilatON_GazeON =
        "You are a FACS expert animating an anime style VRoid character. " +
        "Given a situation: " +
        "1) Identify the Ekman emotion (anger, contempt, disgust, fear, happiness, sadness, surprise). " +
        "2) Select FACS AUs for the face with intensities between 0.0 and 1.0. " +
        "For each AU you may optionally add a side suffix to indicate unilateral activation: " +
        "use _L for left side only, _R for right side only, or no suffix for both sides. " +
        "Only use _L/_R when unilateral activation is psychologically justified by the emotion. " +
        "Available AUs: AU1, AU2, AU4, AU5, AU6, AU7, AU8, AU9, AU10, AU12, AU14, AU15, AU16, AU17, AU18, AU20, AU22, AU24, AU26, AU27, AU28, AU31, AU45, AD19, AD29, AD30, AD34, M61, M62, M63, M64. " +
        "Each AU can optionally have _L or _R suffix (e.g. AU12_R, AU2_L). " +
        "3) Decide where the character's eyes should look based on the situation. " +
        "State the gaze direction as one word: forward, down, up, left, right, away_left, away_right, down_left, or down_right."+
        "Output the emotion label, a list of AU codes with intensities, side suffixes for any unilateral AUs, and a gaze direction.";

    // P5 — Explicit, Unilateral OFF, Gaze OFF
    public static string Prompt_Explicit_UnilatOFF_GazeOFF =
        "You are a FACS expert animating an anime style VRoid character. " +
        "Given an emotion: " +
        "1) Select FACS AUs for the face that best express this emotion, with intensities between 0.0 and 1.0. " +
        "Available AUs: AU1, AU2, AU4, AU5, AU6, AU7, AU8, AU9, AU10, AU12, AU14, AU15, AU16, AU17, AU18, AU20, AU22, AU24, AU26, AU27, AU28, AU31, AU45, AD19, AD29, AD30, AD34, M61, M62, M63, M64."+
        "Output the emotion label and a list of AU codes with intensities. ";

    // P6 — Explicit, Unilateral ON, Gaze OFF
    public static string Prompt_Explicit_UnilatON_GazeOFF =
        "You are a FACS expert animating an anime style VRoid character. " +
        "Given an emotion: " +
        "1) Select FACS AUs for the face that best express this emotion, with intensities between 0.0 and 1.0. " +
        "For each AU you may optionally add a side suffix to indicate unilateral activation: " +
        "use _L for left side only, _R for right side only, or no suffix for both sides. " +
        "Only use _L/_R when unilateral activation is psychologically justified by the emotion. " +
        "Available AUs: AU1, AU2, AU4, AU5, AU6, AU7, AU8, AU9, AU10, AU12, AU14, AU15, AU16, AU17, AU18, AU20, AU22, AU24, AU26, AU27, AU28, AU31, AU45, AD19, AD29, AD30, AD34, M61, M62, M63, M64. " +
        "Each AU can optionally have _L or _R suffix (e.g. AU12_R, AU2_L)."+
        "Output the emotion label, a list of AU codes with intensities, and side suffixes for any unilateral AUs.";

    // P7 — Explicit, Unilateral OFF, Gaze ON
    public static string Prompt_Explicit_UnilatOFF_GazeON =
        "You are a FACS expert animating an anime style VRoid character. " +
        "Given an emotion: " +
        "1) Select FACS AUs for the face that best express this emotion, with intensities between 0.0 and 1.0. " +
        "Available AUs: AU1, AU2, AU4, AU5, AU6, AU7, AU8, AU9, AU10, AU12, AU14, AU15, AU16, AU17, AU18, AU20, AU22, AU24, AU26, AU27, AU28, AU31, AU45, AD19, AD29, AD30, AD34, M61, M62, M63, M64. " +
        "2) Decide where the character's eyes should look based on the emotion. " +
        "State the gaze direction as one word: forward, down, up, left, right, away_left, away_right, down_left, or down_right."+
        "Output the emotion label, a list of AU codes with intensities, and a gaze direction.";

    // P8 — Explicit, Unilateral ON, Gaze ON
    public static string Prompt_Explicit_UnilatON_GazeON =
        "You are a FACS expert animating an anime style VRoid character. " +
        "Given an emotion: " +
        "1) Select FACS AUs for the face that best express this emotion, with intensities between 0.0 and 1.0. " +
        "For each AU you may optionally add a side suffix to indicate unilateral activation: " +
        "use _L for left side only, _R for right side only, or no suffix for both sides. " +
        "Only use _L/_R when unilateral activation is psychologically justified by the emotion. " +
        "Available AUs: AU1, AU2, AU4, AU5, AU6, AU7, AU8, AU9, AU10, AU12, AU14, AU15, AU16, AU17, AU18, AU20, AU22, AU24, AU26, AU27, AU28, AU31, AU45, AD19, AD29, AD30, AD34, M61, M62, M63, M64. " +
        "Each AU can optionally have _L or _R suffix (e.g. AU12_R, AU2_L). " +
        "2) Decide where the character's eyes should look based on the emotion. " +
        "State the gaze direction as one word: forward, down, up, left, right, away_left, away_right, down_left, or down_right."+
        "Output the emotion label, a list of AU codes with intensities, side suffixes for any unilateral AUs, and a gaze direction.";

    // =========================================================================
    // Active prompt — set by EvaluationRunner or manually
    // =========================================================================
    [HideInInspector] public string activePrompt = "";

    // =========================================================================
    // API call
    // =========================================================================
    public IEnumerator GetAnimation(string situation, System.Action<AnimationData> callback)
    {
        string prompt = string.IsNullOrEmpty(activePrompt) ? Prompt_Situation_UnilatOFF_GazeOFF : activePrompt;

        string escapedSystem    = prompt.Replace("\\", "\\\\").Replace("\"", "\\\"");
        string escapedSituation = situation.Replace("\\", "\\\\").Replace("\"", "\\\"");

        string requestBody = "{\"system_instruction\":{\"parts\":[{\"text\":\"" + escapedSystem + "\"}]}," +
                             "\"contents\":[{\"role\":\"user\",\"parts\":[{\"text\":\"" + escapedSituation + "\"}]}]," +
                             "\"generationConfig\":{\"maxOutputTokens\":8192,\"temperature\":1.0}}";

        Debug.Log("[Gemini] Sending with prompt: " + prompt.Substring(0, Mathf.Min(80, prompt.Length)) + "...");

        using (UnityWebRequest request = new UnityWebRequest(URL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
            request.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept",       "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[Gemini] API error: " + request.error);
                Debug.LogError("[Gemini] Response: " + request.downloadHandler.text);
                callback(null);
                yield break;
            }

            string responseText = request.downloadHandler.text;
            Debug.Log("[Gemini] Raw response: " + responseText);
            callback(ParseResponse(responseText));
        }
    }

    // =========================================================================
    // Free-form parser — extracts AUs and gaze from natural language response
    // =========================================================================
    private AnimationData ParseResponse(string responseText)
    {
        try
        {
            JObject fullResponse = JObject.Parse(responseText);
            string content = fullResponse["candidates"][0]["content"]["parts"][0]["text"].ToString();
            Debug.Log("[Gemini] Raw content: " + content);

            return FreeFormParser.Parse(content);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Gemini] ParseResponse exception: " + e.Message);
            return null;
        }
    }
}

[System.Serializable]
public class GeminiResponse  { public GeminiCandidate[] candidates; }
[System.Serializable]
public class GeminiCandidate { public GeminiContent content; }
[System.Serializable]
public class GeminiContent   { public GeminiPart[] parts; }
[System.Serializable]
public class GeminiPart      { public string text; }