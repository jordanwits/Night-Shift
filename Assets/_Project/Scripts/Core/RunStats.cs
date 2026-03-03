using System;

namespace NightShift.Core
{
    /// <summary>
    /// Stats tracked during a single run. Used for grading and credits.
    /// </summary>
    [Serializable]
    public class RunStats
    {
        public int anomaliesSpawned;
        public int anomaliesResolvedCorrect;
        public int anomaliesResolvedIncorrect;
        public int reportsFiled;
        public int falseAlertsReceived;
        public float maxInstabilityReached;
        public bool playerDownedAtAnyPoint;
        public bool survivedTo6AM;

        public static RunStats CreateEmpty()
        {
            return new RunStats();
        }
    }
}
