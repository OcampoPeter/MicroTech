using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// The three states the game can be in.
public enum GameState
{
    Playing,
    Complete,
    Crashed
}

// Step 3: the Game Manager now controls the end screens and the restart.
// Put it on an empty object called "GameManager".
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Screens (leave both turned OFF in the Hierarchy)")]
    public GameObject crashPanel;      // the blue screen
    public GameObject completePanel;   // shown when you reach the finish line

    // Shown in the Inspector while you play, so you can watch it change
    public GameState State = GameState.Playing;

    void Awake() { Instance = this; }

    void Start()
    {
        Time.timeScale = 1f;
        if (crashPanel != null) crashPanel.SetActive(false);
        if (completePanel != null) completePanel.SetActive(false);
    }

    void Update()
    {
        // After the game ends, R restarts. Update still runs while time is frozen.
        if (State != GameState.Playing && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            Restart();
    }

    public void SetState(GameState newState)
    {
        if (State == newState) return;
        State = newState;
        Debug.Log("Game state is now: " + State.ToString());

        if (State == GameState.Crashed)
        {
            if (crashPanel != null) crashPanel.SetActive(true);
            Time.timeScale = 0f;   // freezes the game
        }
        else if (State == GameState.Complete)
        {
            if (completePanel != null) completePanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    public void Restart()
    {
        Time.timeScale = 1f;   // un-freeze before reloading
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}