using System;
using KspConnected.Client.Core;
using KspConnected.Client.Util;
using KspConnected.Shared.Data;
using KspConnected.Shared.Messages;
using UnityEngine;

namespace KspConnected.Client.Sync
{
    /// <summary>
    /// Runs in the Flight scene. Samples the active vessel and sends
    /// VesselUpdateMessage to the server when significant changes occur
    /// or the periodic heartbeat timer fires.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class VesselSyncSender : MonoBehaviour
    {
        private const float HeartbeatInterval = 2f;   // seconds — send even if no change
        private const double PositionDeltaThreshold = 100.0;   // metres
        private const double VelocityDeltaThreshold = 1.0;     // m/s

        private VesselState _lastSent;
        private float       _heartbeatTimer;

        private void Update()
        {
            var mod = KspConnectedMod.Instance;
            if (mod == null || mod.Connection.State != ConnectionState.Connected) return;

            _heartbeatTimer += UnityEngine.Time.deltaTime;
            bool forceHeartbeat = _heartbeatTimer >= HeartbeatInterval;

            Vessel active = FlightGlobals.ActiveVessel;
            if (active == null) return;

            VesselState state = SnapshotVessel(active, mod.Connection.LocalPlayerId, mod.PlayerName);
            if (state == null) return;

            if (forceHeartbeat || ShouldSend(state))
            {
                mod.Connection.SendVesselUpdate(new VesselUpdateMessage { State = state });
                _lastSent = state;
                if (forceHeartbeat) _heartbeatTimer = 0f;
            }
        }

        private bool ShouldSend(VesselState current)
        {
            if (_lastSent == null) return true;

            // Check orbital / surface position delta
            double dLat = current.Latitude  - _lastSent.Latitude;
            double dLon = current.Longitude - _lastSent.Longitude;
            double dAlt = current.AltitudeASL - _lastSent.AltitudeASL;
            // Rough surface distance
            double approxDist = Math.Sqrt(dLat * dLat + dLon * dLon) * 111000 + Math.Abs(dAlt);

            double dVx = current.VelX - _lastSent.VelX;
            double dVy = current.VelY - _lastSent.VelY;
            double dVz = current.VelZ - _lastSent.VelZ;
            double dV  = Math.Sqrt(dVx * dVx + dVy * dVy + dVz * dVz);

            return approxDist > PositionDeltaThreshold || dV > VelocityDeltaThreshold
                   || current.Throttle != _lastSent.Throttle
                   || current.SASEnabled != _lastSent.SASEnabled
                   || current.RCSEnabled != _lastSent.RCSEnabled
                   || current.Situation  != _lastSent.Situation;
        }

        private VesselState SnapshotVessel(Vessel v, int playerId, string playerName)
        {
            try
            {
                var state = new VesselState
                {
                    VesselId   = v.id,
                    VesselName = v.vesselName,
                    PlayerId   = playerId,
                    PlayerName = playerName,
                    BodyName   = v.mainBody?.bodyName ?? "",
                    Latitude   = v.latitude,
                    Longitude  = v.longitude,
                    AltitudeASL = v.altitude,
                    Situation  = (byte)v.situation,
                    Throttle   = v.ctrlState != null ? v.ctrlState.mainThrottle : 0f,
                    SASEnabled = v.ActionGroups[KSPActionGroup.SAS],
                    RCSEnabled = v.ActionGroups[KSPActionGroup.RCS],
                    UniversalTime = Planetarium.GetUniversalTime(),
                };

                // Velocity in world space
                Vector3d vel = v.GetWorldVelocity();
                state.VelX = vel.x;
                state.VelY = vel.y;
                state.VelZ = vel.z;

                // Attitude quaternion
                Quaternion rot = v.transform.rotation;
                state.RotX = rot.x;
                state.RotY = rot.y;
                state.RotZ = rot.z;
                state.RotW = rot.w;

                // Keplerian elements
                if (v.orbit != null)
                {
                    state.SemiMajorAxis       = v.orbit.semiMajorAxis;
                    state.Eccentricity        = v.orbit.eccentricity;
                    state.Inclination         = v.orbit.inclination;
                    state.LAN                 = v.orbit.LAN;
                    state.ArgumentOfPeriapsis = v.orbit.argumentOfPeriapsis;
                    state.MeanAnomalyAtEpoch  = v.orbit.meanAnomalyAtEpoch;
                    state.Epoch               = v.orbit.epoch;
                }

                return state;
            }
            catch (Exception ex)
            {
                Logger.Error("SnapshotVessel: " + ex.Message);
                return null;
            }
        }
    }
}
