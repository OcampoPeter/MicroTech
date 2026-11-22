// WHERE TO PUT THIS FILE:  Assets Task / Missing
// ------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;

// Settings for the Missing task's parts. They live on the MazeTaskController (NOT on PartPickup),
// because PartPickup is added while the game runs, so anything changed on it would be lost
// when you stop Play.
// Which way counts as "the front" of the player. The part floats on the opposite side.
public enum PartFacing
{
    MovementDirection,   // the way the player is moving (the part trails behind you)
    CameraForward,       // the way the camera looks
    PlayerForward        // the way the player object itself points
}

[System.Serializable]
public class PartSettings
{
    [Header("What to spawn")]
    [Tooltip("The part to spawn at each spawn point (a prefab). Leave empty to spawn a simple glowing ball.")]
    public GameObject partPrefab;
    [Tooltip("Color of the glowing ball (only used when no prefab is given).")]
    public Color fallbackColor = new Color(1f, 0.55f, 0.1f);

    [Header("Look of the part (works on your prefab too)")]
    [Tooltip("ON: color and glow the part with the two settings below.\nOFF: keep the prefab's own materials.")]
    public bool colorPart = true;
    [Tooltip("The color of the part.")]
    public Color partColor = new Color(0.25f, 0.6f, 1f);
    [Tooltip("How much the part glows (neon). 0 = no glow. Lower = softer.")]
    public float partGlow = 1.5f;

    [Header("Picking up")]
    [Tooltip("The key that picks up the part (the player must be close). Examples: F, E, G.")]
    public string interactKey = "F";
    [Tooltip("How close the player must be, in meters.")]
    public float interactRange = 3f;
    [Tooltip("Show a 'Press F' message when the player is close.")]
    public bool showPrompt = true;

    [Header("Carrying (floats behind the player)")]
    [Tooltip("Where the part floats, as a fraction of the player's height.\nX = right, Y = up, Z = forward (negative = behind the player).")]
    public Vector3 backOffset = new Vector3(0f, 0.7f, -0.3f);
    [Tooltip("What counts as the FRONT of the player. The part floats on the opposite side.\nMovementDirection = the way you are moving (best if it appears in front).\nCameraForward = the way the camera looks.\nPlayerForward = the way the player object points.")]
    public PartFacing facing = PartFacing.MovementDirection;
    [Tooltip("How quickly the part swings around when you turn. Higher = faster.")]
    public float turnSpeed = 6f;
    [Tooltip("Only used by MovementDirection: the player must move faster than this (meters per second) to change the facing.")]
    public float minMoveSpeed = 0.5f;
    [Tooltip("How smoothly it follows the player. Higher = slower and floatier. Lower = tighter.")]
    public float followSmoothTime = 0.2f;
    [Tooltip("How far it bobs up and down, in meters.")]
    public float bobHeight = 0.15f;
    public float bobSpeed = 3f;
    [Tooltip("How fast the part spins while it waits on the ground (degrees per second).")]
    public float spinSpeed = 90f;
    [Tooltip("How fast the part turns while you carry it (degrees per second). Keep it small for a slow turn. 0 = no turning.")]
    public float carrySpinSpeed = 15f;

    [Header("Dying")]
    [Tooltip("ON: if the player suddenly jumps far in one frame (a respawn after dying), the part goes back to where it started.")]
    public bool autoDetectRespawn = true;
    [Tooltip("How far (in meters, in one frame) the player must jump to count as a respawn.")]
    public float respawnJumpDistance = 10f;
}

// MISSING PARTS (the part): the player walks up, presses F, and the part floats behind them.
// Carry it to a dark platform and step on the platform to install it (see DarkPlatform).
// If the player dies, the part goes back to the spot where it started.
//
// You don't add this yourself: MazeTaskController adds it to ready-made parts and spawns
// parts at empty spawn points. The player carries one part at a time.
public class PartPickup : MonoBehaviour
{
    // The part the player is carrying right now (null if none).
    public static PartPickup Carried { get; private set; }

    public bool IsCarried { get; private set; }
    public bool IsUsed { get; private set; }

    PartSettings s;                 // settings (from the controller)
    Transform player;               // the player (found by the "Player" tag)
    Collider playerCol;             // the player's collider (used for size and distance)
    Vector3 homePos;                // where this part started (it returns here when you die)
    Quaternion homeRot;
    Vector3 followVelocity;         // used by the smooth follow
    Vector3 lastPlayerPos;          // used to notice a respawn
    Vector3 facing = Vector3.forward; // the way the player faces (flat on the ground)
    bool nearPlayer;                // true when the player is close enough to pick it up
    static GUIStyle promptStyle;    // style of the "Press F" message

    // Clear leftovers when you press Play.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Carried = null;

    // Called by MazeTaskController to set this part up.
    public void Init(PartSettings settings)
    {
        s = settings;
        homePos = transform.position;
        homeRot = transform.rotation;

        // Colliders must not block the player: the part is picked up by pressing the key, not by touching.
        foreach (var c in GetComponentsInChildren<Collider>()) c.isTrigger = true;

        // Color and glow the part (prefab or not), if turned on.
        if (s.colorPart)
            foreach (var r in GetComponentsInChildren<Renderer>())
                TaskVisuals.SetColor(r, s.partColor, s.partGlow);
    }

    // Call this when the player dies (from your death / respawn code).
    // The carried part goes back to where it started.
    public static void OnPlayerDied()
    {
        if (Carried != null) Carried.ReturnHome();
    }

    void OnDestroy()
    {
        if (Carried == this) Carried = null;
    }

