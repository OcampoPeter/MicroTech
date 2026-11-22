// ============================================================
// WHERE TO PUT THIS FILE:
//   Map-Code/Assets Task/Spawn/SpawnPlatform.cs
//   (make a new folder called "Spawn" inside Assets Task)
//   If you already have an old SpawnPlatform.cs, DELETE it first.
//
// HOW TO USE:
//   1. Pick a platform (it needs a Collider).
//   2. Add Component > Spawn Platform.
//   3. Done. It finds your object named "SpawnPoint" by itself.
//
// WHAT IT DOES:
//   - The platform starts DARK GRAY.
//   - When the player stands on it, the SpawnPoint moves onto it
//     and the platform lights up WHITE with a soft neon glow.
//   - If the player steps on a different Spawn Platform, the old
//     one goes back to dark gray (only one is lit at a time).
// ============================================================

using UnityEngine;

public class SpawnPlatform : MonoBehaviour
{
    [Header("Spawn point to move (empty = finds an object named SpawnPoint)")]
    public Transform spawnPoint;
    [Tooltip("Where the player appears, measured from the middle of the platform's top.\nRaise Y if the player appears inside the platform.")]
    public Vector3 spawnOffset = new Vector3(0f, 1f, 0f);
    [Tooltip("ON: the spawn point also turns the same way as this platform.")]
    public bool copyRotation = false;

    [Header("Look")]
    public Color offColor = new Color(0.2f, 0.2f, 0.2f);   // dark gray
    public Color onColor = Color.white;                    // white neon
    [Tooltip("Glow when NOT used. 0 = no glow.")]
    public float offGlow = 0f;
    [Tooltip("Glow when lit. Lower = softer. (Fried uses 2, Bent uses 1.)")]
    public float onGlow = 0.6f;

    [Header("Stepping")]
    [Tooltip("How high above the platform the player's feet can be and still count as standing on it.")]
    public float heightTolerance = 0.4f;

    // The platform that is lit right now (null if none)
    public static SpawnPlatform Current { get; private set; }

    public bool IsActive { get; private set; }

    Collider platformCol;
    Transform player;
    CharacterController playerCC;
    Collider playerCol;

    // Clear leftovers when you press Play.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Current = null;

    void Start()
    {
        platformCol = GetComponent<Collider>();
        if (platformCol == null) platformCol = GetComponentInChildren<Collider>();
        if (platformCol == null)
            Debug.LogWarning(name + " is a Spawn Platform but has no Collider.", this);

        if (spawnPoint == null)
        {
            GameObject sp = GameObject.Find("SpawnPoint");
            if (sp != null) spawnPoint = sp.transform;
            else Debug.LogWarning("Spawn Platform '" + name + "': no object named SpawnPoint found. Drag it into the Spawn Point slot.", this);
        }

        SetLook(false);   // start dark gray
    }

    void Update()
    {
        if (IsActive || platformCol == null) return;

        FindPlayer();
        if (player == null) return;

        // The player's feet (bottom middle of the player's collider)
        Vector3 feet = player.position;
        if (playerCC != null)
            feet = new Vector3(playerCC.bounds.center.x, playerCC.bounds.min.y, playerCC.bounds.center.z);
        else if (playerCol != null)
            feet = new Vector3(playerCol.bounds.center.x, playerCol.bounds.min.y, playerCol.bounds.center.z);

        // Is the player over the platform, and standing on top of it?
        Bounds b = platformCol.bounds;
        bool inside = feet.x >= b.min.x && feet.x <= b.max.x &&
                      feet.z >= b.min.z && feet.z <= b.max.z;
        float height = feet.y - b.max.y;

        if (inside && height > -0.5f && height < heightTolerance)
            Activate();
    }

    // The player stepped on this platform.
    void Activate()
    {
        // The old lit platform goes back to dark gray
        if (Current != null && Current != this) Current.SetLook(false);
        Current = this;

        // Move the spawn point onto this platform
        if (spawnPoint != null)
        {
            Bounds b = platformCol.bounds;
            Vector3 top = new Vector3(b.center.x, b.max.y, b.center.z);
            spawnPoint.position = top + spawnOffset;
            if (copyRotation) spawnPoint.rotation = transform.rotation;
        }

        SetLook(true);
        Debug.Log("[SpawnPlatform] Spawn point moved to " + name);
    }

    // true = white neon, false = dark gray
    void SetLook(bool on)
    {
        IsActive = on;
        Color color = on ? onColor : offColor;
        float glow = on ? onGlow : offGlow;

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            TaskVisuals.SetColor(r, color, glow);
    }

    // Find the player once (and again if it was replaced).
    void FindPlayer()
    {
        if (player != null) return;
        GameObject go = GameObject.FindWithTag("Player");
        if (go == null) return;
        player = go.transform;
        playerCC = go.GetComponent<CharacterController>();
        playerCol = go.GetComponent<Collider>();
    }
}