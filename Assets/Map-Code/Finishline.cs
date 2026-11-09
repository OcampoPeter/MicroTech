using UnityEngine;

// Put on the goal. Turn on "Is Trigger" on its collider.
public class FinishLine : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && ScoreSystem.Instance != null)
            ScoreSystem.Instance.Finish();
    }
}