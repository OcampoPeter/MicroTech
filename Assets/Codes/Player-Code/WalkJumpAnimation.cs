using UnityEngine;

// PLAYER ANIMATOR. Put this on the Player (same as before).
// It only READS your movement scripts and sets Animator parameters. It moves nothing.
public class PlayerAnimator : MonoBehaviour
{
    [Tooltip("Leave empty: it finds the Animator on the model under the Player.")]
    public Animator animator;
    [Tooltip("How smoothly Speed follows the real speed. Higher = smoother.")]
    public float speedDamping = 0.25f;
    [Tooltip("Above this speed (m/s) at takeoff, the jump counts as a sprint jump. Walk is 5, sprint is 10.")]
    public float sprintJumpSpeed = 7f;
    [Tooltip("The ground flickers on slopes and steps. Grounded stays true this long (seconds) after leaving it.")]
    public float groundedGrace = 0.08f;

    private PlayerMovement movement;
    private CharacterController controller;
    private float lastGroundedTime;
    private bool wasGrounded = true;
    private bool sprintJump;

    void Start()
    {
        movement = GetComponent<PlayerMovement>();
        controller = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (animator == null || movement == null || controller == null) return;

        // Speed = real sideways speed in meters per second
        animator.SetFloat("Speed", movement.HorizontalSpeed, speedDamping, Time.deltaTime);

        // Grounded, with a short grace so slopes and steps do not flicker.
        // Rising fast always counts as in the air (so a jump starts at once).
        float up = movement.velocity.y;
        if (controller.isGrounded) lastGroundedTime = Time.time;
        bool grounded = controller.isGrounded
            || (Time.time - lastGroundedTime < groundedGrace && up <= 1f);

        // At the moment of takeoff, remember if we were sprinting
        if (wasGrounded && !grounded)
            sprintJump = movement.HorizontalSpeed > sprintJumpSpeed;
        wasGrounded = grounded;

        animator.SetBool("Grounded", grounded);
        animator.SetFloat("VerticalSpeed", up);
        animator.SetBool("SprintJump", sprintJump);
    }
}