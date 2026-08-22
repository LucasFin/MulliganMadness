using UnityEngine;
using UnityEngine.EventSystems;

namespace MulliganMadness.UI
{
    internal sealed class OverlayDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        internal RectTransform Target;
        internal bool ResizeWidth;

        private Vector2 _startPointer;
        private Vector2 _startPos;
        private Vector2 _startSize;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Target == null) return;
            _startPos = Target.anchoredPosition;
            _startSize = Target.sizeDelta;
            ScreenToParent(eventData, out _startPointer);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Target == null) return;
            ScreenToParent(eventData, out var now);
            var delta = now - _startPointer;
            var canvas = StatsUiHelper.OverlaySize;

            if (ResizeWidth)
            {
                var maxWidth = Mathf.Min(640f, canvas.x * 0.55f);
                var width = Mathf.Clamp(_startSize.x - delta.x, 280f, maxWidth);
                var shift = _startSize.x - width;
                Target.sizeDelta = new Vector2(width, _startSize.y);
                Target.anchoredPosition = Clamp(_startPos + new Vector2(shift, 0f), Target.sizeDelta, canvas);
                return;
            }

            Target.anchoredPosition = Clamp(_startPos + delta, _startSize, canvas);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (Target == null || Plugin.Configs == null) return;
            Plugin.Configs.TabPosX.Value = Target.anchoredPosition.x;
            Plugin.Configs.TabPosY.Value = Target.anchoredPosition.y;
            Plugin.Configs.TabPanelWidth.Value = Target.sizeDelta.x;
        }

        private void ScreenToParent(PointerEventData eventData, out Vector2 local)
        {
            var parent = Target.parent as RectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out local);
        }

        private static Vector2 Clamp(Vector2 pos, Vector2 size, Vector2 canvas)
        {
            var margin = 8f;
            pos.x = Mathf.Clamp(pos.x, margin, Mathf.Max(margin, canvas.x - size.x - margin));
            pos.y = Mathf.Clamp(pos.y, margin, Mathf.Max(margin, canvas.y - size.y - margin));
            return pos;
        }
    }
}
