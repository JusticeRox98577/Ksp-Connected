using System;
using System.Collections.Generic;
using KspConnected.Client.Util;
using KspConnected.Shared.Data;
using KspConnected.Shared.Messages;
using UnityEngine;

namespace KspConnected.Client.Sync
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class GhostVesselManager : MonoBehaviour
    {
        public static GhostVesselManager Instance { get; private set; }

        private readonly Dictionary<int, VesselState> _remoteStates = new Dictionary<int, VesselState>();
        private GUIStyle _hudStyle;
        private GUIStyle _labelStyle;
        private bool     _stylesInit;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            Instance = null;
            _remoteStates.Clear();
        }

        public void OnVesselConfig(VesselConfigMessage msg) { }

        public void OnVesselUpdate(VesselState state)
        {
            int myId = Core.KspConnectedMod.Instance?.Connection.LocalPlayerId ?? -1;
            if (state.PlayerId == myId) return;
            _remoteStates[state.PlayerId] = state;
        }

        public void RemoveGhost(int playerId) => _remoteStates.Remove(playerId);
        public void RemoveAll()               => _remoteStates.Clear();

        private void OnGUI()
        {
            if (!_stylesInit) InitStyles();

            DrawHudPanel();

            if (_remoteStates.Count == 0) return;

            Camera cam = ResolveCamera();
            if (cam != null) DrawWorldLabels(cam);
        }

        // ── HUD panel — always visible when in flight ─────────────────────────

        private void DrawHudPanel()
        {
            var mod  = Core.KspConnectedMod.Instance;
            bool connected = mod?.Connection.State == Core.ConnectionState.Connected;
            if (!connected) return;

            const float panelW = 230f;
            const float rowH   = 36f;
            float panelH = _remoteStates.Count > 0
                ? _remoteStates.Count * rowH + 26f
                : 26f;

            float x = Screen.width - panelW - 8f;
            float y = 38f;

            GUI.Box(new Rect(x, y, panelW, panelH), "");

            float ry = y + 4f;
            if (_remoteStates.Count == 0)
            {
                GUI.Label(new Rect(x + 6, ry, panelW - 12, 20), "KSP-Connected: no remote players", _hudStyle);
                return;
            }

            foreach (var state in _remoteStates.Values)
            {
                double dist = ApproxDistance(state);
                string distStr = double.IsNaN(dist) ? "?" : FormatDist(dist);
                GUI.Label(new Rect(x + 6, ry, panelW - 12, rowH - 2),
                          $"[{state.PlayerName}]  {distStr}\n{state.VesselName}",
                          _hudStyle);
                ry += rowH;
            }
        }

        // ── world-space labels ────────────────────────────────────────────────

        private void DrawWorldLabels(Camera cam)
        {
            bool inMap = MapView.MapIsEnabled;

            foreach (var state in _remoteStates.Values)
            {
                CelestialBody body = FindBody(state.BodyName);
                if (body == null) continue;

                Vector3d worldPos = body.GetWorldSurfacePosition(
                    state.Latitude, state.Longitude, state.AltitudeASL);

                Vector3 drawPos = inMap
                    ? ScaledSpace.LocalToScaledSpace(worldPos)
                    : new Vector3((float)worldPos.x, (float)worldPos.y, (float)worldPos.z);

                Vector3 screen = cam.WorldToScreenPoint(drawPos);
                if (screen.z <= 0) continue;

                float sx = screen.x;
                float sy = Screen.height - screen.y;
                GUI.Label(new Rect(sx - 80, sy - 36, 160, 34),
                          $"▲ [{state.PlayerName}]\n{state.VesselName}",
                          _labelStyle);
            }
        }

        // ── camera resolution ─────────────────────────────────────────────────

        private static Camera ResolveCamera()
        {
            if (MapView.MapIsEnabled)
                return PlanetariumCamera.Camera;

            // KSP stacks multiple cameras; try the tagged main camera first,
            // then fall back to whatever camera is active.
            Camera cam = Camera.main;
            if (cam != null) return cam;

            // Walk all cameras and pick the one with the highest depth
            // (typically the near-clip flight camera).
            Camera best = null;
            foreach (Camera c in Camera.allCameras)
            {
                if (best == null || c.depth > best.depth)
                    best = c;
            }
            return best;
        }

        // ── helpers ───────────────────────────────────────────────────────────

        private static double ApproxDistance(VesselState remote)
        {
            Vessel active = FlightGlobals.ActiveVessel;
            if (active == null) return double.NaN;

            double dLat = remote.Latitude    - active.latitude;
            double dLon = remote.Longitude   - active.longitude;
            double dAlt = remote.AltitudeASL - active.altitude;
            return Math.Sqrt(dLat * dLat + dLon * dLon) * 111_000.0 + Math.Abs(dAlt);
        }

        private static string FormatDist(double m)
            => m < 1000 ? $"{m:F0} m" : $"{m / 1000:F1} km";

        private static CelestialBody FindBody(string name)
        {
            foreach (var b in FlightGlobals.Bodies)
                if (b.bodyName == name) return b;
            return null;
        }

        private void InitStyles()
        {
            _hudStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 11,
                alignment = TextAnchor.MiddleLeft,
            };
            _hudStyle.normal.textColor = Color.cyan;

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize  = 13,
                alignment = TextAnchor.MiddleCenter,
            };
            _labelStyle.normal.textColor = Color.cyan;

            _stylesInit = true;
        }
    }
}
