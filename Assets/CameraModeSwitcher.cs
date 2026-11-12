using UnityEngine;
using UnityEngine.SceneManagement;

public enum CameraMode
{
    FirstPerson,
    ThirdPerson
}

// Put this on the Player. It switches between two Cinemachine cameras.
// Automatic: the TS scene is always first person (and the player can't jump there).
// Every other scene is third person (and the player can jump). New scenes need nothing set up.
public class CameraModeSwitcher : MonoBehaviour
{
    public static CameraModeSwitcher Instance;

    [Header("The two cameras (the CinemachineCamera objects)")]
    public GameObject firstPersonCamera;
    public GameObject thirdPersonCamera;

    [Header("Automatic by scene name")]
    public bool decideBySceneName = true;
    [Tooltip("The exact name of the TS scene. This scene is first person with no jumping. Every other scene is third person.")]
    public string firstPersonSceneName = "TS Room";

    [Header("Used only when 'Decide By Scene Name' is off")]
    public CameraMode startMode = CameraMode.ThirdPerson;

    [Tooltip("Parts of the character to hide in first person (leave empty to hide every Renderer under the Player).")]
    public Renderer[] modelRenderers;

    public CameraMode Mode { get; private set; }

    void Awake()
    {
        Instance = this;
        if (modelRenderers == null || modelRenderers.Length == 0)
            modelRenderers = GetComponentsInChildren<Renderer>();
    }

    void Start()
    {
        // If the slot is empty (for example in a new scene), look for the camera by its name
        if (firstPersonCamera == null)
            firstPersonCamera = GameObject.Find("FirstPersonCamera");

        CameraMode chosen = startMode;
        if (decideBySceneName)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            bool isTsScene = sceneName == firstPersonSceneName;
            chosen = isTsScene ? CameraMode.FirstPerson : CameraMode.ThirdPerson;

            // No jumping in the TS room, jumping everywhere else
            PlayerMovement movement = GetComponent<PlayerMovement>();
            if (movement != null) movement.canJump = !isTsScene;

            Debug.Log("Scene '" + sceneName + "' -> " + chosen.ToString() + ", jumping " + (!isTsScene ? "on" : "off"));
        }

        SetMode(chosen);
    }

    public void SetMode(CameraMode mode)
    {
        Mode = mode;
        bool first = mode == CameraMode.FirstPerson;

        // Only the active camera is used by the Cinemachine Brain
        if (firstPersonCamera != null) firstPersonCamera.SetActive(first);
        if (thirdPersonCamera != null) thirdPersonCamera.SetActive(!first);

        // In first person the player must not turn toward its walking direction (it would spin forever)
        PlayerMovement mover = GetComponent<PlayerMovement>();
        if (mover != null) mover.rotateToMovement = !first;

        // Hide the body in first person so the camera isn't inside it
        foreach (Renderer r in modelRenderers)
            if (r != null) r.enabled = !first;

        Debug.Log("Camera mode: " + mode.ToString());
    }

    public void Toggle()
    {
        SetMode(Mode == CameraMode.FirstPerson ? CameraMode.ThirdPerson : CameraMode.FirstPerson);
    }
}