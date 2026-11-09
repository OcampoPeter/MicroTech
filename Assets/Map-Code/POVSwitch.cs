using UnityEngine;

public enum POVBlockAction
{
    Toggle,
    FirstPerson,
    ThirdPerson
}

// Put this on a block (a cube). Turn on "Is Trigger" on its collider.
// When the player steps into it, the camera mode changes.
public class POVSwitchBlock : MonoBehaviour
{
    public POVBlockAction action = POVBlockAction.Toggle;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        CameraModeSwitcher switcher = other.GetComponentInParent<CameraModeSwitcher>();
        if (switcher == null) return;

        if (action == POVBlockAction.Toggle) switcher.Toggle();
        else if (action == POVBlockAction.FirstPerson) switcher.SetMode(CameraMode.FirstPerson);
        else switcher.SetMode(CameraMode.ThirdPerson);
    }
}