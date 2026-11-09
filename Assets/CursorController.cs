using UnityEngine;
using UnityEngine.InputSystem;

// Shift lock: locks the mouse arrow to the middle of the screen and hides it,
// so you can look around without the arrow getting in the way.
// Put this on any object that is always there (for example the Player).
public class CursorController : MonoBehaviour
{
    [Tooltip("Press this key to lock or free the mouse arrow.")]
    public Key toggleKey = Key.LeftCtrl;
    [Tooltip("TS area: ON. Mazes: your choice.")]
    public bool startLocked = true;

    private bool locked;

    void Start()
    {
        locked = startLocked;
    }

    void Update()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current[toggleKey].wasPressedThisFrame) locked = !locked;
            if (Keyboard.current.escapeKey.wasPressedThisFrame) locked = false;   // Esc always frees the arrow
        }

        // The arrow is always free while inspecting an item, and on the crash and complete screens
        bool wantLocked = locked && !InspectMode.Active && Time.timeScale > 0f;

        Cursor.lockState = wantLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !wantLocked;
    }
}