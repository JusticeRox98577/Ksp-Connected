# KSP-Connected

Cross-PC multiplayer mod for **Kerbal Space Program 1.12**. Connects multiple players through a self-hosted server so you can explore the Kerbol system together.

## Features

- **Real-time vessel sync** — other players' orbital elements, position, velocity, and attitude are broadcast to all clients
- **Map overlay** — see connected players' vessel positions on the KSP map view as yellow markers
- **Flight HUD** — compact panel showing each player's vessel name, body, situation, and altitude
- **In-game chat** — text chat window available in flight and the Space Center
- **Time synchronisation** — keeps universal time drift between clients to a minimum using NTP-style round-trip measurement
- **Self-hosted server** — run the server on any PC or VPS; no third-party services required
- **Up to 16 players** by default (configurable)

---

## Requirements

| Component | Requirement |
|-----------|-------------|
| KSP | 1.10 – 1.12.x |
| .NET SDK | 6.0+ (for building) |
| Server OS | Windows / Linux / macOS |

---

## Project Structure

```
KSP-Connected/
├── Shared/                     # Protocol + data types (.NET Standard 2.0)
│   ├── Protocol/               # Wire framing, message types, constants
│   ├── Messages/               # Hello, VesselUpdate, Chat, TimeSync, …
│   └── Data/                   # VesselState data class
├── Client/                     # KSP plugin (.NET 4.6 / Unity 2019.4)
│   ├── Core/                   # KspConnectedMod (singleton), ConnectionManager
│   ├── Sync/                   # VesselSyncSender, RemoteVesselStore
│   ├── Time/                   # TimeSyncManager
│   └── UI/                     # ConnectWindow, ChatWindow, FlightHud, MapOverlay
├── Server/                     # Standalone server (.NET 6)
│   ├── Core/                   # KspServer, ClientSession, PlayerRegistry
│   ├── Handlers/               # Per-message handlers
│   └── Config/                 # ServerConfig (server.json)
├── GameData/KspConnected/      # Drop this folder into your KSP GameData/
│   └── Plugins/                # Compiled DLLs go here (built automatically)
├── build.sh                    # Linux/macOS build script
├── build.bat                   # Windows build script
└── server.json                 # Server configuration
```

---

## Building

### Prerequisites

