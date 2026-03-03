using UnityEngine;
using NightShift.Core;

namespace NightShift.Systems
{
    /// <summary>
    /// Runtime component attached to spawned anomaly instances.
    /// Tracks which definition it is and whether it has been fixed.
    /// </summary>
    public class AnomalyInstance : MonoBehaviour
    {
        public AnomalyDefinition Definition { get; set; }
        public string InstanceId { get; set; }
        public bool IsFixed { get; private set; }

        public void MarkFixed()
        {
            IsFixed = true;
        }

        /// <summary>
        /// Called when player attempts a fix. Returns true if fix was correct.
        /// </summary>
        public bool AttemptFix(string interactionId)
        {
            if (IsFixed) return false;

            bool correct = Definition.fixMethod.type == AnomalyDefinition.FixType.Interact
                || string.IsNullOrEmpty(Definition.fixMethod.correctInteractionId)
                || Definition.fixMethod.correctInteractionId == interactionId;

            if (correct)
                MarkFixed();

            return correct;
        }
    }
}
