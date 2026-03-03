using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NightShift.Generation;
using NightShift.Systems;

namespace NightShift.Editor
{
    /// <summary>
    /// Creates 4 mall section prefabs for Mall Generation v1.
    /// Menu: Night Shift > Create Mall Section Prefabs.
    /// </summary>
    public static class CreateMallSectionPrefabs
    {
        private const string PrefabPath = "Assets/_Project/Art/Prefabs/MallSections";
        private const string MaterialsPath = "Assets/_Project/Resources/MallMaterials";

        // Normalized scale constants
        private const float CeilingHeight = 3.5f;
        private const float FloorThickness = 0.2f;
        /// <summary>Inset floor from wall edges so it stays fully inside walls.</summary>
        private const float FloorInsetFromWalls = 0.05f;
        /// <summary>How far floor stops before each connector opening plane. Prevents overlap at seams.</summary>
        private const float ConnectorCutInset = 0.25f;
        private const float HallWidth = 4.5f;
        private const float HallLength = 10f;
        private const float StoreSize = 7f;
        private const float WallThickness = 0.25f;
        private const float CeilingThickness = 0.1f;
        private const float TrimHeight = 0.1f;
        private const float TrimDepth = 0.15f;
        private const float TrimWallOffset = 0.015f;

        static Material _floorLight;
        static Material _floorDark;
        static Material _wallNeutral;
        static Material _accentColor;

        [MenuItem("Night Shift/Create Mall Section Prefabs")]
        public static void Create()
        {
            EnsureFolderExists("Assets/_Project", "Art", "Prefabs", "MallSections");
            EnsureMallMaterials();

            CreateStartHub();
            CreateHallStraight();
            CreateHallCorner();
            CreateStoreRoom();
            CreateOrUpdateMallGeneratorConfig();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[NightShift] Created mall section prefabs in " + PrefabPath);
        }

        static void EnsureMallMaterials()
        {
            EnsureFolderExists("Assets/_Project", "Resources");
            EnsureFolderExists("Assets/_Project/Resources", "MallMaterials");

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("URP/Lit") ?? Shader.Find("Standard");

            _floorLight = CreateOrLoadMaterial($"{MaterialsPath}/Floor_Light.mat", shader, new Color(0.85f, 0.84f, 0.82f));
            _floorDark = CreateOrLoadMaterial($"{MaterialsPath}/Floor_Dark.mat", shader, new Color(0.55f, 0.54f, 0.52f));
            _wallNeutral = CreateOrLoadMaterial($"{MaterialsPath}/Wall_Neutral.mat", shader, new Color(0.72f, 0.71f, 0.69f));
            _wallNeutral.SetFloat("_Smoothness", 0.2f);
            _accentColor = CreateOrLoadMaterial($"{MaterialsPath}/Accent_Color.mat", shader, new Color(0.35f, 0.55f, 0.52f)); // muted teal
            _accentColor.SetFloat("_Smoothness", 0.15f);
        }

        static Material CreateOrLoadMaterial(string path, Shader shader, Color color)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                mat.color = color;
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.color = color;
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }

