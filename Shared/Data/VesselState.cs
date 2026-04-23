using System;
using System.IO;

namespace KspConnected.Shared.Data
{
    /// <summary>
    /// Complete snapshot of a vessel used for multiplayer synchronisation.
    /// Orbital elements are Keplerian and defined relative to the parent body
    /// at a reference universal time (Epoch).
    /// </summary>
    public class VesselState
    {
        // Identity
        public Guid   VesselId;
        public string VesselName = "";
        public int    PlayerId;
        public string PlayerName = "";

        // Celestial body
        public string BodyName = "";

        // Keplerian elements (used when not landed)
        public double SemiMajorAxis;
        public double Eccentricity;
        public double Inclination;
        public double LAN;              // Longitude of ascending node (degrees)
        public double ArgumentOfPeriapsis;
        public double MeanAnomalyAtEpoch;
        public double Epoch;            // Universal time at which the elements apply

        // Surface / landed state (used when situation is Landed or Splashed)
        public double Latitude;
        public double Longitude;
        public double AltitudeASL;

        // Velocity in world-space (m/s)
        public double VelX;
        public double VelY;
        public double VelZ;

        // Attitude (quaternion, world-space)
        public float RotX;
        public float RotY;
        public float RotZ;
        public float RotW;

        // Flight status
        public byte  Situation;     // cast of Vessel.Situations enum value
        public float Throttle;      // 0..1
        public bool  SASEnabled;
        public bool  RCSEnabled;

        // Universal time at which snapshot was taken
        public double UniversalTime;

        public void Serialize(BinaryWriter w)
        {
            w.Write(VesselId.ToByteArray());
            w.Write(VesselName);
            w.Write(PlayerId);
            w.Write(PlayerName);
            w.Write(BodyName);
            w.Write(SemiMajorAxis);
            w.Write(Eccentricity);
            w.Write(Inclination);
            w.Write(LAN);
            w.Write(ArgumentOfPeriapsis);
            w.Write(MeanAnomalyAtEpoch);
            w.Write(Epoch);
            w.Write(Latitude);
            w.Write(Longitude);
            w.Write(AltitudeASL);
            w.Write(VelX);
            w.Write(VelY);
            w.Write(VelZ);
            w.Write(RotX);
            w.Write(RotY);
            w.Write(RotZ);
            w.Write(RotW);
            w.Write(Situation);
            w.Write(Throttle);
            w.Write(SASEnabled);
            w.Write(RCSEnabled);
            w.Write(UniversalTime);
        }

        public static VesselState Deserialize(BinaryReader r)
        {
            return new VesselState
            {
                VesselId              = new Guid(r.ReadBytes(16)),
                VesselName            = r.ReadString(),
                PlayerId              = r.ReadInt32(),
                PlayerName            = r.ReadString(),
                BodyName              = r.ReadString(),
                SemiMajorAxis         = r.ReadDouble(),
                Eccentricity          = r.ReadDouble(),
                Inclination           = r.ReadDouble(),
                LAN                   = r.ReadDouble(),
                ArgumentOfPeriapsis   = r.ReadDouble(),
                MeanAnomalyAtEpoch    = r.ReadDouble(),
                Epoch                 = r.ReadDouble(),
                Latitude              = r.ReadDouble(),
                Longitude             = r.ReadDouble(),
                AltitudeASL           = r.ReadDouble(),
                VelX                  = r.ReadDouble(),
                VelY                  = r.ReadDouble(),
                VelZ                  = r.ReadDouble(),
                RotX                  = r.ReadSingle(),
                RotY                  = r.ReadSingle(),
                RotZ                  = r.ReadSingle(),
                RotW                  = r.ReadSingle(),
                Situation             = r.ReadByte(),
                Throttle              = r.ReadSingle(),
                SASEnabled            = r.ReadBoolean(),
                RCSEnabled            = r.ReadBoolean(),
                UniversalTime         = r.ReadDouble(),
            };
        }
    }
}
