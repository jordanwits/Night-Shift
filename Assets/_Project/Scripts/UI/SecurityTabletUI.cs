using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using NightShift.Systems;

namespace NightShift.UI
{
    /// <summary>
    /// Security Tablet UI: TAB toggle, camera feed, Q/E cycle, suspicious indicator, instability distortion.
    /// </summary>
    public class SecurityTabletUI : MonoBehaviour
    {
        public static SecurityTabletUI Instance { get; private set; }

        private const int RenderWidth = 512;
        private const int RenderHeight = 384;
        private const float StaticOverlayDuration = 0.5f;
        private const float AutoSwitchMinInterval = 10f;
        private const float AutoSwitchMaxInterval = 20f;

        private Canvas _canvas;
        private GameObject _panel;
        private RawImage _feedImage;
        private Button _prevButton;
        private Button _nextButton;
        private Text _cameraNameText;
        private Text _suspiciousText;
        private GameObject _staticOverlay;
        private Image _staticOverlayImage;
        private Camera _cctvRenderCam;
        private RenderTexture _renderTexture;
        private bool _isOpen;
        private float _nextStaticTime;
        private float _nextAutoSwitchTime;
        private bool _staticActive;

        private void Awake()
        {
            Instance = this;
            CreateRenderCamera();
            CreateUI();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnEnable()
        {
            if (CctvManager.Instance != null)
                CctvManager.Instance.OnCameraChanged += OnCameraChanged;
        }

        private void OnDisable()
        {
            if (CctvManager.Instance != null)
                CctvManager.Instance.OnCameraChanged -= OnCameraChanged;
        }

        private void Start()
        {
            SyncCameraTransform();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.tabKey.wasPressedThisFrame && !EndScreenController.IsEndScreenVisible)
            {
                _isOpen = !_isOpen;
                SetPanelVisible(_isOpen);
                if (_cctvRenderCam != null)
                    _cctvRenderCam.enabled = _isOpen;
                if (_isOpen)
                {
                    EnsureCctvManagerHasCameras();
                    SyncCameraTransform();
                }
            }

            if (_isOpen)
            {
                float scroll = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
                bool prev = scroll < -0.1f || kb.qKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame
                    || kb[Key.LeftBracket].wasPressedThisFrame;
                bool next = scroll > 0.1f || kb.eKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame
                    || kb[Key.RightBracket].wasPressedThisFrame;
                if (prev)
                    CctvManager.Instance?.CyclePrevious();
                else if (next)
                    CctvManager.Instance?.CycleNext();

                UpdateDistortion();
                UpdateSuspiciousIndicator();
                UpdateCameraName();
            }
        }

        private void EnsureCctvManagerHasCameras()
        {
            if (CctvManager.Instance == null) return;
            if (CctvManager.Instance.Cameras.Count > 0) return;
            var points = FindObjectsByType<CctvCameraPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (points != null && points.Length > 0)
                CctvManager.Instance.RegisterCameras(points);
        }

        private void OnPrevClicked()
        {
            EnsureCctvManagerHasCameras();
            if (CctvManager.Instance != null && CctvManager.Instance.Cameras.Count > 0)
            {
                CctvManager.Instance.CyclePrevious();
                SyncCameraTransform();
                UpdateCameraName();
                UpdateSuspiciousIndicator();
            }
        }

        private void OnNextClicked()
        {
            EnsureCctvManagerHasCameras();
            if (CctvManager.Instance != null && CctvManager.Instance.Cameras.Count > 0)
            {
                CctvManager.Instance.CycleNext();
                SyncCameraTransform();
                UpdateCameraName();
                UpdateSuspiciousIndicator();
            }
        }

        private void LateUpdate()
        {
            if (_isOpen && _cctvRenderCam != null && _cctvRenderCam.enabled)
                SyncCameraTransform();
        }

        private void CreateRenderCamera()
        {
            _renderTexture = new RenderTexture(RenderWidth, RenderHeight, 24);
            _renderTexture.name = "CCTV_RT";
            _renderTexture.Create();

            var camGo = new GameObject("CCTVRenderCamera");
            _cctvRenderCam = camGo.AddComponent<Camera>();
            _cctvRenderCam.CopyFrom(Camera.main);
            _cctvRenderCam.targetTexture = _renderTexture;
            _cctvRenderCam.enabled = false;
            _cctvRenderCam.depth = -10;
            _cctvRenderCam.clearFlags = CameraClearFlags.SolidColor;
            _cctvRenderCam.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);
            camGo.SetActive(true);
        }

        private void CreateUI()
        {
            EnsureEventSystem();

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("SecurityTabletCanvas");
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 1000;

            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            _panel = new GameObject("SecurityTablet");
            _panel.transform.SetParent(canvasGo.transform, false);

            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(540, 420);

            var panelImage = _panel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.12f, 0.98f);

