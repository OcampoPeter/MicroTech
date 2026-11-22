using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(1000)]
public class ObjectiveCompass : MonoBehaviour
{
    [Header("Size and position (change these live while playing)")]
    [Tooltip("Makes the whole compass bigger or smaller. 1 = normal.")]
    public float scale = 1f;
    public float barWidth = 600f;
    public float barHeight = 50f;
    [Tooltip("Gap from the top of the screen. Raise it to move the compass down.")]
    public float distanceFromTop = 80f;
    [Tooltip("Move left (negative) or right (positive) from the center.")]
    public float horizontalOffset = 0f;

    [Header("How much you can see")]
    [Tooltip("Degrees shown across the whole bar. 180 = like Skyrim.")]
    public float viewAngle = 180f;
    [Tooltip("Markers fade out near the edges of the bar.")]
    public float edgeFade = 25f;

    [Header("Marker colors")]
    public Color bentColor = new Color(1f, 0.5f, 0.05f);     // orange
    public Color missingColor = new Color(0.15f, 0.3f, 1f);  // blue
    public Color friedColor = new Color(1f, 0.9f, 0.2f);     // yellow
    public Color partColor = new Color(0.55f, 0.9f, 1f);     // light blue (loose parts)
    public Color exitColor = new Color(0.2f, 1f, 0.4f);      // green

    [Header("Exit")]
    [Tooltip("Leave empty: it looks for an object named FinishLine.")]
    public Transform exitTarget;

    [Header("Other")]
    public bool showDistance = true;
    [Tooltip("Turn the N/E/S/W letters if your map's 'north' is not the Z direction. 0 = north is +Z.")]
    public float northOffset = 0f;
    [Tooltip("How often (seconds) the list of tasks is refreshed.")]
    public float refreshTime = 0.25f;
    [Tooltip("Leave EMPTY to pick the camera you are looking through automatically.\nDrag a camera here only if the compass follows the wrong one.")]
    public Camera viewCamera;
    [Tooltip("Optional smoothing. 0 = none (best). Higher = snappier, lower = smoother but it lags behind.")]
    public float headingSmoothing = 0f;

    struct Target
    {
        public Transform t;
        public Color c;
    }

    class Marker
    {
        public RectTransform root;
        public Image icon;
        public Text dist;
        public Color color;
    }

    RectTransform bar;
    Font font;
    Camera cam;
    Transform player;
    float nextRefresh;
    float nextCamCheck;
    string lastCamName = "";
    float smoothYaw;
    bool yawReady;

    // Targets are rebuilt every refresh. Each target KEEPS the same marker,
    // so markers never swap places or colors while you move.
    readonly List<Target> targets = new List<Target>();
    readonly HashSet<Transform> targetSet = new HashSet<Transform>();
    readonly Dictionary<Transform, Marker> active = new Dictionary<Transform, Marker>();
    readonly List<Marker> freeMarkers = new List<Marker>();
    readonly List<Transform> toRemove = new List<Transform>();

    readonly List<Image> ticks = new List<Image>();
    readonly List<float> tickAngles = new List<float>();
    readonly Text[] letters = new Text[4];

    // Heights inside the bar (they follow the bar height)
    float LetterY { get { return -barHeight * 0.28f; } }
    float MarkerY { get { return barHeight * 0.24f; } }

    void Start()
    {
        // Find the Canvas to draw on (the same one as the quest panel if possible)
        Canvas c = null;
        if (ObjectivesUI.Instance != null) c = ObjectivesUI.Instance.GetComponentInParent<Canvas>();
        if (c == null) c = FindFirstObjectByType<Canvas>();
        if (c == null)
        {
            Debug.LogWarning("[ObjectiveCompass] No Canvas found in the scene.");
            enabled = false;
            return;
        }

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (exitTarget == null)
        {
            GameObject f = GameObject.Find("FinishLine");
            if (f != null) exitTarget = f.transform;
        }

        BuildBar(c.rootCanvas.transform);
    }

    // ------------------------------------------------------------ build the bar

    void BuildBar(Transform parent)
    {
        GameObject barGO = new GameObject("ObjectiveCompass", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        bar = barGO.GetComponent<RectTransform>();
        bar.SetParent(parent, false);
        bar.anchorMin = new Vector2(0.5f, 1f);
        bar.anchorMax = new Vector2(0.5f, 1f);
        bar.pivot = new Vector2(0.5f, 1f);
        ApplyLayout();

        Image bg = barGO.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.35f);
        bg.raycastTarget = false;

        // N / E / S / W
        string[] names = { "N", "E", "S", "W" };
        for (int i = 0; i < 4; i++)
            letters[i] = MakeText(bar, "Letter " + names[i], 18, FontStyle.Bold, names[i]);

        // small tick marks every 15 degrees (skip the ones where a letter is)
        for (int a = 0; a < 360; a += 15)
        {
            if (a % 90 == 0) continue;
            Image tick = MakeImage(bar, "Tick", new Vector2(1.5f, 8f));
            ticks.Add(tick);
            tickAngles.Add(a);
        }

        // little mark in the middle (the way you are looking)
        Image mid = MakeImage(bar, "Center", new Vector2(2f, 8f));
        RectTransform mrt = mid.rectTransform;
        mrt.anchorMin = new Vector2(0.5f, 1f);
        mrt.anchorMax = new Vector2(0.5f, 1f);
        mrt.pivot = new Vector2(0.5f, 1f);
        mrt.anchoredPosition = Vector2.zero;
        mid.color = new Color(1f, 1f, 1f, 0.8f);
    }

