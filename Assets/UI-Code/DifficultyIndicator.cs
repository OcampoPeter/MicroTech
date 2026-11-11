using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class DifficultyIndicator : MonoBehaviour
{
    [Header("Color for each level (level 1 = first color, level 5 = last)")]
    public Color[] levelColors = new Color[]
    {
        new Color(0.45f, 1.00f, 0.55f),   // 1 green
        new Color(0.80f, 1.00f, 0.35f),   // 2 lime
        new Color(1.00f, 0.90f, 0.30f),   // 3 yellow
        new Color(1.00f, 0.60f, 0.25f),   // 4 orange
        new Color(1.00f, 0.30f, 0.30f)    // 5 red
    };

    public int maxLevel = 5;

    [Tooltip("Tick to also show the par time under the level.")]
    public bool showPar = false;

    private TMP_Text label;

    void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    void Update()
    {
        if (ScoreSystem.Instance == null) { label.text = ""; return; }

        int level = ScoreSystem.Instance.Difficulty;
        if (level < 1) return;   // ScoreSystem has not worked it out yet

        string text = "LEVEL " + level.ToString() + "/" + maxLevel.ToString();
        if (showPar)
            text += "\nPAR " + ScoreSystem.Instance.CurrentPar.ToString("F0") + "s";
        label.text = text;

        int index = Mathf.Clamp(level - 1, 0, levelColors.Length - 1);
        label.color = levelColors[index];
    }
}