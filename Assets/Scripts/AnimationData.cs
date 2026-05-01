using System;

[Serializable]
public class ActionUnit
{
    public string au;
    public float  intensity;
}

[Serializable]
public class AnimationData
{
    public ActionUnit[] aus;

    // Feature #1 — Temporal Emotion Arcs
    public bool  isMicro;
    public float onset  = -1f;
    public float offset = -1f;

    // Feature #2 — Emotion-Driven Gaze Co-articulation
    public string gazeTarget = "forward";
    public float  gazeHold   = 1.0f;
}