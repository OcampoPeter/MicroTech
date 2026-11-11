using UnityEngine;
using TMPro;

public class SpeedIndicator : MonoBehaviour
{
    [Header("References")]
    public PlayerMovement player;
    public TMP_Text speedText;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color fastColor = Color.yellow;
    public Color veryFastColor = Color.red;

    [Header("When the colors change")]
    [Tooltip("Speed where it is fully RED, as a multiple of your max sprint speed. 1.6 = red at 16 m/s when max sprint is 10.")]
    public float redAtMultiple = 1.6f;
    [Tooltip("How smoothly the number follows your real speed.")]
    public float numberSmoothing = 12f;

    private float shownSpeed;

    void Reset()
    {
        speedText = GetComponent<TMP_Text>();
    }

    void Update()
    {
        if (player == null || speedText == null) return;

        // Smooth the number so it doesn't jitter
        shownSpeed = Mathf.Lerp(shownSpeed, player.HorizontalSpeed, numberSmoothing * Time.unscaledDeltaTime);

        float sprint = player.maxSprintSpeed;
        float red = sprint * redAtMultiple;

        Color color;
        if (shownSpeed <= sprint)
        {
            color = normalColor;                                   // at or under normal sprint
        }
        else
        {
            float t = Mathf.InverseLerp(sprint, red, shownSpeed);  // 0 at sprint speed, 1 at red speed
            if (t < 0.5f) color = Color.Lerp(normalColor, fastColor, t * 2f);          // white to yellow
            else color = Color.Lerp(fastColor, veryFastColor, (t - 0.5f) * 2f);        // yellow to red
        }

        speedText.color = color;
        speedText.text = shownSpeed.ToString("F0") + " m/s";
    }
}