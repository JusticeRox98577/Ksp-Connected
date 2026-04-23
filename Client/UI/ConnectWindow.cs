using KspConnected.Client.Core;
using KspConnected.Shared.Protocol;
using UnityEngine;

namespace KspConnected.Client.UI
{
    /// <summary>
    /// Connection management window — available from Space Center and Main Menu.
    /// Shows connect/disconnect controls and current connection status.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class ConnectWindow : MonoBehaviour
    {
        private Rect   _windowRect = new Rect(20, 20, 340, 260);
        private bool   _showWindow = true;

        private string _host       = "127.0.0.1";
        private string _portStr    = ProtocolConstants.DefaultPort.ToString();
        private string _playerName = "Jebediah";
        private string _statusMsg  = "";

        private GUIStyle _headerStyle;
        private bool     _stylesInit;

        private void Start()
        {
            var mod = KspConnectedMod.Instance;
            if (mod != null) _playerName = mod.PlayerName;
        }

        private void OnGUI()
        {
            if (!_showWindow) return;
            if (!_stylesInit) InitStyles();

            _windowRect = GUILayout.Window(
                GUIUtility.GetControlID(FocusType.Passive),
                _windowRect,
                DrawWindow,
                "KSP-Connected",
                GUILayout.Width(340));
        }

        private void DrawWindow(int id)
        {
            var mod  = KspConnectedMod.Instance;
            var conn = mod?.Connection;
            var state = conn?.State ?? ConnectionState.Disconnected;

            GUILayout.Label("Multiplayer Connection", _headerStyle);
            GUILayout.Space(4);

            // Status bar
            string stateLabel = state == ConnectionState.Connected
                ? $"Connected as #{conn.LocalPlayerId}"
                : state == ConnectionState.Connecting
                    ? "Connecting…"
                    : "Disconnected";
            GUILayout.Label("Status: " + stateLabel);

            GUILayout.Space(8);

            if (state == ConnectionState.Disconnected)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Name", GUILayout.Width(70));
                _playerName = GUILayout.TextField(_playerName, 24);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Host", GUILayout.Width(70));
                _host = GUILayout.TextField(_host);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Port", GUILayout.Width(70));
                _portStr = GUILayout.TextField(_portStr, 5);
                GUILayout.EndHorizontal();

                GUILayout.Space(8);

                if (GUILayout.Button("Connect"))
                {
                    if (int.TryParse(_portStr, out int port) && port > 0 && port < 65536
                        && !string.IsNullOrWhiteSpace(_host)
                        && !string.IsNullOrWhiteSpace(_playerName))
                    {
                        mod.PlayerName = _playerName.Trim();
                        mod.ConnectToServer(_host.Trim(), port);
                        _statusMsg = "";
                    }
                    else
                    {
                        _statusMsg = "Invalid host, port, or name.";
                    }
                }
            }
            else
            {
                GUILayout.Label($"Server: {_host}:{_portStr}");

                if (state == ConnectionState.Connected && GUILayout.Button("Disconnect"))
                    mod.DisconnectFromServer();
            }

            if (!string.IsNullOrEmpty(_statusMsg))
            {
                GUILayout.Space(4);
                GUILayout.Label(_statusMsg);
            }

            GUILayout.Space(8);
            if (GUILayout.Button(_showWindow ? "Hide" : "Show"))
                _showWindow = !_showWindow;

            GUI.DragWindow();
        }

        private void InitStyles()
        {
            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize  = 13,
            };
            _stylesInit = true;
        }
    }
}
