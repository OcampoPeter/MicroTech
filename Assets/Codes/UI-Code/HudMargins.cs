using UnityEngine;

// HUD MARGINS. Put this on an empty UI object called "HUD Area" that sits inside your Canvas.
// Drag your HUD pieces (timer, corruption, difficulty, objectives, compass, speed) INTO "HUD Area".
// Then use the 4 numbers below to move the whole HUD away from the screen edges, so nothing is cramped.
// It updates live, even when the game is not playing, so you can watch the Game view while you change them.
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class HUDMargins : MonoBehaviour
{
    [Header("Space from the screen edge (canvas units, about pixels)")]
    [Range(0f, 300f)] public float left = 40f;
    [Range(0f, 300f)] public float right = 40f;
    [Range(0f, 300f)] public float top = 30f;
    [Range(0f, 300f)] public float bottom = 30f;

    void OnEnable() { Apply(); }
    void OnValidate() { Apply(); }   // runs when you change a number in the Inspector

    void Update()
    {
        // Only needed if the screen size changes while playing
        if (Application.isPlaying) Apply();
    }

    void Apply()
    {
        RectTransform rt = transform as RectTransform;
        if (rt == null) return;

        // Fill the whole canvas, then pull in from each side
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);      // left and bottom edges move in
        rt.offsetMax = new Vector2(-right, -top);      // right and top edges move in
    }
}