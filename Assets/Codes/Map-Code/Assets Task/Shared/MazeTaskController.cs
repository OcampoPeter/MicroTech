// WHERE TO PUT THIS FILE:  Assets Task / Shared
// REPLACE the old MazeTaskController.cs (don't keep 2 copies).
// NEW: it now sends the Bent / Missing / Fried counts to the ObjectivesUI quest panel.
// ------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Put this on ANY object in the scene (one per maze scene). At Start it scans the
/// whole scene (or just its own children, see "Scan Whole Scene") for layers:
///
///   Layer "Bent"    -> platform becomes a BentPlatform
///   Layer "Missing" -> platform becomes a DarkPlatform (needs a part)
///   Layer "Fried"   -> platform becomes a PowerNode (stepping sequence)
///   Layer "Part"    -> an object that can be picked up (or any PartPickup you dragged in)
///
/// Counts let you mark MORE platforms than you need and have the code randomly
/// pick how many are active. Platforms that aren't picked go back to the normal layer.
/// Parts work the same way: you scatter 30, it keeps as many as there are dark
/// platforms and switches the rest off.
/// </summary>
public class MazeTaskController : MonoBehaviour
{
    [Header("Where to look")]
    [Tooltip("ON: finds platforms anywhere in this scene, just change their Layer.\nOFF: only looks at objects inside this object (its children).")]
    public bool scanWholeScene = true;

    [Header("Layer names (must match the layers in Project Settings)")]
    public string bentLayer = "Bent";
    public string missingLayer = "Missing";
    public string friedLayer = "Fried";
    public string partLayer = "Part";
    [Tooltip("Extra layers for extra Fried sets, e.g. Fried2, Fried3. Each layer in this list is its own separate set. Create the layers first in Project Settings > Tags and Layers.")]
    public string[] extraFriedLayers = new string[0];
    [Tooltip("Layer given to marked platforms that were NOT picked.")]
    public string normalLayer = "Default";

    [Header("How many are active  (-1 = every platform marked with that layer)")]
    public int bentCount = -1;
    public int missingCount = -1;
    public int friedCount = -1;

    [Header("Randomizing")]
    public bool randomize = true;
    public bool useSeed = false;
    public int seed = 12345;

    [Header("Bent settings")]
    // How many landings it takes to fix a bent platform.
    public int jumpsToFix = 3;
    // Used only when there is NO Bent Model: how much the platform tilts.
    public Vector3 bentTilt = new Vector3(0f, 0f, 14f);
    [Tooltip("Seconds a bent platform ignores new landings after a hit. Raise it if one jump still counts twice; lower it to hit faster.")]
    public float hitCooldown = 0.8f;

    [Header("Bent look (optional)")]
    [Tooltip("Drag the bent models here, MOST bent first (e.g. VeryBent, Bent, ABitBent). Each landing moves to the next one; after the last, the normal platform comes back. Landings needed = number of models. Leave empty to just tilt the platform.")]
    public GameObject[] bentStages;
    [Tooltip("ON: the bent models keep the size they were made at.\nOFF: each model is resized to fit the width and depth of the platform it is on (small platform = small model, big platform = big model).")]
    public bool bentModelKeepsOwnSize = true;

    [Header("Bent colors")]
    [Tooltip("Color of the most bent look. With bent models, the first model gets this color.")]
    public Color bentColor = new Color(1f, 0.5f, 0.05f);   // orange
    [Tooltip("Color of the fixed platform. Bent models get closer to this color with each hit.")]
    public Color fixedColor = new Color(0.2f, 1f, 0.35f);  // neon green
    [Tooltip("ON: also color the bent models (VeryBent, Bent, ABitBent).\nOFF: keep the models' own materials.")]
    public bool colorBentModels = true;
    [Tooltip("ON: color the normal platform with the fixed color once it is fixed.\nOFF: it keeps its own material.")]
    public bool colorFixedPlatform = true;
    [Tooltip("How much the Bent colors glow (neon). 0 = no glow. Lower = softer glow. Fried uses 2.")]
    public float bentGlow = 1f;
    [Tooltip("ON: the color slowly moves from Bent Color toward Fixed Color with each hit.\nOFF: it stays Bent Color until fixed, then turns Fixed Color.")]
    public bool blendColorWhileBent = true;
    [Tooltip("How many seconds each color change takes. Higher = slower fade.")]
    public float bentColorFadeTime = 0.6f;

