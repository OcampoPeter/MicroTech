using UnityEngine;
using UnityEngine.InputSystem;

// True while the player is inspecting an item. Other scripts can read it.
public static class InspectMode
{
    public static bool Active;
}

// F = pick up / drop. While you carry an item you can still move around.
// Q = inspect the item you carry: you stop moving, the arrow is freed, and you can rotate the item
//     with left mouse + mouse movement. Press Q again to go back to carrying.
// Put this on the Player and drag the Main Camera into the Cam slot.
public class ItemGrabber : MonoBehaviour
{
    [Header("References")]
    public Camera cam;

    [Header("Pick up")]
    public Key pickupKey = Key.F;
    public float pickupRange = 8f;
    [Tooltip("How far in front of the camera the item floats while you carry it.")]
    public float holdDistance = 3f;
    [Tooltip("How quickly the item flies to its spot. Higher = snappier.")]
    public float followSpeed = 12f;

    [Header("Inspect")]
    public Key inspectKey = Key.Q;
    [Tooltip("How close the item comes to the camera while inspecting.")]
    public float inspectDistance = 1.5f;
    public float rotateSpeed = 0.3f;

    // Other scripts can check this while the item is being rotated
    public static bool IsRotatingItem;

    // True while the player carries an item
    public bool IsHolding { get { return held != null; } }

    private Grabbable held;
    private Rigidbody heldBody;
    private bool heldWasKinematic;
    private PlayerMovement movement;
    private PlayerWallRun wallRun;

    void Start()
    {
        if (cam == null) cam = Camera.main;
        movement = GetComponent<PlayerMovement>();
        wallRun = GetComponent<PlayerWallRun>();
        InspectMode.Active = false;   // start every scene not inspecting
    }

    void OnDisable()
    {
        if (InspectMode.Active) ExitInspect();
    }

    void Update()
    {
        if (Keyboard.current == null || Mouse.current == null) return;

        // F picks up what you look at, or drops what you hold
        if (Keyboard.current[pickupKey].wasPressedThisFrame)
        {
            if (held == null) TryPickUp();
            else Drop();
        }

        // Q starts or stops inspecting (only while you hold something)
        if (held != null && Keyboard.current[inspectKey].wasPressedThisFrame)
        {
            if (InspectMode.Active) ExitInspect();
            else EnterInspect();
        }

        IsRotatingItem = held != null && InspectMode.Active && Mouse.current.leftButton.isPressed;

        if (held != null)
        {
            // Float to the point in front of the camera (closer while inspecting)
            float distance = InspectMode.Active ? inspectDistance : holdDistance;
            Vector3 target = cam.transform.position + cam.transform.forward * distance;
            held.transform.position = Vector3.Lerp(held.transform.position, target, followSpeed * Time.deltaTime);

            // Left mouse + mouse movement rotates the item (inspect mode only)
            if (IsRotatingItem)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                held.transform.Rotate(cam.transform.up, -delta.x * rotateSpeed, Space.World);
                held.transform.Rotate(cam.transform.right, delta.y * rotateSpeed, Space.World);
            }
        }
    }

    void EnterInspect()
    {
        InspectMode.Active = true;

        // Freeze the player
        if (movement != null)
        {
            movement.ResetMotion();
            movement.enabled = false;
        }
        if (wallRun != null) wallRun.enabled = false;

        SetCameraInput(false);   // the camera stops orbiting with the mouse
        Debug.Log("Inspecting: " + held.displayName);
    }

    void ExitInspect()
    {
        InspectMode.Active = false;
        IsRotatingItem = false;

        if (movement != null) movement.enabled = true;
        if (wallRun != null) wallRun.enabled = true;

        SetCameraInput(true);
    }

    // Turns the mouse look of every Cinemachine camera on or off
    void SetCameraInput(bool on)
    {
        var controllers = FindObjectsByType<Unity.Cinemachine.CinemachineInputAxisController>(FindObjectsInactive.Include);
        foreach (var c in controllers) c.enabled = on;
    }

    void TryPickUp()
    {
        // A ray from the middle of the screen. In third person the camera is behind the player,
        // so the ray can hit the player first. RaycastAll lets us skip the player and look further.
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, pickupRange + 10f);
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        Grabbable item = null;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;   // ignore ourselves (the player)

            item = hit.collider.GetComponentInParent<Grabbable>();
            if (item == null) break;   // something solid is in the way, stop looking

            // Measure the range from the player, not from the camera
            if (Vector3.Distance(transform.position, item.transform.position) > pickupRange) item = null;
            break;
        }

        if (item == null)
        {
            Debug.Log("ItemGrabber: nothing to pick up. Look straight at the item and get closer.");
            return;
        }

        held = item;
        held.IsHeld = true;
        heldBody = item.GetComponent<Rigidbody>();
        if (heldBody != null)
        {
            heldWasKinematic = heldBody.isKinematic;
            heldBody.isKinematic = true;   // no gravity or pushing while held
        }
        Debug.Log("Picked up: " + item.displayName);
    }

    void Drop()
    {
        if (InspectMode.Active) ExitInspect();   // dropping also ends inspecting

        if (heldBody != null) heldBody.isKinematic = heldWasKinematic;
        held.IsHeld = false;
        Debug.Log("Dropped: " + held.displayName);
        held = null;
        heldBody = null;
        IsRotatingItem = false;
    }
}