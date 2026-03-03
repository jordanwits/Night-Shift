using System.Collections.Generic;
using UnityEngine;
using NightShift.Core;

namespace NightShift.Generation
{
    /// <summary>
    /// Mall dressing v1: lightweight props + signage + landmarks per section.
    /// Procedural, fast, low-poly. Called after MallGenerator layout.
    /// </summary>
    public class MallDresser : MonoBehaviour
    {
        public static MallDresser Instance { get; private set; }

        private static readonly string[] StoreNames =
        {
            "TOYS", "FOOD", "ARCADE", "SHOES", "PHARMACY",
            "JEWELRY", "ELECTRONICS", "BOOKS"
        };

        private static readonly string[] DirectionTargets =
        {
            "FOOD COURT", "ARCADE", "RESTROOMS", "EXIT"
        };

        private const float GroundRaycastHeight = 5f;
        private const float GroundRaycastMaxDist = 20f;
        private const float GroundOffset = 0.02f;
        private const float MaxSignHeightAboveFloor = 4f;

        [Header("Config")]
        [SerializeField] private PropLibrary _propLibrary;
        [SerializeField] private float _overlapCheckRadiusMin = 0.35f;
        [SerializeField] private float _overlapCheckRadiusMax = 0.6f;
        [SerializeField] private LayerMask _overlapLayers = -1;
        [SerializeField] private int _minPropsHall = 2;
        [SerializeField] private int _maxPropsHall = 4;
        [SerializeField] private int _minPropsStore = 3;
        [SerializeField] private int _maxPropsStore = 5;
        [SerializeField] private bool _dressingEnabled = true;

        private Transform _dressingRoot;
        private Transform _generationRoot;
        private System.Random _rng;
        private int _lastSeed;
        private int _propsSpawned;
        private int _landmarksSpawned;

        public int PropsSpawned => _propsSpawned;
        public int LandmarksSpawned => _landmarksSpawned;
        public bool DressingEnabled => _dressingEnabled;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_propLibrary == null)
                _propLibrary = Resources.Load<PropLibrary>("ScriptableObjects/Systems/PropLibrary");
            if (_propLibrary == null)
                _propLibrary = Resources.Load<PropLibrary>("PropLibrary");
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Dress the current mall layout. Call after MallGenerator.Generate.</summary>
        public void Dress(IReadOnlyList<MallSection> sections, Transform generationRoot, int seed)
        {
            ClearDressing();

            if (!_dressingEnabled || sections == null || sections.Count == 0)
                return;

            _generationRoot = generationRoot;
            _lastSeed = seed;
            _rng = new System.Random(seed);
            _propsSpawned = 0;
            _landmarksSpawned = 0;

            _dressingRoot = new GameObject("Dressing").transform;
            _dressingRoot.SetParent(generationRoot);
            _dressingRoot.localPosition = Vector3.zero;
            _dressingRoot.localRotation = Quaternion.identity;
            _dressingRoot.localScale = Vector3.one;

            var usedStoreNames = new HashSet<string>();
            var usedDirectionTargets = new List<string>();

            for (int i = 0; i < sections.Count; i++)
            {
                var section = sections[i];
                if (section == null) continue;
                DressSection(section, usedStoreNames);
                ApplySectionMaterials(section, i);

                if (section.IsStart)
                    AddStartHubDirectionSigns(section, usedDirectionTargets);
            }

            Debug.Log($"[MallDresser] Dressed {sections.Count} sections. Props={_propsSpawned} Landmarks={_landmarksSpawned}");
        }

        private void DressSection(MallSection section, HashSet<string> usedStoreNames)
        {
            bool isStoreRoom = !section.IsStart && section.ConnectorPoints.Count == 1;

            if (isStoreRoom)
                AddStoreSignage(section, usedStoreNames);

            var propPoints = section.PropPoints;
            var landmarkPoints = section.LandmarkPoints;

            if (propPoints.Count > 0 && _propLibrary != null)
            {
                int maxProps = isStoreRoom ? _maxPropsStore : _maxPropsHall;
                int minProps = isStoreRoom ? _minPropsStore : _minPropsHall;
                int count = _rng.Next(minProps, Mathf.Min(maxProps + 1, propPoints.Count + 1));
                var shuffled = new List<Transform>(propPoints);
                Shuffle(shuffled);

                for (int i = 0; i < count && i < shuffled.Count; i++)
                {
                    var pt = shuffled[i];
                    if (pt == null) continue;
                    if (TrySpawnProp(pt, section.transform, false, isStoreRoom))
                        _propsSpawned++;
                }
            }

            if (landmarkPoints.Count > 0 && _propLibrary != null && _rng.NextDouble() < 0.4)
            {
                var pt = landmarkPoints[_rng.Next(landmarkPoints.Count)];
                if (pt != null && TrySpawnProp(pt, section.transform, true, isStoreRoom))
                    _landmarksSpawned++;
            }
        }

