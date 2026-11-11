using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Exact name of the TS Area scene (must be in Build Profiles)")]
    public string tsSceneName = "TS Room";

    public void PlayGame()
    {
        Time.timeScale = 1f;                 // in case the game was frozen before
        SceneManager.LoadScene(tsSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();                  // does nothing in the editor, works in a build
    }
}