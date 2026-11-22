using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class CursorController : MonoBehaviour
{
    [Header("Toggle")]
    [Tooltip("Press this key to switch between the dot and the mouse arrow.")]
    public Key toggleKey = Key.LeftAlt;
    [Tooltip("ON: the scene starts with the dot. OFF: it starts with the mouse arrow.")]
    public bool startLocked = true;

    [Header("Dot crosshair")]
    public Color dotColor = Color.white;
    public Color outlineColor = new Color(0f, 0f, 0f, 0.8f);
    [Tooltip("Size of the dot in pixels. Bigger = easier to see.")]
    public float dotSize = 10f;
    [Tooltip("Dark ring around the dot so it shows on any background (pixels).")]
    public float outlineThickness = 2f;

    private bool locked;
    private GameObject crosshair;
    private Sprite circleSprite;

    void Start()
    {
        locked = startLocked;
        BuildCrosshair();
    }

    void Update()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current[toggleKey].wasPressedThisFrame) locked = !locked;
            if (Keyboard.current.escapeKey.wasPressedThisFrame) locked = false;   // Esc always frees the arrow
        }

        // The arrow is always free while inspecting an item, and on the crash and complete screens
        bool wantLocked = locked && !InspectMode.Active && Time.timeScale > 0f;

        Cursor.lockState = wantLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !wantLocked;
        // The dot only shows in first person. The cursor lock still works in third person.
        bool firstPerson = CameraModeSwitcher.Instance == null
                           || CameraModeSwitcher.Instance.Mode == CameraMode.FirstPerson;
        if (crosshair != null) crosshair.SetActive(wantLocked && firstPerson);
    }

    // Builds the dot in the middle of the screen. No UI setup needed.
    void BuildCrosshair()
    {
        circleSprite = MakeCircleSprite();

        crosshair = new GameObject("Crosshair");
        Canvas canvas = crosshair.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;   // on top of other UI

        MakeCircle("Outline", dotSize + outlineThickness * 2f, outlineColor);
        MakeCircle("Dot", dotSize, dotColor);
    }

    void MakeCircle(string circleName, float size, Color color)
    {
        GameObject go = new GameObject(circleName);
        go.transform.SetParent(crosshair.transform, false);
        Image img = go.AddComponent<Image>();
        img.sprite = circleSprite;
        img.color = color;
        img.raycastTarget = false;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(size, size);
    }

    // Draws a smooth white circle into a small texture
    Sprite MakeCircleSprite()
    {
        int res = 64;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float r = res * 0.5f;
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                float alpha = Mathf.Clamp01(r - d);   // soft edge
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100f);
    }

    void OnDestroy()
    {
        if (crosshair != null) Destroy(crosshair);
    }
}