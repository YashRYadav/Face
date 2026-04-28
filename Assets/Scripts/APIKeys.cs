using UnityEngine;

[CreateAssetMenu(fileName = "APIKeys", menuName = "Config/API Keys")]
public class APIKeys : ScriptableObject
{
    public string geminiApiKey;
    public string openAiApiKey;
}