using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class PlayerInputHandler : MonoBehaviour
{
    [Header("Input Asset Link")]
    public InputActionAsset inputActions;

    public Vector2 MoveInput { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool JumpTriggered { get; private set; }

    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction jumpAction;

    void Awake()
    {
        if (inputActions != null)
        {
            moveAction = inputActions.FindAction("Move");
            sprintAction = inputActions.FindAction("Sprint");
            jumpAction = inputActions.FindAction("Jump");
        }
    }

    void OnEnable() => inputActions?.Enable();
    void OnDisable() => inputActions?.Disable();

    void Update()
    {
        MoveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        IsSprinting = sprintAction != null && sprintAction.IsPressed();
        JumpTriggered = jumpAction != null && jumpAction.WasPressedThisFrame();
    }
}