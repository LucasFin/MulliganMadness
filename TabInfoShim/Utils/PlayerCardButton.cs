using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TabInfo.Utils
{
    public class PlayerCardButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public PlayerCardBar cardBar;
        public CardInfo card;
        private TextMeshProUGUI _text;

        public TextMeshProUGUI Text
        {
            get
            {
                if (_text == null)
                {
                    var child = transform.Find("Text (TMP)");
                    if (child != null) _text = child.GetComponent<TextMeshProUGUI>();
                }

                return _text;
            }
        }

        internal string CardInitials(CardInfo info)
        {
            if (info == null || string.IsNullOrEmpty(info.cardName)) return "?";
            var name = info.cardName;
            if (name.Length == 1) return name.ToUpper();
            return char.ToUpper(name[0]).ToString() + char.ToLower(name[1]);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
        }

        public void OnPointerExit(PointerEventData eventData)
        {
        }
    }
}
