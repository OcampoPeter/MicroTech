using UnityEngine;
using TMPro;

public class ScoreSystem : MonoBehaviour
{
    public static ScoreSystem Instance;

    [Header("UI")]
    public TMP_Text scoreText;

    [Header("Score formula")]
    public int baseScore = 1000;
    [Tooltip("The time you should beat on difficulty 1 (seconds).")]
    public float parTime = 60f;
    public int pointsPerSecondUnderPar = 10;
    public int pointsLostPerFall = 150;
    [Tooltip("Points lost for every 1% of corruption you finish with.")]
    public float pointsLostPerCorruptionPercent = 5f;

    [Header("Difficulty scaling")]
    [Tooltip("Each difficulty level makes the par time this much shorter. 0.1 = 10% shorter.")]
    public float parShrinkPerLevel = 0.1f;
    [Tooltip("Each difficulty level adds this much to the score multiplier. 0.25 = +25%.")]
    public float scoreBonusPerLevel = 0.25f;

    [Tooltip("Only used when you press Play directly in this maze scene (no part carried in). 0 = use 1.")]
    public int directPlayDifficulty = 0;

    // Worked out from the difficulty of the part you came from (1 if you started this scene directly)
    public int Difficulty { get; private set; }
    public float CurrentPar { get; private set; }

    public int Falls { get; private set; }
    public int Score { get; private set; }
    public int BestScore { get; private set; }
    public bool IsNewBest { get; private set; }

    private const string BestKey = "BestScore";   // the name the score is saved under
    public bool IsFinished { get; private set; }

    void Awake() { Instance = this; }

    void Start()
    {
        BestScore = PlayerPrefs.GetInt(BestKey, 0);   // 0 if nothing saved yet

        // Difficulty formula: par = base par x (1 - shrink) ^ (difficulty - 1)
        bool cameFromPart = !string.IsNullOrEmpty(PartRepair.CurrentPart);
        int level = (!cameFromPart && directPlayDifficulty > 0) ? directPlayDifficulty : PartRepair.CurrentDifficulty;
        Difficulty = Mathf.Max(1, level);
        CurrentPar = parTime * Mathf.Pow(1f - parShrinkPerLevel, Difficulty - 1);
        Debug.Log("Difficulty " + Difficulty.ToString() + ", par time " + CurrentPar.ToString("F1") + "s");

        UpdateText();
    }

    public void RegisterFall()
    {
        if (IsFinished) return;
        Falls++;
    }

    public void Finish()
    {
        if (IsFinished) return;
        IsFinished = true;
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameState.Complete);

        RunTimer timer = RunTimer.Instance;
        timer.Stop();

        // Score = base + (seconds under par x points) - (falls x penalty)
        int secondsUnderPar = Mathf.RoundToInt(CurrentPar - timer.Time_);
        float corruption = CorruptionSystem.Instance != null ? CorruptionSystem.Instance.Corruption : 0f;
        int corruptionPenalty = Mathf.RoundToInt(corruption * pointsLostPerCorruptionPercent);
        int rawScore = baseScore + secondsUnderPar * pointsPerSecondUnderPar
                     - Falls * pointsLostPerFall - corruptionPenalty;
        if (rawScore < 0) rawScore = 0;

        // Harder mazes pay more: multiplier = 1 + bonus x (difficulty - 1)
        float multiplier = 1f + scoreBonusPerLevel * (Difficulty - 1);
        Score = Mathf.RoundToInt(rawScore * multiplier);

        // Save it if it beats the best score
        if (Score > BestScore)
        {
            BestScore = Score;
            IsNewBest = true;
            PlayerPrefs.SetInt(BestKey, BestScore);
            PlayerPrefs.Save();
        }

        UpdateText();
    }

    void UpdateText()
    {
        if (scoreText == null) return;
        string best = "BEST " + BestScore.ToString();
        if (!IsFinished)
        {
            scoreText.text = "SCORE ---   " + best;
            return;
        }
        scoreText.text = "FINISHED! SCORE " + Score.ToString() + "\n" + (IsNewBest ? "NEW BEST!" : best);
    }

    // Right-click the ScoreSystem component and choose this to wipe the saved best score
    [ContextMenu("Reset Best Score")]
    void ResetBestScore()
    {
        PlayerPrefs.DeleteKey(BestKey);
        BestScore = 0;
        UpdateText();
    }
}