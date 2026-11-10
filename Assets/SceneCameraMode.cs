using UnityEngine;


public class SceneCameraMode : MonoBehaviour
{
    public CameraMode mode = CameraMode.ThirdPerson;

    void Start()
    {
        if (CameraModeSwitcher.Instance != null)
            CameraModeSwitcher.Instance.SetMode(mode);
        else
            Debug.LogWarning("SceneCameraMode: no CameraModeSwitcher found. Is the Player in this scene?");
    }
}