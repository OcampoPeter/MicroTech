using UnityEngine;

// Put this on any item the player can pick up (a box for now).
// The name and description are used in a later step (the hover label).
public class Grabbable : MonoBehaviour
{
    public string displayName = "Item";
    [TextArea] public string description = "A part of the PC.";

    // True while the player is holding this item. ItemGrabber sets it.
    public bool IsHeld { get; set; }
}   