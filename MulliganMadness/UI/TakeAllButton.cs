using System.Collections;
using MulliganMadness.Utils;
using TMPro;
using UnboundLib;
using UnityEngine;
using UnityEngine.UI;

namespace MulliganMadness.UI
{
    public class TakeAllButton : MonoBehaviour
    {
        private static TakeAllButton _instance;
        private GameObject _root;
        private TextMeshProUGUI _label;
        private Button _button;

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
            if (_root == null) return;
            RefreshVisibility();
        }

        private void BuildUi()
        {
            var canvas = Unbound.Instance.canvas;
            if (canvas == null) return;

            _root = new GameObject("MM_TakeAllButton", typeof(RectTransform));
            _root.transform.SetParent(canvas.transform, false);

            var rect = _root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 48f);
            rect.sizeDelta = new Vector2(360f, 72f);

            var image = _root.AddComponent<Image>();
            image.color = new Color(0.12f, 0.55f, 0.28f, 0.92f);

            _button = _root.AddComponent<Button>();
            _button.targetGraphic = image;
            _button.onClick.AddListener(() =>
            {
                if (TakeAllManager.TryTakeAll())
                {
                    RefreshVisibility();
                }
            });

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(_root.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            _label = textGo.AddComponent<TextMeshProUGUI>();
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = 28f;
            _label.color = Color.white;
            _label.text = "TAKE ALL\n<smallcaps>once per game</smallcaps>";
            _label.enableWordWrapping = true;
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

            var show = TakeAllManager.IsEnabled
                       && TakeAllManager.IsLocalPlayersTurn()
                       && TakeAllManager.HasRemaining(TakeAllManager.GetCurrentPicker());

            _root.SetActive(show);

            if (show && _label != null)
            {
                var picker = TakeAllManager.GetCurrentPicker();
                var remaining = TakeAllManager.HasRemaining(picker);
                _label.text = remaining
                    ? "TAKE ALL\n<smallcaps>once per game — unused</smallcaps>"
                    : "TAKE ALL\n<smallcaps>already used</smallcaps>";
                if (_button != null) _button.interactable = remaining;
            }
        }
    }
}
