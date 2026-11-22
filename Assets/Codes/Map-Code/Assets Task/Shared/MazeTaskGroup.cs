// WHERE TO PUT THIS FILE:  Assets Task / Shared
// REPLACE the old MazeTaskGroup.cs (don't keep 2 copies).
// NEW: tells the ObjectivesUI quest panel when a task is fixed,
// and lets the ObjectiveCompass see which tasks are not fixed yet.
// ------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Put one of these on a maze root (or on a parent of the task objects).
/// Every Bent / MissingParts / Fried item registers itself here, and
/// onAllCompleted fires once when every registered item is done.
/// Use one group per task type per maze (e.g. a "BentTask" empty parent).
/// </summary>
public class MazeTaskGroup : MonoBehaviour
{
    public string taskName = "Task";
    public UnityEvent onAllCompleted = new UnityEvent();

    readonly HashSet<Object> registered = new HashSet<Object>();
    readonly HashSet<Object> completed = new HashSet<Object>();
    bool fired;

    public int Total => registered.Count;
    public int Done => completed.Count;
    public bool IsComplete => registered.Count > 0 && completed.Count >= registered.Count;

    public void Register(Object item) => registered.Add(item);

    // COMPASS: lets the compass see every task and whether it is fixed yet.
    public IEnumerable<Object> Items => registered;
    public bool IsItemDone(Object item) => completed.Contains(item);

    public void MarkComplete(Object item)
    {
        if (!registered.Contains(item)) registered.Add(item);
        if (!completed.Add(item)) return;

        Debug.Log($"[{taskName}] {Done}/{Total}");

        // OBJECTIVES UI: tell the quest panel one more task is fixed.
        ReportToObjectives();

        if (!fired && IsComplete)
        {
            fired = true;
            Debug.Log($"[{taskName}] COMPLETE");
            onAllCompleted?.Invoke();
        }
    }

    // Matches this group's name to the right row on the quest panel.
    void ReportToObjectives()
    {
        if (ObjectivesUI.Instance == null) return;

        if (taskName == "Bent") ObjectivesUI.Instance.ReportFixed(ObjectiveType.Bent);
        else if (taskName == "MissingParts") ObjectivesUI.Instance.ReportFixed(ObjectiveType.Missing);
        else if (taskName == "Fried") ObjectivesUI.Instance.ReportFixed(ObjectiveType.Fried);
    }
}