        private void ApplySectionMaterials(MallSection section, int sectionIndex)
        {
            var floorLight = Resources.Load<Material>("MallMaterials/Floor_Light");
            var floorDark = Resources.Load<Material>("MallMaterials/Floor_Dark");
            if (floorLight == null || floorDark == null) return;

            bool isStoreRoom = !section.IsStart && section.ConnectorPoints.Count == 1;
            var mat = isStoreRoom ? floorDark : (sectionIndex % 2 == 0 ? floorLight : floorDark);

            var floor = section.transform.Find("Floor");
            if (floor == null) return;

            foreach (Transform child in floor)
            {
                if (child == null || !child.name.StartsWith("FloorCore")) continue;
                var r = child.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = mat;
            }
        }

        private bool TrySpawnProp(Transform point, Transform sectionRoot, bool isLandmark, bool isStoreRoom)
        {
            Vector3 rayStart = point.position + Vector3.up * GroundRaycastHeight;
            if (!Physics.Raycast(rayStart, Vector3.down, out var hit, GroundRaycastMaxDist, _overlapLayers))
                return false;

            Vector3 groundPos = hit.point + Vector3.up * GroundOffset;
            float overlapRadius = _overlapCheckRadiusMin + (float)_rng.NextDouble() * (_overlapCheckRadiusMax - _overlapCheckRadiusMin);

            var overlaps = Physics.OverlapSphere(groundPos, overlapRadius, _overlapLayers);
            foreach (var h in overlaps)
            {
                if (h.transform.root == sectionRoot || h.transform.IsChildOf(sectionRoot))
                    continue;
                if (h.gameObject.name == "Floor" || h.gameObject.name.StartsWith("FloorCore") || h.gameObject.name == "Walls")
                    continue;
                return false;
            }

            var entry = _propLibrary.PickRandomForSection(_rng, isLandmark, isStoreRoom);
            if (entry == null) return false;

            GameObject go = null;
            if (!entry.usePrimitive && entry.prefab != null)
            {
                go = Instantiate(entry.prefab);
            }
            else
            {
                go = PropBuilder.Build(entry.primitiveType, Vector3.one);
            }

            if (go == null) return false;

            go.transform.SetParent(_dressingRoot);
            go.transform.position = groundPos;
            var fwd = Vector3.ProjectOnPlane(point.forward, hit.normal);
            go.transform.rotation = entry.alignToSurfaceNormal && hit.normal.sqrMagnitude > 0.01f
                ? (fwd.sqrMagnitude > 0.01f ? Quaternion.LookRotation(fwd, hit.normal) : Quaternion.FromToRotation(Vector3.up, hit.normal))
                : point.rotation;

            var scaleMin = entry.localScaleMin;
            var scaleMax = entry.localScaleMax;
            if (scaleMin != scaleMax)
            {
                float sx = (float)(_rng.NextDouble() * (scaleMax.x - scaleMin.x) + scaleMin.x);
                float sy = (float)(_rng.NextDouble() * (scaleMax.y - scaleMin.y) + scaleMin.y);
                float sz = (float)(_rng.NextDouble() * (scaleMax.z - scaleMin.z) + scaleMin.z);
                go.transform.localScale = new Vector3(sx, sy, sz);
            }

            if (entry.isPhysics)
            {
                var rb = go.GetComponent<Rigidbody>();
                if (rb == null) rb = go.AddComponent<Rigidbody>();
                rb.isKinematic = true;
            }

            return true;
        }

        private void AddStoreSignage(MallSection section, HashSet<string> usedStoreNames)
        {
            var signPoints = section.SignPoints;
            if (signPoints.Count == 0) return;

            string name = PickUnused(StoreNames, usedStoreNames);
            if (string.IsNullOrEmpty(name)) return;
            usedStoreNames.Add(name);
            section.StoreName = name;
            var label = section.GetComponent<Core.SectionLabel>();
            if (label == null) label = section.gameObject.AddComponent<Core.SectionLabel>();
            label.storeName = name;

            var pt = signPoints[_rng.Next(signPoints.Count)];
            if (pt == null) return;

            Vector3 rayStart = pt.position + Vector3.up * 2f;
            if (!Physics.Raycast(rayStart, Vector3.down, out var floorHit, 10f, _overlapLayers))
                return;

            float floorY = floorHit.point.y;
            float signY = Mathf.Min(pt.position.y, floorY + MaxSignHeightAboveFloor);

            var sign = CreateSignObject(name, 2f, 0.3f, pt.forward);
            sign.transform.SetParent(section.transform);
            sign.transform.position = new Vector3(pt.position.x, signY, pt.position.z);
            sign.transform.rotation = Quaternion.LookRotation(pt.forward, Vector3.up);
            sign.transform.localScale = Vector3.one;
            sign.name = $"Sign_{name}";
        }

