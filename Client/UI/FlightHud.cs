using System;
using KspConnected.Client.Core;
using UnityEngine;

namespace KspConnected.Client.UI
{
    /// <summary>
    /// HUD overlay during flight scene showing connected players and their
    /// vessel locations. Drawn as a small panel in the top-right corner.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FlightHud : MonoBehaviour
    {
        private Rect _rect = new Rect(Screen.width - 260, 40, 250, 0);

        private static readonly string[] SituationNames =
        {
            "Pre-launch", "Escaping", "SUB-ORBITAL", "Orbiting",
            "Flying", "Landed", "Splashed", "Flying",
        };

        private void OnGUI()
        {
            var mod = KspConnectedMod.Instance;
            if (mod == null || mod.Connection.State != ConnectionState.Connected) return;
            if (mod.Players.Count == 0) return;

            GUILayout.BeginArea(_rect);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label($"Players online: {mod.Players.Count}");

            foreach (var state in mod.VesselStore.All())
            {
                string situation = state.Situation < SituationNames.Length
                    ? SituationNames[state.Situation] : "Unknown";

                GUILayout.Space(4);
                GUILayout.Label($"<b>{state.PlayerName}</b>");
                GUILayout.Label($"  {state.VesselName}");
                GUILayout.Label($"  {state.BodyName} — {situation}");
                GUILayout.Label($"  Alt: {FormatAlt(state.AltitudeASL)}");

                double utDelta = state.UniversalTime - Planetarium.GetUniversalTime();
                if (Math.Abs(utDelta) > 5)
                    GUILayout.Label($"  UT offset: {utDelta:+0.0;-0.0} s");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private string FormatAlt(double alt)
        {
            if (alt >= 1e6) return $"{alt / 1e6:F2} Mm";
            if (alt >= 1e3) return $"{alt / 1e3:F1} km";
            return $"{alt:F0} m";
        }
    }
}
