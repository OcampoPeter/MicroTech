// WHERE TO PUT THIS FILE:  Assets Task / Bent
// ------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// BENT platform: it starts damaged. Each time the player lands on it, it gets less bent,
// until it is fixed and shows the normal platform.
// You don't add this yourself: set a platform's Layer to "Bent" and MazeTaskController adds it.
//
// Two ways to show it as "bent":
//   1) Bent Stages set on the controller (e.g. VeryBent, Bent, ABitBent):
//      starts on the first model, each landing moves to the next one,
//      the landing after the last model swaps back to the normal platform.
//      So the number of landings needed = the number of models.
//   2) No Bent Stages: the platform tilts and changes color, then straightens when fixed.
public class BentPlatform : MonoBehaviour
{
    // The colors are set on the MazeTaskController (NOT here), because this script is added
    // while the game runs, so anything changed on it in the Inspector is lost when you stop.
    Color bentColor;              // color of the most bent look
    Color fixedColor;             // color of the fixed platform
    bool tintModels;              // true = color the bent models too
    bool tintFixed;              // true = color the platform after it is fixed
    float glow;                   // how much the colors glow (neon). 0 = no glow
    bool blendWhileBent;          // true = color shifts toward "fixed" with each hit
    public float straightenTime = 0.5f;

    public bool IsFixed { get; private set; }
    public int JumpCount { get; private set; }

    MazeTaskGroup group;          // the task this platform belongs to
    int jumpsToFix = 3;           // landings needed to fix it
    Renderer[] ownRenderers;      // the platform's normal look (what it looks like when fixed)
    Collider[] ownColliders;      // the platform's normal collision
    List<GameObject> stages;      // the bent models, most bent first
    GameObject bentVisual;        // the bent model being shown right now
    bool hasModel;                // true if we are using bent models
    bool keepsOwnSize;            // true = model keeps the size it was made at
    Quaternion fixedRotation;     // the rotation it has when fixed
    Bounds platformBounds;        // the platform's size (width and depth) before any model
    float fadeTime;               // seconds a color change takes (a smooth fade)
    Color currentColor;           // the color the platform has right now

    // Called by MazeTaskController to set this platform up.
    public void Init(MazeTaskGroup taskGroup, int jumps, Vector3 tilt,
                     GameObject[] bentStages, bool modelKeepsOwnSize, float hitCooldown,
                     Color bentCol, Color fixedCol, bool colorModels, bool colorFixed,
                     float glowAmount, bool blendColors, float colorFadeTime)
    {
        group = taskGroup;
        bentColor = bentCol;
        fixedColor = fixedCol;
        tintModels = colorModels;
        tintFixed = colorFixed;
        glow = glowAmount;
        blendWhileBent = blendColors;
        fadeTime = colorFadeTime;
        currentColor = bentColor;
        ownRenderers = GetComponentsInChildren<Renderer>();
        ownColliders = GetComponents<Collider>();
        keepsOwnSize = modelKeepsOwnSize;
        platformBounds = TaskVisuals.GetBounds(ownRenderers, ownColliders); // remember the platform's size

        // Collect the stage models, skipping any empty slots.
        stages = new List<GameObject>();
        if (bentStages != null)
            foreach (var m in bentStages)
                if (m != null) stages.Add(m);

        hasModel = stages.Count > 0;

        // With models: one landing per model. Without: use the number from the controller.
        jumpsToFix = hasModel ? stages.Count : Mathf.Max(1, jumps);

        // Make the sensor that counts landings (before any tilt, so it sits flat on top).
        var step = StepTrigger.Create(gameObject);
        step.cooldown = hitCooldown;   // wait this long between counted hits
        step.onPlayerStep += OnStep;

        fixedRotation = transform.localRotation;

        if (hasModel)
        {
            // Hide the normal platform look and show the first (most bent) model.
            foreach (var r in ownRenderers) r.enabled = false;
            ShowStage(0);
        }
        else
        {
            // No models: tilt the platform and color it as "bent".
            transform.localRotation = fixedRotation * Quaternion.Euler(tilt);
            Tint(ownRenderers, bentColor);
        }

        group.Register(this);
    }

