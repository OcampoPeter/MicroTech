using UnityEngine;

// Runs before PlayerMovement so IsWallRunning is up to date when movement checks it.
[DefaultExecutionOrder(-50)]
public class PlayerWallRun : MonoBehaviour
{
    [Header("References")]
    public CharacterController controller;
    private PlayerInputHandler input;
    private PlayerMovement movement;

    [Header("Wall Settings")]
    [Tooltip("Speed along the wall. Lower = slower wall run. (Old value was 9.)")]
    public float wallRunSpeed = 4.5f;
    [Tooltip("If you arrive faster than the wall run speed, you KEEP that speed and it fades down to the wall run speed bit by bit. This is how fast it fades (meters per second, every second). Lower = you keep your speed longer.")]
    public float wallRunSlowdown = 5f;
    [Tooltip("How fast you sink while on the wall (meters per second). 0 = you hold your height.")]
    public float wallRunGravity = 0.5f;
    [Tooltip("How quickly your up/down speed settles once you are on the wall. Lower = you carry more of your jump's rise onto the wall.")]
    public float wallRunVerticalSmoothing = 40f;
    [Tooltip("Small push into the wall so you keep touching it while running along it.")]
    public float wallStickForce = 3f;
    [Tooltip("Seconds you stay on the wall after contact is lost for a moment.")]
    public float contactGrace = 0.15f;

    [Header("Wall Jump Settings")]
    public float wallJumpUpForce = 6f;
    public float wallJumpSideForce = 12f;     // Pushes you away from the wall
    public float wallJumpForwardForce = 6f;   // Pushes you on along the wall
    [Tooltip("After you wall jump off a wall, you can't wall run or wall jump that same wall again for this many seconds. Jumping off a DIFFERENT wall frees it again straight away.")]
    public float sameWallLockTime = 1.5f;

    [Header("Leaving the wall")]
    [Tooltip("If you press toward AWAY from the wall at least this much (1 = straight away), the wall run lets go. Lower = lets go more easily.")]
    [Range(0.1f, 1f)] public float awayLetGoAmount = 0.5f;

    [Header("Detection Settings")]
    public LayerMask whatIsWall;

    public bool IsWallRunning { get; private set; }
    private Vector3 currentWallNormal;
    private float lastTouchTime = -10f;

    private float runSpeed;               // the speed we are running at right now (can be above wallRunSpeed)
    private float blockedUntil = -10f;   // other scripts (the wall climb jump) can block wall running for a moment

    private float wallJumpCooldown = 0.35f;
    private float lastWallJumpTime = -10f;

    // The wall you last jumped off. Only one wall is locked at a time.
    private GameObject currentWallObject;
    private GameObject lockedWall;
    private Vector3 lockedWallNormal;
    private float lockedWallUntil;

    void Start()
    {
        input = GetComponent<PlayerInputHandler>();
        movement = GetComponent<PlayerMovement>();
        if (controller == null) controller = GetComponent<CharacterController>();
    }

    // Called by PlayerWallClimb after a climb jump, so the wall run does not grab you straight back
    public void BlockFor(float seconds)
    {
        blockedUntil = Time.time + seconds;
        StopWallRunning();
    }

