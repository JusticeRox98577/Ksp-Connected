// Minimal KSP 1.12 API stubs for CI builds — compile-time only, never shipped.
using System;
using System.Collections.Generic;
using UnityEngine;

// ── Attributes ──────────────────────────────────────────────────────────────

[AttributeUsage(AttributeTargets.Class)]
public class KSPAddon : Attribute
{
    public enum Startup
    {
        Instantly, MainMenu, SpaceCentre, Flight, TrackingStation,
        EveryScene, PSystem, TimewarpTransition
    }

    public KSPAddon(Startup startup, bool once) { }
}

// ── Action groups ────────────────────────────────────────────────────────────

public enum KSPActionGroup { None, SAS, RCS, Light, Gear, Brakes, Abort, Custom01 }

public enum Vessel_Situations
{
    PRELAUNCH = 0, ESCAPING = 1, SUB_ORBITAL = 2, ORBITING = 3,
    FLYING = 4, LANDED = 5, SPLASHED = 6
}

// ── ConfigNode ───────────────────────────────────────────────────────────────

public class ConfigNode
{
    public string name { get; set; }
    public ConfigNode(string name = "") { this.name = name; }
    public static ConfigNode Parse(string text) => new ConfigNode();
    public void SetValue(string key, string value, bool createIfNotFound = false) { }
    public string GetValue(string key) => null;
    public override string ToString() => "";
    public void Save(ConfigNode node) { }
}

// ── Orbit ─────────────────────────────────────────────────────────────────────

public class Orbit
{
    public double semiMajorAxis        { get; set; }
    public double eccentricity         { get; set; }
    public double inclination          { get; set; }
    public double LAN                  { get; set; }
    public double argumentOfPeriapsis  { get; set; }
    public double meanAnomalyAtEpoch   { get; set; }
    public double epoch                { get; set; }
    public CelestialBody referenceBody { get; set; }

    public void SetOrbit(double inc, double ecc, double sma, double lan,
                         double argPe, double mEpoch, double epoch,
                         CelestialBody body) { }
}

// ── OrbitDriver ───────────────────────────────────────────────────────────────

public class OrbitDriver
{
    public Orbit orbit { get; } = new Orbit();
    public void UpdateOrbit() { }
}

// ── CelestialBody ─────────────────────────────────────────────────────────────

public class CelestialBody
{
    public string bodyName { get; set; } = "";
    public Vector3d GetWorldSurfacePosition(double lat, double lon, double alt) => new Vector3d();
}

// ── DiscoveryInfo ─────────────────────────────────────────────────────────────

[Flags]
public enum DiscoveryLevels { None = 0, Presence = 1, Name = 2, StateChange = 4, Owned = 255 }

public class DiscoveryInfo
{
    public void SetLevel(DiscoveryLevels level) { }
}

// ── FlightCtrlState ───────────────────────────────────────────────────────────

public class FlightCtrlState
{
    public float mainThrottle { get; set; }
}

// ── VesselActionGroups ────────────────────────────────────────────────────────

public class VesselActionGroups
{
    public bool this[KSPActionGroup group] => false;
}

// ── Vessel ────────────────────────────────────────────────────────────────────

public class Vessel : UnityEngine.Object
{
    public Guid             id             { get; set; } = Guid.NewGuid();
    public string           vesselName     { get; set; } = "";
    public CelestialBody    mainBody       { get; set; } = new CelestialBody();
    public double           latitude       { get; set; }
    public double           longitude      { get; set; }
    public double           altitude       { get; set; }
    public byte             situation      { get; set; }
    public FlightCtrlState  ctrlState      { get; set; } = new FlightCtrlState();
    public VesselActionGroups ActionGroups { get; }      = new VesselActionGroups();
    public Orbit            orbit          { get; set; } = new Orbit();
    public OrbitDriver      orbitDriver    { get; set; } = new OrbitDriver();
    public bool             packed         { get; set; }
    public DiscoveryInfo    DiscoveryInfo  { get; }      = new DiscoveryInfo();
    public Transform        transform      { get; }      = new Transform();

    public ProtoVessel  BackupVessel()    => new ProtoVessel();
    public void         Die()             { }
    public void         SetPosition(Vector3d pos) { }
    public void         SetWorldVelocity(Vector3d vel) { }
    public Vector3d     GetWorldVelocity() => new Vector3d();
    public Vector3d     GetWorldPos3D()    => new Vector3d();
}

// ── ProtoVessel ───────────────────────────────────────────────────────────────

public class ProtoVessel
{
    public Guid vesselID { get; set; } = Guid.NewGuid();
    public ProtoVessel() { }
    public ProtoVessel(ConfigNode node, Game game) { }
    public void Load(FlightState state) { }
    public void Save(ConfigNode node) { }
}

// ── FlightState ───────────────────────────────────────────────────────────────

public class FlightState { }

// ── Game ──────────────────────────────────────────────────────────────────────

public class Game
{
    public FlightState flightState { get; } = new FlightState();
}

// ── HighLogic ─────────────────────────────────────────────────────────────────

public static class HighLogic
{
    public static Game CurrentGame { get; } = new Game();
}

// ── FlightGlobals ─────────────────────────────────────────────────────────────

public static class FlightGlobals
{
    public static Vessel              ActiveVessel { get; } = null;
    public static List<Vessel>        Vessels      { get; } = new List<Vessel>();
    public static List<CelestialBody> Bodies       { get; } = new List<CelestialBody>();
}

// ── MapView ───────────────────────────────────────────────────────────────────

public static class MapView
{
    public static bool MapIsEnabled { get; } = false;
}

// ── Planetarium ───────────────────────────────────────────────────────────────

public static class Planetarium
{
    public static double GetUniversalTime() => 0.0;
}

// ── ScaledSpace ───────────────────────────────────────────────────────────────

public static class ScaledSpace
{
    public static Vector3 LocalToScaledSpace(Vector3d worldPos) => Vector3.zero;
}

// ── PlanetariumCamera ─────────────────────────────────────────────────────────

public static class PlanetariumCamera
{
    public static Camera Camera { get; } = null;
}
