using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public CharacterController controller;
    public Transform playerCamera;
    private PlayerInputHandler input;
    private PlayerWallRun wallRun;

    [Header("Speeds")]
    public float walkSpeed = 5f;
    public float maxSprintSpeed = 10f;
    public float accelerationRate = 18f;     // How fast the sprint speed builds up
    public float decelerationRate = 14f;     // How fast it falls back down
    public float modelRotationOffset = 180f;
    [Tooltip("Turn the character to face where it walks. CameraModeSwitcher turns this off in first person.")]
    public bool rotateToMovement = true;

    [Header("Smoothness")]
    [Tooltip("How quickly you reach your target speed on the ground. Higher = snappier, lower = smoother.")]
    public float groundAcceleration = 45f;
    [Tooltip("How quickly you stop on the ground. Higher = stops faster.")]
    public float groundDeceleration = 35f;
    [Tooltip("Steering strength in the air while you hold a direction.")]
    public float airAcceleration = 20f;
    [Tooltip("How fast you lose momentum in the air with no input. Low = you keep drifting.")]
    public float airDeceleration = 6f;
    [Tooltip("Extra strength when you steer AGAINST your current direction in the air. Higher = easier to turn around mid-jump.")]
    public float airReverseMultiplier = 3f;
    [Tooltip("How fast the character turns to face where it moves (degrees per second).")]
    public float rotationSpeed = 720f;

    [Header("Crouch and Dash (set by PlayerCrouch and PlayerDash)")]
    [Tooltip("Walking speed while crouched = normal speed x this. You also cannot sprint or jump while crouched.")]
    public float crouchSpeedMultiplier = 0.5f;
    [HideInInspector] public bool crouching;
    [HideInInspector] public bool IsDashing;   // while true, PlayerDash moves the player and this script waits
    [HideInInspector] public bool IsSliding;   // while true, PlayerSlide moves the player and this script waits
    [HideInInspector] public bool IsClimbing;  // while true, PlayerClimb moves the player and this script waits

    [Header("Gravity & Jump")]
    public float gravity = -19.62f;
    public float jumpHeight = 2.5f;
    [Tooltip("Turn off to stop the player from jumping (the TS room does this through SceneCameraMode).")]
    public bool canJump = true;
    [Tooltip("Grace time after leaving the ground where you can still jump.")]
    public float coyoteTime = 0.12f;
    [Tooltip("How long a jump press is remembered if you pressed it a moment too early.")]
    public float jumpBufferTime = 0.12f;
    // Only the UP/DOWN speed lives here. Sideways pushes use the push below.
    [HideInInspector] public Vector3 velocity;

    [Header("Snappy Feel")]
    [Tooltip("Gravity is this many times stronger while you FALL. Higher = sharper, less floaty drops. 1 = the old floaty fall.")]
    public float fallGravityMultiplier = 1.7f;
    [Tooltip("Gravity is this many times stronger while you rise and have let go of Space. Higher = a quick tap gives a much shorter hop. 1 = no variable jump.")]
    public float shortJumpMultiplier = 2.8f;
    [Tooltip("The jump when you hold Space the whole way is unchanged (it still reaches Jump Height).")]
    public bool variableJump = true;
    [Tooltip("The little burst of speed (m/s) you get the moment you start sprinting on the ground. 0 = no kick.")]
    public float sprintKick = 2.5f;
    [Tooltip("The kick can push you this much (m/s) above max sprint speed, then it settles back down by itself.")]
    public float sprintKickOvershoot = 1.5f;

    [Header("Wall Jump Push")]
    [Tooltip("How fast the sideways push from a wall jump fades while in the air (units per second).")]
    public float airPushDecay = 10f;
    [Tooltip("How fast the sideways push fades once you are on the ground.")]
    public float groundPushDecay = 40f;

    [Header("Air Time Boost")]
    [Tooltip("Seconds in the air before you start gaining extra speed.")]
    public float airBoostDelay = 0.6f;
    [Tooltip("Extra speed gained per second of air time (meters per second, per second).")]
    public float airSpeedGain = 3f;
    [Tooltip("The most bonus speed you can gain from air time.")]
    public float maxAirBonus = 8f;
    [Tooltip("How fast the bonus fades after you land.")]
    public float airBonusFadeOnLand = 30f;
    [Tooltip("Off = the air bonus stops growing while you wall run, so wall running no longer makes you faster and faster.")]
    public bool airBonusGrowsOnWall = false;

    [Header("High Fall Momentum")]
    public float fallSpeedThreshold = 12f;
    public float momentumCarryFactor = 0.4f;

    // Real horizontal speed in meters per second (for a HUD)
    public float HorizontalSpeed { get; private set; }

    private float currentSpeed;
    private float airTime;                // seconds since you left the ground
    private float airBonus;               // extra speed earned from air time
    private float highestFallSpeedThisJump;
    private Vector3 push;                 // sideways push that fades out by itself
    private Vector3 horizontalVelocity;   // the real sideways speed, smoothed
    private float lastGroundedTime = -999f;
    private float lastJumpPressedTime = -999f;
    private bool jumpCutActive;           // true only while rising from a normal ground jump (so wall jumps and climb jumps are never cut short)
    private bool wasSprintWanted;

    void Start()
    {
        input = GetComponent<PlayerInputHandler>();
        wallRun = GetComponent<PlayerWallRun>();
        if (controller == null) controller = GetComponent<CharacterController>();
        currentSpeed = walkSpeed;
    }

    // Used by PlayerWallRun for the wall jump
    public void AddPush(Vector3 amount)
    {
        amount.y = 0f;
        push += amount;
    }

    public void ClearPush()
    {
        push = Vector3.zero;
    }

    // Used by PlayerWallRun every frame while wall running, so your speed carries on smoothly when you leave the wall
    public void SetHorizontalVelocity(Vector3 value)
    {
        value.y = 0f;
        horizontalVelocity = value;
        HorizontalSpeed = value.magnitude;
    }

    // Used by PlayerSlide: after a slide you keep your speed instead of dropping to walking speed at once
    public void CarrySpeed(float speed)
    {
        currentSpeed = Mathf.Max(currentSpeed, speed);
    }

    // The real sideways velocity (used by PlayerSlide to keep your speed and direction)
    public Vector3 HorizontalVelocity { get { return horizontalVelocity; } }

    // Used by PlayerSlide when a slide ends, so the speed carries on and then fades by itself
    public void SetRunSpeed(float value)
    {
        currentSpeed = value;
    }

    // Used by PlayerWallRun so a wall jump can never also trigger a normal jump
    public void CancelGroundJumpWindow()
    {
        lastGroundedTime = -999f;
        jumpCutActive = false;
    }

    // Call this when the player respawns
    public void ResetMotion()
    {
        velocity = Vector3.zero;
        push = Vector3.zero;
        horizontalVelocity = Vector3.zero;
        currentSpeed = walkSpeed;
        highestFallSpeedThisJump = 0f;
        airTime = 0f;
        airBonus = 0f;
        jumpCutActive = false;
    }

    void Update()
    {
        // Air time keeps counting while wall running too, so a long chain of jumps and wall runs keeps building speed
        if (controller.isGrounded)
        {
            airTime = 0f;
            airBonus = Mathf.MoveTowards(airBonus, 0f, airBonusFadeOnLand * Time.deltaTime);
        }
        else
        {
            airTime += Time.deltaTime;
            bool onWall = wallRun != null && wallRun.IsWallRunning;
            if (airTime > airBoostDelay && (!onWall || airBonusGrowsOnWall))
                airBonus = Mathf.MoveTowards(airBonus, maxAirBonus, airSpeedGain * Time.deltaTime);
        }

        // The dash and slide scripts move the player themselves, so skip this movement math
        if (IsDashing || IsSliding || IsClimbing) { jumpCutActive = false; return; }

        // CRUCIAL: the wall run script moves the player while wall running, so skip this movement math
        if (wallRun != null && wallRun.IsWallRunning) { jumpCutActive = false; return; }

        // Safety: sideways speed must never be stored here, or you would drift forever
        velocity.x = 0f;
        velocity.z = 0f;

        if (controller.isGrounded) lastGroundedTime = Time.time;
        if (input.JumpTriggered) lastJumpPressedTime = Time.time;

        if (!controller.isGrounded)
        {
            if (velocity.y < highestFallSpeedThisJump)
            {
                highestFallSpeedThisJump = velocity.y;
            }
        }

        if (controller.isGrounded && velocity.y < 0)
        {
            float positiveFallSpeed = Mathf.Abs(highestFallSpeedThisJump);
            if (positiveFallSpeed > fallSpeedThreshold)
            {
                currentSpeed += (positiveFallSpeed - fallSpeedThreshold) * momentumCarryFactor;
            }

            velocity.y = -2f;
            highestFallSpeedThisJump = 0f;
        }

        // Sprint Shift Ramp Math
        bool sprintWanted = input.IsSprinting && input.MoveInput.magnitude > 0 && !crouching;

        // Sprint kick: a quick burst the moment you start sprinting on the ground
        if (sprintWanted && !wasSprintWanted && controller.isGrounded && sprintKick > 0f)
            currentSpeed = Mathf.Min(currentSpeed + sprintKick, maxSprintSpeed + sprintKickOvershoot);
        wasSprintWanted = sprintWanted;
        float targetSpeed = sprintWanted ? maxSprintSpeed : walkSpeed;
        if (crouching) targetSpeed *= crouchSpeedMultiplier;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, (currentSpeed < targetSpeed ? accelerationRate : decelerationRate) * Time.deltaTime);

        Vector3 positionBefore = transform.position;

        // The speed we WANT to have
        Vector3 desiredVelocity = Vector3.zero;
        Vector3 direction = new Vector3(input.MoveInput.x, 0f, input.MoveInput.y).normalized;
        if (direction.magnitude >= 0.1f && playerCamera != null)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + playerCamera.eulerAngles.y;
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

            desiredVelocity = moveDir.normalized * (currentSpeed + airBonus);

            // Turn the model gradually instead of snapping.
            // Not in first person: the camera is part of the player there, so turning the player turns the camera,
            // and the player would keep chasing its own view in circles.
            if (rotateToMovement)
            {
                Quaternion targetRotation = Quaternion.Euler(0f, targetAngle + modelRotationOffset, 0f);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        // Move the real speed toward the wanted speed. This is what makes it smooth.
        bool hasDesired = desiredVelocity.sqrMagnitude > 0.0001f;
        float accel;
        if (controller.isGrounded) accel = hasDesired ? groundAcceleration : groundDeceleration;
        else accel = hasDesired ? airAcceleration : airDeceleration;

        // Holding the opposite direction in the air brakes and turns you around faster
        if (!controller.isGrounded && hasDesired && Vector3.Dot(horizontalVelocity, desiredVelocity) < 0f)
            accel *= airReverseMultiplier;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desiredVelocity, accel * Time.deltaTime);

        // Jump (with a little grace time so it never feels like it ignored you)
        bool jumpBuffered = Time.time - lastJumpPressedTime <= jumpBufferTime;
        bool recentlyGrounded = Time.time - lastGroundedTime <= coyoteTime;
        if (jumpBuffered && recentlyGrounded && canJump && !crouching)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpCutActive = true;   // this rise can be cut short by letting go of Space
            // Use up both timers so one press can't trigger two jumps
            lastJumpPressedTime = -999f;
            lastGroundedTime = -999f;
        }

        // Gravity: normal on the way up, stronger on the way down (sharper falls),
        // and stronger on the way up if you let go of Space early (a tap = a short hop).
        float gravityNow = gravity;
        if (velocity.y < 0f)
        {
            gravityNow *= fallGravityMultiplier;
            jumpCutActive = false;
        }
        else if (variableJump && jumpCutActive && Keyboard.current != null && !Keyboard.current[Key.Space].isPressed)
        {
            gravityNow *= shortJumpMultiplier;
        }
        velocity.y += gravityNow * Time.deltaTime;

        // Walking + the fading wall-jump push + up/down speed, all in one move
        float decay = controller.isGrounded ? groundPushDecay : airPushDecay;
        push = Vector3.MoveTowards(push, Vector3.zero, decay * Time.deltaTime);

        Vector3 total = horizontalVelocity + push + new Vector3(0f, velocity.y, 0f);
        controller.Move(total * Time.deltaTime);

        Vector3 moved = transform.position - positionBefore;
        moved.y = 0f;
        HorizontalSpeed = Time.deltaTime > 0f ? moved.magnitude / Time.deltaTime : 0f;
    }
}