    [Header("Fried settings (colors and timing)")]
    public FriedSettings fried = new FriedSettings();

    [Header("Missing settings (dark platform colors)")]
    public MissingSettings missingLook = new MissingSettings();

    [Header("Part settings (the part the player carries)")]
    public PartSettings partSettings = new PartSettings();

    [Header("Events")]
    public UnityEvent onMazeCompleted = new UnityEvent();

    public bool IsMazeComplete { get; private set; }

    readonly List<MazeTaskGroup> activeGroups = new List<MazeTaskGroup>();
    System.Random rng;
    bool fired;

    void Start()
    {
        rng = useSeed ? new System.Random(seed) : new System.Random();
        Setup();
    }

    // ------------------------------------------------------------------ setup

    void Setup()
    {
        int bentL = LayerIndex(bentLayer);
        int missingL = LayerIndex(missingLayer);
        int friedL = LayerIndex(friedLayer);
        int partL = LayerIndex(partLayer);
        int normalL = Mathf.Max(0, LayerIndex(normalLayer));

        var bent = new List<GameObject>();
        var missing = new List<GameObject>();
        var fried = new List<GameObject>();
        var partObjects = new List<GameObject>();

        // Extra Fried layers: each one is its own separate set.
        var extraIdx = new int[extraFriedLayers.Length];
        var extraSets = new List<List<GameObject>>();
        for (int i = 0; i < extraFriedLayers.Length; i++)
        {
            extraIdx[i] = LayerIndex(extraFriedLayers[i]);
            extraSets.Add(new List<GameObject>());
        }

        // Go through every object in the search area and sort it by its layer.
        foreach (Transform t in GetAllTransforms())
        {
            if (t == transform) continue;
            int l = t.gameObject.layer;
            if (l == bentL && bentL >= 0) bent.Add(t.gameObject);
            else if (l == missingL && missingL >= 0) missing.Add(t.gameObject);
            else if (l == friedL && friedL >= 0) fried.Add(t.gameObject);
            else if (l == partL && partL >= 0) partObjects.Add(t.gameObject);
            else
            {
                // Is it on one of the extra Fried layers?
                int k = System.Array.IndexOf(extraIdx, l);
                if (k >= 0) extraSets[k].Add(t.gameObject);
            }
        }

        SetupBent(Pick(bent, bentCount, normalL));
        SetupMissing(Pick(missing, missingCount, normalL), partObjects);
        SetupFried(fried, extraSets, normalL);

        if (activeGroups.Count == 0)
            Debug.LogWarning($"[{name}] No task platforms found. Set a platform's Layer to " +
                             $"{bentLayer} / {missingLayer} / {friedLayer}.", this);

        // OBJECTIVES UI: tell the quest panel how many of each task are on this map.
        StartCoroutine(SendObjectiveTotals());
    }

    void SetupBent(List<GameObject> platforms)
    {
        if (platforms.Count == 0) return;
        var group = MakeGroup("Bent");
        foreach (var p in platforms)
        {
            if (!HasCollider(p)) continue;
            p.AddComponent<BentPlatform>().Init(group, jumpsToFix, bentTilt, bentStages, bentModelKeepsOwnSize, hitCooldown,
                bentColor, fixedColor, colorBentModels, colorFixedPlatform, bentGlow, blendColorWhileBent, bentColorFadeTime);
        }
    }