    // Show one of the bent models (replacing the one shown before).
    void ShowStage(int index)
    {
        if (bentVisual != null) Destroy(bentVisual);

        GameObject model = stages[index];
        bentVisual = Instantiate(model, transform);
        bentVisual.name = "BentModel_" + index;
        bentVisual.transform.localPosition = Vector3.zero;
        bentVisual.transform.localRotation = Quaternion.identity;

        // Size the model. Keep Own Size ON: the model keeps the size it was made at.
        // OFF: the model is resized to fit the platform's width and depth.
        float fit = keepsOwnSize ? 1f : TaskVisuals.FitFactor(model, platformBounds);

        // Cancel the platform's own scale so only the size we chose is applied.
        Vector3 s = transform.lossyScale;
        Vector3 inverse = new Vector3(Safe(1f / s.x), Safe(1f / s.y), Safe(1f / s.z));
        bentVisual.transform.localScale = Vector3.Scale(model.transform.localScale * fit, inverse);

        // Color the model: the first model is the "bent" color. With blending on, each later
        // model fades a step closer to the "fixed" color (slowly, over the fade time).
        if (tintModels)
        {
            Color target = blendWhileBent ? Color.Lerp(bentColor, fixedColor, (float)index / jumpsToFix) : bentColor;
            Renderer[] modelRenderers = bentVisual.GetComponentsInChildren<Renderer>();

            if (index == 0) Tint(modelRenderers, target);                                    // first one: no fade
            else StartCoroutine(FadeColor(modelRenderers, currentColor, target));            // later ones: fade
            currentColor = target;
        }

        // If this model has its own collision, use it instead of the platform's normal one.
        bool modelHasCollider = bentVisual.GetComponentInChildren<Collider>() != null;
        foreach (var c in ownColliders) c.enabled = !modelHasCollider;
    }

    // Runs every time the player lands on the platform.
    void OnStep()
    {
        if (IsFixed) return;

        JumpCount++;

        if (JumpCount >= jumpsToFix)
        {
            Fix();
        }
        else if (hasModel)
        {
            // Move to the next, less bent model.
            ShowStage(JumpCount);
        }
        else if (blendWhileBent)
        {
            // No models: shift the color a bit closer to "fixed" (only if blending is on).
            Color target = Color.Lerp(bentColor, fixedColor, (float)JumpCount / jumpsToFix);
            StartCoroutine(FadeColor(ownRenderers, currentColor, target));
            currentColor = target;
        }
    }

    // The platform is repaired: swap back to the normal look.
    void Fix()
    {
        IsFixed = true;

        if (hasModel)
        {
            // Remove the bent model and bring back the normal platform and collision.
            if (bentVisual != null) Destroy(bentVisual);
            foreach (var r in ownRenderers) r.enabled = true;
            foreach (var c in ownColliders) c.enabled = true;

            // Color the normal platform with the "fixed" color (if turned on).
            // It fades slowly from the last bent color to the fixed color.
            if (tintFixed) StartCoroutine(FadeColor(ownRenderers, currentColor, fixedColor));
        }
        else
        {
            StartCoroutine(Straighten());
            StartCoroutine(FadeColor(ownRenderers, currentColor, fixedColor));
        }

        group.MarkComplete(this);
    }

    // Smoothly rotate the tilted platform back to flat.
    IEnumerator Straighten()
    {
        Quaternion start = transform.localRotation;
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, straightenTime))
        {
            transform.localRotation = Quaternion.Slerp(start, fixedRotation, t);
            yield return null;
        }
        transform.localRotation = fixedRotation;
    }

    // Slowly change the color of some renderers from one color to another.
    IEnumerator FadeColor(Renderer[] targets, Color from, Color to)
    {
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, fadeTime))
        {
            Tint(targets, Color.Lerp(from, to, t));
            yield return null;
        }
        Tint(targets, to);
    }

    // Color every renderer in the list.
    void Tint(Renderer[] renderers, Color color)
    {
        foreach (var r in renderers) TaskVisuals.SetColor(r, color, glow); // glow makes it neon
    }

    // Avoid infinity if a scale value is 0.
    float Safe(float v) => float.IsInfinity(v) || float.IsNaN(v) ? 1f : v;
}