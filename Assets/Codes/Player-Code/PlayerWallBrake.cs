using UnityEngine;
using UnityEngine.InputSystem;

// WALL BRAKE. Put this on the Player (next to PlayerMovement).
//
// How it works:
//  1. You are in the AIR, next to a wall in front of you (it does not need to be head-on). Hold W and press Space = you start braking.
//  2. BRAKE: you keep moving the way you were going, but your speed fades out smoothly.
//     The fade is a steady slowdown (not a sudden stop). Faster = you drift longer before you stop.
//  3. HANG: once you have stopped, you hang still on the wall for 2 seconds.
//  4. SLIP: then you slide down for 3 seconds, getting faster: 0.5, 1, 2, 3.5 m/s.
//  5. After that you fall, and you cannot brake again for 5 seconds (the cooldown).
//  W is only needed to START. After that, just keep HOLDING Space. Let go of Space and you fall.
//  You can look around freely (and press W) while you hang.
//  Exception: let go of Space and press it again quickly = a climb jump (see PlayerWallClimb).
//  It only starts with W alone (no A or D), and never during or right after a wall run.
[DefaultExecutionOrder(10)]   // runs after PlayerMovement
public class PlayerWallBrake : MonoBehaviour
{
    [Header("General")]
    public bool canBrake = true;
    [Tooltip("Layers that count as a wall. Everything = any solid wall.")]
    public LayerMask wallLayers = ~0;
    [Tooltip("How far in front of you a wall can be (meters).")]
    public float wallCheckDistance = 1.0f;
    [Tooltip("How wide to look for a wall, in degrees left and right of where you look. 90 = any wall in front of you or beside you. 0 = only straight ahead.")]
    [Range(0f, 90f)] public float wallSearchAngle = 90f;
    [Tooltip("Hold Space the whole time. Let go and you fall. (W is only needed to start.)")]
    public bool needHoldSpace = true;
    [Tooltip("Seconds after a wall run ends before braking can start. Stops the wall run and the brake from mixing.")]
    public float wallRunLockout = 0.4f;

    [Header("Brake (the drift)")]
    [Tooltip("OFF: any speed going UP is removed when the brake starts, so you only slide down and sideways, never up. ON: you keep the upward speed.")]
    public bool keepUpwardSpeed = false;
    [Tooltip("How fast your speed fades (meters per second, every second). Lower = longer, smoother drift. Stop time = speed / this number.")]
    public float brakeDeceleration = 10f;

    [Header("Hang and slip")]
    public float hangBeforeSlip = 2f;  // seconds you hang still before the slip starts
    public float slipTime = 3f;        // seconds the slip lasts, then you fall
    [Tooltip("Sliding speeds (m/s), spread evenly over the slip time. It speeds up gradually from the first number to the last.")]
    public float[] slipSpeeds = new float[] { 0.5f, 1f, 2f, 3.5f };

    [Header("Climb jump")]
    [Tooltip("After you let go of Space, you have this long (seconds) to press it again for a climb jump. Too slow and you fall.")]
    public float climbWindow = 0.6f;

    [Header("Cooldown")]
    public float cooldown = 5f;        // seconds before you can brake again

    [Header("Testing")]
    public bool showDebug = true;      // small text in the top-left of the Game view

    private enum State { None, Brake, Hang, Slip }
    private State state = State.None;

    private CharacterController controller;
    private PlayerMovement movement;
    private PlayerInputHandler input;
    private PlayerWallRun wallRun;
    private PlayerWallClimb climb;
    private float spaceReleasedAt = -1f;

    private Vector3 vel;            // the speed we control while braking
    private Vector3 wallNormal;
    private float timer;            // time spent in Hang or Slip
    private float noWallTime;
    private float cooldownEnd;
    private bool wasGrounded;       // were we on the ground last frame?
    private float lastWallRunTime = -10f;

