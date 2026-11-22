using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TerraMap
{
    public sealed class TerraMapPad : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IEndDragHandler
    {
        public Action Tap;
        public Action<Vector2> Look;
        public bool Joystick;
        public float Radius = 100f;
        public bool Held { get; private set; }
        public Vector2 Value { get; private set; }
        int pointer;

        public void OnPointerDown(PointerEventData data)
        {
            if (Held) return;
            pointer = data.pointerId;
            Held = true;
            Tap?.Invoke();
            if (Joystick) OnDrag(data);
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (Held && data.pointerId == pointer) ResetPointer();
        }

        public void OnDrag(PointerEventData data)
        {
            if (!Held || data.pointerId != pointer) return;
            if (Joystick)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, data.position, data.pressEventCamera, out var local);
                Value = Vector2.ClampMagnitude(local / Radius, 1f);
            }
            else Look?.Invoke(data.delta);
        }

        public void OnEndDrag(PointerEventData data) => OnPointerUp(data);
        public void ResetPointer() { Held = false; Value = Vector2.zero; }
        void OnDisable() => ResetPointer();
    }
}
