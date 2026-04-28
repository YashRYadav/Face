using System;

[Serializable]
public class ActionUnit
{
    public string au;
    public float  intensity;
}

[Serializable]
public class LabanData
{
    public float weight;
    public float time;
    public float space;
    public float flow;
}

[Serializable]
public class AnimationData
{
    public ActionUnit[] aus;
    public LabanData    laban;

    // Feature #1 — Temporal Emotion Arcs
    public bool  isMicro;
    public float onset  = -1f;
    public float offset = -1f;

    // Feature #2 — Emotion-Driven Gaze Co-articulation
    public string gazeTarget = "forward";   // forward | down | away | side | up
    public float  gazeHold   = 1.0f;        // seconds to hold gaze before returning to neutral
}