    public float CooldownLeft { get { return Mathf.Max(0f, cooldownEnd - Time.time); } }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        movement = GetComponent<PlayerMovement>();
        input = GetComponent<PlayerInputHandler>();
        wallRun = GetComponent<PlayerWallRun>();
        climb = GetComponent<PlayerWallClimb>();
    }

    void OnDisable()
    {
        if (state != State.None) EndBrake("script turned off", false);
    }

    void Update()
    {
        bool groundedNow = controller.isGrounded;
        if (wallRun != null && wallRun.IsWallRunning) lastWallRunTime = Time.time;

        if (state != State.None) RunBrake();
        else TryStart(groundedNow);

        wasGrounded = groundedNow;
    }

    // ---------- Start ----------
    void TryStart(bool groundedNow)
    {
        if (!canBrake || Time.timeScale == 0f) return;
        if (!input.JumpTriggered) return;
        if (groundedNow || wasGrounded) return;                       // on the ground, Space is a normal jump
        if (CooldownLeft > 0f) return;
        // W alone: no A or D (A or D is the wall run)
        if (input.MoveInput.y < 0.5f || Mathf.Abs(input.MoveInput.x) > 0.2f) return;
        if (wallRun != null && wallRun.IsWallRunning) return;         // never start while wall running
        if (Time.time < lastWallRunTime + wallRunLockout) return;     // or right after one

        Vector3 normal;
        if (!FindWall(out normal)) return;

        wallNormal = normal;
        vel = new Vector3(movement.HorizontalVelocity.x, movement.velocity.y, movement.HorizontalVelocity.z);

        // Remove the part of the speed that pushes into the wall, so you slide along it
        float into = Vector3.Dot(vel, -wallNormal);
        if (into > 0f) vel += wallNormal * into;

        // The brake is for going down and sideways, not up
        if (!keepUpwardSpeed && vel.y > 0f) vel.y = 0f;

        movement.ResetMotion();
        movement.IsClimbing = true;   // PlayerMovement waits while this is true
        state = State.Brake;
        timer = 0f;
        noWallTime = 0f;
        spaceReleasedAt = -1f;
        Debug.Log("[Brake] Started. Speed " + vel.magnitude.ToString("F1") + " m/s, it will take about " + (vel.magnitude / brakeDeceleration).ToString("F1") + " s to stop.");
    }

    // ---------- Running ----------
    void RunBrake()
    {
        // Climb jump: Space was let go, and now it is pressed again in time.
        // (This is checked FIRST, because on the frame you press it, Space counts as held again.)
        if (spaceReleasedAt >= 0f && input.JumpTriggered && climb != null && climb.CanJump
            && Time.time - spaceReleasedAt <= climbWindow)
        {
            EndBrake("wall climb jump", false);
            climb.Jump(wallNormal);
            return;
        }

        if (needHoldSpace && Keyboard.current != null && !Keyboard.current[Key.Space].isPressed)
        {
            bool canClimbJump = climb != null && climb.CanJump;
            if (spaceReleasedAt < 0f)
            {
                spaceReleasedAt = Time.time;
                Debug.Log("[Brake] Space released. " + (canClimbJump ? "Press it again within " + climbWindow + " s for a climb jump." : "No climb jump available (" + (climb == null ? "PlayerWallClimb is not on the Player" : "no jumps left") + ") so you fall."));
            }
            if (!canClimbJump || Time.time - spaceReleasedAt > climbWindow)
            {
                EndBrake("Space released", true);
                return;
            }
        }
        else if (spaceReleasedAt >= 0f && Time.time - spaceReleasedAt > climbWindow)
        {
            spaceReleasedAt = -1f;
        }

        Vector3 n;
        if (FindWall(out n, 180f)) { wallNormal = n; noWallTime = 0f; }   // all around you, so looking away does not lose the wall
        else
        {
            noWallTime += Time.deltaTime;
            if (noWallTime > 0.25f) { EndBrake("wall is gone", true); return; }
        }

        if (state == State.Brake)
        {
            // A steady slowdown: the same amount every second. That is what makes it smooth.
            float speed = Mathf.MoveTowards(vel.magnitude, 0f, brakeDeceleration * Time.deltaTime);
            vel = vel.sqrMagnitude > 0.0001f ? vel.normalized * speed : Vector3.zero;
            if (speed <= 0.05f)
            {
                vel = Vector3.zero;
                state = State.Hang;
                timer = 0f;
                Debug.Log("[Brake] Stopped. Hanging for " + hangBeforeSlip + " s.");
            }
        }
        else if (state == State.Hang)
        {
            vel = Vector3.zero;
            timer += Time.deltaTime;
            if (timer >= hangBeforeSlip)
            {
                state = State.Slip;
                timer = 0f;
                Debug.Log("[Brake] Slipping down for " + slipTime + " s.");
            }
        }
        else if (state == State.Slip)
        {
            float t = slipTime > 0f ? Mathf.Clamp01(timer / slipTime) : 1f;
            vel = Vector3.down * SlipSpeedAt(t);
            timer += Time.deltaTime;
            if (timer >= slipTime) { EndBrake("time is up, falling", true); return; }
        }

        controller.Move(vel * Time.deltaTime);

        // Touched the floor while going down = done
        if ((controller.collisionFlags & CollisionFlags.Below) != 0 && vel.y <= 0.01f)
            EndBrake("landed", false);
    }

    // ---------- End ----------
    void EndBrake(string why, bool startCooldown)
    {
        state = State.None;
        if (movement != null)
        {
            movement.IsClimbing = false;
            // Hand the speed back so normal movement carries on from here (and gravity takes over)
            movement.velocity = new Vector3(0f, Mathf.Min(vel.y, 0f), 0f);
            movement.SetHorizontalVelocity(new Vector3(vel.x, 0f, vel.z));
        }
        if (startCooldown) cooldownEnd = Time.time + cooldown;
        Debug.Log("[Brake] Ended: " + why + (startCooldown ? ". Cooldown " + cooldown + " s." : "."));
    }

    // Goes through the slipSpeeds list: t = 0 is the first number, t = 1 is the last, evenly in between.
    float SlipSpeedAt(float t)
    {
        if (slipSpeeds == null || slipSpeeds.Length == 0) return 1f;
        if (slipSpeeds.Length == 1) return slipSpeeds[0];
        float f = t * (slipSpeeds.Length - 1);
        int i = Mathf.Min((int)f, slipSpeeds.Length - 2);
        return Mathf.Lerp(slipSpeeds[i], slipSpeeds[i + 1], f - i);
    }

    // ---------- Wall check ----------
    // Looks for the nearest upright wall in front of you. It checks a fan of directions
    // (left to right of where you look), so you do not have to face the wall head-on.
    bool FindWall(out Vector3 normal)
    {
        return FindWall(out normal, wallSearchAngle);
    }

    bool FindWall(out Vector3 normal, float searchAngle)
    {
        normal = Vector3.zero;
        Transform cam = movement.playerCamera;
        Vector3 look = cam != null ? cam.forward : -transform.forward;
        look.y = 0f;
        if (look.sqrMagnitude < 0.0001f) return false;
        look.Normalize();

        Vector3 origin = transform.position + controller.center;
        float dist = controller.radius + wallCheckDistance;
        float best = float.MaxValue;
        bool found = false;

        int rays = searchAngle <= 0f ? 1 : Mathf.CeilToInt(searchAngle / 20f) * 2 + 1;
        for (int r = 0; r < rays; r++)
        {
            float angle = rays == 1 ? 0f : Mathf.Lerp(-searchAngle, searchAngle, r / (float)(rays - 1));
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * look;

            foreach (RaycastHit h in Physics.RaycastAll(origin, dir, dist, wallLayers, QueryTriggerInteraction.Ignore))
            {
                if (h.collider.transform.IsChildOf(transform)) continue;   // never the player's own parts
                if (h.distance >= best) continue;
                if (Mathf.Abs(h.normal.y) > 0.3f) continue;                // only upright walls
                best = h.distance;
                normal = new Vector3(h.normal.x, 0f, h.normal.z).normalized;
                found = true;
            }
        }
        return found;
    }

    // ---------- Test text ----------
    void OnGUI()
    {
        if (!showDebug || Event.current.type != EventType.Repaint) return;
        string text = "Wall brake: " + state +
            "\nspeed: " + vel.magnitude.ToString("F1") + " m/s" +
            ((state == State.Hang || state == State.Slip) ? "\ntimer: " + timer.ToString("F1") + " s" : "") +
            "\ncooldown: " + CooldownLeft.ToString("F1") + " s" +
            (spaceReleasedAt >= 0f && state != State.None ? "\nPRESS SPACE NOW: " + Mathf.Max(0f, climbWindow - (Time.time - spaceReleasedAt)).ToString("F2") + " s" : "");
        GUI.Box(new Rect(10f, 10f, 220f, 105f), text);
    }
}