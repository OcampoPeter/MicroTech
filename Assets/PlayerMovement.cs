using UnityEngine;

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
    public float accelerationRate = 18f;     // MUCH FASTER: Instantly snaps closer to sprint speed
    public float decelerationRate = 14f;     // Clean decay back down
    public float modelRotationOffset = 180f;

    [Header("Gravity & Jump")]
    public float gravity = -19.62f;
    public float jumpHeight = 2.5f;
    [HideInInspector] public Vector3 velocity;

    [Header("High Fall Momentum")]
    public float fallSpeedThreshold = 12f;  
    public float momentumCarryFactor = 0.4f; 
    
    private float currentSpeed;
    private float highestFallSpeedThisJump;

    void Start()
    {
        input = GetComponent<PlayerInputHandler>();
        wallRun = GetComponent<PlayerWallRun>();
        if (controller == null) controller = GetComponent<CharacterController>();
        currentSpeed = walkSpeed;
    }

    void Update()
    {
        // CRUCIAL: Completely freeze this movement math if wall running so it doesn't fight physics
        if (wallRun != null && wallRun.IsWallRunning) return;

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
        float targetSpeed = (input.IsSprinting && input.MoveInput.magnitude > 0) ? maxSprintSpeed : walkSpeed;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, (currentSpeed < targetSpeed ? accelerationRate : decelerationRate) * Time.deltaTime);

        Vector3 direction = new Vector3(input.MoveInput.x, 0f, input.MoveInput.y).normalized;
        if (direction.magnitude >= 0.1f && playerCamera != null)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + playerCamera.eulerAngles.y;
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            
            controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, targetAngle + modelRotationOffset, 0f);
        }

        if (input.JumpTriggered && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