    void Update()
    {
        if (IsUsed || s == null) return;

        FindPlayer();
        if (player == null) return;

        if (IsCarried)
        {
            // Dying teleports the player far away: put the part back where it started.
            if (s.autoDetectRespawn && Vector3.Distance(player.position, lastPlayerPos) > s.respawnJumpDistance)
            {
                ReturnHome();
                return;
            }
            Vector3 moved = player.position - lastPlayerPos;   // how far the player moved this frame
            lastPlayerPos = player.position;
            UpdateFacing(moved);
            FollowPlayer();
            return;
        }

        // Waiting to be picked up: spin and bob in place.
        transform.Rotate(Vector3.up, s.spinSpeed * Time.deltaTime, Space.World);
        transform.position = homePos + Vector3.up * Mathf.Sin(Time.time * s.bobSpeed) * s.bobHeight;

        // Is the player close enough (and not carrying another part)?
        nearPlayer = Carried == null && DistanceToPlayer() <= s.interactRange;
        if (nearPlayer && KeyPressed(s.interactKey)) PickUp();
    }

    // Find the player once (and again if it was replaced).
    void FindPlayer()
    {
        if (player != null) return;
        GameObject go = GameObject.FindWithTag("Player");
        if (go == null) return;
        player = go.transform;
        playerCol = go.GetComponent<Collider>();
        lastPlayerPos = player.position;
    }

    // Distance from the part to the closest point of the player.
    float DistanceToPlayer()
    {
        Vector3 target = playerCol != null
            ? playerCol.bounds.ClosestPoint(transform.position)
            : player.position;
        return Vector3.Distance(target, transform.position);
    }

    // The player picked up the part.
    void PickUp()
    {
        IsCarried = true;
        Carried = this;
        nearPlayer = false;
        followVelocity = Vector3.zero;
        lastPlayerPos = player.position;
        facing = StartFacing();
    }

    // Float smoothly behind the player.
    void FollowPlayer()
    {
        // Player size, so the part sits at a good spot whatever the player's scale is.
        float height = 2f;
        Vector3 feet = player.position;
        if (playerCol != null)
        {
            Bounds b = playerCol.bounds;
            height = b.size.y;
            feet = new Vector3(player.position.x, b.min.y, player.position.z);
        }

        // The spot behind the player (turns with the player), plus a gentle bob.
        Vector3 offset = Quaternion.LookRotation(facing, Vector3.up) * (s.backOffset * height);
        Vector3 target = feet + offset + Vector3.up * Mathf.Sin(Time.time * s.bobSpeed) * s.bobHeight;

        transform.position = Vector3.SmoothDamp(transform.position, target, ref followVelocity, Mathf.Max(0.01f, s.followSmoothTime));
        transform.Rotate(Vector3.up, s.carrySpinSpeed * Time.deltaTime, Space.World); // slow turn while carried
    }

    // Which way the player faces when the part is first picked up.
    Vector3 StartFacing()
    {
        Vector3 f;
        if (s.facing == PartFacing.PlayerForward)
        {
            f = player.forward;
        }
        else
        {
            Camera cam = Camera.main;
            f = cam != null ? cam.transform.forward : player.forward;
        }
        return Flat(f, Vector3.forward);
    }

    // Updates which way the player faces, using the chosen Facing mode.
    void UpdateFacing(Vector3 moved)
    {
        Vector3 target = facing;

        switch (s.facing)
        {
            case PartFacing.PlayerForward:
                target = Flat(player.forward, facing);
                break;

            case PartFacing.CameraForward:
                Camera cam = Camera.main;
                if (cam != null) target = Flat(cam.transform.forward, facing);
                break;

            default: // MovementDirection
                Vector3 flatMove = new Vector3(moved.x, 0f, moved.z);
                if (Time.deltaTime > 0f && flatMove.magnitude / Time.deltaTime > s.minMoveSpeed)
                    target = flatMove.normalized;
                break;
        }

        // Turn smoothly so the part does not snap around.
        float t = 1f - Mathf.Exp(-s.turnSpeed * Time.deltaTime);
        facing = Vector3.Slerp(facing, target, t);
        if (facing.sqrMagnitude < 0.0001f) facing = target;
    }

    // A direction with the up/down part removed, so the part stays level behind the player.
    static Vector3 Flat(Vector3 v, Vector3 fallback)
    {
        v.y = 0f;
        return v.sqrMagnitude > 0.0001f ? v.normalized : fallback;
    }

    // Put the part back where it started so it can be picked up again.
    void ReturnHome()
    {
        IsCarried = false;
        if (Carried == this) Carried = null;
        followVelocity = Vector3.zero;
        transform.position = homePos;
        transform.rotation = homeRot;
    }

    // Called by a DarkPlatform when it takes this part.
    public void Consume()
    {
        IsUsed = true;
        IsCarried = false;
        if (Carried == this) Carried = null;
        gameObject.SetActive(false);
    }

    // Was this key pressed this frame? Works with the new Input System and the old Input Manager.
    static bool KeyPressed(string keyName)
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null && System.Enum.TryParse<UnityEngine.InputSystem.Key>(keyName, true, out var key))
            return keyboard[key].wasPressedThisFrame;
        return false;
#else
        return System.Enum.TryParse<KeyCode>(keyName, true, out var code) && Input.GetKeyDown(code);
#endif
    }

    // Draws the "Press F" message above the part when the player is close.
    void OnGUI()
    {
        if (!nearPlayer || IsCarried || IsUsed || s == null || !s.showPrompt) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * 0.8f);
        if (screen.z < 0f) return; // behind the camera

        if (promptStyle == null)
        {
            promptStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };
            promptStyle.normal.textColor = Color.white;
        }

        GUI.Label(new Rect(screen.x - 100f, Screen.height - screen.y - 20f, 200f, 40f),
                  "Press " + s.interactKey.ToUpper(), promptStyle);
    }
}