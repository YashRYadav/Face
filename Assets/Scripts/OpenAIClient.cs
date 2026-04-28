using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

/// <summary>
/// OpenAIClient — mirrors GeminiClient with same 8 prompts.
/// Uses GPT-5-mini via OpenAI Chat Completions API.
/// Free-form response parsed by FreeFormParser.
/// </summary>
public class OpenAIClient : MonoBehaviour
{
    private const string API_KEY = "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";
    private const string URL     = "https://api.openai.com/v1/chat/completions";
    private const string MODEL   = "gpt-5-mini";

    // Active prompt — set by EvaluationRunner
    [HideInInspector] public string activePrompt = "";

    public IEnumerator GetAnimation(string situation, System.Action<AnimationData> callback)
    {
        string prompt = string.IsNullOrEmpty(activePrompt)
            ? GeminiClient.Prompt_Situation_UnilatOFF_GazeOFF
            : activePrompt;

        string escapedSystem    = prompt.Replace("\\", "\\\\").Replace("\"", "\\\"");
        string escapedSituation = situation.Replace("\\", "\\\\").Replace("\"", "\\\"");

        string requestBody =
            "{\"model\":\"" + MODEL + "\"," +
            "\"max_completion_tokens\":20000," +
            "\"messages\":[" +
            "{\"role\":\"system\",\"content\":\"" + escapedSystem + "\"}," +
            "{\"role\":\"user\",\"content\":\"" + escapedSituation + "\"}" +
            "]}";

        Debug.Log("[OpenAI] Sending with prompt: " + prompt.Substring(0, Mathf.Min(80, prompt.Length)) + "...");

        using (UnityWebRequest request = new UnityWebRequest(URL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
            request.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type",  "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + API_KEY);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[OpenAI] API error: " + request.error);
                Debug.LogError("[OpenAI] Response: " + request.downloadHandler.text);
                callback(null);
                yield break;
            }

            string responseText = request.downloadHandler.text;
            Debug.Log("[OpenAI] Raw response: " + responseText);
            callback(ParseResponse(responseText));
        }
    }

    private AnimationData ParseResponse(string responseText)
    {
        try
        {
            JObject fullResponse = JObject.Parse(responseText);

            string content = null;

            var messageToken = fullResponse["choices"]?[0]?["message"];
            if (messageToken != null)
            {
                var contentToken = messageToken["content"];
                if (contentToken != null && contentToken.Type != JTokenType.Null)
                    content = contentToken.ToString();
            }

            if (string.IsNullOrEmpty(content))
            {
                var outputArray = fullResponse["output"] as JArray;
                if (outputArray != null)
                {
                    foreach (var item in outputArray)
                    {
                        if (item["type"]?.ToString() == "message")
                        {
                            var contentArray = item["content"] as JArray;
                            if (contentArray != null)
                            {
                                foreach (var block in contentArray)
                                {
                                    if (block["type"]?.ToString() == "output_text")
                                    {
                                        content = block["text"]?.ToString();
                                        break;
                                    }
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(content)) break;
                    }
                }
            }

            if (string.IsNullOrEmpty(content))
            {
                Debug.LogError("[OpenAI] Could not extract content from response");
                Debug.LogError("[OpenAI] Full response: " + responseText);
                return null;
            }

            Debug.Log("[OpenAI] Raw content: " + content);
            return FreeFormParser.Parse(content);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[OpenAI] ParseResponse exception: " + e.Message);
            return null;
        }
    }
}