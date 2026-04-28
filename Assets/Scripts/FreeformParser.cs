using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// FreeFormParser — extracts AnimationData from free-form LLM text responses.
///
/// Handles both natural language and JSON-like formats.
/// Extracts:
///   - AU codes with optional _L/_R suffixes and intensities
///   - Gaze direction if mentioned
///
/// No temporal data extracted (temporality, onset, offset removed).
/// </summary>
public static class FreeFormParser
{
    // Valid AU codes
    private static readonly HashSet<string> validAUs = new HashSet<string>
    {
        "AU1","AU2","AU4","AU5","AU6","AU7","AU8","AU9","AU10",
        "AU12","AU14","AU15","AU16","AU17","AU18","AU20","AU22","AU24","AU26","AU27","AU28","AU45",
        "AD19","AD29","AD30","AD31","AD34","M61","M62","M63","M64"
    };

    // Valid gaze directions
    private static readonly HashSet<string> validGaze = new HashSet<string>
    {
        "forward","down","up","left","right","away_left","away_right","down_left","down_right"
    };

    /// <summary>
    /// Parse free-form LLM text and return AnimationData.
    /// </summary>
    public static AnimationData Parse(string text)
    {
        AnimationData data = new AnimationData();
        data.gazeTarget = "forward"; // default
        data.gazeHold   = 1.0f;
        data.isMicro    = false;
        data.onset      = -1f;
        data.offset     = -1f;
        data.laban      = new LabanData { weight = 0.5f, time = 0.5f, space = 0.5f, flow = 0.5f };

        // ── Extract AUs ───────────────────────────────────────────────────────
        // Matches patterns like:
        //   AU4: 0.8
        //   AU12_R (0.7)
        //   AU6 with intensity 0.5
        //   AU4 = 0.9
        //   "au": "AU12_R", "intensity": 0.8   (JSON-like)
        //   AU4_L: intensity 0.6

        var auList = new List<ActionUnit>();

        // Pattern 1: AU code followed by optional _L/_R, then intensity value
        // Covers: AU4: 0.8 | AU12_R: 0.7 | AU4 (0.8) | AU4 = 0.8 | AU4 0.8
        string auPattern = @"\b((?:AU|AD|M)\d+(?:_[LR])?)\b[\s:=()\-]*([0-1](?:\.\d+)?)";
        var matches = Regex.Matches(text, auPattern, RegexOptions.IgnoreCase);

        var seen = new HashSet<string>();
        foreach (Match m in matches)
        {
            string auCode = m.Groups[1].Value.ToUpper();
            string baseCode = auCode.Replace("_L", "").Replace("_R", "");

            if (!validAUs.Contains(baseCode)) continue;
            if (seen.Contains(auCode)) continue;
            seen.Add(auCode);

            float intensity = 0.5f;
            float.TryParse(m.Groups[2].Value, out intensity);
            intensity = Mathf.Clamp01(intensity);

            auList.Add(new ActionUnit { au = auCode, intensity = intensity });
            Debug.Log($"[Parser] Found AU: {auCode} intensity={intensity:F2}");
        }

        // Pattern 2: JSON-style "au": "AU12_R", "intensity": 0.8
        string jsonAuPattern = "\"au\"\\s*:\\s*\"([^\"]+)\"[^}]*\"intensity\"\\s*:\\s*([0-9.]+)";
        var jsonMatches = Regex.Matches(text, jsonAuPattern, RegexOptions.IgnoreCase);
        foreach (Match m in jsonMatches)
        {
            string auCode = m.Groups[1].Value.ToUpper();
            string baseCode = auCode.Replace("_L", "").Replace("_R", "");
            if (!validAUs.Contains(baseCode)) continue;
            if (seen.Contains(auCode)) continue;
            seen.Add(auCode);

            float intensity = 0.5f;
            float.TryParse(m.Groups[2].Value, out intensity);
            intensity = Mathf.Clamp01(intensity);

            auList.Add(new ActionUnit { au = auCode, intensity = intensity });
            Debug.Log($"[Parser] Found AU (JSON): {auCode} intensity={intensity:F2}");
        }

        data.aus = auList.ToArray();

        if (data.aus.Length == 0)
            Debug.LogWarning("[Parser] No AUs found in response. Check LLM output format.");
        else
            Debug.Log($"[Parser] Total AUs extracted: {data.aus.Length}");

        // ── Extract Gaze ──────────────────────────────────────────────────────
        // Looks for gaze direction words anywhere in the text
        // Checks longer patterns first to avoid partial matches
        //   "away_left", "down_left", "down_right", "away_right" before "down", "left", "right"

        string[] gazeByLength = new[]
        {
            "away_left", "away_right", "down_left", "down_right",
            "forward", "down", "up", "left", "right"
        };

        string textLower = text.ToLower();
        foreach (string gaze in gazeByLength)
        {
            if (Regex.IsMatch(textLower, @"\b" + gaze.Replace("_", "[_ ]?") + @"\b"))
            {
                data.gazeTarget = gaze;
                Debug.Log($"[Parser] Found gaze: {gaze}");
                break;
            }
        }

        // Also check JSON-style "gaze_target": "down"
        var gazeJsonMatch = Regex.Match(text, "\"gaze[_\\s]?target\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase);
        if (gazeJsonMatch.Success)
        {
            string g = gazeJsonMatch.Groups[1].Value.ToLower().Trim();
            if (validGaze.Contains(g))
            {
                data.gazeTarget = g;
                Debug.Log($"[Parser] Found gaze (JSON): {g}");
            }
        }

        Debug.Log($"[Parser] Final gaze: {data.gazeTarget}");
        return data;
    }
}