    // MISSING PARTS: the player finds a part and carries it to a dark platform.
    // Parts come from two places, both on the Part layer:
    //   - ready-made parts (objects with a look: a Renderer or Collider)
    //   - spawn points (EMPTY objects: no Renderer and no Collider). A part spawns there.
    // Only as many parts as there are dark platforms are used. Which ones is picked at random.
    void SetupMissing(List<GameObject> darkPlatforms, List<GameObject> partLayerObjects)
    {
        int needed = darkPlatforms.Count;

        // Sort the Part layer objects: empty = spawn point, anything with a look = ready-made part.
        var spawnPoints = new List<Transform>();
        var readyParts = new List<GameObject>();
        foreach (var go in partLayerObjects)
        {
            bool hasLook = go.GetComponentInChildren<Renderer>() != null || go.GetComponent<Collider>() != null;
            if (hasLook) readyParts.Add(go);
            else spawnPoints.Add(go.transform);
        }

        // Also include parts that already have the PartPickup script (placed by hand).
        foreach (Transform t in GetAllTransforms())
            if (t.GetComponent<PartPickup>() != null && !readyParts.Contains(t.gameObject))
                readyParts.Add(t.gameObject);

        // Every ready-made part and every spawn point is one possible place for a part.
        var slots = new List<(GameObject part, Transform spawn)>();
        foreach (var p in readyParts) slots.Add((p, null));
        foreach (var sp in spawnPoints) slots.Add((null, sp));
        if (randomize) Shuffle(slots);

        // Use only as many as the dark platforms need. The other ready-made parts are switched off.
        int used = 0;
        foreach (var slot in slots)
        {
            bool use = used < needed;

            if (slot.part != null)
            {
                if (use)
                {
                    var pickup = slot.part.GetComponent<PartPickup>();
                    if (pickup == null) pickup = slot.part.AddComponent<PartPickup>();
                    pickup.Init(partSettings);
                    slot.part.SetActive(true);
                }
                else
                {
                    slot.part.SetActive(false);
                }
            }
            else if (use)
            {
                SpawnPart(slot.spawn);
            }

            if (use) used++;
        }

        if (needed == 0) return;

        if (used < needed)
            Debug.LogWarning($"[{name}] {needed} dark platforms but only {used} parts / spawn points on the " +
                             $"{partLayer} layer. The Missing task can't be finished.", this);

        var group = MakeGroup("MissingParts");
        foreach (var p in darkPlatforms)
        {
            if (!HasCollider(p)) continue;
            p.AddComponent<DarkPlatform>().Init(group, missingLook);
        }
    }

    // Creates a part at a spawn point (the empty object).
    void SpawnPart(Transform point)
    {
        GameObject go;
        if (partSettings.partPrefab != null)
        {
            // Keep the prefab's own rotation (so a tilted cube stays tilted), not the spawn point's rotation.
            go = Instantiate(partSettings.partPrefab, point.position, partSettings.partPrefab.transform.rotation);
        }
        else
        {
            // No prefab given: make a simple glowing ball so the part can be seen.
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.position = point.position;
            go.transform.localScale = Vector3.one * 0.6f;
            TaskVisuals.SetColor(go.GetComponent<Renderer>(), partSettings.fallbackColor, 1.5f);
        }

        go.name = "Part (spawned at " + point.name + ")";

        var pickup = go.GetComponent<PartPickup>();
        if (pickup == null) pickup = go.AddComponent<PartPickup>();
        pickup.Init(partSettings);
    }

    // Fried platforms are sorted into SETS. Platforms inside a FriedGroup object form that
    // group's own set. All other Fried platforms share one default set.
    // Every set has its own flash order; the maze needs ALL sets finished.
    void SetupFried(List<GameObject> marked, List<List<GameObject>> extraSets, int normalL)
    {
        // Nothing marked on any Fried layer: nothing to do.
        bool anyExtra = false;
        foreach (var s in extraSets) if (s.Count > 0) anyExtra = true;
        if (marked.Count == 0 && !anyExtra) return;

        // Sort the platforms into the default set or into their FriedGroup's set.
        var defaultSet = new List<GameObject>();
        var groupSets = new Dictionary<FriedGroup, List<GameObject>>();
        foreach (var go in marked)
        {
            var fg = go.GetComponentInParent<FriedGroup>();
            if (fg == null)
            {
                defaultSet.Add(go);
            }
            else
            {
                if (!groupSets.TryGetValue(fg, out var list))
                    groupSets[fg] = list = new List<GameObject>();
                list.Add(go);
            }
        }

        // One task group for all Fried sets, so the maze waits for every set.
        MazeTaskGroup taskGroup = null;

        // The default set uses the controller's Fried Count and Randomize.
        if (defaultSet.Count > 0)
        {
            taskGroup = MakeGroup("Fried");
            BuildFriedSet(Pick(defaultSet, friedCount, normalL), taskGroup, randomize);
        }

        // Each FriedGroup uses its own Node Count and Randomize Order.
        foreach (var pair in groupSets)
        {
            if (taskGroup == null) taskGroup = MakeGroup("Fried");
            BuildFriedSet(Pick(pair.Value, pair.Key.nodeCount, normalL), taskGroup, pair.Key.randomizeOrder);
        }

        // Each extra Fried layer (Fried2, Fried3...) is its own separate set.
        foreach (var set in extraSets)
        {
            if (set.Count == 0) continue;
            if (taskGroup == null) taskGroup = MakeGroup("Fried");
            BuildFriedSet(Pick(set, friedCount, normalL), taskGroup, randomize);
        }
    }

