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

        [MenuItem("Night Shift/Create Mall Section Prefabs")]
        public static void Create()
        {
            EnsureFolderExists("Assets/_Project", "Art", "Prefabs", "MallSections");

            CreateStartHub();
            CreateHallStraight();
            CreateHallCorner();
            CreateStoreRoom();
            CreateOrUpdateMallGeneratorConfig();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[NightShift] Created mall section prefabs in " + PrefabPath);
        }

        private static void CreateOrUpdateMallGeneratorConfig()
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

        private static void EnsureFolderExists(string parent, string folder)
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

        private static void EnsureFolderExists(string a, string b, string c, string d)
        {
            if (!AssetDatabase.IsValidFolder(a)) AssetDatabase.CreateFolder(a.Substring(0, a.LastIndexOf('/')), a.Substring(a.LastIndexOf('/') + 1));
            string ab = a + "/" + b;
            if (!AssetDatabase.IsValidFolder(ab)) AssetDatabase.CreateFolder(a, b);
            string abc = ab + "/" + c;
            if (!AssetDatabase.IsValidFolder(abc)) AssetDatabase.CreateFolder(ab, c);
            string abcd = abc + "/" + d;
            if (!AssetDatabase.IsValidFolder(abcd)) AssetDatabase.CreateFolder(abc, d);
        }

        private static GameObject CreateMarker(string name)
        {
            var go = new GameObject(name);
            return go;
        }

        private static void CreateStartHub()
        {
            var root = new GameObject("MallSection");
            root.AddComponent<MallSection>();

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(root.transform);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localScale = new Vector3(10f, 0.2f, 10f);

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
                c.transform.localPosition = new Vector3(Mathf.Sin(angle) * 5f, 0f, Mathf.Cos(angle) * 5f);
                c.transform.localRotation = Quaternion.Euler(0, i * 90f, 0);
            }

            var playerSpawn = CreateMarker("PlayerSpawn");
            playerSpawn.transform.SetParent(root.transform);
            playerSpawn.transform.localPosition = new Vector3(0f, 1f, 0f);

            var anomaly = CreateMarker("AnomalySpawnPoint");
            anomaly.transform.SetParent(root.transform);
            anomaly.transform.localPosition = new Vector3(2f, 1f, 2f);

            var cctv = CreateMarker("CctvPoint");
            cctv.transform.SetParent(root.transform);
            cctv.transform.localPosition = new Vector3(0f, 4f, 0f);
            cctv.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
            cctv.AddComponent<CctvCameraPoint>();

            SavePrefab(root, "StartHub");
        }

        private static void CreateHallStraight()
        {
            var root = new GameObject("MallSection");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(root.transform);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localScale = new Vector3(6f, 0.2f, 12f);

            var ms = root.AddComponent<MallSection>();
            SerializeMallSection(ms, isStart: false, connectorCount: 2, anomalyCount: 2, cctvCount: 1, hasPlayerSpawn: false);

            var c1 = CreateMarker("ConnectorPoint");
            c1.transform.SetParent(root.transform);
            c1.transform.localPosition = new Vector3(0f, 0f, -6f);
            c1.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var c2 = CreateMarker("ConnectorPoint");
            c2.transform.SetParent(root.transform);
            c2.transform.localPosition = new Vector3(0f, 0f, 6f);
            c2.transform.localRotation = Quaternion.identity;

            var a1 = CreateMarker("AnomalySpawnPoint");
            a1.transform.SetParent(root.transform);
            a1.transform.localPosition = new Vector3(-1.5f, 1f, 0f);
            var a2 = CreateMarker("AnomalySpawnPoint");
            a2.transform.SetParent(root.transform);
            a2.transform.localPosition = new Vector3(1.5f, 1f, 2f);

            var cctv = CreateMarker("CctvPoint");
            cctv.transform.SetParent(root.transform);
            cctv.transform.localPosition = new Vector3(0f, 3f, 0f);
            cctv.AddComponent<CctvCameraPoint>();

            SavePrefab(root, "HallStraight");
        }

        private static void CreateHallCorner()
        {
            var root = new GameObject("MallSection");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(root.transform);
            floor.transform.localPosition = new Vector3(4f, 0f, 4f);
            floor.transform.localScale = new Vector3(8f, 0.2f, 8f);

            var ms = root.AddComponent<MallSection>();
            SerializeMallSection(ms, isStart: false, connectorCount: 2, anomalyCount: 1, cctvCount: 1, hasPlayerSpawn: false);

            var c1 = CreateMarker("ConnectorPoint");
            c1.transform.SetParent(root.transform);
            c1.transform.localPosition = new Vector3(0f, 0f, 0f);
            c1.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var c2 = CreateMarker("ConnectorPoint");
            c2.transform.SetParent(root.transform);
            c2.transform.localPosition = new Vector3(8f, 0f, 8f);
            c2.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);

            var a1 = CreateMarker("AnomalySpawnPoint");
            a1.transform.SetParent(root.transform);
            a1.transform.localPosition = new Vector3(4f, 1f, 4f);

            var cctv = CreateMarker("CctvPoint");
            cctv.transform.SetParent(root.transform);
            cctv.transform.localPosition = new Vector3(4f, 3f, 4f);
            cctv.AddComponent<CctvCameraPoint>();

            SavePrefab(root, "HallCorner");
        }

        private static void CreateStoreRoom()
        {
            var root = new GameObject("MallSection");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(root.transform);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localScale = new Vector3(8f, 0.2f, 8f);

            var walls = GameObject.CreatePrimitive(PrimitiveType.Cube);
            walls.name = "Walls";
            walls.transform.SetParent(root.transform);
            walls.transform.localPosition = new Vector3(0f, 2f, 0f);
            walls.transform.localScale = new Vector3(8f, 4f, 8f);
            Object.DestroyImmediate(walls.GetComponent<Collider>());

            var ms = root.AddComponent<MallSection>();
            SerializeMallSection(ms, isStart: false, connectorCount: 1, anomalyCount: 2, cctvCount: 1, hasPlayerSpawn: false);

            var c1 = CreateMarker("ConnectorPoint");
            c1.transform.SetParent(root.transform);
            c1.transform.localPosition = new Vector3(0f, 0f, -4f);
            c1.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var a1 = CreateMarker("AnomalySpawnPoint");
            a1.transform.SetParent(root.transform);
            a1.transform.localPosition = new Vector3(-2f, 1f, 0f);
            var a2 = CreateMarker("AnomalySpawnPoint");
            a2.transform.SetParent(root.transform);
            a2.transform.localPosition = new Vector3(2f, 1f, 1f);

            var cctv = CreateMarker("CctvPoint");
            cctv.transform.SetParent(root.transform);
            cctv.transform.localPosition = new Vector3(0f, 3.5f, 0f);
            cctv.AddComponent<CctvCameraPoint>();

            SavePrefab(root, "StoreRoom");
        }

        private static void SerializeMallSection(MallSection ms, bool isStart, int connectorCount, int anomalyCount, int cctvCount, bool hasPlayerSpawn)
        {
            var so = new SerializedObject(ms);
            so.FindProperty("_isStart").boolValue = isStart;
            so.ApplyModifiedPropertiesWithoutUndo();
            ms.CollectMarkersIfNeeded();
        }

        private static void SavePrefab(GameObject root, string name)
        {
            string path = $"{PrefabPath}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }
    }
}
