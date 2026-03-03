using UnityEngine;
using UnityEngine.UI;
using NightShift.Core;
using NightShift.Systems;

namespace NightShift.UI
{
    /// <summary>
    /// End-of-run summary: grade, credits, stats. Buttons: Shop, Run Again, Quit.
    /// Creates UI programmatically. Saves credits to ProgressionData on EndRun.
    /// </summary>
    public class EndScreenController : MonoBehaviour, IGameStateListener
    {
        public static bool IsShopOpen { get; private set; }

        /// <summary>True when end run panel or shop is visible — use to lock player input.</summary>
        public static bool IsEndScreenVisible => _instance != null && _instance._endScreenVisible;
        private static EndScreenController _instance;
        private bool _endScreenVisible;

        private Canvas _canvas;
        private GameObject _panel;
        private GameObject _shopPanel;
        private Text _shopCreditsText;
        private Text _titleText;
        private Text _summaryText;
        private Text _creditsText;
        private Button _shopButton;
        private Button _runAgainButton;
        private Button _quitButton;

        private void Awake()
        {
            _instance = this;
            CreateUI();
            if (_panel != null) _panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Start()
        {
            CreateShopUI();
            if (_shopPanel != null) _shopPanel.SetActive(false);
        }

        private void CreateUI()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("EndScreenCanvas");
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 2000;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            _panel = new GameObject("EndRunPanel");
            _panel.transform.SetParent(canvasGo.transform, false);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(500, 420);
            panelRect.anchoredPosition = Vector2.zero;
            var panelImg = _panel.AddComponent<Image>();
            panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.98f);

            _titleText = AddText(_panel, "Title", "GAME OVER", font, 32, new Vector2(0, 155));
            _summaryText = AddText(_panel, "Summary", "", font, 17, new Vector2(0, 35));
            _summaryText.rectTransform.sizeDelta = new Vector2(440, 100);
            _creditsText = AddText(_panel, "Credits", "", font, 20, new Vector2(0, -70));
            _creditsText.rectTransform.sizeDelta = new Vector2(400, 50);

            _shopButton = AddButton(_panel, "Shop", new Vector2(-120, -155), () => OpenShop());
            _runAgainButton = AddButton(_panel, "Run Again", new Vector2(120, -155), OnRunAgain);
            _quitButton = AddButton(_panel, "Quit", new Vector2(0, -210), () => Application.Quit());

            canvasGo.transform.SetParent(transform);
        }

        private void CreateShopUI()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _shopPanel = new GameObject("ShopPanel");
            _shopPanel.transform.SetParent(_canvas.transform, false);
            var shopRect = _shopPanel.AddComponent<RectTransform>();
            shopRect.anchorMin = new Vector2(0.5f, 0.5f);
            shopRect.anchorMax = new Vector2(0.5f, 0.5f);
            shopRect.sizeDelta = new Vector2(560, 480);
            shopRect.anchoredPosition = Vector2.zero;
            var shopImg = _shopPanel.AddComponent<Image>();
            shopImg.color = new Color(0.06f, 0.06f, 0.1f, 0.98f);

            var title = AddText(_shopPanel, "ShopTitle", "UPGRADE SHOP", font, 26, new Vector2(0, 200));
            _shopCreditsText = AddText(_shopPanel, "ShopCredits", "Credits: 0", font, 18, new Vector2(0, 165));

            var closeBtn = AddButton(_shopPanel, "Close", new Vector2(0, -210), () => CloseShop());
            var closeText = closeBtn.GetComponentInChildren<Text>();
            if (closeText != null) closeText.text = "Close";

            var scrollRoot = new GameObject("ShopItemsRoot");
            scrollRoot.transform.SetParent(_shopPanel.transform, false);
            var rootRect = scrollRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(520, 320);
            rootRect.anchoredPosition = new Vector2(0, 10);

            var mgr = UpgradeManager.Instance;
            if (mgr != null)
            {
                float y = 130f;
                foreach (var def in mgr.GetAllDefinitions())
                {
                    if (def == null) continue;
                    var item = CreateShopItem(scrollRoot, def, font, y);
                    y -= 70f;
                }
            }

