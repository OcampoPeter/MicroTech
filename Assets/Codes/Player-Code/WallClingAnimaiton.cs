using UnityEngine;

/// <summary>
/// Drives the wall hang animations in PlayerAnimController.
/// Does NOT touch PlayerAnimator or any locomotion / air parameter.
///
/// ANIMATOR PARAMETERS (add these):
///   WallCling   (Bool)
///   HangAction  (Float)   0 = Drop, 1 = Climb up, 2 = Jump away   (blend tree parameter)
///   HangExit    (Trigger)
///
/// USAGE (from your wall script):
///   clingAnim.StartCling();   // player grabs the wall  -> "Hang Braced", then "Hang Idle"
///   clingAnim.DropFromWall(); // player lets go         -> Hang Drop
///   clingAnim.ClimbUp();      // player climbs up       -> Hang Jump Up
///   clingAnim.JumpAway();     // player jumps off wall  -> Hang Jump Opposite Wall
///
/// NOTE: Apply Root Motion is off, so the animation plays in place. Your own code
/// still has to move the player (climb up onto the ledge, push away from the wall).
/// </summary>
public class WallClingAnimation : MonoBehaviour
{
    private enum HangAction { Drop = 0, Climb = 1, JumpAway = 2 }

    [Header("References")]
    [Tooltip("Leave empty to find the Animator on the model automatically.")]
    [SerializeField] private Animator animator;

    private static readonly int WallClingHash = Animator.StringToHash("WallCling");
    private static readonly int HangActionHash = Animator.StringToHash("HangAction");
    private static readonly int HangExitHash = Animator.StringToHash("HangExit");

    private bool clinging;

    public bool IsClinging => clinging;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    /// <summary>Call when the player grabs the wall.</summary>
    public void StartCling()
    {
        if (clinging || animator == null) return;
        clinging = true;
        animator.ResetTrigger(HangExitHash);
        animator.SetBool(WallClingHash, true);
    }

    /// <summary>Player lets go and falls.</summary>
    public void DropFromWall() => Leave(HangAction.Drop);

    /// <summary>Player climbs up onto the ledge.</summary>
    public void ClimbUp() => Leave(HangAction.Climb);

    /// <summary>Player jumps away from the wall.</summary>
    public void JumpAway() => Leave(HangAction.JumpAway);

    /// <summary>
    /// Fallback if the cling ends for any other reason (wall gone, landed, etc.).
    /// Plays the drop animation so the player is never stuck in the hang pose.
    /// </summary>
    public void StopCling() => Leave(HangAction.Drop);

    private void Leave(HangAction action)
    {
        if (!clinging || animator == null) return;
        clinging = false;
        animator.SetBool(WallClingHash, false);

        // Set the exact value (no damping) so the blend tree plays ONE clip, not a mix.
        animator.SetFloat(HangActionHash, (float)action);
        animator.SetTrigger(HangExitHash);
    }

    private void OnDisable()
    {
        // Never leave the bool stuck on if the component is disabled.
        if (clinging && animator != null)
        {
            clinging = false;
            animator.SetBool(WallClingHash, false);
        }
    }
}