    // Applies size, position and scale (called every frame so Inspector changes show live)
    void ApplyLayout()
    {
        bar.sizeDelta = new Vector2(barWidth, barHeight);
        bar.anchoredPosition = new Vector2(horizontalOffset, -distanceFromTop);
        bar.localScale = Vector3.one * Mathf.Max(0.1f, scale);
    }

    Image MakeImage(Transform parent, string objName, Vector2 size)
    {
        GameObject go = new GameObject(objName, typeof(RectTransform), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = size;
        Image img = go.GetComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    Text MakeText(Transform parent, string objName, int size, FontStyle style, string content)
    {
        GameObject go = new GameObject(objName, typeof(RectTransform), typeof(Text));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(60f, 22f);
        Text t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        t.text = content;
        return t;
    }

    Marker NewMarker()
    {
        Marker m = new Marker();

        GameObject go = new GameObject("Marker", typeof(RectTransform));
        m.root = go.GetComponent<RectTransform>();
        m.root.SetParent(bar, false);
        m.root.sizeDelta = new Vector2(20f, 20f);

        // diamond = a square turned 45 degrees
        m.icon = MakeImage(m.root, "Icon", new Vector2(11f, 11f));
        m.icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        // distance text under the diamond
        m.dist = MakeText(m.root, "Distance", 11, FontStyle.Normal, "");
        m.dist.rectTransform.anchoredPosition = new Vector2(0f, -13f);

        return m;
    }

    // ------------------------------------------------------------ find targets

    void AddTarget(Transform t, Color c)
    {
        if (t == null) return;
        if (!targetSet.Add(t)) return;   // already in the list
        targets.Add(new Target { t = t, c = c });
    }

    void RebuildTargets()
    {
        targets.Clear();
        targetSet.Clear();

        bool anyTasks = false;
        bool allComplete = true;

        // 1) Every task that is not fixed yet
        MazeTaskGroup[] groups = FindObjectsByType<MazeTaskGroup>(FindObjectsSortMode.None);
        foreach (MazeTaskGroup g in groups)
        {
            if (g.Total == 0) continue;
            anyTasks = true;
            if (!g.IsComplete) allComplete = false;

            Color col = ColorFor(g.taskName);
            foreach (Object item in g.Items)
            {
                if (item == null) continue;
                if (g.IsItemDone(item)) continue;

                Transform t = ToTransform(item);
                if (t == null || !t.gameObject.activeInHierarchy) continue;

                AddTarget(t, col);
            }
        }

        if (anyTasks && allComplete)
        {
            // 3) Everything fixed: show only the exit
            targets.Clear();
            targetSet.Clear();
            if (exitTarget != null) AddTarget(exitTarget, exitColor);
        }
        else
        {
            // 2) Loose parts lying around (not carried, not used)
            PartPickup[] parts = FindObjectsByType<PartPickup>(FindObjectsSortMode.None);
            foreach (PartPickup p in parts)
            {
                if (p == null || p.IsCarried || p.IsUsed) continue;
                AddTarget(p.transform, partColor);
            }
        }

        SyncMarkers();
    }

    // Gives every target ITS OWN marker, and frees markers of targets that are gone.
    void SyncMarkers()
    {
        // free markers whose target is gone
        toRemove.Clear();
        foreach (KeyValuePair<Transform, Marker> kv in active)
        {
            if (kv.Key == null || !targetSet.Contains(kv.Key)) toRemove.Add(kv.Key);
        }
        foreach (Transform key in toRemove)
        {
            Marker old = active[key];
            old.root.gameObject.SetActive(false);
            freeMarkers.Add(old);
            active.Remove(key);
        }

        // give new targets a marker
        foreach (Target tg in targets)
        {
            Marker m;
            if (active.TryGetValue(tg.t, out m))
            {
                m.color = tg.c;
                continue;
            }

            if (freeMarkers.Count > 0)
            {
                m = freeMarkers[freeMarkers.Count - 1];
                freeMarkers.RemoveAt(freeMarkers.Count - 1);
            }
            else
            {
                m = NewMarker();
            }

            m.color = tg.c;
            m.root.gameObject.SetActive(true);
            active[tg.t] = m;
        }
    }

    Color ColorFor(string taskName)
    {
        if (taskName == "Bent") return bentColor;
        if (taskName == "MissingParts") return missingColor;
        if (taskName == "Fried") return friedColor;
        return Color.white;
    }

    static Transform ToTransform(Object o)
    {
        if (o == null) return null;
        Component c = o as Component;
        if (c != null) return c.transform;
        GameObject g = o as GameObject;
        if (g != null) return g.transform;
        return null;
    }

    // ------------------------------------------------------------ draw each frame

    void LateUpdate()
    {
        if (bar == null) return;

        ApplyLayout();

        // Pick the camera that is really drawing the screen (checked twice a second,
        // so it also follows when you switch between first / third person)
        if (cam == null || !cam.isActiveAndEnabled || Time.time >= nextCamCheck)
        {
            nextCamCheck = Time.time + 0.5f;
            cam = PickCamera();
            if (cam != null && cam.name != lastCamName)
            {
                lastCamName = cam.name;
                Debug.Log("[ObjectiveCompass] Using camera: " + cam.name);
            }
        }
        if (cam == null) return;

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (Time.time >= nextRefresh)
        {
            nextRefresh = Time.time + refreshTime;
            RebuildTargets();
        }

        float yaw = SteadyYaw(GetYaw(cam.transform));

        // letters
        for (int i = 0; i < 4; i++)
        {
            float delta = Mathf.DeltaAngle(yaw, i * 90f + northOffset);
            float a = AlphaFor(delta);
            letters[i].rectTransform.anchoredPosition = new Vector2(XFor(delta), LetterY);
            letters[i].color = new Color(1f, 1f, 1f, a);
        }

        // ticks
        for (int i = 0; i < ticks.Count; i++)
        {
            float delta = Mathf.DeltaAngle(yaw, tickAngles[i] + northOffset);
            float a = AlphaFor(delta);
            ticks[i].rectTransform.anchoredPosition = new Vector2(XFor(delta), LetterY);
            ticks[i].color = new Color(1f, 1f, 1f, a * 0.6f);
        }

        // markers
        Vector3 camPos = cam.transform.position;
        Vector3 from = player != null ? player.position : camPos;

        foreach (Target tg in targets)
        {
            Marker m;
            if (tg.t == null || !active.TryGetValue(tg.t, out m)) continue;

            Vector3 d = tg.t.position - camPos;          // direction as seen from the camera
            float bearing = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            float delta = Mathf.DeltaAngle(yaw, bearing);
            float a = AlphaFor(delta);

            m.root.anchoredPosition = new Vector2(XFor(delta), MarkerY);
            m.icon.color = new Color(m.color.r, m.color.g, m.color.b, a);

            m.dist.gameObject.SetActive(showDistance);
            if (showDistance)
            {
                m.dist.text = Mathf.RoundToInt((tg.t.position - from).magnitude) + "m";
                m.dist.color = new Color(1f, 1f, 1f, a * 0.85f);
            }
        }
    }

    // The camera that is really drawing the screen: the one you dragged in, otherwise
    // the enabled camera with the highest Depth that draws to the screen.
    Camera PickCamera()
    {
        if (viewCamera != null) return viewCamera;

        Camera best = null;
        foreach (Camera c in Camera.allCameras)
        {
            if (!c.enabled || !c.gameObject.activeInHierarchy) continue;
            if (c.targetTexture != null) continue;           // skip cameras that draw to a texture
            if (best == null || c.depth > best.depth) best = c;
        }
        return best != null ? best : Camera.main;
    }

    // Which way the camera faces, left/right only (0 = +Z).
    // eulerAngles.y goes wild when the camera looks (almost) straight up or down,
    // so in that case we use the top of the screen (camera up) instead.
    static float GetYaw(Transform t)
    {
        Vector3 f = t.forward;
        Vector3 flatF = new Vector3(f.x, 0f, f.z);
        Vector3 dir;

        if (flatF.magnitude > 0.35f)
        {
            dir = flatF;                         // looking fairly level: normal
        }
        else
        {
            Vector3 u = t.up;
            Vector3 flatU = new Vector3(u.x, 0f, u.z);
            dir = f.y > 0f ? -flatU : flatU;     // looking up / down
        }

        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
        return Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
    }

    // Small smoothing so tiny camera shakes do not make the markers twitch
    float SteadyYaw(float rawYaw)
    {
        if (!yawReady || headingSmoothing <= 0f)
        {
            smoothYaw = rawYaw;
            yawReady = true;
            return smoothYaw;
        }
        float k = 1f - Mathf.Exp(-headingSmoothing * Time.deltaTime);
        smoothYaw = Mathf.LerpAngle(smoothYaw, rawYaw, k);
        return smoothYaw;
    }

    // Left / right position on the bar for an angle difference
    float XFor(float delta)
    {
        float half = Mathf.Max(1f, viewAngle * 0.5f);
        return delta / half * (barWidth * 0.5f);
    }

    // 1 = fully visible, 0 = hidden (fades near the edges)
    float AlphaFor(float delta)
    {
        float half = viewAngle * 0.5f;
        return Mathf.Clamp01((half - Mathf.Abs(delta)) / Mathf.Max(1f, edgeFade));
    }

    void OnDestroy()
    {
        if (bar != null) Destroy(bar.gameObject);
    }
}