using System.Collections.Generic;
using KspConnected.Client.Util;
using KspConnected.Shared.Data;
using KspConnected.Shared.Messages;
using UnityEngine;

namespace KspConnected.Client.Sync
{
    /// <summary>
    /// Draws a label at each remote player's world position.
    /// Avoids spawning real ProtoVessels, which crash KSP when the ghost is
    /// inside the physics bubble (e.g. both rovers on the same runway).
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class GhostVesselManager : MonoBehaviour
    {
        public static GhostVesselManager Instance { get; private set; }

        private readonly Dictionary<int, VesselState> _remoteStates = new Dictionary<int, VesselState>();
        private GUIStyle _labelStyle;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            Instance = null;
            _remoteStates.Clear();
        }

        // ── called from the main thread via KspConnectedMod ─────────────────

        public void OnVesselConfig(VesselConfigMessage msg) { }  // position comes from VesselUpdate

        public void OnVesselUpdate(VesselState state)
        {
            int myId = Core.KspConnectedMod.Instance?.Connection.LocalPlayerId ?? -1;
            if (state.PlayerId == myId) return;
            _remoteStates[state.PlayerId] = state;
        }

        public void RemoveGhost(int playerId) => _remoteStates.Remove(playerId);

        public void RemoveAll() => _remoteStates.Clear();

        // ── rendering ────────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (_remoteStates.Count == 0) return;

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize  = 12,
                    alignment = TextAnchor.MiddleCenter,
                };
                _labelStyle.normal.textColor = Color.cyan;
            }

            bool inMap = MapView.MapIsEnabled;
            Camera cam = inMap ? PlanetariumCamera.Camera : Camera.main;
            if (cam == null) return;

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
                if (screen.z <= 0) continue;  // behind camera

                float sx = screen.x;
                float sy = Screen.height - screen.y;
                string label = $"▲ [{state.PlayerName}]\n{state.VesselName}";
                GUI.Label(new Rect(sx - 75, sy - 30, 150, 40), label, _labelStyle);
            }
        }

        private static CelestialBody FindBody(string name)
        {
            foreach (var b in FlightGlobals.Bodies)
                if (b.bodyName == name) return b;
            return null;
        }
    }
}