1. Install the [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
2. Have KSP 1.12 installed

### Linux / macOS

```bash
# Point to your KSP install (defaults to Steam on Linux)
export KSP_PATH="$HOME/.steam/steam/steamapps/common/Kerbal Space Program"

chmod +x build.sh
./build.sh          # builds both client plugin and server
```

### Windows

```bat
set KSP_PATH=C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program
build.bat
```

### Manual (dotnet CLI)

```bash
# Build the shared library first
dotnet build Shared/KspConnected.Shared.csproj -c Release

# Build the server
dotnet build Server/KspConnected.Server.csproj -c Release

# Build the KSP plugin (requires KSP assemblies)
dotnet build Client/KspConnected.Client.csproj -c Release -p:KspPath="/path/to/KSP"
```

After building, `GameData/KspConnected/Plugins/` will contain the compiled DLLs.

---

## Installation

1. **Build** the project (see above) or download a release.
2. Copy `GameData/KspConnected/` into your KSP installation's `GameData/` folder:

```
<KSP install>/
└── GameData/
    └── KspConnected/       ← copy this whole folder here
        └── Plugins/
            ├── KspConnected.Client.dll
            └── KspConnected.Shared.dll
```

3. Launch KSP — no further configuration needed on the client side.

---

## Running the Server

The server is a cross-platform .NET console application. One player (or a VPS) runs it and shares their IP with others.

### Using dotnet

```bash
dotnet Server/bin/Release/net6.0/KspConnected.Server.dll --port 7654
```

### Published single-file binary (Linux)

```bash
./build.sh server publish
./dist/server/KspConnected.Server --port 7654 --max-players 8
```

### CLI options

| Option | Default | Description |
|--------|---------|-------------|
| `--port` | 7654 | TCP port to listen on |
| `--max-players` | 16 | Maximum concurrent players |
| `--name` | (from server.json) | Server display name |

### server.json

Edit `server.json` to configure the server persistently:

```json
{
  "Port": 7654,
  "MaxPlayers": 16,
  "ServerName": "My KSP Server",
  "WelcomeMessage": "Welcome! Have fun exploring the Kerbol system."
}
```

### Port forwarding

If the server is behind a home router, forward **TCP port 7654** to the server machine. Players connect using your public IP address.

---

## Connecting In-Game

1. Launch KSP and load a save (or go to the Space Center).
2. The **KSP-Connected** window appears in the top-left corner.
3. Enter:
   - **Name** — your in-game display name
   - **Host** — the server's IP address (e.g. `192.168.1.10` or a public IP)
   - **Port** — default `7654`
4. Click **Connect**.
5. Once connected you will see:
   - Other players listed in the **Flight HUD** (top-right during flight)
   - Yellow markers on the **Map View** at remote vessels' surface positions
   - The **Chat** window (bottom-left during flight)

---

## Network Protocol

All communication is TCP. Messages are framed as:

```
[4 bytes: payload length (LE int32)] [1 byte: message type] [N bytes: payload]
```

| Type byte | Name | Direction | Description |
|-----------|------|-----------|-------------|
| 0x01 | Hello | C→S | Player name + protocol version |
| 0x02 | HelloAck | S→C | Assigned player ID, accept/reject |
| 0x03 | PlayerList | S→C | Full list of connected players |
| 0x04 | VesselUpdate | Both | Keplerian + surface state snapshot |
| 0x05 | Chat | Both | Sender ID, name, text |
| 0x06 | TimeSync | C→S | Client UT + tick timestamp |
| 0x07 | TimeSyncReply | S→C | Echoed client data + server UT |
| 0x08 | Disconnect | Both | Graceful disconnect with reason |
| 0x09 | Ping | Either | Keepalive |
| 0x0A | Pong | Either | Keepalive reply |

---

## Vessel Synchronisation

Vessels are synchronised using **Keplerian orbital elements** (SMA, eccentricity, inclination, LAN, argument of periapsis, mean anomaly at epoch). This means:

- **Very low bandwidth** — a vessel in a stable orbit only needs to send updates when thrust, staging, or SoI changes occur.
- **Threshold-based sending** — the client only sends an update when position changes by >100 m, velocity changes by >1 m/s, throttle changes, or every 2 seconds as a heartbeat.
- Other players are shown on the **map view** via projected surface-position markers.

> **Note:** Full physics-bubble vessel spawning (seeing other vessels as actual KSP objects in the 2.5 km physics bubble) is a planned future feature. Currently other players appear as HUD entries and map markers.

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Cannot connect | Check the server is running, port is open/forwarded, no firewall blocking TCP 7654 |
| Protocol mismatch error | Ensure client and server are built from the same source version |
| KSP assemblies not found | Set `KSP_PATH` to your KSP installation directory |
| No map markers visible | Open the map view (M) and ensure you are connected |
| Build fails on `Assembly-CSharp.dll` | Verify KSP is installed at `KSP_PATH` and that the managed folder exists |

---

## Architecture Notes

- **ThreadDispatcher** — the network receive loop runs on a background thread. All callbacks are marshalled back to Unity's main thread via a thread-safe `Action` queue drained in `Update()`.
- **RemoteVesselStore** — a `ConcurrentDictionary` holding the latest `VesselState` per player. UI components read it without locking.
- **TimeSyncManager** — NTP-style two-packet exchange every 15 seconds. Maintains a rolling 8-sample average of UT offset to smooth jitter.

---

## Licence

MIT — see [LICENCE](LICENCE) for details.
