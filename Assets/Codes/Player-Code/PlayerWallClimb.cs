using UnityEngine;

// WALL CLIMB. Put this on the Player (next to PlayerWallBrake).
//
// While you are braking on a wall (PlayerWallBrake), let go of Space and press it again quickly
// = a climb jump. You shoot UP the wall.
// You get 2 climb jumps. They come back when you touch the floor.
// This script only does the jumps. PlayerWallBrake decides when one happens.
public class PlayerWallClimb : MonoBehaviour
{
    [Header("Wall climb jump")]
    [Tooltip("How many climb jumps you get before touching the floor.")]
    public int maxJumps = 2;
    [Tooltip("How high one climb jump goes (meters). Speed = sqrt(height x 2 x gravity).")]
    public float jumpHeight = 3.5f;
    [Tooltip("A small push away from the wall so you do not scrape it. 0 = straight up.")]
    public float awayForce = 1.5f;

    [Tooltip("After a climb jump the wall run is blocked for this long (seconds), so it does not grab you back onto the wall.")]
    public float wallRunBlockTime = 0.6f;

    [Header("Testing")]
    public bool showDebug = true;   // small text under the brake box

    public int JumpsLeft { get; private set; }
    public bool CanJump { get { return JumpsLeft > 0; } }

    private CharacterController controller;
    private PlayerMovement movement;
    private PlayerWallRun wallRun;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        movement = GetComponent<PlayerMovement>();
        wallRun = GetComponent<PlayerWallRun>();
        JumpsLeft = maxJumps;
    }

    void Update()
    {
        // Touch the floor = the jumps come back
        if (controller.isGrounded) JumpsLeft = maxJumps;
    }

    // Called by PlayerWallBrake. wallNormal points away from the wall.
    public void Jump(Vector3 wallNormal)
    {
        if (!CanJump) return;
        JumpsLeft--;

        float up = Mathf.Sqrt(jumpHeight * -2f * movement.gravity);
        movement.velocity = new Vector3(0f, up, 0f);
        movement.SetHorizontalVelocity(Vector3.zero);
        movement.AddPush(wallNormal * awayForce);
        if (wallRun != null) wallRun.BlockFor(wallRunBlockTime);
        Debug.Log("[Climb] Climb jump! Jumps left: " + JumpsLeft);
    }

    void OnGUI()
    {
        if (!showDebug || Event.current.type != EventType.Repaint) return;
        GUI.Box(new Rect(10f, 125f, 220f, 28f), "Climb jumps left: " + JumpsLeft + " / " + maxJumps);
    }
}