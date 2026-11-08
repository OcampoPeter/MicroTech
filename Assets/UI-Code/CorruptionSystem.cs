using UnityEngine;
using TMPro;

// Corruption is our health system. 0% = healthy, 100% = the PC crashes.
// Put this on an empty object called "CorruptionSystem".
public class CorruptionSystem : MonoBehaviour
{
    public static CorruptionSystem Instance;

    [Header("UI")]
    public TMP_Text corruptionText;

    [Header("Settings")]
    public float maxCorruption = 100f;

    public float Corruption { get; private set; }
    public bool IsCrashed { get; private set; }

    void Awake() { Instance = this; }

    void Start() { UpdateText(); }

    public void AddCorruption(float amount)
    {
        if (IsCrashed) return;

        Corruption = Mathf.Min(maxCorruption, Corruption + amount);
        UpdateText();

        if (Corruption >= maxCorruption) Crash();
    }

    void Crash()
    {
        IsCrashed = true;
        // The Game Manager shows the blue screen and freezes the game
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Crashed);
    }

    void UpdateText()
    {
        if (corruptionText != null)
            corruptionText.text = "CORRUPTION " + Corruption.ToString("F0") + "%";
    }
}