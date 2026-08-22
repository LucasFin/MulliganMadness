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
        private float _visibilityTimer;

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

            _visibilityTimer -= Time.unscaledDeltaTime;
            if (_visibilityTimer > 0f) return;
            _visibilityTimer = 0.15f;
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
            rect.sizeDelta = new Vector2(400f, 70f);

            var shadowGo = CreateChild("Shadow", _root.transform, Vector2.zero, Vector2.one, new Vector2(6f, -6f), Vector2.zero);
            var shadow = shadowGo.AddComponent<Image>();
            shadow.color = new Color(0f, 0f, 0f, 0.45f);
            shadow.raycastTarget = false;

            var borderGo = CreateChild("Border", _root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _border = borderGo.AddComponent<Image>();
            _border.color = new Color(0.95f, 0.82f, 0.35f, 1f);

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
            _button.onClick.AddListener(OnClick);

            var hover = fillGo.AddComponent<TakeAllHover>();
            hover.fill = _fill;
            hover.border = _border;

            var titleGo = CreateChild("Title", fillGo.transform, new Vector2(0f, 0.42f), new Vector2(1f, 1f), new Vector2(12f, 0f), new Vector2(-12f, -4f));
            _title = titleGo.AddComponent<TextMeshProUGUI>();
            _title.alignment = TextAlignmentOptions.Center;
            _title.fontSize = 24f;
            _title.fontStyle = FontStyles.Bold;
            _title.color = new Color(1f, 0.98f, 0.90f, 1f);
            _title.text = "TAKE ALL";
            _title.raycastTarget = false;
            _title.enableWordWrapping = false;
            _title.overflowMode = TextOverflowModes.Overflow;
            _title.outlineWidth = 0.18f;
            _title.outlineColor = new Color(0f, 0f, 0f, 0.75f);

            var subGo = CreateChild("Subtitle", fillGo.transform, new Vector2(0f, 0f), new Vector2(1f, 0.48f), new Vector2(12f, 6f), new Vector2(-12f, 4f));
            _subtitle = subGo.AddComponent<TextMeshProUGUI>();
            _subtitle.alignment = TextAlignmentOptions.Center;
            _subtitle.fontSize = 14f;
            _subtitle.color = new Color(0.92f, 0.88f, 0.70f, 0.95f);
            _subtitle.text = "once per game";
            _subtitle.raycastTarget = false;
            _subtitle.enableWordWrapping = false;
        }

        private void OnClick()
        {
            var picker = TakeAllManager.GetCurrentPicker();
            if (picker != null && TakeAllManager.HasAuthorization(picker.playerID))
            {
                if (TakeAllManager.TryExecuteAuthorization()) RefreshVisibility();
                return;
            }

            if (TakeAllManager.TryTakeAll()) RefreshVisibility();
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
            var remaining = TakeAllManager.CanUseTakeAll(picker);
            var authorized = picker != null && TakeAllManager.HasAuthorization(picker.playerID);
            var voteMode = SessionSettings.Current.TakeAllMode == TakeAllMode.Vote;
            var nest = TakeAllManager.HasNestBonus(picker);
            var silver = !nest && TakeAllManager.HasSilverBonus(picker);
            var show = TakeAllManager.IsLocalPlayersTurn()
                       && (remaining || authorized)
                       && TakeAllManager.IsOfferedHandReady()
                       && !ItemShopGuard.AnyPlayerInShop()
                       && !TakeAllVoteManager.IsActive;

            _root.SetActive(show);
            if (!show) return;

            var usesLeft = TakeAllManager.UsesRemaining(picker);
            var curse = SessionSettings.Current.TakeAllCurseCost && !nest && !silver && !authorized;
            if (_title != null)
            {
                if (authorized) _title.text = SessionSettings.Current.TakeAllCurseCost ? "TAKE ALL + CURSE" : "TAKE ALL";
                else if (nest) _title.text = "TAKE ALL";
                else if (silver) _title.text = "TAKE HALF";
                else if (voteMode) _title.text = curse ? "VOTE TAKE ALL + CURSE" : "REQUEST TAKE ALL VOTE";
                else _title.text = curse ? "TAKE ALL + CURSE" : "TAKE ALL";
            }

            if (_subtitle != null)
            {
                string modeLabel;
                if (authorized) modeLabel = "optional - pick as usual if you want";
                else if (nest) modeLabel = "curse-free · Nest Egg";
                else if (silver) modeLabel = "curse-free · Silver Egg · half the hand";
                else if (voteMode) modeLabel = "others vote; you can still pick if they accept";
                else modeLabel = usesLeft > 1 ? $"{usesLeft} uses left" : "once per game";
                _subtitle.text = curse
                    ? $"{modeLabel} · you get a random MM curse"
                    : modeLabel;
            }

            var fill = nest
                ? new Color(0.42f, 0.32f, 0.08f, 0.98f)
                : silver ? new Color(0.28f, 0.32f, 0.38f, 0.98f)
                : curse ? new Color(0.42f, 0.14f, 0.28f, 0.98f) : new Color(0.10f, 0.42f, 0.28f, 0.98f);
            var border = nest
                ? new Color(0.95f, 0.82f, 0.35f, 1f)
                : silver ? new Color(0.78f, 0.82f, 0.88f, 1f)
                : curse ? new Color(0.95f, 0.45f, 0.72f, 1f) : new Color(0.95f, 0.82f, 0.35f, 1f);
            if (_button != null) _button.interactable = true;
            if (_fill != null) _fill.color = fill;
            if (_border != null) _border.color = border;
            if (_group != null) _group.alpha = 1f;

            var hover = _fill != null ? _fill.GetComponent<TakeAllHover>() : null;
            if (hover != null) hover.SetStyle(curse, nest, silver);
        }

        private class TakeAllHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public Image fill;
            public Image border;
            private bool _curse;
            private bool _nest;
            private bool _silver;
            private readonly Color _fillNormal = new Color(0.10f, 0.42f, 0.28f, 0.98f);
            private readonly Color _fillHover = new Color(0.14f, 0.52f, 0.34f, 1f);
            private readonly Color _borderNormal = new Color(0.95f, 0.82f, 0.35f, 1f);
            private readonly Color _borderHover = new Color(1f, 0.92f, 0.55f, 1f);
            private readonly Color _curseFill = new Color(0.42f, 0.14f, 0.28f, 0.98f);
            private readonly Color _curseFillHover = new Color(0.52f, 0.18f, 0.34f, 1f);
            private readonly Color _curseBorder = new Color(0.95f, 0.45f, 0.72f, 1f);
            private readonly Color _curseBorderHover = new Color(1f, 0.62f, 0.82f, 1f);
            private readonly Color _bonusFill = new Color(0.42f, 0.32f, 0.08f, 0.98f);
            private readonly Color _bonusFillHover = new Color(0.52f, 0.42f, 0.12f, 1f);
            private readonly Color _bonusBorder = new Color(0.95f, 0.82f, 0.35f, 1f);
            private readonly Color _bonusBorderHover = new Color(1f, 0.92f, 0.55f, 1f);
            private readonly Color _silverFill = new Color(0.28f, 0.32f, 0.38f, 0.98f);
            private readonly Color _silverFillHover = new Color(0.36f, 0.40f, 0.46f, 1f);
            private readonly Color _silverBorder = new Color(0.78f, 0.82f, 0.88f, 1f);
            private readonly Color _silverBorderHover = new Color(0.90f, 0.93f, 0.97f, 1f);

            internal void SetStyle(bool curse, bool nest, bool silver)
            {
                _curse = curse;
                _nest = nest;
                _silver = silver;
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (fill != null) fill.color = _nest ? _bonusFillHover : _silver ? _silverFillHover : _curse ? _curseFillHover : _fillHover;
                if (border != null) border.color = _nest ? _bonusBorderHover : _silver ? _silverBorderHover : _curse ? _curseBorderHover : _borderHover;
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (fill != null) fill.color = _nest ? _bonusFill : _silver ? _silverFill : _curse ? _curseFill : _fillNormal;
                if (border != null) border.color = _nest ? _bonusBorder : _silver ? _silverBorder : _curse ? _curseBorder : _borderNormal;
            }
        }
    }
}
