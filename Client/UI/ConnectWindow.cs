using KspConnected.Client.Core;
using KspConnected.Shared.Protocol;
using UnityEngine;

namespace KspConnected.Client.UI
{
    /// <summary>
    /// Connection management window — available from Space Center.
    /// Supports direct TCP connections and relay-based connections (no port forwarding).
    /// </summary>
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class ConnectWindow : MonoBehaviour
    {
        private Rect _windowRect = new Rect(20, 20, 360, 320);
        private bool _showWindow = true;

        // Shared
        private string _playerName = "Jebediah";
        private string _statusMsg  = "";

        // Direct tab
        private string _host    = "127.0.0.1";
        private string _portStr = ProtocolConstants.DefaultPort.ToString();

        // Relay tab
        private string _relayHost    = "";
        private string _relayPortStr = ProtocolConstants.DefaultPort.ToString();
        private string _roomCodeJoin = "";
        private string _roomCodeDisplay = "";   // shown after Create Room succeeds

        // 0 = Direct, 1 = Relay
        private int _tab = 0;

        private GUIStyle _headerStyle;
        private GUIStyle _tabActiveStyle;
        private GUIStyle _tabStyle;
        private GUIStyle _roomCodeStyle;
        private bool     _stylesInit;

        private void Start()
        {
            var mod = KspConnectedMod.Instance;
            if (mod != null)
            {
                _playerName = mod.PlayerName;
                mod.Connection.OnRoomCreated += code => _roomCodeDisplay = code;
            }
        }

        private void OnGUI()
        {
            if (!_stylesInit) InitStyles();

            if (!_showWindow)
            {
                if (GUI.Button(new Rect(20, 20, 120, 22), "KSP-Connected ▲"))
                    _showWindow = true;
                return;
            }

            _windowRect = GUILayout.Window(
                GUIUtility.GetControlID(FocusType.Passive),
                _windowRect,
                DrawWindow,
                "KSP-Connected",
                GUILayout.Width(360));
        }

        private void DrawWindow(int id)
        {
            var mod   = KspConnectedMod.Instance;
            var conn  = mod?.Connection;
            var state = conn?.State ?? ConnectionState.Disconnected;

            GUILayout.Label("Multiplayer Connection", _headerStyle);
            GUILayout.Space(4);

            // Status
            string stateLabel = state == ConnectionState.Connected
                ? (string.IsNullOrEmpty(conn.RoomCode)
                    ? $"Connected as #{conn.LocalPlayerId}"
                    : $"Room {conn.RoomCode}  —  player #{conn.LocalPlayerId}")
                : state == ConnectionState.Connecting ? "Connecting…" : "Disconnected";
            GUILayout.Label("Status: " + stateLabel);

            GUILayout.Space(6);

            if (state == ConnectionState.Disconnected)
            {
                // Tab bar
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Direct",             _tab == 0 ? _tabActiveStyle : _tabStyle)) _tab = 0;
                if (GUILayout.Button("Relay (no port fw)", _tab == 1 ? _tabActiveStyle : _tabStyle)) _tab = 1;
                GUILayout.EndHorizontal();

                GUILayout.Space(6);

                // Player name (shared)
                GUILayout.BeginHorizontal();
                GUILayout.Label("Name", GUILayout.Width(80));
                _playerName = GUILayout.TextField(_playerName, 24);
                GUILayout.EndHorizontal();

                GUILayout.Space(4);

                if (_tab == 0)
                    DrawDirectTab(mod);
                else
                    DrawRelayTab(mod);
            }
            else
            {
                if (_tab == 1 && !string.IsNullOrEmpty(_roomCodeDisplay))
                {
                    GUILayout.Label("Room code — share with friends:");
                    GUILayout.Label(_roomCodeDisplay, _roomCodeStyle);
                    GUILayout.Space(4);
                }

                if (state == ConnectionState.Connected && GUILayout.Button("Disconnect"))
                {
                    _roomCodeDisplay = "";
                    mod.DisconnectFromServer();
                }
            }

            if (!string.IsNullOrEmpty(_statusMsg))
            {
                GUILayout.Space(4);
                GUILayout.Label(_statusMsg);
            }

            GUILayout.Space(6);
            if (GUILayout.Button("Hide"))
                _showWindow = false;

            GUI.DragWindow();
        }

        // ── Direct tab ────────────────────────────────────────────────────────

        private void DrawDirectTab(KspConnectedMod mod)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Host IP", GUILayout.Width(80));
            _host = GUILayout.TextField(_host);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Port", GUILayout.Width(80));
            _portStr = GUILayout.TextField(_portStr, 5);
            GUILayout.EndHorizontal();

            GUILayout.Space(8);

            if (GUILayout.Button("Connect"))
            {
                if (ValidatePortAndName(_portStr, out int port))
                {
                    mod.PlayerName = _playerName.Trim();
                    mod.ConnectToServer(_host.Trim(), port);
                    _statusMsg = "";
                }
            }
        }

        // ── Relay tab ─────────────────────────────────────────────────────────

        private void DrawRelayTab(KspConnectedMod mod)
        {
            GUILayout.Label("Relay server — deploy one free on Railway/Render/Fly.io");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Relay host", GUILayout.Width(80));
            _relayHost = GUILayout.TextField(_relayHost);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Port", GUILayout.Width(80));
            _relayPortStr = GUILayout.TextField(_relayPortStr, 5);
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // Create room
            if (GUILayout.Button("Create Room  (host)"))
            {
                if (ValidatePortAndName(_relayPortStr, out int port) && ValidateRelayHost())
                {
                    mod.PlayerName = _playerName.Trim();
                    mod.Connection.CreateRelayRoom(_relayHost.Trim(), port, _playerName.Trim());
                    _statusMsg = "";
                }
            }

            GUILayout.Space(4);
            GUILayout.Label("── or join a room ──");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Room code", GUILayout.Width(80));
            _roomCodeJoin = GUILayout.TextField(_roomCodeJoin.ToUpperInvariant(), 6);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Join Room"))
            {
                if (ValidatePortAndName(_relayPortStr, out int port)
                    && ValidateRelayHost()
                    && _roomCodeJoin.Length == 6)
                {
                    mod.PlayerName = _playerName.Trim();
                    mod.Connection.JoinRelayRoom(_relayHost.Trim(), port,
                                                 _roomCodeJoin.Trim(), _playerName.Trim());
                    _statusMsg = "";
                }
                else if (_roomCodeJoin.Length != 6)
                {
                    _statusMsg = "Room code must be 6 characters.";
                }
            }
        }

        // ── helpers ───────────────────────────────────────────────────────────

        private bool ValidatePortAndName(string portStr, out int port)
        {
            port = 0;
            if (string.IsNullOrWhiteSpace(_playerName))
            {
                _statusMsg = "Enter a player name.";
                return false;
            }
            if (!int.TryParse(portStr, out port) || port <= 0 || port > 65535)
            {
                _statusMsg = "Invalid port.";
                return false;
            }
            return true;
        }

        private bool ValidateRelayHost()
        {
            if (string.IsNullOrWhiteSpace(_relayHost))
            {
                _statusMsg = "Enter the relay server address.";
                return false;
            }
            return true;
        }

        private void InitStyles()
        {
            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize  = 13,
            };
            _tabStyle = new GUIStyle(GUI.skin.button);
            _tabActiveStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
            };
            _roomCodeStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize  = 22,
                alignment = TextAnchor.MiddleCenter,
            };
            _stylesInit = true;
        }
    }
}
