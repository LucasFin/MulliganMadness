using MulliganMadness.Utils;
using TMPro;
using UnboundLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MulliganMadness.UI
{
    public class TakeAllButton : MonoBehaviour
    {
        private static TakeAllButton _instance;
        private GameObject _root;
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _subtitle;
        private Button _button;
        private Image _fill;
        private Image _border;
        private CanvasGroup _group;

        private void Awake()
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            BuildUi();
            Hide();
        }

        private void Update()
        {
            if (_root == null)
            {
                if (Unbound.Instance != null && Unbound.Instance.canvas != null)
                {
                    BuildUi();
                    Hide();
                }
                return;
            }

            RefreshVisibility();
        }

        private void BuildUi()
        {
            var canvas = Unbound.Instance?.canvas;
            if (canvas == null) return;
            if (_root != null) return;

            _root = new GameObject("MM_TakeAllButton", typeof(RectTransform), typeof(CanvasGroup));
            _root.transform.SetParent(canvas.transform, false);
            _group = _root.GetComponent<CanvasGroup>();

            var rect = _root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 36f);
            rect.sizeDelta = new Vector2(320f, 64f);

            // Soft shadow
            var shadowGo = CreateChild("Shadow", _root.transform, Vector2.zero, Vector2.one, new Vector2(6f, -6f), Vector2.zero);
            var shadow = shadowGo.AddComponent<Image>();
            shadow.color = new Color(0f, 0f, 0f, 0.45f);
            shadow.raycastTarget = false;

            // Border plate
            var borderGo = CreateChild("Border", _root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _border = borderGo.AddComponent<Image>();
            _border.color = new Color(0.95f, 0.82f, 0.35f, 1f);

            // Inner fill
            var fillGo = CreateChild("Fill", borderGo.transform, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
            _fill = fillGo.AddComponent<Image>();
            _fill.color = new Color(0.10f, 0.42f, 0.28f, 0.98f);

            _button = fillGo.AddComponent<Button>();
            _button.targetGraphic = _fill;
            var colors = _button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.7f);
            colors.fadeDuration = 0.08f;
            _button.colors = colors;
            _button.onClick.AddListener(() =>
            {
                if (TakeAllManager.TryTakeAll())
                {
                    RefreshVisibility();
                }
            });

            // Hover brighten
            var hover = fillGo.AddComponent<TakeAllHover>();
            hover.fill = _fill;
            hover.border = _border;

            var titleGo = CreateChild("Title", fillGo.transform, new Vector2(0f, 0.42f), new Vector2(1f, 1f), new Vector2(12f, 0f), new Vector2(-12f, -4f));
            _title = titleGo.AddComponent<TextMeshProUGUI>();
            _title.alignment = TextAlignmentOptions.Center;
            _title.fontSize = 26f;
            _title.fontStyle = FontStyles.Bold;
            _title.color = new Color(1f, 0.98f, 0.90f, 1f);
            _title.text = "TAKE ALL";
            _title.raycastTarget = false;
            _title.outlineWidth = 0.18f;
            _title.outlineColor = new Color(0f, 0f, 0f, 0.75f);

            var subGo = CreateChild("Subtitle", fillGo.transform, new Vector2(0f, 0f), new Vector2(1f, 0.48f), new Vector2(12f, 6f), new Vector2(-12f, 4f));
            _subtitle = subGo.AddComponent<TextMeshProUGUI>();
            _subtitle.alignment = TextAlignmentOptions.Center;
            _subtitle.fontSize = 15f;
            _subtitle.color = new Color(0.92f, 0.88f, 0.70f, 0.95f);
            _subtitle.text = "once per game";
            _subtitle.raycastTarget = false;
        }

        private static GameObject CreateChild(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return go;
        }

        public static void RefreshVisibility()
        {
            if (_instance == null) return;
            _instance.ApplyVisibility();
        }

        public static void Hide()
        {
            if (_instance == null || _instance._root == null) return;
            _instance._root.SetActive(false);
        }

        private void ApplyVisibility()
        {
            if (_root == null) return;

            var picker = TakeAllManager.GetCurrentPicker();
            var remaining = TakeAllManager.HasRemaining(picker);
            var show = TakeAllManager.IsEnabled
                       && TakeAllManager.IsLocalPlayersTurn()
                       && remaining
                       && TakeAllManager.IsOfferedHandReady();

            _root.SetActive(show);
            if (!show) return;

            if (_subtitle != null)
            {
                _subtitle.text = "once per game · unused";
            }

            if (_button != null) _button.interactable = true;
            if (_fill != null) _fill.color = new Color(0.10f, 0.42f, 0.28f, 0.98f);
            if (_border != null) _border.color = new Color(0.95f, 0.82f, 0.35f, 1f);
            if (_group != null) _group.alpha = 1f;
        }

        private class TakeAllHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public Image fill;
            public Image border;
            private readonly Color _fillNormal = new Color(0.10f, 0.42f, 0.28f, 0.98f);
            private readonly Color _fillHover = new Color(0.14f, 0.52f, 0.34f, 1f);
            private readonly Color _borderNormal = new Color(0.95f, 0.82f, 0.35f, 1f);
            private readonly Color _borderHover = new Color(1f, 0.92f, 0.55f, 1f);

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (fill != null) fill.color = _fillHover;
                if (border != null) border.color = _borderHover;
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (fill != null) fill.color = _fillNormal;
                if (border != null) border.color = _borderNormal;
            }
        }
    }
}
