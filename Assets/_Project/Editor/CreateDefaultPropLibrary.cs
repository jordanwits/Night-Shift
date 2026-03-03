using UnityEngine;
using UnityEditor;
using NightShift.Generation;

namespace NightShift.Editor
{
    /// <summary>
    /// Creates default PropLibrary with primitive prop entries. Menu: Night Shift > Create Default Prop Library.
    /// Auto-creates on first domain reload if missing.
    /// </summary>
    [InitializeOnLoad]
    public static class CreateDefaultPropLibrary
    {
        private const string Folder = "Assets/_Project/Resources/ScriptableObjects/Systems";
        private const string AssetPath = Folder + "/PropLibrary.asset";

        static CreateDefaultPropLibrary()
        {
            EditorApplication.delayCall += EnsurePropLibraryExists;
        }

        private static void EnsurePropLibraryExists()
        {
            if (AssetDatabase.LoadAssetAtPath<PropLibrary>(AssetPath) != null)
                return;
            if (EditorApplication.isPlaying)
                return;
            Create();
        }

        [MenuItem("Night Shift/Mall Dressing/Create Prop Library")]
        public static void Create()
        {
            EnsureFolderExists("Assets/_Project", "Resources");
            EnsureFolderExists("Assets/_Project/Resources", "ScriptableObjects");
            EnsureFolderExists("Assets/_Project/Resources/ScriptableObjects", "Systems");

            var existing = AssetDatabase.LoadAssetAtPath<PropLibrary>(AssetPath);
            PropLibrary lib;
            if (existing != null)
            {
                lib = existing;
                lib.props.Clear();
            }
            else
            {
                lib = ScriptableObject.CreateInstance<PropLibrary>();
                AssetDatabase.CreateAsset(lib, AssetPath);
            }

            AddProp(lib, "bench", 1.5f, PropPrimitiveType.Bench, true, true, preferHall: true);
            AddProp(lib, "trashcan", 1.2f, PropPrimitiveType.TrashCan, true, true, preferHall: true);
            AddProp(lib, "plant", 1f, PropPrimitiveType.PottedPlant, true, false, preferHall: true);
            AddProp(lib, "wetsign", 0.8f, PropPrimitiveType.WetFloorSign, true, false, preferHall: true);
            AddProp(lib, "kiosk", 0.5f, PropPrimitiveType.Kiosk, true, true, preferStore: true);
            AddProp(lib, "boxstack", 1.3f, PropPrimitiveType.BoxStack, true, false, preferStore: true);
            AddProp(lib, "chair", 1.1f, PropPrimitiveType.Chair, true, false, preferStore: true);
            AddProp(lib, "cone", 0.7f, PropPrimitiveType.Cone, true, false, preferHall: true);
            AddProp(lib, "soda", 0.4f, PropPrimitiveType.SodaMachine, true, false, preferStore: true);

            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            AddPropPointsToSections.Execute();
            AssetDatabase.Refresh();
            Debug.Log("[NightShift] Created PropLibrary at " + AssetPath);
        }

        private static void EnsureFolderExists(string parent, string folder)
        {
            string full = parent + "/" + folder;
            if (AssetDatabase.IsValidFolder(full)) return;
            if (!AssetDatabase.IsValidFolder(parent))
            {
                int i = parent.LastIndexOf('/');
                if (i > 0) EnsureFolderExists(parent.Substring(0, i), parent.Substring(i + 1));
            }
            AssetDatabase.CreateFolder(parent, folder);
        }

        private static void AddProp(PropLibrary lib, string id, float weight, PropPrimitiveType type, bool usePrim, bool isLandmark, bool preferHall = false, bool preferStore = false)
        {
            lib.props.Add(new PropEntry
            {
                id = id,
                weight = weight,
                usePrimitive = usePrim,
                primitiveType = type,
                alignToSurfaceNormal = true,
                isPhysics = false,
                isLandmark = isLandmark,
                preferHall = preferHall,
                preferStore = preferStore,
                localScaleMin = Vector3.one * 0.8f,
                localScaleMax = Vector3.one * 1.2f
            });
        }
    }
}
