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

        private static bool HasChildNamed(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t != root && t.name == name) return true;
            }
            return false;
        }

        private static void ModifyPrefab(string path, string sectionName)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) return;

            var ms = root.GetComponent<MallSection>();
            if (ms == null) ms = root.AddComponent<MallSection>();

            bool changed = false;

            if (!HasChildNamed(root.transform, "PropPoint"))
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

            if (!HasChildNamed(root.transform, "LandmarkPoint"))
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

            if (!HasChildNamed(root.transform, "SignPoint"))
            {
                if (sectionName == "StoreRoom")
                {
                    AddSignPoint(root, 0f, 2.5f, -3.2f, 0f, 180f, 0f);
                    changed = true;
                }
            }

            if (!HasChildNamed(root.transform, "ArrowSignPoint"))
            {
                if (sectionName == "StartHub")
                {
                    AddArrowSignPoint(root, -3.5f, 2f, 0f, 0f, 0f, 0f);
                    AddArrowSignPoint(root, 3.5f, 2f, 0f, 0f, 180f, 0f);
                    changed = true;
                }
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
            go.transform.SetParent(root.transform);
            go.transform.localPosition = new Vector3(x, y, z);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
        }

        private static void AddLandmarkPoint(GameObject root, float x, float y, float z)
        {
            var go = new GameObject("LandmarkPoint");
            go.transform.SetParent(root.transform);
            go.transform.localPosition = new Vector3(x, y, z);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
        }

        private static void AddSignPoint(GameObject root, float x, float y, float z, float rx, float ry, float rz)
        {
            var go = new GameObject("SignPoint");
            go.transform.SetParent(root.transform);
            go.transform.localPosition = new Vector3(x, y, z);
            go.transform.localRotation = Quaternion.Euler(rx, ry, rz);
            go.transform.localScale = Vector3.one;
        }

        private static void AddArrowSignPoint(GameObject root, float x, float y, float z, float rx, float ry, float rz)
        {
            var go = new GameObject("ArrowSignPoint");
            go.transform.SetParent(root.transform);
            go.transform.localPosition = new Vector3(x, y, z);
            go.transform.localRotation = Quaternion.Euler(rx, ry, rz);
            go.transform.localScale = Vector3.one;
        }
    }
}
