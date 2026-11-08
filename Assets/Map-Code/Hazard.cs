using UnityEngine;

// Step 2b: a hazard (lava, spikes, laser). Turn on "Is Trigger" on its collider.
public class Hazard : MonoBehaviour
{
    [Tooltip("Seconds added to your time when you touch it.")]
    public float timePenalty = 3f;
    [Tooltip("Corruption percent added when you touch it.")]
    public float corruptionAmount = 10f;
    [Tooltip("Seconds before it can hit you again, so it doesn't hit every frame.")]
    public float cooldown = 1f;

    private float lastHitTime = -10f;

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time - lastHitTime < cooldown) return;

        lastHitTime = Time.time;
        RunTimer.Instance.AddPenalty(timePenalty);
        if (CorruptionSystem.Instance != null)
            CorruptionSystem.Instance.AddCorruption(corruptionAmount);
        Debug.Log("Hazard hit! +" + timePenalty.ToString() + "s");
    }
}