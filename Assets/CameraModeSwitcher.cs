using UnityEngine;

public enum CameraMode
{
    FirstPerson,
    ThirdPerson
}

// Put this on the Player. It switches between two Cinemachine cameras.
// Set "Start Mode" differently in each scene: FirstPerson in the TS area, ThirdPerson in the mazes.
public class CameraModeSwitcher : MonoBehaviour
{
    public static CameraModeSwitcher Instance;

    [Header("The two cameras (the CinemachineCamera objects)")]
    public GameObject firstPersonCamera;
    public GameObject thirdPersonCamera;

    [Header("Mode")]
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
        SetMode(startMode);
    }

    public void SetMode(CameraMode mode)
    {
        Mode = mode;
        bool first = mode == CameraMode.FirstPerson;

        // Only the active camera is used by the Cinemachine Brain
        if (firstPersonCamera != null) firstPersonCamera.SetActive(first);
        if (thirdPersonCamera != null) thirdPersonCamera.SetActive(!first);

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