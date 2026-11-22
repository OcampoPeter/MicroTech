// WHERE TO PUT THIS FILE:  Assets Task / Fried
// ------------------------------------------------------------
using System.Collections;
using UnityEngine;

// Settings for the Fried task. They live on the MazeTaskController (NOT on PowerNode),
// because PowerNode is added while the game runs, so anything changed on it would be lost
// when you stop Play.
[System.Serializable]
public class FriedSettings
{
    [Header("Models (optional)")]
    [Tooltip("Model shown while a node is OFF (dark). Leave empty to keep the platform's own look.")]
    public GameObject darkModel;
    [Tooltip("Model shown while a node is flashing, lit, or wrong. Leave empty to keep the platform's own look.")]
    public GameObject litModel;
    [Tooltip("ON: models keep the size they were made at.\nOFF: each model is resized to fit the width and depth of the platform it is on (small platform = small model, big platform = big model).")]
    public bool modelsKeepOwnSize = true;
    [Tooltip("ON: also color the models with the colors below.\nOFF: models keep their own materials.\n(Without models, the platform itself is always colored.)")]
    public bool colorModels = false;

    [Header("Colors")]
    [Tooltip("A node that is off.")]
    public Color darkColor = new Color(0.1f, 0.1f, 0.12f);
    [Tooltip("The color a node flashes when it shows you the order.")]
    public Color flashColor = new Color(0.3f, 0.8f, 1f);
    [Tooltip("A node you stepped on in the right order.")]
    public Color litColor = new Color(0.4f, 1f, 0.5f);
    [Tooltip("A node you stepped on in the wrong order.")]
    public Color wrongColor = new Color(1f, 0.25f, 0.2f);
    [Tooltip("How much the lit colors glow. 0 = no glow.")]
    public float emission = 2f;

    [Header("Timing (seconds)")]
    [Tooltip("Wait before the first flashing starts when the scene begins.")]
    public float startDelay = 1.0f;
    [Tooltip("How long each node stays lit while flashing.")]
    public float flashDuration = 0.5f;
    [Tooltip("Pause between one flash and the next.")]
    public float gapBetweenFlashes = 0.25f;
    [Tooltip("How long the red wrong-step color shows before everything turns off.")]
    public float wrongShowTime = 0.6f;
    [Tooltip("Pause after a wrong step, before the order flashes again.")]
    public float delayBeforeReplay = 1.0f;
    [Tooltip("A node ignores new jumps for this long after being jumped on. Keep it a bit longer than one jump.")]
    public float stepCooldown = 0.8f;
}

// FRIED: dark power nodes flash in an order, and the player steps on them in that same order.
// A wrong step resets everything and the order flashes again.
// Done when the whole sequence is lit.
//
// You don't add this yourself: set platforms' Layer to "Fried" and MazeTaskController
// adds it, picks the nodes (Fried Count) and shuffles the order (Randomize).
// The whole sequence counts as ONE item for the task group.
public class PowerNode : MonoBehaviour
{
    public int sequenceIndex;     // this node's place in the order (0 = first)
    public bool IsLit { get; private set; }

    Renderer[] ownRenderers;      // the platform's own look
    Collider[] ownColliders;      // the platform's own collision
    GameObject visual;            // the model being shown right now (null = platform's own look)
    GameObject shownPrefab;       // which model prefab that is
    Bounds platformBounds;        // the platform's size (width and depth) before any model
    bool usesModels;              // true if a dark or lit model was given
    FriedSettings look;           // colors, models and timings (from the controller)
    MazeTaskGroup group;          // the task this belongs to (only the first node uses it)
    PowerNode[] sequence;         // all nodes in the correct order
    PowerNode controller;         // the first node runs the whole sequence
    int progress;                 // controller only: which node the player must step on next
    bool busy;                    // controller only: true while flashing, so steps are ignored
    bool completed;               // controller only: true when the sequence is done

    // Called by MazeTaskController to set this node up.
    public void Init(MazeTaskGroup taskGroup, PowerNode[] orderedNodes, FriedSettings settings)
    {
        look = settings;
        ownRenderers = GetComponentsInChildren<Renderer>();
        ownColliders = GetComponents<Collider>();
        platformBounds = TaskVisuals.GetBounds(ownRenderers, ownColliders); // remember the platform's size
        usesModels = look.darkModel != null || look.litModel != null;
        sequence = orderedNodes;
        controller = orderedNodes[0];
        SetDark();

        // Make the sensor. For Fried, like Bent: get onto the node, then JUMP on it to activate it.
        // Just walking or jumping onto the node does not count.
        var step = StepTrigger.Create(gameObject);
        step.mode = StepMode.JumpOnIt;
        step.cooldown = look.stepCooldown;
        step.onPlayerStep += () => controller.HandleStep(this);

        // Only the first node runs the logic and starts the flashing.
        if (this == controller)
        {
            group = taskGroup;
            group.Register(this);
            StartCoroutine(PlayDemo(look.startDelay));
        }
    }

