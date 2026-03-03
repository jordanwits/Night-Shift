using UnityEngine;

namespace NightShift.Generation
{
    /// <summary>Marker for directional arrow sign spawn points in mall sections.</summary>
    public class ArrowSignPoint : MonoBehaviour
    {
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.22f);
        }
#endif
    }
}
