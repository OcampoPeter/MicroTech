using UnityEngine;
using TMPro;

public class RunTimer : MonoBehaviour
{
    public static RunTimer Instance;

    [Header("UI")]
    public TMP_Text timerText;
    [Tooltip("A second text placed next to the timer. It shows +3s when a hazard hits.")]
    public TMP_Text penaltyText;
    [Tooltip("How long the +3s stays on screen (seconds).")]
    public float penaltyShowTime = 1.5f;

    public float Time_ { get; private set; }   // seconds on the clock
    public float PenaltyTotal { get; private set; }

    private float penaltyShownAt = -10f;
    private bool stopped;

    void Awake() { Instance = this; }

    void Start()
    {
        if (penaltyText != null) penaltyText.text = "";
    }

    void Update()
    {
        if (!stopped) Time_ += Time.deltaTime;
        if (timerText != null)
            timerText.text = "TIME " + Time_.ToString("F1") + "s";

        // Fade the +3s out after it appears
        if (penaltyText != null)
        {
            float age = Time.time - penaltyShownAt;
            if (age < penaltyShowTime)
            {
                Color c = penaltyText.color;
                c.a = 1f - (age / penaltyShowTime);
                penaltyText.color = c;
            }
            else
            {
                penaltyText.text = "";
            }
        }
    }

    // Used by the finish line: freezes the clock
    public void Stop()
    {
        stopped = true;
    }

    // Used by hazards: adds seconds to the clock as a penalty
    public void AddPenalty(float seconds)
    {
        Time_ += seconds;
        PenaltyTotal += seconds;

        if (penaltyText != null)
        {
            penaltyText.text = "+" + seconds.ToString("F0") + "s";
            penaltyText.color = Color.red;
            penaltyShownAt = Time.time;
        }
    }
}