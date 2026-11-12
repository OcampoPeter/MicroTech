using UnityEngine;

[DefaultExecutionOrder(100)]   // runs after CameraModeSwitcher's own Start
public class SceneCameraMode : MonoBehaviour
{
    public CameraMode mode = CameraMode.ThirdPerson;

    [Tooltip("Untick in the TS room so the player can only walk.")]
    public bool allowJump = true;

    void Start()
    {
        if (CameraModeSwitcher.Instance != null)
            CameraModeSwitcher.Instance.SetMode(mode);
        else
            Debug.LogWarning("SceneCameraMode: no CameraModeSwitcher found. Is the Player in this scene?");

        PlayerMovement movement = FindFirstObjectByType<PlayerMovement>();
        if (movement != null) movement.canJump = allowJump;
        else Debug.LogWarning("SceneCameraMode: no PlayerMovement found, so Allow Jump was not applied.");

        Debug.Log("SceneCameraMode: view = " + mode.ToString() + ", allow jump = " + allowJump.ToString());
    }
}