using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

// Corruption is our health system. 0% = healthy, 100% = the PC crashes.
// Put this on an empty object called "CorruptionSystem".
public class CorruptionSystem : MonoBehaviour
{
    public static CorruptionSystem Instance;

    [Header("UI")]
    public TMP_Text corruptionText;
    [Tooltip("The blue crash panel. Leave it turned OFF in the Hierarchy at the start.")]
    public GameObject crashPanel;

    [Header("Settings")]
    public float maxCorruption = 100f;

    public float Corruption { get; private set; }
    public bool IsCrashed { get; private set; }

    void Awake() { Instance = this; }

    void Start()
    {
        Time.timeScale = 1f;
        if (crashPanel != null) crashPanel.SetActive(false);
        UpdateText();
    }

    void Update()
    {
        // While crashed, wait for R. Update still runs even though time is frozen.
        if (IsCrashed && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            Time.timeScale = 1f;   // un-freeze before reloading
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

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
        Time.timeScale = 0f;   // freezes the game
        if (crashPanel != null) crashPanel.SetActive(true);
    }

    void UpdateText()
    {
        if (corruptionText != null)
            corruptionText.text = "CORRUPTION " + Corruption.ToString("F0") + "%";
    }
}