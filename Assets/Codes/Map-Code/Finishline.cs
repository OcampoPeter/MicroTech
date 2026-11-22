using UnityEngine;

public class FinishLine : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && ScoreSystem.Instance != null)
            ScoreSystem.Instance.Finish();
    }
}