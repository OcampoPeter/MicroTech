using UnityEngine;

public class PlayerWallRun : MonoBehaviour
{
    [Header("References")]
    public CharacterController controller;
    private PlayerInputHandler input;
    private PlayerMovement movement;

    [Header("Wall Settings")]
    public float wallRunSpeed = 9f;
    public float wallRunGravity = 1.5f;      // Kept low so your slide downward is completely smooth
    
    [Header("Wall Jump Settings")]
    public float wallJumpUpForce = 6f;       
    public float wallJumpSideForce = 12f;     // Increased to cleanly blow you away from the wall normal
    public float wallJumpForwardForce = 6f;  
    
    [Header("Detection Settings")]
    public LayerMask whatIsWall;

    public bool IsWallRunning { get; private set; }
    private Vector3 currentWallNormal;
    private bool touchingWallThisFrame;
    
    private float wallJumpCooldown = 0.35f;
    private float lastWallJumpTime;

    void Start()
    {
        input = GetComponent<PlayerInputHandler>();
        movement = GetComponent<PlayerMovement>();
        if (controller == null) controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (Time.time < lastWallJumpTime + wallJumpCooldown)
        {
            StopWallRunning();
            return;
        }

        bool isMovingForward = input.MoveInput.y > 0;
        bool inAir = !controller.isGrounded;
        bool holdingLateralInput = Mathf.Abs(input.MoveInput.x) > 0.1f;

        if (inAir && isMovingForward && touchingWallThisFrame && holdingLateralInput)
        {
            StartWallRunning();
        }
        else
        {
            StopWallRunning();
        }

        touchingWallThisFrame = false;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (Time.time < lastWallJumpTime + wallJumpCooldown) return;

        if (((1 << hit.gameObject.layer) & whatIsWall) != 0)
        {
            if (Mathf.Abs(hit.normal.y) < 0.3f) 
            {
                touchingWallThisFrame = true;
                currentWallNormal = hit.normal; 
            }
        }
    }

    void StartWallRunning()
    {
        IsWallRunning = true;
        
        Vector3 wallForward = Vector3.Cross(currentWallNormal, transform.up);
        if ((transform.forward - wallForward).magnitude > (transform.forward - -wallForward).magnitude)
        {
            wallForward = -wallForward;
        }

        // Drop down physics applied directly via custom frame translation
        Vector3 runVelocity = wallForward * wallRunSpeed;
        runVelocity.y = -wallRunGravity;

        controller.Move(runVelocity * Time.deltaTime);

        if (movement != null)
        {
            // Sync current track gravity profile
            movement.velocity = runVelocity;

            if (input.JumpTriggered)
            {
                lastWallJumpTime = Time.time;
                
                // Construct instant hard explosion outward away from the geometry normal
                Vector3 directionalPush = currentWallNormal * wallJumpSideForce; 
                Vector3 forwardPush = transform.forward * wallJumpForwardForce;
                float verticalPush = Mathf.Sqrt(wallJumpUpForce * -2f * movement.gravity);

                // Re-inject physical trajectory overrides directly to movement component
                movement.velocity = directionalPush + forwardPush;
                movement.velocity.y = verticalPush;
                
                StopWallRunning();
            }
        }
    }

    void StopWallRunning()
    {
        IsWallRunning = false;
    }
}
