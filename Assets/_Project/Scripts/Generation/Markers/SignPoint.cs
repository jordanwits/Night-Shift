using UnityEngine;

namespace NightShift.Generation
{
    /// <summary>Marker for store sign spawn points in mall sections.</summary>
    public class SignPoint : MonoBehaviour
    {
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, 0.22f);
        }
#endif
    }
}
