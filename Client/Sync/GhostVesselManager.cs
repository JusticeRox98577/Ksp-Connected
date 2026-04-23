using System;
using System.Collections.Generic;
using KspConnected.Client.Util;
using KspConnected.Shared.Data;
using KspConnected.Shared.Messages;
using UnityEngine;

namespace KspConnected.Client.Sync
{
    /// <summary>
    /// Draws a HUD panel and world-space labels for each remote player.
    /// Does not spawn real vessels — ProtoVessel spawning crashes KSP when
    /// the ghost is inside the physics bubble.
    /// </summary>
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
            if (_remoteStates.Count == 0) return;
            if (!_stylesInit) InitStyles();

            DrawHudPanel();

            Camera cam = MapView.MapIsEnabled
                ? PlanetariumCamera.Camera
                : Camera.main;
            if (cam != null)
                DrawWorldLabels(cam);
        }

        // ── always-visible panel (top-right corner) ──────────────────────────

        private void DrawHudPanel()
        {
            float panelW = 220f;
            float rowH   = 38f;
            float panelH = _remoteStates.Count * rowH + 6f;
            float x      = Screen.width - panelW - 6f;
            float y      = 40f;

            GUI.BeginGroup(new Rect(x, y, panelW, panelH));
            float ry = 3f;
            foreach (var state in _remoteStates.Values)
            {
                double dist = ApproxDistance(state);
                string distStr = double.IsNaN(dist) ? "?" : FormatDist(dist);
                string text = $"[{state.PlayerName}]  {distStr}\n{state.VesselName}";
                GUI.Label(new Rect(4, ry, panelW - 8, rowH - 2), text, _hudStyle);
                ry += rowH;
            }
            GUI.EndGroup();
        }

        // ── world-space labels projected onto the screen ──────────────────────

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

        // ── helpers ──────────────────────────────────────────────────────────

        private static double ApproxDistance(VesselState remote)
        {
            Vessel active = FlightGlobals.ActiveVessel;
            if (active == null) return double.NaN;

            double dLat = remote.Latitude  - active.latitude;
            double dLon = remote.Longitude - active.longitude;
            double dAlt = remote.AltitudeASL - active.altitude;
            return Math.Sqrt(dLat * dLat + dLon * dLon) * 111_000.0 + Math.Abs(dAlt);
        }

        private static string FormatDist(double m)
        {
            if (m < 1000) return $"{m:F0} m";
            return $"{m / 1000:F1} km";
        }

        private static CelestialBody FindBody(string name)
        {
            foreach (var b in FlightGlobals.Bodies)
                if (b.bodyName == name) return b;
            return null;
        }

        private void InitStyles()
        {
            _hudStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize  = 11,
                alignment = TextAnchor.MiddleLeft,
            };
            _hudStyle.normal.textColor = Color.cyan;

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize  = 12,
                alignment = TextAnchor.MiddleCenter,
            };
            _labelStyle.normal.textColor = Color.cyan;

            _stylesInit = true;
        }
    }
}
