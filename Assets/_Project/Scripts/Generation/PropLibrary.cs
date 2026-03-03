using System.Collections.Generic;
using UnityEngine;

namespace NightShift.Generation
{
    /// <summary>
    /// Primitive types that can be built from Unity primitives at runtime.
    /// No custom assets required.
    /// </summary>
    public enum PropPrimitiveType
    {
        Bench,
        TrashCan,
        PottedPlant,
        WetFloorSign,
        Kiosk,
        BoxStack,
        Chair,
        Cone,
        SodaMachine
    }

    [System.Serializable]
    public class PropEntry
    {
        public string id;
        public float weight = 1f;
        public GameObject prefab;
        public PropPrimitiveType primitiveType;
        public bool usePrimitive = true;
        public Vector3 localScaleMin = Vector3.one;
        public Vector3 localScaleMax = Vector3.one;
        public bool alignToSurfaceNormal = true;
        public bool isPhysics = false;
        public bool isLandmark;
        public bool preferHall;
        public bool preferStore;
    }

    /// <summary>
    /// ScriptableObject library of props for mall dressing.
    /// Supports prefabs or primitive recipes built from cubes/cylinders.
    /// Place in Resources or assign to MallDresser.
    /// </summary>
    [CreateAssetMenu(fileName = "PropLibrary", menuName = "Night Shift/Prop Library")]
    public class PropLibrary : ScriptableObject
    {
        public List<PropEntry> props = new List<PropEntry>();

        /// <summary>Get total weight of all props (or landmark-only) for weighted random.</summary>
        public float GetTotalWeight(bool landmarksOnly)
        {
            float t = 0f;
            foreach (var p in props)
            {
                if (p != null && p.weight > 0f && p.isLandmark == landmarksOnly)
                    t += p.weight;
            }
            return t;
        }

        /// <summary>Pick a random prop by weight.</summary>
        public PropEntry PickRandom(System.Random rng, bool landmarksOnly)
        {
            float total = GetTotalWeight(landmarksOnly);
            if (total <= 0f) return null;

            float r = (float)(rng.NextDouble() * total);
            foreach (var p in props)
            {
                if (p == null || p.weight <= 0f || p.isLandmark != landmarksOnly) continue;
                r -= p.weight;
                if (r <= 0f) return p;
            }
            return props.Count > 0 ? props[0] : null;
        }

        /// <summary>Get total weight for section-type filtered props.</summary>
        public float GetTotalWeightForSection(bool landmarksOnly, bool isStoreRoom)
        {
            float t = 0f;
            foreach (var p in props)
            {
                if (p == null || p.weight <= 0f || p.isLandmark != landmarksOnly) continue;
                if (isStoreRoom && p.preferHall && !p.preferStore) continue;
                if (!isStoreRoom && p.preferStore && !p.preferHall) continue;
                t += p.weight;
            }
            return t;
        }

        /// <summary>Pick prop by weight, preferring Hall props (Bench, TrashCan, Plant, Cone) or Store props (BoxStack, Chair, Kiosk, SodaMachine).</summary>
        public PropEntry PickRandomForSection(System.Random rng, bool landmarksOnly, bool isStoreRoom)
        {
            float total = GetTotalWeightForSection(landmarksOnly, isStoreRoom);
            if (total <= 0f) return PickRandom(rng, landmarksOnly);

            float r = (float)(rng.NextDouble() * total);
            foreach (var p in props)
            {
                if (p == null || p.weight <= 0f || p.isLandmark != landmarksOnly) continue;
                if (isStoreRoom && p.preferHall && !p.preferStore) continue;
                if (!isStoreRoom && p.preferStore && !p.preferHall) continue;
                r -= p.weight;
                if (r <= 0f) return p;
            }
            return PickRandom(rng, landmarksOnly);
        }
    }
}
