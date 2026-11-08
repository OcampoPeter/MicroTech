using UnityEngine;
using TMPro;

// Step 4: score. Put this on an empty object called "ScoreSystem".
// The score is worked out when you reach the finish line.
public class ScoreSystem : MonoBehaviour
{
    public static ScoreSystem Instance;

    [Header("UI")]
    public TMP_Text scoreText;

    [Header("Score formula")]
    public int baseScore = 1000;
    [Tooltip("The time you should beat (seconds).")]
    public float parTime = 60f;
    public int pointsPerSecondUnderPar = 10;
    public int pointsLostPerFall = 150;

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

        RunTimer timer = RunTimer.Instance;
        timer.Stop();

        // Score = base + (seconds under par x points) - (falls x penalty)
        int secondsUnderPar = Mathf.RoundToInt(parTime - timer.Time_);
        Score = baseScore + secondsUnderPar * pointsPerSecondUnderPar - Falls * pointsLostPerFall;
        if (Score < 0) Score = 0;

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