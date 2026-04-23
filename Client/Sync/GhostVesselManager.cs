using System;
using System.Collections.Generic;
using KspConnected.Client.Util;
using KspConnected.Shared.Data;
using KspConnected.Shared.Messages;
using UnityEngine;

namespace KspConnected.Client.Sync
{
    /// <summary>
    /// Manages real KSP Vessel objects for remote players.
    ///
    /// Lifecycle:
    ///   1. VesselConfigMessage received  → spawn a ProtoVessel from the
    ///      ConfigNode; the vessel appears in the game world as a real object.
    ///   2. VesselUpdate received         → reposition the vessel using orbital
    ///      elements (on-rails) or direct world position (off-rails / nearby).
    ///   3. Player disconnects            → vessel is removed from the world.
    ///
    /// Collision is handled automatically because the ghost vessel is a real
    /// KSP physics object; no special handling is needed.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class GhostVesselManager : MonoBehaviour
    {
        public static GhostVesselManager Instance { get; private set; }

        // playerId → KSP Vessel object
        private readonly Dictionary<int, Vessel> _ghostVessels = new Dictionary<int, Vessel>();

        // playerId → Guid string of the vessel config we've already loaded
        private readonly Dictionary<int, string> _loadedConfigs = new Dictionary<int, string>();

        // Physics bubble: vessels within this distance get position/velocity set
        // directly; beyond this they are moved by setting orbital elements.
        private const double PhysicsBubbleRadius = 2500.0; // metres

        private void Awake()
        {
            Instance = this;
            // Process any VesselConfig messages that arrived before this scene loaded
            Core.KspConnectedMod.Instance?.DrainPendingConfigs(this);
        }

        private void OnDestroy()
        {
            Instance = null;
            RemoveAll();
        }

        // ── called from the main thread (via KspConnectedMod) ────────────────

        public void OnVesselConfig(VesselConfigMessage msg)
        {
            if (msg.PlayerId == Core.KspConnectedMod.Instance?.Connection.LocalPlayerId)
                return; // ignore our own config echo

            // If we already loaded this exact vessel, skip re-spawn
            if (_loadedConfigs.TryGetValue(msg.PlayerId, out string existingId)
                && existingId == msg.VesselId)
                return;

            RemoveGhost(msg.PlayerId);
            SpawnGhostVessel(msg);
        }

        public void OnVesselUpdate(VesselState state)
        {
            if (!_ghostVessels.TryGetValue(state.PlayerId, out Vessel vessel)) return;
            if (vessel == null) { _ghostVessels.Remove(state.PlayerId); return; }

            UpdateGhostPosition(vessel, state);
        }

        public void RemoveGhost(int playerId)
        {
            if (_ghostVessels.TryGetValue(playerId, out Vessel vessel) && vessel != null)
            {
                try { vessel.Die(); }
                catch (Exception ex) { KspLog.Warn("GhostVesselManager.RemoveGhost: " + ex.Message); }
            }
            _ghostVessels.Remove(playerId);
            _loadedConfigs.Remove(playerId);
        }

        public void RemoveAll()
        {
            foreach (var id in new List<int>(_ghostVessels.Keys))
                RemoveGhost(id);
        }

        // ── spawning ─────────────────────────────────────────────────────────

        private void SpawnGhostVessel(VesselConfigMessage msg)
        {
            try
            {
                string configText = VesselConfigMessage.Decompress(msg.ConfigData);
                ConfigNode node   = ConfigNode.Parse(configText);
                if (node == null)
                {
                    KspLog.Warn($"GhostVesselManager: failed to parse ConfigNode for player {msg.PlayerId}");
                    return;
                }

                // KSP requires a game state to load a ProtoVessel
                Game game = HighLogic.CurrentGame;
                if (game == null) return;

                // Give the ghost vessel a unique ID to prevent save conflicts
                ReplaceVesselId(node, msg.PlayerId);

                var proto = new ProtoVessel(node, game);
                proto.Load(game.flightState);

                // Find the vessel that was just spawned
                Vessel spawned = FindVesselByProto(proto);
                if (spawned == null)
                {
                    KspLog.Warn($"GhostVesselManager: ProtoVessel loaded but Vessel not found for player {msg.PlayerId}");
                    return;
                }

                // Tag the vessel so we know it's a ghost
                spawned.vesselName = $"[{GetPlayerName(msg.PlayerId)}] {spawned.vesselName}";
                spawned.DiscoveryInfo.SetLevel(DiscoveryLevels.Owned);

                _ghostVessels[msg.PlayerId]  = spawned;
                _loadedConfigs[msg.PlayerId] = msg.VesselId;

                KspLog.Log($"Ghost vessel spawned for player #{msg.PlayerId}: {spawned.vesselName}");
            }
            catch (Exception ex)
            {
                KspLog.Error($"GhostVesselManager.SpawnGhostVessel (player {msg.PlayerId}): {ex}");
            }
        }

        private static void ReplaceVesselId(ConfigNode node, int playerId)
        {
            // Give it a deterministic but unique Guid so it doesn't collide
            string newId = new Guid(playerId, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0).ToString();
            node.SetValue("pid",  newId, true);
            node.SetValue("name", node.GetValue("name") ?? "Ghost", true);
        }

        private static Vessel FindVesselByProto(ProtoVessel proto)
        {
            foreach (Vessel v in FlightGlobals.Vessels)
            {
                if (v.id == proto.vesselID) return v;
            }
            return null;
        }

        // ── repositioning ─────────────────────────────────────────────────────

        private void UpdateGhostPosition(Vessel vessel, VesselState state)
        {
            try
            {
                CelestialBody body = FindBody(state.BodyName);
                if (body == null) return;

                // Switch body if needed
                if (vessel.mainBody != body)
                    vessel.orbitDriver.orbit.referenceBody = body;

                double distToUs = double.MaxValue;
                Vessel active   = FlightGlobals.ActiveVessel;
                if (active != null)
                    distToUs = Vector3d.Distance(
                        body.GetWorldSurfacePosition(state.Latitude, state.Longitude, state.AltitudeASL),
                        active.GetWorldPos3D());

                if (distToUs < PhysicsBubbleRadius && !vessel.packed)
                {
                    // ── off-rails: set position + velocity directly ──────────
                    Vector3d worldPos = body.GetWorldSurfacePosition(
                        state.Latitude, state.Longitude, state.AltitudeASL);
                    vessel.SetPosition(worldPos);
                    vessel.SetWorldVelocity(new Vector3d(state.VelX, state.VelY, state.VelZ));

                    // Apply attitude
                    vessel.transform.rotation = new Quaternion(
                        state.RotX, state.RotY, state.RotZ, state.RotW);
                }
                else
                {
                    // ── on-rails: set orbital elements ───────────────────────
                    if (state.SemiMajorAxis > 0)
                    {
                        vessel.orbitDriver.orbit.SetOrbit(
                            state.Inclination,
                            state.Eccentricity,
                            state.SemiMajorAxis,
                            state.LAN,
                            state.ArgumentOfPeriapsis,
                            state.MeanAnomalyAtEpoch,
                            state.Epoch,
                            body);
                        vessel.orbitDriver.UpdateOrbit();
                    }
                    else
                    {
                        // Landed / splashed — position on surface
                        Vector3d surfPos = body.GetWorldSurfacePosition(
                            state.Latitude, state.Longitude, state.AltitudeASL);
                        vessel.SetPosition(surfPos);
                    }
                }
            }
            catch (Exception ex)
            {
                KspLog.Error($"UpdateGhostPosition (player {state.PlayerId}): {ex.Message}");
            }
        }

        private static CelestialBody FindBody(string name)
        {
            foreach (var b in FlightGlobals.Bodies)
                if (b.bodyName == name) return b;
            return null;
        }

        private static string GetPlayerName(int playerId)
        {
            var store = Core.KspConnectedMod.Instance?.Players.GetSnapshot();
            if (store != null)
                foreach (var p in store)
                    if (p.PlayerId == playerId) return p.PlayerName;
            return $"P{playerId}";
        }
    }
}