            var nameGo = new GameObject("CameraName");
            nameGo.transform.SetParent(_panel.transform, false);
            var nameRect = nameGo.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 1);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.pivot = new Vector2(0.5f, 1);
            nameRect.anchoredPosition = new Vector2(0, -8);
            nameRect.sizeDelta = new Vector2(-20, 28);
            _cameraNameText = nameGo.AddComponent<Text>();
            _cameraNameText.font = font;
            _cameraNameText.fontSize = 18;
            _cameraNameText.color = Color.white;
            _cameraNameText.alignment = TextAnchor.UpperCenter;
            _cameraNameText.text = "CAM-01";

            var feedGo = new GameObject("Feed");
            feedGo.transform.SetParent(_panel.transform, false);
            var feedRect = feedGo.AddComponent<RectTransform>();
            feedRect.anchorMin = new Vector2(0.5f, 0.5f);
            feedRect.anchorMax = new Vector2(0.5f, 0.5f);
            feedRect.pivot = new Vector2(0.5f, 0.5f);
            feedRect.anchoredPosition = new Vector2(0, -10);
            feedRect.sizeDelta = new Vector2(500, 360);
            _feedImage = feedGo.AddComponent<RawImage>();
            _feedImage.texture = _renderTexture;
            _feedImage.color = Color.white;

            var prevBtn = CreateTabletButton("<\nPrev", new Vector2(-180, -10));
            prevBtn.transform.SetParent(_panel.transform, false);
            _prevButton = prevBtn.GetComponent<Button>();
            _prevButton.onClick.AddListener(OnPrevClicked);

            var nextBtn = CreateTabletButton("Next\n>", new Vector2(180, -10));
            nextBtn.transform.SetParent(_panel.transform, false);
            _nextButton = nextBtn.GetComponent<Button>();
            _nextButton.onClick.AddListener(OnNextClicked);

            var suspiciousGo = new GameObject("SuspiciousIndicator");
            suspiciousGo.transform.SetParent(_panel.transform, false);
            var suspRect = suspiciousGo.AddComponent<RectTransform>();
            suspRect.anchorMin = new Vector2(0.5f, 0);
            suspRect.anchorMax = new Vector2(0.5f, 0);
            suspRect.pivot = new Vector2(0.5f, 0);
            suspRect.anchoredPosition = new Vector2(0, 12);
            suspRect.sizeDelta = new Vector2(400, 24);
            _suspiciousText = suspiciousGo.AddComponent<Text>();
            _suspiciousText.font = font;
            _suspiciousText.fontSize = 14;
            _suspiciousText.color = new Color(1f, 0.3f, 0.2f);
            _suspiciousText.alignment = TextAnchor.MiddleCenter;
            _suspiciousText.text = "SUSPICIOUS ACTIVITY";

            var overlayGo = new GameObject("StaticOverlay");
            overlayGo.transform.SetParent(_panel.transform, false);
            overlayGo.transform.SetAsLastSibling();
            var overlayRect = overlayGo.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            _staticOverlayImage = overlayGo.AddComponent<Image>();
            _staticOverlayImage.color = new Color(1f, 1f, 1f, 0f);
            _staticOverlayImage.raycastTarget = false;
            _staticOverlay = overlayGo;
            _staticOverlay.SetActive(false);

            canvasGo.transform.SetParent(transform);
            _panel.SetActive(false);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();
        }

        private static GameObject CreateTabletButton(string label, Vector2 position)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go = new GameObject(label.Replace("\n", "") + "Button");
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(80, 50);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.3f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            text.text = label;
            text.font = font;
            text.fontSize = 12;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            return go;
        }

        private void SetPanelVisible(bool visible)
        {
            _panel.SetActive(visible);
        }

        private void OnCameraChanged(int index)
        {
            SyncCameraTransform();
        }

        private void SyncCameraTransform()
        {
            var mgr = CctvManager.Instance;
            var cam = mgr?.CurrentCamera;
            if (_cctvRenderCam == null || cam == null) return;

            var view = cam.ViewTransform;
            _cctvRenderCam.transform.position = view.position;
            _cctvRenderCam.transform.rotation = view.rotation;
        }

        private void UpdateCameraName()
        {
            var cam = CctvManager.Instance?.CurrentCamera;
            if (_cameraNameText != null)
                _cameraNameText.text = cam != null ? cam.CameraName : "--";
        }

        private void UpdateSuspiciousIndicator()
        {
            var mgr = CctvManager.Instance;
            bool show = mgr != null && mgr.IsCurrentCameraSuspicious;
            if (_suspiciousText != null)
            {
                _suspiciousText.gameObject.SetActive(show);
            }
        }

        private void UpdateDistortion()
        {
            float inst = InstabilityManager.Instance != null ? InstabilityManager.Instance.Instability : 0f;

            if (inst >= 30f)
            {
                if (Time.time >= _nextStaticTime && !_staticActive)
                {
                    float reduction = NightShift.Systems.UpgradeManager.Instance != null
                        ? NightShift.Systems.UpgradeManager.Instance.GetTabletDistortionReduction()
                        : 0f;
                    if (Random.value >= reduction)
                    {
                        _nextStaticTime = Time.time + Random.Range(8f, 18f);
                        StartCoroutine(StaticOverlayCoroutine());
                    }
                    else
                    {
                        _nextStaticTime = Time.time + Random.Range(2f, 6f);
                    }
                }
            }

            if (inst >= 80f)
            {
                if (Time.time >= _nextAutoSwitchTime && CctvManager.Instance != null && CctvManager.Instance.Cameras.Count > 1)
                {
                    _nextAutoSwitchTime = Time.time + Random.Range(AutoSwitchMinInterval, AutoSwitchMaxInterval);
                    CctvManager.Instance.CycleNext();
                }
            }
        }

        private IEnumerator StaticOverlayCoroutine()
        {
            _staticActive = true;
            if (_staticOverlay != null)
            {
                _staticOverlay.SetActive(true);
                float t = 0f;
                while (t < StaticOverlayDuration)
                {
                    t += Time.deltaTime;
                    float alpha = 0.3f + Mathf.PerlinNoise(Time.time * 50f, 0) * 0.5f;
                    if (_staticOverlayImage != null)
                        _staticOverlayImage.color = new Color(1f, 1f, 1f, alpha);
                    yield return null;
                }
                _staticOverlay.SetActive(false);
                if (_staticOverlayImage != null)
                    _staticOverlayImage.color = new Color(1f, 1f, 1f, 0f);
            }
            _staticActive = false;
        }

        public bool IsTabletOpen => _isOpen;
    }
}
