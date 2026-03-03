using System.Collections.Generic;
using UnityEngine;
using NightShift.Core;
using NightShift.Systems;

namespace NightShift.Generation
{
    /// <summary>
    /// Graph-based procedural mall generator. Uses section prefabs and deterministic seed.
    /// </summary>
    public class MallGenerator : MonoBehaviour, IGameStateListener
    {
        public static MallGenerator Instance { get; private set; }

        [Header("Generation")]
        [SerializeField] private MallSectionData[] _sectionTypes;
        [SerializeField] private int _sectionCount = 5;
        [SerializeField] private int _seed = 12345;
        [SerializeField] private Transform _generationRoot;
        [SerializeField] private float _sectionSpacing = 20f;

        private System.Random _rng;
        private readonly List<GameObject> _spawnedSections = new List<GameObject>();

        public int Seed => _seed;
        public IReadOnlyList<GameObject> SpawnedSections => _spawnedSections;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (_generationRoot == null)
                _generationRoot = transform;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void OnGameStateEntered(GameState state)
        {
            if (state == GameState.Bootstrap)
                Generate();
        }

        public void OnGameStateExited(GameState state) { }

        /// <summary>
        /// Generate mall with current or given seed.
        /// </summary>
        public void Generate(int? seed = null)
        {
            ClearExisting();
            _seed = seed ?? _seed;
            _rng = new System.Random(_seed);

            // Simple linear layout for Phase 1: place sections in a line/curve
            for (int i = 0; i < _sectionCount; i++)
            {
                GameObject section;
                if (_sectionTypes != null && _sectionTypes.Length > 0)
                {
                    MallSectionData data = _sectionTypes[_rng.Next(_sectionTypes.Length)];
                    if (data?.sectionPrefab != null)
                    {
                        Vector3 pos = _generationRoot.position + Vector3.forward * (i * _sectionSpacing);
                        section = Instantiate(data.sectionPrefab, pos, Quaternion.identity, _generationRoot);
                        section.name = $"{data.sectionId}_{i}";
                    }
                    else
                        section = CreatePlaceholderSection(i);
                }
                else
                    section = CreatePlaceholderSection(i);

                if (section != null)
                    _spawnedSections.Add(section);
            }
        }

        private GameObject CreatePlaceholderSection(int index)
        {
            string[] types = { "Corridor", "Store", "Plaza" };
            string type = types[_rng.Next(types.Length)];
            Vector3 pos = _generationRoot.position + Vector3.forward * (index * _sectionSpacing);
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(_generationRoot);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(15, 4, 15);
            go.name = $"{type}_{index}";
            return go;
        }

        private void ClearExisting()
        {
            foreach (var go in _spawnedSections)
            {
                if (go != null)
                    Destroy(go);
            }
            _spawnedSections.Clear();
        }

        /// <summary>
        /// Debug: Regenerate with new seed.
        /// </summary>
        public void DebugRegenerate(int newSeed)
        {
            _seed = newSeed;
            Generate(_seed);
        }
    }
}