    void Update()
    {
        if (Time.time < blockedUntil)
        {
            StopWallRunning();
            return;
        }

        // Landing frees every wall again
        if (controller.isGrounded) lockedWall = null;

        // A climb is moving the player, so no wall running meanwhile
        if (movement != null && movement.IsClimbing)
        {
            StopWallRunning();
            return;
        }

        if (Time.time < lastWallJumpTime + wallJumpCooldown)
        {
            StopWallRunning();
            return;
        }

        bool isMovingForward = input.MoveInput.y > 0;
        bool inAir = !controller.isGrounded;
        bool holdingLateralInput = Mathf.Abs(input.MoveInput.x) > 0.1f;
        bool touchingWall = Time.time - lastTouchTime <= contactGrace;

        // Pressing away from the wall = you want to leave it, so no wall run
        bool pressingAway = false;
        if (movement != null && movement.playerCamera != null && input.MoveInput.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(input.MoveInput.x, input.MoveInput.y) * Mathf.Rad2Deg + movement.playerCamera.eulerAngles.y;
            Vector3 want = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            pressingAway = Vector3.Dot(want, currentWallNormal) > awayLetGoAmount;
        }

        if (inAir && isMovingForward && touchingWall && holdingLateralInput && !pressingAway && !IsWallLocked())
        {
            StartWallRunning();
        }
        else
        {
            StopWallRunning();
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (Time.time < lastWallJumpTime + wallJumpCooldown) return;

        if (((1 << hit.gameObject.layer) & whatIsWall) != 0)
        {
            if (Mathf.Abs(hit.normal.y) < 0.3f)
            {
                lastTouchTime = Time.time;
                currentWallObject = hit.gameObject;

                // Keep the normal flat so it only points sideways
                Vector3 flat = new Vector3(hit.normal.x, 0f, hit.normal.z);
                if (flat.sqrMagnitude > 0.0001f) currentWallNormal = flat.normalized;
            }
        }
    }

    void StartWallRunning()
    {
        bool justStarted = !IsWallRunning;
        IsWallRunning = true;

        // Arriving fast: keep the speed you had (never slower than the normal wall run speed)
        if (justStarted)
        {
            float arrive = movement != null ? movement.HorizontalVelocity.magnitude : 0f;
            runSpeed = Mathf.Max(wallRunSpeed, arrive);
        }
        // Then it fades down to the wall run speed with a steady slowdown, like the brake
        runSpeed = Mathf.MoveTowards(runSpeed, wallRunSpeed, wallRunSlowdown * Time.deltaTime);

        // Run along the wall in the direction you are looking.
        // (The model is turned by an offset, so transform.forward would point the wrong way.)
        Vector3 look = (movement != null && movement.playerCamera != null) ? movement.playerCamera.forward : -transform.forward;
        look.y = 0f;
        Vector3 wallForward = Vector3.Cross(currentWallNormal, Vector3.up);
        if (Vector3.Dot(wallForward, look) < 0f)
        {
            wallForward = -wallForward;
        }
        wallForward.Normalize();

        // Settle the up/down speed smoothly, so the rise of your jump carries onto the wall
        float currentUp = movement != null ? movement.velocity.y : 0f;
        float verticalSpeed = Mathf.MoveTowards(currentUp, -wallRunGravity, wallRunVerticalSmoothing * Time.deltaTime);

        // Run along the wall, with a small pull into it so you keep touching it
        Vector3 runVelocity = wallForward * runSpeed - currentWallNormal * wallStickForce;
        controller.Move((runVelocity + Vector3.up * verticalSpeed) * Time.deltaTime);

        if (movement != null)
        {
            // Face the way you are running
            Quaternion faceRun = Quaternion.Euler(0f, Mathf.Atan2(wallForward.x, wallForward.z) * Mathf.Rad2Deg + movement.modelRotationOffset, 0f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, faceRun, movement.rotationSpeed * Time.deltaTime);

            // Only the up/down part is shared with PlayerMovement as a velocity. The run speed is
            // handed over separately so you leave the wall with your speed, not a stale direction.
            movement.velocity = new Vector3(0f, verticalSpeed, 0f);
            movement.SetHorizontalVelocity(wallForward * runSpeed);
            movement.ClearPush();

            if (input.JumpTriggered)
            {
                lastWallJumpTime = Time.time;
                movement.CancelGroundJumpWindow();

                // Lock this wall for a while. This replaces the old lock, so the wall you jumped off before is free again.
                lockedWall = currentWallObject;
                lockedWallNormal = currentWallNormal;
                lockedWallUntil = Time.time + sameWallLockTime;

                // Sideways push away from the wall + on along the wall, which fades out by itself
                Vector3 directionalPush = currentWallNormal * wallJumpSideForce;
                Vector3 forwardPush = wallForward * wallJumpForwardForce;
                movement.AddPush(directionalPush + forwardPush);

                // Upward jump
                float verticalPush = Mathf.Sqrt(wallJumpUpForce * -2f * movement.gravity);
                movement.velocity = new Vector3(0f, verticalPush, 0f);

                StopWallRunning();
            }
        }
    }

    void StopWallRunning()
    {
        IsWallRunning = false;
    }

    // True while the wall you are touching is the one you just jumped off.
    // It counts as the same wall only if it is the same object AND faces the same way,
    // so opposite walls inside one big mesh still count as different walls.
    bool IsWallLocked()
    {
        if (lockedWall == null || Time.time >= lockedWallUntil) return false;
        if (currentWallObject != lockedWall) return false;
        return Vector3.Dot(currentWallNormal, lockedWallNormal) > 0.7f;
    }
}