            _shopPanel.SetActive(false);
        }

        private GameObject CreateShopItem(GameObject parent, UpgradeDefinition def, Font font, float y)
        {
            var go = new GameObject($"ShopItem_{def.id}");
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(500, 62);
            rect.anchoredPosition = new Vector2(0, y);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.14f, 0.14f, 0.18f, 0.95f);

            var nameText = AddTextLeftAligned(go, "Name", def.displayName, font, 17, 12, 16, 200);
            var descText = AddTextLeftAligned(go, "Desc", def.description, font, 12, -10, 16, 200);
            descText.color = new Color(0.72f, 0.72f, 0.76f);

            var costText = AddText(go, "Cost", $"{def.cost} credits", font, 15, new Vector2(90, 0));
            costText.rectTransform.sizeDelta = new Vector2(100, 24);

            var buyBtn = AddButton(go, "Buy", new Vector2(210, 0), () => TryPurchase(def));
            var buyBtnRect = buyBtn.GetComponent<RectTransform>();
            buyBtnRect.sizeDelta = new Vector2(80, 32);
            var buyText = buyBtn.GetComponentInChildren<Text>();
            if (buyText != null) buyText.text = "Buy";

            var itemView = go.AddComponent<UpgradeShopItemView>();
            itemView.Init(def, buyBtn, costText);

            itemView.Refresh();
            return go;
        }

        private void TryPurchase(UpgradeDefinition def)
        {
            if (UpgradeManager.Instance == null) return;
            if (UpgradeManager.Instance.TryPurchase(def.id, def.cost))
            {
                RefreshShopItems();
                RefreshShopCredits();
            }
        }

        private void RefreshShopCredits()
        {
            if (_shopCreditsText != null && UpgradeManager.Instance != null)
                _shopCreditsText.text = $"Credits: {UpgradeManager.Instance.Credits}";
        }

        private void RefreshShopItems()
        {
            foreach (var item in _shopPanel.GetComponentsInChildren<UpgradeShopItemView>())
                item?.Refresh();
        }

        private static Text AddText(GameObject parent, string name, string text, Font font, int size, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(400, 30);
            rect.anchoredPosition = pos;
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.color = Color.white;
            t.text = text;
            t.alignment = TextAnchor.MiddleCenter;
            return t;
        }

        private static Text AddTextLeftAligned(GameObject parent, string name, string text, Font font, int size, float posY, float padLeft, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(padLeft, posY);
            rect.sizeDelta = new Vector2(width, size + 8);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.color = Color.white;
            t.text = text;
            t.alignment = TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            return t;
        }

        private static Button AddButton(GameObject parent, string name, Vector2 pos, System.Action onClick)
        {
            var go = new GameObject(name + "Button");
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(140, 35);
            rect.anchoredPosition = pos;
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
            text.text = name;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            btn.onClick.AddListener(() => onClick?.Invoke());
            return btn;
        }

        private void OpenShop()
        {
            IsShopOpen = true;
            _endScreenVisible = true;
            _panel?.SetActive(false);
            _shopPanel?.SetActive(true);
            RefreshShopCredits();
        }

        private void CloseShop()
        {
            IsShopOpen = false;
            _endScreenVisible = true;
            _shopPanel?.SetActive(false);
            _panel?.SetActive(true);
        }

        private void OnRunAgain()
        {
            CloseShop();
            GameStateManager.Instance?.RestartRun();
        }

        public void OnGameStateEntered(GameState state)
        {
            if (state == GameState.EndRun)
                ShowEndScreen();
            else
            {
                _endScreenVisible = false;
                if (_panel != null) _panel.SetActive(false);
                if (_shopPanel != null) _shopPanel.SetActive(false);
                IsShopOpen = false;
            }
        }

        public void OnGameStateExited(GameState state) { }

        private void ShowEndScreen()
        {
            var reason = RunEndHandler.LastRunEndReason;
            bool survived = reason == RunEndReason.Survived;

            if (_titleText != null)
                _titleText.text = survived ? "SURVIVED" : "GAME OVER";

            var stats = RunStatsTracker.CurrentStats;
            char grade = 'F';
            int baseCredits = 0;
            if (stats != null)
            {
                grade = GradeCalculator.GetGradeLetter(stats);
                baseCredits = GradeCalculator.GetBaseCreditsEarned(stats);
            }

            float reportBonus = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetReportRewardBonusMultiplier() : 1f;
            int creditsEarned = Mathf.RoundToInt(baseCredits * reportBonus);

            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.AddCredits(creditsEarned);

            int creditsTotal = UpgradeManager.Instance != null ? UpgradeManager.Instance.Credits : 0;

            if (_summaryText != null && stats != null)
            {
                string time = GameClockForDisplay();
                string outcome = reason switch
                {
                    RunEndReason.Survived => "Reached 6AM",
                    RunEndReason.InstabilityMax => "Instability maxed",
                    RunEndReason.PlayerDied => "Downed too long",
                    _ => "Unknown"
                };
                _summaryText.text = $"Grade: {grade}\nCorrect: {stats.anomaliesResolvedCorrect} | Incorrect: {stats.anomaliesResolvedIncorrect}\n" +
                    $"Max Instability: {stats.maxInstabilityReached:F0}%\n{outcome}";
            }

            if (_creditsText != null)
                _creditsText.text = $"Credits earned: {creditsEarned}\nTotal credits: {creditsTotal}";

            _endScreenVisible = true;
            if (_panel != null)
                _panel.SetActive(true);
            if (_shopPanel != null)
                _shopPanel.SetActive(false);
            IsShopOpen = false;
        }

        private static string GameClockForDisplay()
        {
            var clock = Object.FindFirstObjectByType<GameClock>();
            return clock != null ? clock.CurrentTimeText : "6:00 AM";
        }
    }
}
