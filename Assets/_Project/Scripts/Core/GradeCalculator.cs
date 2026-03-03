namespace NightShift.Core
{
    /// <summary>
    /// Calculates grade letter and credits from RunStats.
    /// </summary>
    public static class GradeCalculator
    {
        private const int BaseScore = 100;
        private const int PenaltyPerIncorrectReport = 10;
        private const int PenaltyPerMissedAnomaly = 5;
        private const int PenaltyIfDowned = 15;
        private const int BonusPerCorrectReport = 5;
        private const int CorrectReportBonusCap = 30;

        public static char GetGradeLetter(RunStats stats)
        {
            int score = CalculateScore(stats);
            if (score >= 95) return 'S';
            if (score >= 85) return 'A';
            if (score >= 70) return 'B';
            if (score >= 55) return 'C';
            if (score >= 40) return 'D';
            return 'F';
        }

        public static int GetBaseCreditsEarned(RunStats stats)
        {
            char grade = GetGradeLetter(stats);
            return grade switch
            {
                'S' => 120,
                'A' => 90,
                'B' => 70,
                'C' => 50,
                'D' => 30,
                _ => 10
            };
        }

        public static int CalculateScore(RunStats stats)
        {
            int score = BaseScore;

            score -= stats.anomaliesResolvedIncorrect * PenaltyPerIncorrectReport;
            int missed = stats.anomaliesSpawned - stats.anomaliesResolvedCorrect - stats.anomaliesResolvedIncorrect;
            if (missed > 0)
                score -= missed * PenaltyPerMissedAnomaly;
            if (stats.playerDownedAtAnyPoint)
                score -= PenaltyIfDowned;
            score -= UnityEngine.Mathf.RoundToInt(stats.maxInstabilityReached / 10f);

            int correctBonus = UnityEngine.Mathf.Min(stats.anomaliesResolvedCorrect * BonusPerCorrectReport, CorrectReportBonusCap);
            score += correctBonus;

            return UnityEngine.Mathf.Max(0, score);
        }
    }
}