        private void AddStartHubDirectionSigns(MallSection section, List<string> usedTargets)
        {
            var arrowPoints = section.ArrowSignPoints;
            if (arrowPoints.Count == 0) return;

            int count = Mathf.Min(arrowPoints.Count, DirectionTargets.Length);
            var targets = new List<string>(DirectionTargets);
            ShuffleList(targets);

            for (int i = 0; i < count; i++)
            {
                var pt = arrowPoints[i];
                if (pt == null) continue;

                string target = targets[i];
                string text = $"→ {target}";

                Vector3 rayStart = pt.position + Vector3.up * 2f;
                float signY = pt.position.y;
                if (Physics.Raycast(rayStart, Vector3.down, out var floorHit, 10f, _overlapLayers))
                    signY = Mathf.Min(pt.position.y, floorHit.point.y + MaxSignHeightAboveFloor);

                var sign = CreateSignObject(text, 1.5f, 0.2f, pt.forward);
                sign.transform.SetParent(section.transform);
                sign.transform.position = new Vector3(pt.position.x, signY, pt.position.z);
                sign.transform.rotation = Quaternion.LookRotation(pt.forward, Vector3.up);
                sign.transform.localScale = Vector3.one;
                sign.name = $"Direction_{target}";
            }
        }

        private GameObject CreateSignObject(string text, float width, float height, Vector3 facing)
        {
            var go = new GameObject("Sign");
            var backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backing.name = "Backing";
            backing.transform.SetParent(go.transform);
            backing.transform.localPosition = new Vector3(0f, 0f, -0.02f);
            backing.transform.localRotation = Quaternion.identity;
            backing.transform.localScale = new Vector3(width + 0.1f, height + 0.1f, 0.05f);
            var accentMat = Resources.Load<Material>("MallMaterials/Accent_Color");
            if (accentMat != null)
                backing.GetComponent<Renderer>().sharedMaterial = accentMat;
            Object.DestroyImmediate(backing.GetComponent<Collider>());

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Quad";
            quad.transform.SetParent(go.transform);
            quad.transform.localPosition = new Vector3(0f, 0f, 0.01f);
            quad.transform.localRotation = Quaternion.identity;
            quad.transform.localScale = new Vector3(width, height, 1f);

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard"));
            if (mat.shader.name.Contains("Unlit"))
                mat.color = new Color(0.95f, 0.95f, 0.9f);
            quad.GetComponent<Renderer>().sharedMaterial = mat;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform);
            textGo.transform.localPosition = new Vector3(0, 0, 0.02f);
            textGo.transform.localRotation = Quaternion.Euler(0, 180f, 0);
            textGo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            var textMesh = textGo.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.fontSize = 24;
            textMesh.characterSize = 1f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.black;
            textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return go;
        }

        private string PickUnused(string[] pool, HashSet<string> used)
        {
            var available = new List<string>();
            foreach (var s in pool)
            {
                if (!used.Contains(s))
                    available.Add(s);
            }
            if (available.Count == 0) return null;
            return available[_rng.Next(available.Count)];
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                var t = list[i];
                list[i] = list[j];
                list[j] = t;
            }
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                var t = list[i];
                list[i] = list[j];
                list[j] = t;
            }
        }

        /// <summary>Clear all dressing. Call before regenerating.</summary>
        public void ClearDressing()
        {
            if (_dressingRoot != null)
            {
                Destroy(_dressingRoot.gameObject);
                _dressingRoot = null;
            }
            _propsSpawned = 0;
            _landmarksSpawned = 0;
        }

        /// <summary>Toggle dressing on/off and regenerate dressing only.</summary>
        public void ToggleDressing()
        {
            _dressingEnabled = !_dressingEnabled;
            if (MallGenerator.Instance != null)
            {
                ClearDressing();
                if (_dressingEnabled)
                    Dress(MallGenerator.Instance.SpawnedSections, MallGenerator.Instance.transform, MallGenerator.Instance.Seed);
            }
            Debug.Log($"[MallDresser] Dressing {( _dressingEnabled ? "ON" : "OFF" )}");
        }

        /// <summary>Force re-dress current layout with same seed.</summary>
        public void ForceRedress()
        {
            if (MallGenerator.Instance != null)
            {
                ClearDressing();
                Dress(MallGenerator.Instance.SpawnedSections, MallGenerator.Instance.transform, MallGenerator.Instance.Seed);
            }
        }
    }
}
