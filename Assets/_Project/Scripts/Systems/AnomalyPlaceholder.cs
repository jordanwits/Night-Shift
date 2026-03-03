using UnityEngine;
using NightShift.Core;

namespace NightShift.Systems
{
    /// <summary>
    /// Creates a simple placeholder anomaly when no prefab is assigned.
    /// </summary>
    public static class AnomalyPlaceholder
    {
        public static GameObject Create(AnomalyDefinition definition)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = definition != null ? (string.IsNullOrEmpty(definition.id) ? "Anomaly" : definition.id) : "Anomaly";
            go.transform.localScale = Vector3.one * 2f;

            var instance = go.AddComponent<AnomalyInstance>();
            instance.Definition = definition;

            return go;
        }
    }
}
