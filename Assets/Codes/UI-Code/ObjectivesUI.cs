// ============================================================
// WHERE TO PUT THIS FILE:
//   Map-Code/Assets Task/Shared/ObjectivesUI.cs
//   REPLACE the old ObjectivesUI.cs (don't keep 2 copies).
//
// WHAT IT DOES:
//   Quest list: "Bent  1 / 3", "Missing  0 / 2", "Fried  0 / 1".
//   A row turns green + says DONE when all of that type are fixed.
//   A row is hidden if the map has 0 of that type.
//   NEW: when EVERYTHING is fixed, the task rows vanish and
//        the panel shows "Find the Exit".
//
// WORKS WITH BOTH kinds of text:
//   - TextMeshPro text
//   - normal (legacy) UI > Text
//
// SETUP: put this on your Panel. The 4 text objects are found
//   automatically by NAME (title, Bent, Missing, Fried).
//   The "Find the Exit" line is made automatically (no extra setup).
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

// Named ObjectiveType so it won't clash with your future damage enum.
public enum ObjectiveType { Bent, Missing, Fried }

public class ObjectivesUI : MonoBehaviour
{
    // Easy access from other scripts: ObjectivesUI.Instance
    public static ObjectivesUI Instance;

    [Header("Text rows (auto-found by name if left empty)")]
    public GameObject titleText;
    public GameObject bentText;
    public GameObject missingText;
    public GameObject friedText;

    [Header("Find the Exit line (made automatically if empty)")]
    public GameObject exitText;
    public string exitMessage = "Find the Exit";
    public Color exitColor = new Color(1f, 0.9f, 0.3f);

    [Header("Panel (leave EMPTY if this script is ON the panel)")]
    public GameObject panel;

    [Header("Look")]
    public string title = "OBJECTIVES";
    public Color pendingColor = Color.white;
    public Color doneColor = new Color(0.2f, 1f, 0.4f);

    [Header("Event")]
    public UnityEvent onAllComplete;   // optional: open a door, play a sound...

    // [0] = Bent, [1] = Missing, [2] = Fried
    int[] total = new int[3];
    int[] fixedCount = new int[3];
    bool allDone;

    void Awake()
    {
        Instance = this;

        // Find the text objects by name if slots are empty
        if (titleText == null) titleText = FindChildByName("title");
        if (bentText == null) bentText = FindChildByName("Bent");
        if (missingText == null) missingText = FindChildByName("Missing");
        if (friedText == null) friedText = FindChildByName("Fried");

        // Make the "Find the Exit" line by copying the Bent row (same font and size)
        if (exitText == null && bentText != null)
        {
            exitText = Instantiate(bentText, bentText.transform.parent);
            exitText.name = "ExitRow";
        }
        if (exitText != null) exitText.SetActive(false);
    }

    // Looks through all children for an object with this name
    GameObject FindChildByName(string objName)
    {
        Transform[] all = GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
        {
            if (t == transform) continue;
            if (t.gameObject.name.ToLower() == objName.ToLower()) return t.gameObject;
        }
        return null;
    }

    // Sets text + color on either TextMeshPro or legacy Text
    void SetLabel(GameObject obj, string message, Color color)
    {
        if (obj == null) return;

        TMP_Text tmp = obj.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = message;
            tmp.color = color;
            return;
        }

        Text legacy = obj.GetComponent<Text>();
        if (legacy != null)
        {
            legacy.text = message;
            legacy.color = color;
            return;
        }

        Debug.LogWarning("[ObjectivesUI] '" + obj.name + "' has no Text component!");
    }

    // ---- CALL THIS ONCE when the controller knows the counts ----
    public void SetTotals(int bent, int missing, int fried)
    {
        total[0] = bent;
        total[1] = missing;
        total[2] = fried;
        fixedCount = new int[3];
        allDone = false;
        Refresh();
    }

    // ---- CALL THIS each time ONE task is fixed ----
    public void ReportFixed(ObjectiveType type)
    {
        int i = (int)type;
        if (fixedCount[i] < total[i]) fixedCount[i]++;   // can't go over the total
        Refresh();
    }

    // ---- CALL THIS if a task goes back to broken (optional) ----
    public void ReportUnfixed(ObjectiveType type)
    {
        int i = (int)type;
        if (fixedCount[i] > 0) fixedCount[i]--;
        Refresh();
    }

    void Refresh()
    {
        int all = total[0] + total[1] + total[2];

        // Is everything fixed?
        bool complete = all > 0 && fixedCount[0] == total[0]
                                && fixedCount[1] == total[1]
                                && fixedCount[2] == total[2];

        if (titleText != null)
        {
            titleText.SetActive(true);
            SetLabel(titleText, title, pendingColor);
        }

        if (complete)
        {
            // Task rows vanish, "Find the Exit" appears
            if (bentText != null) bentText.SetActive(false);
            if (missingText != null) missingText.SetActive(false);
            if (friedText != null) friedText.SetActive(false);

            if (exitText != null)
            {
                exitText.SetActive(true);
                SetLabel(exitText, exitMessage, exitColor);
            }
        }
        else
        {
            if (exitText != null) exitText.SetActive(false);

            SetRow(bentText, "Bent", 0);
            SetRow(missingText, "Missing", 1);
            SetRow(friedText, "Fried", 2);
        }

        // Hide the whole panel if no tasks (skipped if script is ON the panel)
        if (panel != null && panel != gameObject) panel.SetActive(all > 0);

        // Fire the event only once, when everything is fixed
        if (complete && !allDone)
        {
            allDone = true;
            onAllComplete.Invoke();
        }
        else if (!complete)
        {
            allDone = false;
        }
    }

    void SetRow(GameObject row, string label, int i)
    {
        if (row == null) return;

        // Hide the row if this map has none of this type
        row.SetActive(total[i] > 0);
        if (total[i] == 0) return;

        bool done = fixedCount[i] >= total[i];
        string message = label + "  " + fixedCount[i] + " / " + total[i] + (done ? "  DONE" : "");
        SetLabel(row, message, done ? doneColor : pendingColor);
    }
}