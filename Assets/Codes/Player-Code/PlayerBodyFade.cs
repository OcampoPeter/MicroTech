using UnityEngine;

// BODY FADE. Put this on the Player.
// Like Genshin / Wuthering Waves: the character becomes see-through when the camera is
// very close to it, or when you look up (the camera sits low and the body would block the view).
// It only changes how the body LOOKS. It never touches the hitbox or movement.
public class PlayerBodyFade : MonoBehaviour
{
    [Header("Fade by camera distance (meters)")]
    [Tooltip("THE RADIUS: when the camera is closer to the player than this, the body is fully faded.")]
    public float fadeRadius = 2f;
    [Tooltip("Farther than this the body is fully solid. Between the two it fades smoothly.")]
    public float solidBeyond = 3.5f;

    [Header("Fade by looking up (degrees) - OFF by default, the camera distance does the work")]
    public bool useLookUpFade = false;
    [Tooltip("Looking up less than this = no fade.")]
    public float lookUpStart = 20f;
    [Tooltip("Looking up this much or more = fully faded.")]
    public float lookUpFull = 50f;

    [Header("Fade by looking down at the player from above (degrees)")]
    public bool useLookDownFade = false;
    [Tooltip("Looking down less steeply than this = no fade.")]
    public float lookDownStart = 35f;
    [Tooltip("Looking down this steeply or more = fully faded.")]
    public float lookDownFull = 65f;

    [Header("Look")]
    [Tooltip("How see-through the body gets at most. 0 = invisible, 1 = solid.")]
    [Range(0f, 1f)] public float minAlpha = 0.12f;
    [Tooltip("How fast it fades in and out.")]
    public float fadeSpeed = 6f;

    private CharacterController controller;
    private PlayerMovement movement;
    private Renderer[] renderers;
    private Material[] mats;          // our own copies of the materials, so the originals stay untouched
    private float[] baseAlpha;
    private bool[] isTransparent;
    private float alpha = 1f;
    private float lastDistance;

    [Header("Testing")]
    public bool showDebug = true;   // small text: how far the camera is, and how solid the body is

    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int ColorProp = Shader.PropertyToID("_Color");

    void Start()
    {
        controller = GetComponent<CharacterController>();
        movement = GetComponent<PlayerMovement>();

        renderers = GetComponentsInChildren<Renderer>(true);
        System.Collections.Generic.List<Material> list = new System.Collections.Generic.List<Material>();
        foreach (Renderer r in renderers)
        {
            if (r.name.Contains("Hitbox")) continue;       // leave the hitbox alone
            foreach (Material m in r.materials) list.Add(m);   // r.materials makes copies for this renderer
        }
        mats = list.ToArray();
        baseAlpha = new float[mats.Length];
        isTransparent = new bool[mats.Length];
        for (int i = 0; i < mats.Length; i++) baseAlpha[i] = GetAlpha(mats[i]);
    }

    void LateUpdate()
    {
        if (mats == null || mats.Length == 0) return;

        // In first person the body is hidden by the camera switcher already
        if (CameraModeSwitcher.Instance != null && CameraModeSwitcher.Instance.Mode == CameraMode.FirstPerson) return;

        // The camera that is really on screen (the one Cinemachine moves)
        Transform cam = Camera.main != null ? Camera.main.transform : ((movement != null) ? movement.playerCamera : null);
        if (cam == null) return;

        Vector3 center = transform.position + (controller != null ? controller.center : Vector3.up);

        // 1) Camera close to the body
        float dist = Vector3.Distance(cam.position, center);
        lastDistance = dist;
        float distanceAlpha = Mathf.Lerp(minAlpha, 1f, Mathf.InverseLerp(fadeRadius, solidBeyond, dist));

        // 2) Looking up (positive = looking up)
        float pitchUp = Mathf.Asin(Mathf.Clamp(cam.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
        float lookAlpha = 1f;
        if (useLookUpFade)
            lookAlpha = Mathf.Lerp(1f, minAlpha, Mathf.InverseLerp(lookUpStart, lookUpFull, pitchUp));

        // 3) Looking down at the top of the player (camera above, looking steeply down)
        float lookDownAlpha = 1f;
        if (useLookDownFade)
            lookDownAlpha = Mathf.Lerp(1f, minAlpha, Mathf.InverseLerp(lookDownStart, lookDownFull, -pitchUp));

        float target = Mathf.Min(distanceAlpha, Mathf.Min(lookAlpha, lookDownAlpha));
        alpha = Mathf.MoveTowards(alpha, target, fadeSpeed * Time.deltaTime);

        bool needTransparent = alpha < 0.99f;
        for (int i = 0; i < mats.Length; i++)
        {
            if (mats[i] == null) continue;
            if (needTransparent != isTransparent[i]) SetTransparent(mats[i], needTransparent, i);
            if (isTransparent[i]) SetAlpha(mats[i], baseAlpha[i] * alpha);
        }
    }

    // ---- material helpers (URP Lit) ----
    float GetAlpha(Material m)
    {
        if (m.HasProperty(BaseColor)) return m.GetColor(BaseColor).a;
        if (m.HasProperty(ColorProp)) return m.GetColor(ColorProp).a;
        return 1f;
    }

    void SetAlpha(Material m, float a)
    {
        if (m.HasProperty(BaseColor)) { Color c = m.GetColor(BaseColor); c.a = a; m.SetColor(BaseColor, c); }
        else if (m.HasProperty(ColorProp)) { Color c = m.GetColor(ColorProp); c.a = a; m.SetColor(ColorProp, c); }
    }

    // Switches a material between solid and see-through. Only see-through while it is actually faded,
    // because see-through materials can look wrong up close when they are fully solid.
    void SetTransparent(Material m, bool on, int index)
    {
        isTransparent[index] = on;
        if (!m.HasProperty("_Surface")) return;   // not a URP Lit style material

        if (on)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else
        {
            SetAlpha(m, baseAlpha[index]);
            m.SetFloat("_Surface", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
            m.SetFloat("_ZWrite", 1f);
            m.SetOverrideTag("RenderType", "Opaque");
            m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
        }
    }

    void OnGUI()
    {
        if (!showDebug || Event.current.type != EventType.Repaint) return;
        GUI.Box(new Rect(10f, 160f, 260f, 48f), "camera distance: " + lastDistance.ToString("F1") + " m\nbody opacity: " + alpha.ToString("F2"));
    }
}