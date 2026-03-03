using System;
using System.Collections.Generic;
using UnityEngine;

namespace NightShift.Systems
{
    /// <summary>
    /// Holds CCTV cameras, tracks current index, manages suspicious flags.
    /// Event-driven; AnomalyManager/DispatchManager notify for suspicious cameras.
    /// </summary>
    public class CctvManager : MonoBehaviour
    {
        public static CctvManager Instance { get; private set; }

        private readonly List<CctvCameraPoint> _cameras = new List<CctvCameraPoint>();
        private int _currentIndex;
        private int _suspiciousCameraIndex = -1;
        private int _displayedSuspiciousIndex = -1; // may be wrong at high instability
        private float _suspiciousUntilTime;
        private bool _suspiciousIsReal; // true = real anomaly, false = false dispatch

        public event Action<int> OnCameraChanged;
        public event Action<int, bool, float> OnSuspiciousCameraSet;

        public IReadOnlyList<CctvCameraPoint> Cameras => _cameras;
        public int CurrentIndex => _currentIndex;
        public CctvCameraPoint CurrentCamera => _cameras.Count > 0 && _currentIndex >= 0 && _currentIndex < _cameras.Count
            ? _cameras[_currentIndex] : null;
        public bool IsSuspicious => _suspiciousCameraIndex >= 0 && Time.time < _suspiciousUntilTime;
        public int SuspiciousCameraIndex => _suspiciousCameraIndex;

        /// <summary>True if currently viewed camera is shown as suspicious (may be wrong at high instability).</summary>
        public bool IsCurrentCameraSuspicious => IsSuspicious && _displayedSuspiciousIndex == _currentIndex;

        /// <summary>Index shown to player as "suspicious" - may be wrong at high instability.</summary>
        public int DisplayedSuspiciousIndex => _displayedSuspiciousIndex;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            var found = FindObjectsByType<CctvCameraPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _cameras.Clear();
            foreach (var c in found)
            {
                if (c != null)
                    _cameras.Add(c);
            }

            if (_cameras.Count > 0)
                _currentIndex = 0;
            else
                _currentIndex = -1;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Register cameras manually if not in scene at Awake.</summary>
        public void RegisterCameras(IEnumerable<CctvCameraPoint> cameras)
        {
            _cameras.Clear();
            foreach (var c in cameras)
            {
                if (c != null && !_cameras.Contains(c))
                    _cameras.Add(c);
            }
            if (_cameras.Count > 0 && _currentIndex < 0)
                _currentIndex = 0;
        }

        public void CycleNext()
        {
            if (_cameras.Count == 0) return;
            _currentIndex = (_currentIndex + 1) % _cameras.Count;
            OnCameraChanged?.Invoke(_currentIndex);
        }

        public void CyclePrevious()
        {
            if (_cameras.Count == 0) return;
            _currentIndex = _currentIndex <= 0 ? _cameras.Count - 1 : _currentIndex - 1;
            OnCameraChanged?.Invoke(_currentIndex);
        }

        public void SetCurrentIndex(int index)
        {
            if (_cameras.Count == 0) return;
            _currentIndex = Mathf.Clamp(index, 0, _cameras.Count - 1);
            OnCameraChanged?.Invoke(_currentIndex);
        }

        /// <summary>Mark a camera suspicious. durationSeconds = how long the flag lasts.</summary>
        public void MarkSuspicious(int cameraIndex, bool isReal, float durationSeconds = 20f)
        {
            if (cameraIndex < 0 || cameraIndex >= _cameras.Count) return;
            _suspiciousCameraIndex = cameraIndex;
            float inst = InstabilityManager.Instance != null ? InstabilityManager.Instance.Instability : 0f;
            _displayedSuspiciousIndex = (inst >= 60f && UnityEngine.Random.value < 0.2f && _cameras.Count > 1)
                ? GetWrongCameraIndex(cameraIndex)
                : cameraIndex;
            _suspiciousUntilTime = Time.time + durationSeconds;
            _suspiciousIsReal = isReal;
            OnSuspiciousCameraSet?.Invoke(cameraIndex, isReal, durationSeconds);
        }

        private int GetWrongCameraIndex(int exclude)
        {
            int wrong = UnityEngine.Random.Range(0, _cameras.Count);
            return wrong == exclude ? (exclude + 1) % _cameras.Count : wrong;
        }

        /// <summary>Mark a random camera suspicious. Used by AnomalyManager (real) and DispatchManager (false).</summary>
        public void MarkRandomSuspicious(bool isReal, float durationSeconds = 20f)
        {
            if (_cameras.Count == 0) return;
            int idx = UnityEngine.Random.Range(0, _cameras.Count);
            MarkSuspicious(idx, isReal, durationSeconds);
        }

        /// <summary>Debug: mark a random camera suspicious (real).</summary>
        public void DebugMarkSuspiciousReal() => MarkRandomSuspicious(true, 20f);

        /// <summary>Debug: mark a random camera suspicious (false).</summary>
        public void DebugMarkSuspiciousFalse() => MarkRandomSuspicious(false, 20f);
    }
}
