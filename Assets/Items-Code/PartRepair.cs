using System.Collections.Generic;
using UnityEngine;

// Remembers which PC parts are fixed. It is not on any object: it lives between scenes.
// Example: finish the RAM maze -> MarkCurrentFixed() -> back in the TS room the RAM shows as fixed.
public static class PartRepair
{
    // The part you are fixing right now (set by MapPortal when you enter its maze)
    public static string CurrentPart;

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
    }

    // Starts every Play session clean (also when Unity skips the domain reload)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ClearOnPlay() { ResetAll(); }
}