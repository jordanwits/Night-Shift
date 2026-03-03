using UnityEngine;

namespace NightShift.Generation
{
    /// <summary>Marker for landmark prop spawn points in mall sections.</summary>
    public class LandmarkPoint : MonoBehaviour
    {
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(transform.position, 0.25f);
        }
#endif
    }
}
