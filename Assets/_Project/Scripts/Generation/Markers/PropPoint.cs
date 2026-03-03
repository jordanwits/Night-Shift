using UnityEngine;

namespace NightShift.Generation
{
    /// <summary>Marker for prop spawn points in mall sections.</summary>
    public class PropPoint : MonoBehaviour
    {
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.2f);
        }
#endif
    }
}
