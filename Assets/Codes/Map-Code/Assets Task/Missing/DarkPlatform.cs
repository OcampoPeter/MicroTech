// WHERE TO PUT THIS FILE:  Assets Task / Missing
// ------------------------------------------------------------
using System.Collections;
using UnityEngine;

// Settings for the dark platforms. They live on the MazeTaskController (NOT on DarkPlatform),
// because DarkPlatform is added while the game runs, so anything changed on it would be lost
// when you stop Play.
[System.Serializable]
public class MissingSettings
{
    [Header("Colors")]
    [Tooltip("A platform that is still missing its part: a faded blue block.\n(For a see-through look the platform's material must be a transparent one. The color's Alpha is the amount of fade.)")]
    public Color darkColor = new Color(0.12f, 0.25f, 0.6f, 0.5f);
    [Tooltip("A small glow on the faded block so it can be seen from far away. 0 = no glow.")]
    public float darkEmission = 0.35f;
    [Tooltip("A platform that got its part and lit up.")]
    public Color litColor = new Color(0.25f, 0.6f, 1f);
    [Tooltip("How much the lit color glows (neon). 0 = no glow.")]
    public float emission = 1f;
    [Tooltip("How many seconds the platform takes to light up.")]
    public float litFadeTime = 0.6f;

    [Header("Timing")]
    [Tooltip("A platform ignores new steps for this long after being stepped on.")]
    public float stepCooldown = 0.3f;
}

// MISSING PARTS (the platform): a darkened platform. When the player steps on it while
// carrying a part, the part is used up and the platform lights up.
// Done when every dark platform is lit.
//
// You don't add this yourself: set a platform's Layer to "Missing" and MazeTaskController adds it.
public class DarkPlatform : MonoBehaviour
{
    public bool IsLit { get; private set; }

    Renderer[] renderers;      // what we color
    MissingSettings look;      // colors and timing (from the controller)
    MazeTaskGroup group;       // the task this platform belongs to

    // Called by MazeTaskController to set this platform up.
    public void Init(MazeTaskGroup taskGroup, MissingSettings settings)
    {
        group = taskGroup;
        look = settings;
        renderers = GetComponentsInChildren<Renderer>();
        Tint(look.darkColor, look.darkEmission);

        // Just standing on the platform counts (no jump needed).
        var step = StepTrigger.Create(gameObject);
        step.mode = StepMode.StandOnIt;
        step.debugLog = false;   // standing reports often, so keep the Console quiet
        step.cooldown = look.stepCooldown;
        step.onPlayerStep += OnStep;

        group.Register(this);
    }

    // The player stepped on the platform.
    void OnStep()
    {
        if (IsLit) return;

        PartPickup part = PartPickup.Carried;
        if (part == null) return; // not carrying a part yet

        part.Consume();           // the part is used up
        IsLit = true;
        StartCoroutine(FadeIn());
        group.MarkComplete(this);
    }

    // Slowly light the platform up.
    IEnumerator FadeIn()
    {
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, look.litFadeTime))
        {
            Tint(Color.Lerp(look.darkColor, look.litColor, t), Mathf.Lerp(look.darkEmission, look.emission, t));
            yield return null;
        }
        Tint(look.litColor, look.emission);
    }

    // Color every renderer on this platform.
    void Tint(Color color, float glow)
    {
        foreach (var r in renderers) TaskVisuals.SetColor(r, color, glow);
    }
}