        static void ApplyMaterial(GameObject go, Material mat)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null && mat != null)
                r.sharedMaterial = mat;
        }

        static void EnsureStructuralCollider(GameObject go)
        {
            if (go == null) return;
            go.layer = 0;
            var col = go.GetComponent<BoxCollider>();
            if (col == null) col = go.AddComponent<BoxCollider>();
            if (col != null) col.isTrigger = false;
        }

        static GameObject CreateWallCube(float width, float height, float depth)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.localScale = new Vector3(width, height, depth);
            EnsureStructuralCollider(go);
            return go;
        }

        static void AddPointLight(GameObject root, Vector3 localPos, float intensity, float range, bool warmTint)
        {
            var lightGo = new GameObject("PointLight");
            lightGo.transform.SetParent(root.transform);
            lightGo.transform.localPosition = localPos;
            lightGo.transform.localRotation = Quaternion.identity;
            lightGo.transform.localScale = Vector3.one;

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.intensity = intensity;
            light.range = range;
            light.color = warmTint ? new Color(1f, 0.95f, 0.88f) : new Color(0.98f, 0.98f, 1f);
        }

        static void CreateOrUpdateMallGeneratorConfig()
        {
            EnsureFolderExists("Assets/_Project", "Resources");
            EnsureFolderExists("Assets/_Project/Resources", "MallSections");
            string configPath = "Assets/_Project/Resources/MallSections/MallGeneratorConfig.asset";

            var config = AssetDatabase.LoadAssetAtPath<MallGeneratorConfig>(configPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<MallGeneratorConfig>();
                AssetDatabase.CreateAsset(config, configPath);
            }

            var startHub = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/StartHub.prefab");
            var hallStraight = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/HallStraight.prefab");
            var hallCorner = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/HallCorner.prefab");
            var storeRoom = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/StoreRoom.prefab");

            config.startHubPrefab = startHub;
            config.sectionPrefabs = new[] { hallStraight, hallCorner, storeRoom };
            EditorUtility.SetDirty(config);
        }

        static void EnsureFolderExists(string parent, string folder)
        {
            string fullPath = parent + "/" + folder;
            if (AssetDatabase.IsValidFolder(fullPath)) return;
            if (!AssetDatabase.IsValidFolder(parent))
            {
                int i = parent.LastIndexOf('/');
                if (i > 0)
                    EnsureFolderExists(parent.Substring(0, i), parent.Substring(i + 1));
            }
            AssetDatabase.CreateFolder(parent, folder);
        }

        static void EnsureFolderExists(string a, string b, string c, string d)
        {
            if (!AssetDatabase.IsValidFolder(a)) AssetDatabase.CreateFolder(a.Substring(0, a.LastIndexOf('/')), a.Substring(a.LastIndexOf('/') + 1));
            string ab = a + "/" + b;
            if (!AssetDatabase.IsValidFolder(ab)) AssetDatabase.CreateFolder(a, b);
            string abc = ab + "/" + c;
            if (!AssetDatabase.IsValidFolder(abc)) AssetDatabase.CreateFolder(ab, c);
            string abcd = abc + "/" + d;
            if (!AssetDatabase.IsValidFolder(abcd)) AssetDatabase.CreateFolder(abc, d);
        }

        static GameObject CreateMarker(string name)
        {
            return new GameObject(name);
        }

        /// <summary>Creates a single FloorCore primitive under Floor parent. Size is in local space; position is local to root.</summary>
        static GameObject CreateFloorCorePiece(Transform floorParent, string name, Vector3 localPos, Vector3 size, Material material)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = name;
            piece.transform.SetParent(floorParent);
            piece.transform.localPosition = localPos;
            piece.transform.localRotation = Quaternion.identity;
            piece.transform.localScale = size;
            ApplyMaterial(piece, material);
            EnsureStructuralCollider(piece);
            return piece;
        }

        /// <summary>Creates Floor parent with FloorCore piece(s). Floor never reaches connector openings (connectorCutInset) or wall edges (floorInsetFromWalls).</summary>
        static GameObject CreateFloorParent(Transform root)
        {
            var floorParent = new GameObject("Floor");
            floorParent.transform.SetParent(root);
            floorParent.transform.localPosition = Vector3.zero;
            floorParent.transform.localRotation = Quaternion.identity;
            floorParent.transform.localScale = Vector3.one;
            floorParent.layer = 0;
            return floorParent;
        }

        static void CreateStartHub()
        {
            var root = new GameObject("MallSection");
            root.AddComponent<MallSection>();

            float size = 8f;
            float half = size * 0.5f;
            float radius = half - 0.5f;
            float cut = ConnectorCutInset;
            float coreHalfX = radius - cut;
            float coreHalfZ = radius - cut;
            var floorParent = CreateFloorParent(root.transform);
            CreateFloorCorePiece(floorParent.transform, "FloorCore", Vector3.zero,
                new Vector3(coreHalfX * 2f, FloorThickness, coreHalfZ * 2f), _floorLight);

            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(root.transform);
            ceiling.transform.localPosition = new Vector3(0f, CeilingHeight + CeilingThickness * 0.5f, 0f);
            ceiling.transform.localScale = new Vector3(size, CeilingThickness, size);
            ApplyMaterial(ceiling, _wallNeutral);
            Object.DestroyImmediate(ceiling.GetComponent<Collider>());

            var ms = root.GetComponent<MallSection>();
            if (ms == null) ms = root.AddComponent<MallSection>();
            SerializeMallSection(ms, isStart: true, connectorCount: 4, anomalyCount: 1, cctvCount: 1, hasPlayerSpawn: true);

            var connParent = new GameObject("ConnectorPoints");
            connParent.transform.SetParent(root.transform);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                var c = CreateMarker("ConnectorPoint");
                c.transform.SetParent(connParent.transform);
                c.transform.localPosition = new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
                c.transform.localRotation = Quaternion.Euler(0, i * 90f, 0);
            }

            var playerSpawn = CreateMarker("PlayerSpawn");
            playerSpawn.transform.SetParent(root.transform);
            playerSpawn.transform.localPosition = new Vector3(0f, 1f, 0f);

            var anomaly = CreateMarker("AnomalySpawnPoint");
            anomaly.transform.SetParent(root.transform);
            anomaly.transform.localPosition = new Vector3(1.5f, 1f, 1.5f);

            var cctv = CreateMarker("CctvPoint");
            cctv.transform.SetParent(root.transform);
            cctv.transform.localPosition = new Vector3(0f, CeilingHeight - 0.5f, 0f);
            cctv.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
            cctv.AddComponent<CctvCameraPoint>();

            AddPointLight(root, new Vector3(0f, CeilingHeight - 0.8f, 0f), 0.9f, 7f, warmTint: false);

            SavePrefab(root, "StartHub");
        }

        static void CreateHallStraight()
        {
            var root = new GameObject("MallSection");
            float halfW = HallWidth * 0.5f;
            float halfL = HallLength * 0.5f;

            float coreX = HallWidth - 2f * FloorInsetFromWalls;
            float coreZ = HallLength - 2f * ConnectorCutInset;
            var floorParent = CreateFloorParent(root.transform);
            CreateFloorCorePiece(floorParent.transform, "FloorCore", Vector3.zero,
                new Vector3(coreX, FloorThickness, coreZ), _floorLight);

            float wallX = halfW + WallThickness * 0.5f;
            var wallL = CreateWallCube(HallLength + WallThickness * 2f, CeilingHeight, WallThickness);
            wallL.name = "WallLeft";
            wallL.transform.SetParent(root.transform);
            wallL.transform.localPosition = new Vector3(-wallX, CeilingHeight * 0.5f, 0f);
            ApplyMaterial(wallL, _wallNeutral);

            var wallR = CreateWallCube(HallLength + WallThickness * 2f, CeilingHeight, WallThickness);
            wallR.name = "WallRight";
            wallR.transform.SetParent(root.transform);
            wallR.transform.localPosition = new Vector3(wallX, CeilingHeight * 0.5f, 0f);
            ApplyMaterial(wallR, _wallNeutral);

            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(root.transform);
            ceiling.transform.localPosition = new Vector3(0f, CeilingHeight + CeilingThickness * 0.5f, 0f);
            ceiling.transform.localScale = new Vector3(HallWidth + WallThickness * 2f, CeilingThickness, HallLength + WallThickness * 2f);
            ApplyMaterial(ceiling, _wallNeutral);
            Object.DestroyImmediate(ceiling.GetComponent<Collider>());

            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? (-wallX - TrimWallOffset) : (wallX + TrimWallOffset);
                var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trim.name = "Trim" + (i == 0 ? "Left" : "Right");
                trim.transform.SetParent(root.transform);
                trim.transform.localPosition = new Vector3(x, CeilingHeight - TrimHeight * 0.5f, 0f);
                trim.transform.localScale = new Vector3(TrimDepth, TrimHeight, HallLength);
                ApplyMaterial(trim, _accentColor);
                Object.DestroyImmediate(trim.GetComponent<Collider>());
            }

            var ms = root.AddComponent<MallSection>();
            SerializeMallSection(ms, isStart: false, connectorCount: 2, anomalyCount: 2, cctvCount: 1, hasPlayerSpawn: false);

            var c1 = CreateMarker("ConnectorPoint");
            c1.transform.SetParent(root.transform);
            c1.transform.localPosition = new Vector3(0f, 0f, -halfL);
            c1.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var c2 = CreateMarker("ConnectorPoint");
            c2.transform.SetParent(root.transform);
            c2.transform.localPosition = new Vector3(0f, 0f, halfL);
            c2.transform.localRotation = Quaternion.identity;

            var a1 = CreateMarker("AnomalySpawnPoint");
            a1.transform.SetParent(root.transform);
            a1.transform.localPosition = new Vector3(-1.2f, 1f, 0f);
            var a2 = CreateMarker("AnomalySpawnPoint");
            a2.transform.SetParent(root.transform);
            a2.transform.localPosition = new Vector3(1.2f, 1f, 2f);

            var cctv = CreateMarker("CctvPoint");
            cctv.transform.SetParent(root.transform);
            cctv.transform.localPosition = new Vector3(0f, CeilingHeight - 0.5f, 0f);
            cctv.AddComponent<CctvCameraPoint>();

            AddPointLight(root, new Vector3(0f, CeilingHeight - 0.8f, 0f), 0.7f, 6f, warmTint: false);

            SavePrefab(root, "HallStraight");
        }

        static void CreateHallCorner()
        {
            var root = new GameObject("MallSection");
            float cornerSize = 4.5f;
            float half = cornerSize * 0.5f;

            var floorParent = CreateFloorParent(root.transform);
            floorParent.transform.localPosition = new Vector3(half, 0f, half);
            float zLegSize = 4.2f;
            float xLegSize = 2.2f;
            CreateFloorCorePiece(floorParent.transform, "FloorCore_A",
                new Vector3(-1.1f, 0f, 0.1f), new Vector3(2.2f, FloorThickness, zLegSize), _floorLight);
            CreateFloorCorePiece(floorParent.transform, "FloorCore_B",
                new Vector3(1f, 0f, -1.1f), new Vector3(2f, FloorThickness, xLegSize), _floorLight);

            var wallBack = CreateWallCube(cornerSize + WallThickness * 2f, CeilingHeight, WallThickness);
            wallBack.name = "WallBack";
            wallBack.transform.SetParent(root.transform);
            wallBack.transform.localPosition = new Vector3(half, CeilingHeight * 0.5f, cornerSize + WallThickness * 0.5f);
            ApplyMaterial(wallBack, _wallNeutral);

            var wallSide = CreateWallCube(WallThickness, CeilingHeight, cornerSize + WallThickness * 2f);
            wallSide.name = "WallSide";
            wallSide.transform.SetParent(root.transform);
            wallSide.transform.localPosition = new Vector3(cornerSize + WallThickness * 0.5f, CeilingHeight * 0.5f, half);
            ApplyMaterial(wallSide, _wallNeutral);

            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(root.transform);
            ceiling.transform.localPosition = new Vector3(half, CeilingHeight + CeilingThickness * 0.5f, half);
            ceiling.transform.localScale = new Vector3(cornerSize + WallThickness * 2f, CeilingThickness, cornerSize + WallThickness * 2f);
            ApplyMaterial(ceiling, _wallNeutral);
            Object.DestroyImmediate(ceiling.GetComponent<Collider>());

            var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = "Trim";
            trim.transform.SetParent(root.transform);
            float trimOffset = TrimWallOffset * 0.707f;
            trim.transform.localPosition = new Vector3(half - trimOffset, CeilingHeight - TrimHeight * 0.5f, half - trimOffset);
            trim.transform.localScale = new Vector3(cornerSize, TrimHeight, TrimDepth);
            trim.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            ApplyMaterial(trim, _accentColor);
            Object.DestroyImmediate(trim.GetComponent<Collider>());

            var ms = root.AddComponent<MallSection>();
            SerializeMallSection(ms, isStart: false, connectorCount: 2, anomalyCount: 1, cctvCount: 1, hasPlayerSpawn: false);

            var c1 = CreateMarker("ConnectorPoint");
            c1.transform.SetParent(root.transform);
            c1.transform.localPosition = new Vector3(0f, 0f, 0f);
            c1.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var c2 = CreateMarker("ConnectorPoint");
            c2.transform.SetParent(root.transform);
            c2.transform.localPosition = new Vector3(cornerSize, 0f, cornerSize);
            c2.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);

            var a1 = CreateMarker("AnomalySpawnPoint");
            a1.transform.SetParent(root.transform);
            a1.transform.localPosition = new Vector3(half, 1f, half);

            var cctv = CreateMarker("CctvPoint");
            cctv.transform.SetParent(root.transform);
            cctv.transform.localPosition = new Vector3(half, CeilingHeight - 0.5f, half);
            cctv.AddComponent<CctvCameraPoint>();

            AddPointLight(root, new Vector3(half, CeilingHeight - 0.8f, half), 0.7f, 6f, warmTint: false);

            SavePrefab(root, "HallCorner");
        }

        static void CreateStoreRoom()
        {
            var root = new GameObject("MallSection");
            float half = StoreSize * 0.5f;
            float wallOuterX = half + WallThickness * 0.5f;

            float coreX = StoreSize - 2f * FloorInsetFromWalls;
            float coreZ = StoreSize - ConnectorCutInset - FloorInsetFromWalls;
            float centerZ = 0.1f;
            var floorParent = CreateFloorParent(root.transform);
            CreateFloorCorePiece(floorParent.transform, "FloorCore",
                new Vector3(0f, 0f, centerZ),
                new Vector3(coreX, FloorThickness, coreZ), _floorDark);

            var backWall = CreateWallCube(StoreSize + WallThickness * 2f, CeilingHeight, WallThickness);
            backWall.name = "WallBack";
            backWall.transform.SetParent(root.transform);
            backWall.transform.localPosition = new Vector3(0f, CeilingHeight * 0.5f, half + WallThickness * 0.5f);
            ApplyMaterial(backWall, _wallNeutral);

            var leftWall = CreateWallCube(WallThickness, CeilingHeight, StoreSize);
            leftWall.name = "WallLeft";
            leftWall.transform.SetParent(root.transform);
            leftWall.transform.localPosition = new Vector3(-wallOuterX, CeilingHeight * 0.5f, 0f);
            ApplyMaterial(leftWall, _wallNeutral);

            var rightWall = CreateWallCube(WallThickness, CeilingHeight, StoreSize);
            rightWall.name = "WallRight";
            rightWall.transform.SetParent(root.transform);
            rightWall.transform.localPosition = new Vector3(wallOuterX, CeilingHeight * 0.5f, 0f);
            ApplyMaterial(rightWall, _wallNeutral);

            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(root.transform);
            ceiling.transform.localPosition = new Vector3(0f, CeilingHeight + CeilingThickness * 0.5f, 0f);
            ceiling.transform.localScale = new Vector3(StoreSize + WallThickness * 2f, CeilingThickness, StoreSize + WallThickness * 2f);
            ApplyMaterial(ceiling, _wallNeutral);
            Object.DestroyImmediate(ceiling.GetComponent<Collider>());

            float trimY = CeilingHeight - TrimHeight * 0.5f;
            var trimBack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trimBack.name = "TrimBack";
            trimBack.transform.SetParent(root.transform);
            trimBack.transform.localPosition = new Vector3(0f, trimY, half - TrimWallOffset);
            trimBack.transform.localScale = new Vector3(StoreSize, TrimHeight, TrimDepth);
            ApplyMaterial(trimBack, _accentColor);
            Object.DestroyImmediate(trimBack.GetComponent<Collider>());

            var trimLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trimLeft.name = "TrimLeft";
            trimLeft.transform.SetParent(root.transform);
            trimLeft.transform.localPosition = new Vector3(-half + TrimWallOffset, trimY, 0f);
            trimLeft.transform.localScale = new Vector3(TrimDepth, TrimHeight, StoreSize);
            ApplyMaterial(trimLeft, _accentColor);
            Object.DestroyImmediate(trimLeft.GetComponent<Collider>());

            var trimRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trimRight.name = "TrimRight";
            trimRight.transform.SetParent(root.transform);
            trimRight.transform.localPosition = new Vector3(half - TrimWallOffset, trimY, 0f);
            trimRight.transform.localScale = new Vector3(TrimDepth, TrimHeight, StoreSize);
            ApplyMaterial(trimRight, _accentColor);
            Object.DestroyImmediate(trimRight.GetComponent<Collider>());

            var ms = root.AddComponent<MallSection>();
            SerializeMallSection(ms, isStart: false, connectorCount: 1, anomalyCount: 2, cctvCount: 1, hasPlayerSpawn: false);

            var c1 = CreateMarker("ConnectorPoint");
            c1.transform.SetParent(root.transform);
            c1.transform.localPosition = new Vector3(0f, 0f, -half);
            c1.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var a1 = CreateMarker("AnomalySpawnPoint");
            a1.transform.SetParent(root.transform);
            a1.transform.localPosition = new Vector3(-1.5f, 1f, 0f);
            var a2 = CreateMarker("AnomalySpawnPoint");
            a2.transform.SetParent(root.transform);
            a2.transform.localPosition = new Vector3(1.5f, 1f, 1f);

            var cctv = CreateMarker("CctvPoint");
            cctv.transform.SetParent(root.transform);
            cctv.transform.localPosition = new Vector3(0f, CeilingHeight - 0.5f, 0f);
            cctv.AddComponent<CctvCameraPoint>();

            AddPointLight(root, new Vector3(0f, CeilingHeight - 0.8f, 0f), 1.0f, 7f, warmTint: true);

            SavePrefab(root, "StoreRoom");
        }

        static void SerializeMallSection(MallSection ms, bool isStart, int connectorCount, int anomalyCount, int cctvCount, bool hasPlayerSpawn)
        {
            var so = new SerializedObject(ms);
            so.FindProperty("_isStart").boolValue = isStart;
            so.ApplyModifiedPropertiesWithoutUndo();
            ms.CollectMarkersIfNeeded();
        }

        static void SavePrefab(GameObject root, string name)
        {
            string path = $"{PrefabPath}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }
    }
}
