using System.Collections.Generic;
using KspConnected.Client.Core;
using UnityEngine;

namespace KspConnected.Client.UI
{
    /// <summary>
    /// Scrollable chat window available in Flight and Space Center scenes.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ChatWindow : MonoBehaviour
    {
        private Rect        _windowRect = new Rect(20, Screen.height - 260, 380, 230);
        private bool        _showWindow = true;
        private Vector2     _scroll     = Vector2.zero;
        private string      _input      = "";
        private bool        _scrollToBottom;
        private List<ChatEntry> _displayed = new List<ChatEntry>();

        private void Start()
        {
            var chat = KspConnectedMod.Instance?.Chat;
            if (chat != null)
            {
                _displayed = chat.GetHistory();
                chat.OnNewMessage += OnNewMessage;
            }
        }

        private void OnDestroy()
        {
            var chat = KspConnectedMod.Instance?.Chat;
            if (chat != null) chat.OnNewMessage -= OnNewMessage;
        }

        private void OnNewMessage(ChatEntry entry)
        {
            _displayed.Add(entry);
            _scrollToBottom = true;
        }

        private void OnGUI()
        {
            if (!_showWindow) return;
            var mod = KspConnectedMod.Instance;
            if (mod == null || mod.Connection.State != ConnectionState.Connected) return;

            _windowRect = GUILayout.Window(
                GUIUtility.GetControlID(FocusType.Passive) + 1,
                _windowRect,
                DrawWindow,
                "Chat",
                GUILayout.Width(380));
        }

        private void DrawWindow(int id)
        {
            // Message log
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(160));
            foreach (var entry in _displayed)
                GUILayout.Label($"[{entry.SenderName}] {entry.Text}");

            if (_scrollToBottom)
            {
                _scroll.y = float.MaxValue;
                _scrollToBottom = false;
            }
            GUILayout.EndScrollView();

            // Input row
            GUILayout.BeginHorizontal();
            GUI.SetNextControlName("ChatInput");
            _input = GUILayout.TextField(_input, 200);

            bool enterPressed = Event.current.type == EventType.KeyDown
                                && Event.current.keyCode == KeyCode.Return
                                && GUI.GetNameOfFocusedControl() == "ChatInput";

            if ((GUILayout.Button("Send", GUILayout.Width(50)) || enterPressed)
                && !string.IsNullOrWhiteSpace(_input))
            {
                KspConnectedMod.Instance?.Chat.Send(_input);
                _input = "";
                GUI.FocusControl("ChatInput");
            }
            GUILayout.EndHorizontal();

            GUI.DragWindow();
        }
    }
}
