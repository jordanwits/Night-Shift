using UnityEngine;

namespace NightShift.Systems
{
    /// <summary>
    /// A CCTV camera position in the mall. View is from viewTransform (or this transform).
    /// </summary>
    public class CctvCameraPoint : MonoBehaviour
    {
        [SerializeField] private string _cameraName = "";
        [SerializeField] private Transform _viewTransform;

        public string CameraName => string.IsNullOrEmpty(_cameraName) ? name : _cameraName;
        public Transform ViewTransform => _viewTransform != null ? _viewTransform : transform;

        private void Awake()
        {
            if (string.IsNullOrEmpty(_cameraName))
                _cameraName = name;
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_cameraName))
                _cameraName = name;
        }
    }
}
