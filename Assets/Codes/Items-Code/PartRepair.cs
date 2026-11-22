using System.Collections.Generic;
using UnityEngine;

public static class PartRepair
{
    // The part you are fixing right now (set by MapPortal when you enter its maze)
    public static string CurrentPart;

    // How hard the maze you are entering is (1 = easiest). Set by MapPortal from the part's PartState.
    public static int CurrentDifficulty = 1;

    private static readonly HashSet<string> fixedParts = new HashSet<string>();

    public static bool IsFixed(string id) { return fixedParts.Contains(id); }

    public static void MarkFixed(string id)
    {
        if (!string.IsNullOrEmpty(id)) fixedParts.Add(id);
    }

    public static void MarkCurrentFixed() { MarkFixed(CurrentPart); }

    public static void ResetAll()
    {
        fixedParts.Clear();
        CurrentPart = null;
        CurrentDifficulty = 1;
    }

    // Starts every Play session clean (also when Unity skips the domain reload)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ClearOnPlay() { ResetAll(); }
}