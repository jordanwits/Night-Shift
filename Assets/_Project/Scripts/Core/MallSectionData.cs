using UnityEngine;

namespace NightShift.Core
{
    [CreateAssetMenu(fileName = "NewSection", menuName = "Night Shift/Mall Section Data")]
    /// <summary>
    /// ScriptableObject defining a mall section type for procedural generation.
    /// </summary>
    public class MallSectionData : ScriptableObject
    {
        [Header("Identity")]
        public string sectionId;
        public string displayName;

        [Header("Generation")]
        public GameObject sectionPrefab;
        public int connectionPoints = 4;  // Number of connector nodes
        public MallSectionType sectionType;

        public enum MallSectionType
        {
            Corridor,
            Store,
            Plaza
        }
    }
}
