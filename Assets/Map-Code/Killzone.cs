using UnityEngine;

public class KillZone : MonoBehaviour
{
    [Header("Where the player goes back to")]
    public Transform respawnPoint;

    [Header("Corruption added when you fall")]
    public float fallCorruption = 20f;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // The CharacterController must be off while we teleport, or it fights the move
        CharacterController cc = other.GetComponent<CharacterController>();
        cc.enabled = false;
        other.transform.position = respawnPoint.position;
        cc.enabled = true;

        // Clear leftover speed so you don't keep falling or sliding
        PlayerMovement movement = other.GetComponent<PlayerMovement>();
        if (movement != null) movement.ResetMotion();

        if (ScoreSystem.Instance != null)
            ScoreSystem.Instance.RegisterFall();

        if (CorruptionSystem.Instance != null)
            CorruptionSystem.Instance.AddCorruption(fallCorruption);

        Debug.Log("Fell in the void, respawned.");
    }
}