    // Turns one set of platforms into Fried nodes with their own flash order.
    void BuildFriedSet(List<GameObject> platforms, MazeTaskGroup taskGroup, bool shuffleOrder)
    {
        // The stepping order: random shuffle, or Hierarchy order.
        var ordered = new List<GameObject>(platforms);
        if (shuffleOrder) Shuffle(ordered);

        var components = new List<PowerNode>();
        for (int i = 0; i < ordered.Count; i++)
        {
            if (!HasCollider(ordered[i])) continue;
            var node = ordered[i].AddComponent<PowerNode>();
            node.sequenceIndex = i;
            components.Add(node);
        }

        var array = components.ToArray();
        if (array.Length == 0) return;
        foreach (var n in array) n.Init(taskGroup, array, fried);
    }

    // --------------------------------------------------------------- objectives UI

    // OBJECTIVES UI: waits 1 frame (so every task has registered), then sends the counts.
    System.Collections.IEnumerator SendObjectiveTotals()
    {
        yield return null;
        if (ObjectivesUI.Instance == null) yield break;

        int b = 0, m = 0, f = 0;
        foreach (var g in activeGroups)
        {
            if (g.taskName == "Bent") b += g.Total;
            else if (g.taskName == "MissingParts") m += g.Total;
            else if (g.taskName == "Fried") f += g.Total;
        }
        Debug.Log($"[{name}] Objectives -> Bent {b}, Missing {m}, Fried {f}");
        ObjectivesUI.Instance.SetTotals(b, m, f);
    }

    // --------------------------------------------------------------- helpers

    // Returns every object to check: the whole scene, or only this object's children.
    List<Transform> GetAllTransforms()
    {
        var result = new List<Transform>();

        if (scanWholeScene)
        {
            // Walk every top-level object in this scene and collect everything under it.
            foreach (var root in gameObject.scene.GetRootGameObjects())
                result.AddRange(root.GetComponentsInChildren<Transform>(true));
        }
        else
        {
            result.AddRange(GetComponentsInChildren<Transform>(true));
        }

        return result;
    }

    MazeTaskGroup MakeGroup(string taskName)
    {
        var go = new GameObject(taskName + "Task");
        go.transform.SetParent(transform, false);
        var group = go.AddComponent<MazeTaskGroup>();
        group.taskName = taskName;
        group.onAllCompleted.AddListener(CheckAllDone);
        activeGroups.Add(group);
        return group;
    }

    void CheckAllDone()
    {
        if (fired) return;
        foreach (var g in activeGroups)
            if (g.Total > 0 && !g.IsComplete) return;

        fired = true;
        IsMazeComplete = true;
        Debug.Log($"[{name}] All maze tasks complete");
        onMazeCompleted?.Invoke();
    }

    /// <summary>Choose which marked platforms are active; the others go back to the normal layer.</summary>
    List<GameObject> Pick(List<GameObject> marked, int count, int normalL)
    {
        var chosen = PickSubset(marked, count);
        var set = new HashSet<GameObject>(chosen);
        foreach (var go in marked)
            if (!set.Contains(go)) go.layer = normalL;
        return chosen;
    }

    List<GameObject> PickSubset(List<GameObject> source, int count)
    {
        if (count < 0 || count >= source.Count) return new List<GameObject>(source);
        var copy = new List<GameObject>(source);
        if (randomize) Shuffle(copy);
        return copy.GetRange(0, Mathf.Max(0, count));
    }

    void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    int LayerIndex(string layerName)
    {
        int i = LayerMask.NameToLayer(layerName);
        if (i < 0)
            Debug.LogWarning($"[{name}] Layer '{layerName}' doesn't exist. " +
                             "Use Tools > Maze Tasks > Create Task Layers.", this);
        return i;
    }

    bool HasCollider(GameObject go)
    {
        if (go.GetComponent<Collider>() != null) return true;
        Debug.LogWarning($"{go.name} is marked as a task platform but has no Collider.", go);
        return false;
    }
}