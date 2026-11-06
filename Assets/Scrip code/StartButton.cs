using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartButton : MonoBehaviour
{
    // Optional: type the exact scene name in the Inspector.
    // If left empty, the script loads the next scene in Build Settings.
    [SerializeField] private string sceneName = "";

    private void Start()
    {
        // Automatically hooks up the click if this script is on the button
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(LoadNextScene);
        }
    }

    // You can also call this from the Button's On Click() list in the Inspector
    public void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;

            if (nextIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextIndex);
            }
            else
            {
                Debug.LogWarning("No next scene found. Add your scenes to File > Build Profiles (Build Settings).");
            }
        }
    }
}