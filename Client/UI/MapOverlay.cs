using System;
using KspConnected.Client.Core;
using KspConnected.Shared.Data;
using UnityEngine;

namespace KspConnected.Client.UI
{
    /// <summary>
    /// Draws player name labels on the KSP map view next to each ghost vessel.
    /// For players whose ghost vessel hasn't spawned yet, also draws a fallback
    /// position marker projected from their last known surface coordinates.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class MapOverlay : MonoBehaviour
    {
        private GUIStyle _labelStyle;
        private Texture2D _markerTex;

        private void Start()
        {
            // Simple 8x8 yellow marker texture
            _markerTex = new Texture2D(8, 8);
            Color yellow = Color.yellow;
            for (int x = 0; x < 8; x++)
                for (int y = 0; y < 8; y++)
                    _markerTex.SetPixel(x, y, yellow);
            _markerTex.Apply();
        }

        private void OnDestroy()
        {
            if (_markerTex != null) Destroy(_markerTex);
        }

        private void OnGUI()
        {
            if (!MapView.MapIsEnabled) return;
            var mod = KspConnectedMod.Instance;
            if (mod == null || mod.Connection.State != ConnectionState.Connected) return;

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = Color.yellow },
                    fontStyle = FontStyle.Bold,
                    fontSize  = 11,
                };
            }

            Camera mapCam = PlanetariumCamera.Camera;
            if (mapCam == null) return;

            foreach (var state in mod.VesselStore.All())
            {
                Vector3? screenPos = WorldToMapScreen(state, mapCam);
                if (screenPos == null) continue;

                Vector3 sp = screenPos.Value;
                // Flip Y: Unity screen y=0 is bottom; GUI y=0 is top
                sp.y = Screen.height - sp.y;

                if (sp.z < 0) continue; // behind camera

                // Draw marker dot
                GUI.DrawTexture(new Rect(sp.x - 4, sp.y - 4, 8, 8), _markerTex);
                // Draw label
                GUI.Label(new Rect(sp.x + 6, sp.y - 8, 200, 20),
                          $"{state.PlayerName} ({state.BodyName})", _labelStyle);
            }
        }

        private Vector3? WorldToMapScreen(VesselState state, Camera cam)
        {
            try
            {
                CelestialBody body = FindBody(state.BodyName);
                if (body == null) return null;

                // Compute world position from surface coords
                Vector3d worldPos = body.GetWorldSurfacePosition(
                    state.Latitude, state.Longitude, state.AltitudeASL);

                // Convert to ScaledSpace for map view
                Vector3 scaledPos = ScaledSpace.LocalToScaledSpace(worldPos);
                Vector3 screenPos = cam.WorldToScreenPoint(scaledPos);
                return screenPos;
            }
            catch
            {
                return null;
            }
        }

        private CelestialBody FindBody(string name)
        {
            foreach (var body in FlightGlobals.Bodies)
                if (body.bodyName == name) return body;
            return null;
        }
    }
}
