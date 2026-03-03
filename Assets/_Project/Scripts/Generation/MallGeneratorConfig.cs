using UnityEngine;

namespace NightShift.Generation
{
    /// <summary>
    /// Runtime-loadable config for MallGenerator section prefabs.
    /// Place in Resources/MallSections/MallGeneratorConfig.
    /// </summary>
    public class MallGeneratorConfig : ScriptableObject
    {
        public GameObject startHubPrefab;
        public GameObject[] sectionPrefabs;
    }
}
