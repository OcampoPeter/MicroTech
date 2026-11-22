// WHERE TO PUT THIS FILE:  Assets Task / Shared
// ------------------------------------------------------------
using UnityEngine;

// Goes on the player (added automatically to the object tagged "Player").
// It notices WHAT the player is standing on and tells that platform's StepTrigger
// each time the player lands on it.
//
// A hit = the player jumps while standing on a platform and lands back on the SAME platform.
// Getting onto the platform (walking or jumping onto it) does not count.
// Works with a CharacterController player. A Rigidbody player works too.
public class PlayerStepReporter : MonoBehaviour
{
    // The reporter that is currently active in the scene.
    public static PlayerStepReporter Current;

    // If the player touched no ground for this many seconds, they count as "in the air".
    const float AirTime = 0.12f;

    // Surfaces pointing up more than this count as "ground" (ignores walls and ceilings).
    const float MinUpNormal = 0.5f;

    StepTrigger lastSurface;     // the platform the player was last standing on
    float lastTouchTime = -10f;  // when the player last touched ground

    // Finds the player and adds this reporter to it (does nothing if it already exists).
    public static void EnsureAttached()
    {
        if (Current != null) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        var existing = player.GetComponent<PlayerStepReporter>();
        Current = existing != null ? existing : player.AddComponent<PlayerStepReporter>();
    }

    void Awake() => Current = this;

    Collider playerCol;   // the player's own collider (used to look straight down)

    // Standing still: the player may not touch anything new each frame, so look straight down
    // and report the platform under the feet (only platforms in "StandOnIt" mode).
    void Update()
    {
        if (playerCol == null) playerCol = GetComponent<Collider>();
        if (playerCol == null) return;

        Bounds b = playerCol.bounds;
        if (Physics.Raycast(b.center, Vector3.down, out RaycastHit hit, b.extents.y + 0.35f,
                            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            StepTrigger surface = hit.collider.GetComponentInParent<StepTrigger>();
            if (surface != null && surface.mode == StepMode.StandOnIt)
                surface.Report(hit.collider);
        }
    }

    // CharacterController players: called when the controller touches something while moving.
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.normal.y < MinUpNormal) return; // not the top of something
        Touch(hit.collider);
    }

    // Rigidbody players: called every physics step while touching something.
    void OnCollisionStay(Collision collision)
    {
        foreach (var contact in collision.contacts)
        {
            if (contact.normal.y >= MinUpNormal)
            {
                Touch(collision.collider);
                return;
            }
        }
    }

    // The player is touching the top of this collider right now.
    void Touch(Collider ground)
    {
        // Which task platform does this collider belong to? (the platform itself or a child of it)
        StepTrigger surface = ground.GetComponentInParent<StepTrigger>();

        float now = Time.time;
        bool wasInAir = now - lastTouchTime > AirTime;

        // Remember where the player was standing BEFORE this touch.
        StepTrigger previous = lastSurface;

        lastTouchTime = now;
        lastSurface = surface;

        // Standing on something that is not a task platform: nothing to report.
        if (surface == null) return;

        if (surface.mode == StepMode.JumpOnIt)
        {
            // Bent: count only a jump made while standing on THIS platform that lands back on it.
            // Arriving from somewhere else (walking or jumping onto it) does NOT count.
            if (wasInAir && previous == surface)
                surface.Report(ground);
        }
        else if (surface.mode == StepMode.StepOnIt)
        {
            // Fried: count when the player gets onto the platform
            // (came from somewhere else, or landed on it after being in the air).
            if (previous != surface || wasInAir)
                surface.Report(ground);
        }
        else
        {
            // Missing: just standing on the platform is enough. The platform's own cooldown
            // keeps this from repeating too fast.
            surface.Report(ground);
        }
    }
}