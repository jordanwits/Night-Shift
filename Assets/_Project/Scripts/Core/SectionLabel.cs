using UnityEngine;

namespace NightShift.Core
{
    /// <summary>
    /// Optional label on a mall section for dispatch messaging. Used by AnomalyManager to resolve store names.
    /// Lives in Core to avoid circular asmdef references.
    /// </summary>
    public class SectionLabel : MonoBehaviour
    {
        public string storeName;
    }
}
