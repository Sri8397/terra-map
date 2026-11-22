using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TerraMap
{
    public sealed class TerraMapControls : MonoBehaviour
    {
        public TerraMapGame Game;
        TerraMapPad stick, look, up, down, fight;
        Text status, pauseLabel;
        Font font;
        bool sprint;
        float nextStatus;

        void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var events = new GameObject("EventSystem");
            events.AddComponent<EventSystem>();
            events.AddComponent<InputSystemUIInputModule>();
            var root = new GameObject("Controls", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            var surface = Rect(root.transform, "Look", Vector2.zero, Vector2.zero, Vector2.zero);
            surface.anchorMax = Vector2.one;
            surface.offsetMin = surface.offsetMax = Vector2.zero;
            surface.gameObject.AddComponent<Image>().color = Color.clear;
            look = surface.gameObject.AddComponent<TerraMapPad>();
            look.Look = delta => { if (!Game.Paused) Game.Player.Look(delta / root.GetComponent<Canvas>().scaleFactor * 0.18f); };

            stick = Button(root.transform, "MOVE", new Vector2(170, 170), true);
            stick.Joystick = true;
            stick.Radius = 100f;
            Button(root.transform, "JUMP", new Vector2(-150, 150), false).Tap = () => Game.Player.Jump();
            Button(root.transform, "RUN", new Vector2(-320, 140), false).Tap = () => sprint = !sprint;
            Button(root.transform, "CROUCH", new Vector2(-320, 290), false).Tap = Crouch;
            Button(root.transform, "PRONE", new Vector2(-490, 290), false).Tap = Prone;
            Button(root.transform, "JET", new Vector2(-320, 440), false).Tap = () => Game.Player.ToggleJet();
            Button(root.transform, "RETURN", new Vector2(-490, 440), false).Tap = () => Game.Player.Return();
            fight = Button(root.transform, "FIST", new Vector2(-150, 440), false);
            up = Button(root.transform, "UP", new Vector2(-150, 600), false);
            down = Button(root.transform, "DOWN", new Vector2(-320, 600), false);
            Button(root.transform, "VIEW", new Vector2(160, 930), true).Tap = () => Game.Player.ChangeView();
            Button(root.transform, "EYE", new Vector2(330, 930), true).Tap = () => Game.Player.FreeLook = !Game.Player.FreeLook;
            Button(root.transform, "RESET", new Vector2(500, 930), true).Tap = () => { if (Game.MapReady) Game.Player.ResetToStreet(); };
            var pause = Button(root.transform, "PAUSE", new Vector2(-150, 930), false);
            pause.Tap = Game.TogglePause;
            pauseLabel = pause.GetComponentInChildren<Text>();
            status = Label(root.transform, "Status", "", 24);
            Place(status.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -35f), new Vector2(1250f, 64f));
            status.raycastTarget = false;
            var crosshair = Label(root.transform, "Crosshair", "+", 28);
            Place(crosshair.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 40f));
            crosshair.raycastTarget = false;
        }

        void Crouch()
        {
            if (!Game.Player.CanAct || Game.Player.Flying) return;
            Game.Player.Crouch = !Game.Player.Crouch;
            Game.Player.Prone = false;
        }

        void Prone()
        {
            if (!Game.Player.CanAct || Game.Player.Flying) return;
            Game.Player.Prone = !Game.Player.Prone;
            Game.Player.Crouch = false;
        }

        void Update()
        {
            var player = Game.Player;
            if (player == null || stick == null) return;
            var keys = Keyboard.current;
            if (keys != null && keys.escapeKey.wasPressedThisFrame) Game.TogglePause();
            if (keys != null && keys.backquoteKey.wasPressedThisFrame) player.PointerFree = !player.PointerFree;
            var lockMouse = !Application.isMobilePlatform && Application.isFocused && !Game.Paused && !player.PointerFree && Game.MapReady;
            Cursor.lockState = lockMouse ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !lockMouse;
            pauseLabel.text = Game.Paused ? "RESUME" : "PAUSE";
            if (Game.Paused)
            {
                player.ClearInput();
                sprint = false;
                stick.ResetPointer(); up.ResetPointer(); down.ResetPointer(); fight.ResetPointer();
            }
            else if (player.CanAct)
            {
                var movement = stick.Value;
                var lift = (up.Held ? 1f : 0f) - (down.Held ? 1f : 0f);
                if (keys != null)
                {
                    var keyboardMove = new Vector2((keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1f : 0f) - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1f : 0f),
                        (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1f : 0f) - (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1f : 0f));
                    if (keyboardMove != Vector2.zero) movement = keyboardMove;
                    if (keys.qKey.wasPressedThisFrame) player.ToggleJet();
                    if (keys.yKey.wasPressedThisFrame) player.Return();
                    if (keys.pKey.wasPressedThisFrame) player.ChangeView();
                    if (keys.vKey.wasPressedThisFrame) player.FreeLook = !player.FreeLook;
                    if (keys.gKey.wasPressedThisFrame) player.ResetToStreet();
                    if (keys.zKey.wasPressedThisFrame) Prone();
                    if (player.Flying)
                    {
                        if (keys.spaceKey.isPressed) lift += 1f;
                        if (keys.cKey.isPressed || keys.leftCtrlKey.isPressed) lift -= 1f;
                    }
                    else
                    {
                        if (keys.spaceKey.wasPressedThisFrame) player.Jump();
                        if (keys.cKey.wasPressedThisFrame || keys.leftCtrlKey.wasPressedThisFrame) Crouch();
                    }
                }
                player.Move = Vector2.ClampMagnitude(movement, 1f);
                player.Lift = Mathf.Clamp(lift, -1f, 1f);
                player.Sprint = sprint || (keys != null && keys.leftShiftKey.isPressed);
                var mouse = Mouse.current;
                if (mouse != null && lockMouse) player.Look(mouse.delta.ReadValue() * 0.15f);
                if (fight.Held || (mouse != null && lockMouse && mouse.leftButton.isPressed)) player.Punch();
            }
            up.gameObject.SetActive(player.Flying);
            down.gameObject.SetActive(player.Flying);
            fight.gameObject.SetActive(!player.Flying);
            if (Time.unscaledTime >= nextStatus)
            {
                nextStatus = Time.unscaledTime + 0.5f;
                status.text = Game.Paused ? "PAUSED · Tap RESUME" : Game.Status;
            }
        }

        TerraMapPad Button(Transform parent, string title, Vector2 offset, bool left)
        {
            var rect = Rect(parent, title, new Vector2(left ? 0f : 1f, 0f), offset, new Vector2(140f, 110f));
            rect.gameObject.AddComponent<Image>().color = new Color(0.025f, 0.035f, 0.05f, 0.7f);
            var text = Label(rect, "Label", title, 24);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            text.raycastTarget = false;
            return rect.gameObject.AddComponent<TerraMapPad>();
        }

        Text Label(Transform parent, string name, string value, int size)
        {
            var rect = Rect(parent, name, Vector2.zero, Vector2.zero, Vector2.zero);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            return text;
        }

        static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Place(rect, anchor, offset, size);
            return rect;
        }

        static void Place(RectTransform rect, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
        }
    }
}
