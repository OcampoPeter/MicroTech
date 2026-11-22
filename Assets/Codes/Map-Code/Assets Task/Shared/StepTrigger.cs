// WHERE TO PUT THIS FILE:  Assets Task / Shared
// ------------------------------------------------------------
using System;
using UnityEngine;

// Goes on a task platform (BentPlatform, DarkPlatform and PowerNode add it automatically).
// It tells the task script when the player LANDS on this platform.
//
// How it works: the player carries a PlayerStepReporter (added automatically). The reporter
// sees which collider the player is standing on. If that collider belongs to this platform
// (the platform itself OR a model/collider placed inside it, like a bent model),
// the reporter calls Report() and the task script gets onPlayerStep.
// How a platform decides that the player "stepped" on it.
public enum StepMode
{
    JumpOnIt,   // only a jump made while standing on it counts (used by Bent)
    StepOnIt,   // getting onto it counts: walking or jumping onto it (used by Fried)
    StandOnIt   // just standing on it counts, no jump or movement needed (used by Missing)
}

public class StepTrigger : MonoBehaviour
{
    // Which kind of step counts on this platform. Set by the task script (Bent, Fried...).
    public StepMode mode = StepMode.JumpOnIt;

    // Fired each time the player lands on this platform.
    public event Action onPlayerStep;

    // After a hit, ignore new landings on this platform for this many seconds.
    // This stops one jump from counting twice (for example when the platform changes
    // shape and the player drops a little onto the new, lower model).
    public float cooldown = 0.8f;
    float lastHitTime = -999f;   // when the last hit was counted

    // DEBUG: prints a message in the Console on every landing.
    // Untick this in the Inspector once everything works.
    public bool debugLog = true;

    // Adds a StepTrigger to the platform (or returns the one it already has).
    // The second value is not used anymore; it is kept so older scripts still compile.
    public static StepTrigger Create(GameObject platform, float worldHeight = 0.3f)
    {
        var step = platform.GetComponent<StepTrigger>();
        if (step == null) step = platform.AddComponent<StepTrigger>();

        // Make sure the player can report landings.
        PlayerStepReporter.EnsureAttached();
        return step;
    }

    // Called by PlayerStepReporter when the player lands on this platform.
    public void Report(Collider surface)
    {
        // Too soon after the last hit: ignore it.
        if (Time.time - lastHitTime < cooldown) return;
        lastHitTime = Time.time;

        if (debugLog)
            Debug.Log($"[StepTrigger] player landed on '{name}' (collider: '{surface.name}')", this);

        onPlayerStep?.Invoke();
    }

    // If the player is created later than the platform, keep looking for it.
    void Update()
    {
        if (PlayerStepReporter.Current == null)
            PlayerStepReporter.EnsureAttached();
    }
}