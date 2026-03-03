using System;

namespace NightShift.Core
{
    /// <summary>
    /// Data model for a single anomaly report.
    /// </summary>
    public struct AnomalyReportData
    {
        public string reportId;
        public float timestamp;
        public string reportedDefinitionId;
        public string actualDefinitionId;
        public bool wasCorrect;

        public static AnomalyReportData Create(string reportedDefinitionId, string actualDefinitionId, bool wasCorrect, float timestamp)
        {
            return new AnomalyReportData
            {
                reportId = Guid.NewGuid().ToString(),
                timestamp = timestamp,
                reportedDefinitionId = reportedDefinitionId ?? string.Empty,
                actualDefinitionId = actualDefinitionId ?? string.Empty,
                wasCorrect = wasCorrect
            };
        }
    }
}
