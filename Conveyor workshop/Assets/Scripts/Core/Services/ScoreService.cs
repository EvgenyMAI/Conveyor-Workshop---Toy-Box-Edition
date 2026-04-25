using UnityEngine;

public sealed class ScoreService
{
    private readonly int maxDefects;
    private readonly int correctCargoScore;
    private readonly int wrongCargoPenalty;
    private readonly int wrongCargoDefectCost;

    public int Score { get; private set; }
    public int Defects { get; private set; }
    public int Streak { get; private set; }
    public int StreakTier { get; private set; }
    public int MaxDefects => maxDefects;

    public ScoreService(int maxDefects, int correctCargoScore, int wrongCargoPenalty, int wrongCargoDefectCost)
    {
        this.maxDefects = Mathf.Max(1, maxDefects);
        this.correctCargoScore = Mathf.Max(0, correctCargoScore);
        this.wrongCargoPenalty = Mathf.Max(0, wrongCargoPenalty);
        this.wrongCargoDefectCost = Mathf.Max(0, wrongCargoDefectCost);
    }

    public bool ApplyDelivery(bool isCorrect, int cargoScore)
    {
        if (isCorrect)
        {
            Streak++;
            int basePoints = cargoScore > 0 ? cargoScore : correctCargoScore;
            int newTier = Mathf.Min(3, Streak / 5);
            int streakBonus = newTier * 5;
            StreakTier = newTier;
            Score += basePoints + streakBonus;
            return false;
        }

        Streak = 0;
        StreakTier = 0;
        Score = Mathf.Max(0, Score - wrongCargoPenalty);
        Defects += wrongCargoDefectCost;
        return Defects >= maxDefects;
    }
}
