using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Put this on an item that also has Grabbable. Pick it up first (F), then press E to go to its map.
// Every item can point to a different map, just by typing a different scene name.
[RequireComponent(typeof(Grabbable))]
public class MapPortal : MonoBehaviour
{
    [Header("Where this item sends you")]
    [Tooltip("The exact name of the scene, for example Maze. It must be in File > Build Profiles > Scene List.")]
    public string sceneToLoad = "Maze";

    [Header("Interaction")]
    public Key interactKey = Key.E;
    [Tooltip("Seconds to wait before the scene loads. Gives time for a loading screen later.")]
    public float loadDelay = 0.5f;

    // The scene to come back to. Another script can read this later to send you home.
    public static string ReturnScene;

    private Grabbable grabbable;
    private bool loading;

    void Start()
    {
        grabbable = GetComponent<Grabbable>();
    }

    void Update()
    {
        if (loading || Keyboard.current == null) return;

        // Only works while you are holding this item
        if (!grabbable.IsHeld) return;

        if (Keyboard.current[interactKey].wasPressedThisFrame)
            StartCoroutine(GoToMap());
    }

    System.Collections.IEnumerator GoToMap()
    {
        loading = true;
        ReturnScene = SceneManager.GetActiveScene().name;   // remember where we came from
        Debug.Log("Going to map: " + sceneToLoad);

        yield return new WaitForSeconds(loadDelay);
        SceneManager.LoadScene(sceneToLoad);
    }
}