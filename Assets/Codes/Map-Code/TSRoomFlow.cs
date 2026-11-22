using System.Collections;
using UnityEngine;

// Remembers tutorial progress between scenes (it is not on any object).
public static class TutorialProgress
{
    public static bool PcDelivered;   // true once the first PC has come out of the chute

    // Starts every Play session clean (also when Unity skips the domain reload)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ClearOnPlay() { PcDelivered = false; }
}

public class TSRoomFlow : MonoBehaviour
{
    [Header("References")]
    public DeliveryChute chute;
    public GameObject powerSupplyPrefab;
    public GameObject pcPrefab;

    [Header("Timing")]
    public float startDelay = 2f;
    public float gapBetweenPackages = 2f;

    [Header("Testing only")]
    [Tooltip("Tick this to skip the tutorial: the chute sends the fixed power supply and the PC straight away.")]
    public bool skipTutorial = false;

    public const string PowerSupplyId = "PSU";

    IEnumerator Start()
    {
        if (chute == null)
        {
            Debug.LogError("TSRoomFlow: drag the ChuteMouth object into the Chute slot.");
            yield break;
        }

        if (skipTutorial) PartRepair.MarkFixed(PowerSupplyId);

        yield return new WaitForSeconds(startDelay);

        bool tutorialDone = PartRepair.IsFixed(PowerSupplyId);

        if (!tutorialDone)
        {
            // Tutorial part 1: only the (dead) power supply comes out
            chute.Deliver(powerSupplyPrefab);
            yield break;
        }

        // The tutorial maze is done. The first time back, send the fixed power supply and the PC.
        if (!TutorialProgress.PcDelivered)
        {
            chute.Deliver(powerSupplyPrefab);
            yield return new WaitForSeconds(gapBetweenPackages);
            chute.Deliver(pcPrefab);
            TutorialProgress.PcDelivered = true;
        }
    }
}