    // ---- Controller logic (runs on the first node) ----

    // The player stepped on a node.
    void HandleStep(PowerNode stepped)
    {
        if (completed || busy) return;   // ignore steps while flashing or when done
        if (stepped.IsLit) return;       // already lit: stepping on it again is not a mistake

        if (stepped == sequence[progress])
        {
            // Right node: light it and wait for the next one.
            stepped.IsLit = true;
            stepped.ShowLit(look.litColor);
            progress++;

            if (progress >= sequence.Length)
            {
                completed = true;
                group.MarkComplete(this);
            }
        }
        else
        {
            // Wrong node: show red, reset, and flash the order again.
            StartCoroutine(WrongStep(stepped));
        }
    }

    // Show the wrong node in red, turn everything off, then flash the order again.
    IEnumerator WrongStep(PowerNode stepped)
    {
        busy = true;
        stepped.ShowLit(look.wrongColor);
        yield return new WaitForSeconds(look.wrongShowTime);

        progress = 0;
        foreach (var n in sequence) n.SetDark();

        yield return PlayDemo(look.delayBeforeReplay);
    }

    // Flash the nodes one by one in the correct order. Steps are ignored while this plays.
    IEnumerator PlayDemo(float initialDelay)
    {
        busy = true;
        yield return new WaitForSeconds(Mathf.Max(0.1f, initialDelay));

        foreach (var n in sequence)
        {
            n.ShowLit(look.flashColor);
            yield return new WaitForSeconds(look.flashDuration);
            n.SetDark();
            yield return new WaitForSeconds(look.gapBetweenFlashes);
        }

        busy = false;
    }

    // ---- Looks of one node ----

    // Turn the node off (dark model / dark color).
    void SetDark()
    {
        IsLit = false;
        Show(look.darkModel, look.darkColor, 0f);
    }

    // Light the node up (lit model / given color).
    void ShowLit(Color color)
    {
        Show(look.litModel, color, look.emission);
    }

    // Show a model (or the platform's own look) and color it if needed.
    void Show(GameObject prefab, Color color, float glow)
    {
        if (usesModels) SwapModel(prefab);

        // Color the platform's own look always; color a model only if "Color Models" is on.
        if (visual == null || look.colorModels)
        {
            Renderer[] targets = visual != null ? visual.GetComponentsInChildren<Renderer>() : ownRenderers;
            foreach (var r in targets) TaskVisuals.SetColor(r, color, glow);
        }
    }

    // Replace the model on this node. A null prefab means "use the platform's own look".
    void SwapModel(GameObject prefab)
    {
        // Already showing this one: nothing to do.
        if (prefab == shownPrefab && (visual != null || prefab == null)) return;

        if (visual != null) Destroy(visual);
        visual = null;
        shownPrefab = prefab;

        if (prefab == null)
        {
            // Back to the platform's own look and collision.
            foreach (var r in ownRenderers) r.enabled = true;
            foreach (var c in ownColliders) c.enabled = true;
            return;
        }

        // Hide the platform's own look and place the model in its spot.
        foreach (var r in ownRenderers) r.enabled = false;

        visual = Instantiate(prefab, transform);
        visual.name = "FriedModel";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;

        // Size the model. Keep Own Size ON: the model keeps the size it was made at.
        // OFF: the model is resized to fit the platform's width and depth.
        float fit = look.modelsKeepOwnSize ? 1f : TaskVisuals.FitFactor(prefab, platformBounds);

        // Cancel the platform's own scale so only the size we chose is applied.
        Vector3 s = transform.lossyScale;
        Vector3 inverse = new Vector3(Safe(1f / s.x), Safe(1f / s.y), Safe(1f / s.z));
        visual.transform.localScale = Vector3.Scale(prefab.transform.localScale * fit, inverse);

        // If the model has its own collision, use it instead of the platform's own.
        bool modelHasCollider = visual.GetComponentInChildren<Collider>() != null;
        foreach (var c in ownColliders) c.enabled = !modelHasCollider;
    }

    // Avoid infinity if a scale value is 0.
    float Safe(float v) => float.IsInfinity(v) || float.IsNaN(v) ? 1f : v;
}