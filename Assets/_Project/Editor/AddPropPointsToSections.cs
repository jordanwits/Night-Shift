using UnityEngine;
using UnityEditor;
using NightShift.Generation;

namespace NightShift.Editor
{
    /// <summary>
    /// Adds PropPoint and LandmarkPoint markers to mall section prefabs. Menu: Night Shift > Add Prop Points to Sections.
    /// </summary>
    public static class AddPropPointsToSections
    {
        private const string PrefabPath = "Assets/_Project/Art/Prefabs/MallSections";
        private static readonly string[] SectionNames = { "StartHub", "HallStraight", "HallCorner", "StoreRoom" };

        [MenuItem("Night Shift/Mall Dressing/Add Prop Points to Sections")]
        public static void Execute()
        {
            foreach (var name in SectionNames)
            {
                string path = $"{PrefabPath}/{name}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                ModifyPrefab(path, name);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[NightShift] Added PropPoints and LandmarkPoints to mall section prefabs.");
        }

        private static bool HasComponentInChildren<T>(Transform root) where T : Component
        {
            return root.GetComponentsInChildren<T>(true).Length > 0;
        }

        private static int CountComponentsInChildren<T>(Transform root) where T : Component
        {
            return root.GetComponentsInChildren<T>(true).Length;
        }

        private static bool EnsureMarkerComponents(Transform root)
        {
            bool changed = false;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == root) continue;
                string n = t.name;
                if ((n == "PropPoint" || n.StartsWith("PropPoint_")) && t.GetComponent<PropPoint>() == null)
                {
                    t.gameObject.AddComponent<PropPoint>();
                    changed = true;
                }
                else if ((n == "LandmarkPoint" || n.StartsWith("LandmarkPoint_")) && t.GetComponent<LandmarkPoint>() == null)
                {
                    t.gameObject.AddComponent<LandmarkPoint>();
                    changed = true;
                }
                else if ((n == "SignPoint" || n.StartsWith("SignPoint_")) && t.GetComponent<SignPoint>() == null)
                {
                    t.gameObject.AddComponent<SignPoint>();
                    changed = true;
                }
                else if ((n == "ArrowSignPoint" || n.StartsWith("ArrowSignPoint_")) && t.GetComponent<ArrowSignPoint>() == null)
                {
                    t.gameObject.AddComponent<ArrowSignPoint>();
                    changed = true;
                }
            }
            return changed;
        }

        private static void ModifyPrefab(string path, string sectionName)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) return;

            var ms = root.GetComponent<MallSection>();
            if (ms == null) ms = root.AddComponent<MallSection>();

            bool changed = EnsureMarkerComponents(root.transform);

            if (!HasComponentInChildren<PropPoint>(root.transform))
            {
                switch (sectionName)
                {
                    case "StartHub":
                        AddPropPoint(root, -1.5f, 0.2f, 1.5f);
                        AddPropPoint(root, 1.5f, 0.2f, -1f);
                        AddPropPoint(root, 0f, 0.2f, 2.5f);
                        break;
                    case "HallStraight":
                        AddPropPoint(root, -1.2f, 0.2f, 0f);
                        AddPropPoint(root, 1.2f, 0.2f, 2f);
                        AddPropPoint(root, 0f, 0.2f, -2f);
                        break;
                    case "HallCorner":
                        AddPropPoint(root, 2.2f, 0.2f, 2.2f);
                        AddPropPoint(root, 1.5f, 0.2f, 1.5f);
                        break;
                    case "StoreRoom":
                        AddPropPoint(root, -1.8f, 0.2f, 1f);
                        AddPropPoint(root, 1.8f, 0.2f, -1f);
                        AddPropPoint(root, -1f, 0.2f, -1.5f);
                        AddPropPoint(root, 1.2f, 0.2f, 1.5f);
                        break;
                }
                changed = true;
            }

            if (!HasComponentInChildren<LandmarkPoint>(root.transform))
            {
                if (sectionName == "StartHub")
                {
                    AddLandmarkPoint(root, -2f, 0.2f, 2f);
                    changed = true;
                }
                else if (sectionName == "HallCorner")
                {
                    AddLandmarkPoint(root, 2f, 0.2f, 2f);
                    changed = true;
                }
            }

            if (!HasComponentInChildren<SignPoint>(root.transform))
            {
                if (sectionName == "StoreRoom")
                {
                    AddSignPoint(root, 0f, 2.5f, -3.2f, 0f, 180f, 0f);
                    changed = true;
                }
            }

            int arrowCount = CountComponentsInChildren<ArrowSignPoint>(root.transform);
            if (sectionName == "StartHub" && arrowCount < 4)
            {
                for (int a = arrowCount; a < 4; a++)
                {
                    if (a == 2) AddArrowSignPoint(root, -2f, 2f, 2f, 0f, 0f, 0f);
                    else if (a == 3) AddArrowSignPoint(root, 2f, 2f, 2f, 0f, 180f, 0f);
                }
                changed = true;
            }
            else if ((sectionName == "HallStraight" || sectionName == "HallCorner") && arrowCount < 1)
            {
                if (sectionName == "HallStraight")
                    AddArrowSignPoint(root, 0f, 2f, 5f, 0f, 0f, 0f);
                else
                    AddArrowSignPoint(root, 2.5f, 2f, 2.5f, 0f, -45f, 0f);
                changed = true;
            }

            if (changed)
            {
                ms.CollectMarkersIfNeeded();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }

            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void AddPropPoint(GameObject root, float x, float y, float z)
        {
            var go = new GameObject("PropPoint");
            go.AddComponent<PropPoint>();
            go.transform.SetParent(root.transform);
            go.transform.localPosition = new Vector3(x, y, z);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
        }

        private static void AddLandmarkPoint(GameObject root, float x, float y, float z)
        {
            var go = new GameObject("LandmarkPoint");
            go.AddComponent<LandmarkPoint>();
            go.transform.SetParent(root.transform);
            go.transform.localPosition = new Vector3(x, y, z);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
        }

        private static void AddSignPoint(GameObject root, float x, float y, float z, float rx, float ry, float rz)
        {
            var go = new GameObject("SignPoint");
            go.AddComponent<SignPoint>();
            go.transform.SetParent(root.transform);
            go.transform.localPosition = new Vector3(x, y, z);
            go.transform.localRotation = Quaternion.Euler(rx, ry, rz);
            go.transform.localScale = Vector3.one;
        }

        private static void AddArrowSignPoint(GameObject root, float x, float y, float z, float rx, float ry, float rz)
        {
            var go = new GameObject("ArrowSignPoint");
            go.AddComponent<ArrowSignPoint>();
            go.transform.SetParent(root.transform);
            go.transform.localPosition = new Vector3(x, y, z);
            go.transform.localRotation = Quaternion.Euler(rx, ry, rz);
            go.transform.localScale = Vector3.one;
        }
    }
}
