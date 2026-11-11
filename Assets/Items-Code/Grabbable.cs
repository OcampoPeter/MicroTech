using UnityEngine;

public class Grabbable : MonoBehaviour
{
    public string displayName = "Item";
    [TextArea] public string description = "A part of the PC.";

    // True while the player is holding this item. ItemGrabber sets it.
    public bool IsHeld { get; set; }
}   