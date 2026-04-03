# CyberClub – PC-Club Management System (MVP)

A **Senet-style** PC-club management system built with:

- **Server**: ASP.NET Core 8, SignalR, MySQL (via Pomelo EF Core)
- **Agent**: Windows Service + WPF Locker UI (.NET 8, Windows)

---

## Repository Structure

```
/server      – Core ASP.NET Core Web API
/agent
  /CyberAgent.Service  – Windows Service (heartbeat, SignalR client, commands)
  /CyberAgent.UI       – WPF Locker UI (full-screen lock screen)
docker-compose.yml     – MySQL for local dev
```

---

## Quick Start (Local Dev)

### 1. Start MySQL

```bash
docker compose up -d
```

MySQL will be available at `localhost:3306` with:
- Database: `cyberclub`
- User: `cyberuser` / Password: `cyberpass`

### 2. Run the Server

```bash
cd server
dotnet run
```

On first run, EF Core will **auto-migrate** the database schema.

- Swagger UI: `http://localhost:5000/swagger`
- SignalR Hub: `http://localhost:5000/hubs/agent`

### 3. Build the Windows Agent (Windows only)

```bash
cd agent
dotnet build CyberAgent.Service/CyberAgent.Service.csproj
dotnet build CyberAgent.UI/CyberAgent.UI.csproj
```

The agent requires Windows (WPF + Windows Service + WMI).

---

## Server API Reference

### Agent Endpoints (no auth required)

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/agents/register` | Register workstation, receive `workstationId` + `secret` |
| `POST` | `/api/agents/heartbeat` | Post heartbeat with status/metrics |

**Register request:**
```json
{
  "machineFingerprint": "ABC123...",
  "workstationName": "PC-01",
  "agentVersion": "1.0.0",
  "osVersion": "Windows 11"
}
```

**Register response:**
```json
{
  "workstationId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "secret": "base64-secret",
  "workstationName": "PC-01"
}
```

**Heartbeat request:**
```json
{
  "workstationId": "xxxxxxxx-...",
  "secret": "base64-secret",
  "agentVersion": "1.0.0",
  "state": "Locked",
  "cpuUsage": 12.5,
  "ramUsageMb": 256.0
}
```

### Admin Endpoints (protected by `X-Admin-Key` header)

#### Workstations

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/admin/workstations` | List all workstations with online status |
| `GET` | `/api/admin/workstations/{id}` | Get single workstation |
| `POST` | `/api/admin/workstations/{id}/commands` | Send command to workstation |
| `GET` | `/api/admin/workstations/{id}/commands` | Get command/audit log for workstation |
| `GET` | `/api/admin/commands` | Get global command audit log |
| `PATCH` | `/api/admin/workstations/{id}/integration` | Set MeshCentralDeviceId / FogHostId / ImageGroup |
| `GET` | `/api/admin/workstations/{id}/remote-link` | Get MeshCentral remote URL for workstation |
| `POST` | `/api/admin/workstations/{id}/mark-for-reimage` | Mark workstation for reimaging (audit log only) |
| `POST` | `/api/admin/external-receipts/link` | Link an external receipt (ERPNext/POS) to a workstation/session |

**Send command request:**
```json
{
  "command": "Lock",
  "issuedBy": "admin",
  "notes": "End of session"
}
```

Commands: `Lock`, `Unlock`, `Reboot`, `Shutdown`, `Message`, `Reimage`

#### Tariff Plans

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/admin/tariffs` | List active tariff plans (add `?includeInactive=true` for all) |
| `GET` | `/api/admin/tariffs/{id}` | Get tariff plan by ID |
| `POST` | `/api/admin/tariffs` | Create new tariff plan |
| `PATCH` | `/api/admin/tariffs/{id}` | Update tariff plan |
| `DELETE` | `/api/admin/tariffs/{id}` | Deactivate tariff plan (soft delete) |

**Create hourly plan:**
```json
{
  "name": "1 Hour",
  "type": "Hourly",
  "durationMinutes": 60,
  "price": 300,
  "isActive": true,
  "sortOrder": 1
}
```

**Create monthly plan:**
```json
{
  "name": "Monthly Subscription",
  "type": "Monthly",
  "durationDays": 30,
  "price": 5000,
  "isActive": true,
  "sortOrder": 10
}
```

#### Customers

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/admin/customers` | List all active customers |
| `GET` | `/api/admin/customers/{id}` | Get customer by ID |
| `GET` | `/api/admin/customers/lookup?username=&phone=` | Lookup customer by username or phone |
| `POST` | `/api/admin/customers` | Create new customer |

**Create customer:**
```json
{
  "username": "john_doe",
  "phone": "+77771234567"
}
```

#### Sessions (Prepaid Flow)

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/admin/sessions` | List sessions (`?workstationId=&date=2026-04-03`) |
| `GET` | `/api/admin/sessions/{id}` | Get session by ID |
| `POST` | `/api/admin/sessions/start` | Sell time & start session (creates Sale + Session, unlocks PC) |
| `POST` | `/api/admin/sessions/{id}/extend` | Extend session with additional paid time |
| `POST` | `/api/admin/sessions/{id}/end` | End session early (locks PC, optionally reboots) |

**Start session (prepaid flow):**
```json
{
  "workstationId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "tariffPlanId": "yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy",
  "customerId": null,
  "guestName": "Guest",
  "amount": 300.00,
  "currency": "KZT",
  "paymentMethod": "Cash",
  "operatorName": "Cashier1"
}
```

**Response:**
```json
{
  "id": "zzzzzzzz-...",
  "workstationId": "xxxxxxxx-...",
  "customerId": null,
  "guestName": "Guest",
  "tariffPlanId": "yyyyyyyy-...",
  "tariffPlanName": "1 Hour",
  "saleId": "aaaaaaaa-...",
  "startedAt": "2026-04-03T10:00:00Z",
  "endsAt": "2026-04-03T11:00:00Z",
  "status": "Active",
  "endedAt": null
}
```

On `start`, the server:
1. Creates a `Sale` record (payment proof)
2. Creates a `Session` record with `endsAt = now + tariffDuration`
3. Sends `Unlock` command to the agent
4. Sends `SessionStarted` SignalR event to the agent (with `endsAt`)

**Extend session:**
```json
{
  "tariffPlanId": "yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy",
  "amount": 300.00,
  "currency": "KZT",
  "paymentMethod": "Cash",
  "operatorName": "Cashier1"
}
```

Extension adds time from the current `endsAt` (not from now), ensuring no overlap.

**End session early:**
```json
{ "reboot": true }
```

When session ends (early or expired):
1. Session status → `Ended`
2. `SessionEnded` event sent to agent
3. `Lock` command sent
4. If `reboot: true` (or auto-expiry): `Reboot` command sent → PC reboots → lock screen appears on next login

### SignalR Hub

**Endpoint:** `/hubs/agent`

**Agent → Server:**
- `JoinAsync(workstationId, secret)` – authenticate and join group
- `AcknowledgeCommandAsync(commandLogId)` – confirm command received

**Server → Agent:**
- `ReceiveCommand({ CommandLogId, Command, Notes })` – command delivery
- `Joined(workstationId)` – join confirmation
- `ReceiveSessionUpdate({ Type, SessionId, EndsAt? })` – session state changes

Session update types:
- `SessionStarted` – session started, includes `EndsAt`
- `SessionExtended` – session extended, includes new `EndsAt`
- `SessionEnded` – session ended (lock + reboot will follow as commands)

---

## Session Expiry (Background Service)

The server runs a background service (`SessionExpiryService`) that:
- Polls every **30 seconds** for sessions where `endsAt <= now` and `status = Active`
- Marks each expired session as `Ended`
- Sends `Lock` + `Reboot` commands to the workstation
- Logs the action in the command audit log

The agent also has a **local timer** that fires at `endsAt` to reboot the PC, as a fallback in case of network issues.

---

## Authentication

### Agent Authentication
Agents authenticate using `workstationId` + `secret` (obtained during registration) in the request body or SignalR join message.

### Admin Authentication
Admin endpoints (prefix `/api/admin/`) require the `X-Admin-Key` header:

```
X-Admin-Key: your-admin-key-here
```

Set `AdminApiKey` in `appsettings.json`. Leave empty in development (no auth).

> **Production:** Always set a strong `AdminApiKey` and serve over HTTPS.

---

## Configuration

### Server (`server/appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=cyberclub;User=cyberuser;Password=cyberpass;"
  },
  "AdminApiKey": "change-me-in-production",
  "Integrations": {
    "MeshCentral": {
      "BaseUrl": "https://mesh.yourclub.local",
      "DeviceGroupId": "",
      "RemoteLinkTemplate": "{BaseUrl}/?viewid=50&id={DeviceId}"
    },
    "Fog": {
      "BaseUrl": "http://fog.yourclub.local"
    },
    "ErpNext": {
      "BaseUrl": "http://erp.yourclub.local"
    }
  }
}
```

See `server/appsettings.template.json` for the full template.

### Agent

Agent config is stored at:
```
C:\ProgramData\CyberClub\Agent\agent.json
```

File ACLs are automatically restricted to `SYSTEM` and `Administrators`.

The agent reads `ServerUrl` and `WorkstationName` from this file. On first run (if not registered), it auto-registers with the server.

---

## Prepaid Session Flow (Step by Step)

```
Cashier                  Server                 Agent (PC)
  │                        │                        │
  │── POST /sessions/start ──>                      │
  │    (tariffPlanId, amount,                       │
  │     customerId / guestName)                     │
  │                        │                        │
  │                        │── Creates Sale record  │
  │                        │── Creates Session      │
  │                        │   (endsAt = now+60min) │
  │                        │                        │
  │                        │── ReceiveCommand(Unlock) ──>
  │                        │── ReceiveSessionUpdate  ──>
  │                        │   (SessionStarted,      │
  │                        │    endsAt)              │
  │<── 201 Session ────────│                        │
  │                        │                [PC Unlocked]
  │                        │                [Countdown shown]
  │                        │                        │
  │          ... user plays for 1 hour ...          │
  │                        │                        │
  │         [SessionExpiryService fires]            │
  │                        │── ReceiveSessionUpdate ──>
  │                        │   (SessionEnded)        │
  │                        │── ReceiveCommand(Lock) ──>
  │                        │── ReceiveCommand(Reboot) ->
  │                        │                [PC Reboots]
  │                        │                [Locked again]
```

---

## Database Schema

Tables (managed by EF Core migrations):

- **Workstations** – registered workstations, state, lastSeen, integration fields
- **AgentHeartbeats** – periodic heartbeat records (CPU, RAM, state)
- **CommandLogs** – audit log of all commands issued
- **ExternalReceipts** – external receipts from POS/ERPNext
- **Customers** – customer accounts (username / phone)
- **TariffPlans** – tariff plans (Hourly / Monthly), admin-configurable
- **Sales** – payment records (cash / card / other)
- **Sessions** – active/ended sessions tied to workstation, customer, tariff, and sale
- **Subscriptions** – monthly subscriptions tied to customer
- **ClubSettings** – singleton club configuration (idle shutdown, auto-restart, map display)
- **MapLayouts** – floor-plan canvas metadata (dimensions, grid size)
- **MapItems** – placed elements on the map (PCs, walls, decorations, etc.)
- **Zones** – named rectangular zones drawn on the map (VIP, gaming, etc.)

---

## Admin Web UI (Map + Configuration)

The server serves an **admin web UI** at the root URL (`http://localhost:5000/`).

### Pages

| Page | URL | Description |
|------|-----|-------------|
| Map | `/` or `/index.html` | Interactive floor-plan map |
| Configuration | `/configuration.html` | Club settings |
| Workstations | `/workstations.html` | List & control all PCs |

### Map page features

- **Grid canvas**: Displays a configurable grid (default 40 px cells).
- **Drag from palette**: Drag element types (PC, Console, Wall, Corner, WallT, Triangle, Decoration, Desk, Chair) from the right panel onto the canvas.
- **Move items**: Click and drag placed items to reposition them; position is saved on mouse-up.
- **Select & edit**: Click an item to select it; edit its label, rotation, and linked workstation in the right panel.
- **Right-click menu**: Lock / Unlock / Reboot / Shutdown the linked workstation directly from the map.
- **Zone drawing**: Click "Add Zone" in the Zones tab, then click-drag on the canvas to define a rectangular zone.
- **Workstation status**: Tiles update color based on workstation state every 5 seconds:
  - 🟤 Dark — no linked workstation
  - ⚫ Off-black — offline
  - 🔴 Red — online but Locked
  - 🟢 Green — Unlocked (session active)
- **Zoom**: Use +/−/⤢ buttons to zoom in/out/reset.
- **Right panel tabs**: Settings (grid/canvas size) | Elements (palette) | Zones.

### Configuration page features

- **Shutdown idle PC**: Number of seconds before auto-shutdown (0 = disabled).
- **Auto restart after session**: Toggle on/off + delay in seconds.
- **Show gamer name on map**: Toggle (affects map tile display).
- **Single PC action menu**: Choose right-click context menu or inline buttons.

### Admin Key

All admin API calls require `X-Admin-Key` header. Enter your key in the input field at the top-right of every page. The field is persisted for the browser session.

### Map & Settings API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/admin/settings` | Get club settings |
| `PUT` | `/api/admin/settings` | Update club settings |
| `GET` | `/api/admin/map` | Get primary layout (auto-created on first call) |
| `PUT` | `/api/admin/map` | Update layout metadata |
| `GET` | `/api/admin/map/items` | List all map items |
| `POST` | `/api/admin/map/items` | Create map item |
| `PUT` | `/api/admin/map/items/{id}` | Update map item (position, label, rotation) |
| `DELETE` | `/api/admin/map/items/{id}` | Delete map item |
| `GET` | `/api/admin/map/zones` | List all zones |
| `POST` | `/api/admin/map/zones` | Create zone |
| `PUT` | `/api/admin/map/zones/{id}` | Update zone |
| `DELETE` | `/api/admin/map/zones/{id}` | Delete zone |

---

## MeshCentral Integration

### How MeshCentral uses TCP

MeshCentral agents connect to the MeshCentral server via **TCP over HTTPS/WebSocket (port 443 by default)**. This is standard TCP — just wrapped in a secure web transport that works reliably through NAT and firewalls.

- Default port: **443/TCP** (HTTPS/WSS)
- Optional alternate port: **4443/TCP** (configurable in MeshCentral config)

### Setup steps

1. Install MeshCentral on your server (see [MeshCentral docs](https://github.com/Ylianst/MeshCentral)).
2. Open port **443/TCP** in your firewall.
3. Install the MeshCentral agent on each club PC.
4. In MeshCentral web UI, find each device and copy its **Node ID**.
5. In CyberClub Core, associate the device:
   ```http
   PATCH /api/admin/workstations/{workstationId}/integration
   X-Admin-Key: your-key

   { "meshCentralDeviceId": "abc123nodeId" }
   ```
6. Set `Integrations:MeshCentral:BaseUrl` in `appsettings.json`.
7. Retrieve the remote link:
   ```http
   GET /api/admin/workstations/{workstationId}/remote-link
   X-Admin-Key: your-key
   ```

---

## Windows Agent: Installation

### As a Windows Service

```powershell
# Publish
dotnet publish agent/CyberAgent.Service -r win-x64 -o publish/service

# Install service (run as Administrator)
sc create "CyberClub Agent" binPath= "C:\path\to\CyberAgent.Service.exe"
sc start "CyberClub Agent"
```

### Locker UI

The UI should be launched at Windows startup for the target user (via GPO or registry autorun). It will show a full-screen lock screen when the station is locked, and a countdown timer when a session is active.

---

## Architecture Notes

- Workstations are online if `lastSeenAt` is within the last 60 seconds.
- SignalR agents join a group named by their `workstationId` for targeted commands.
- The lock/unlock signal between service and UI uses a named Windows Event (`Global\CyberClub_Unlock`).
- The locker UI disables the close button and suppresses Alt+F4 / Win key.
- Session expiry is enforced by both the server (background service) and the agent (local timer) for reliability.
- Time is sold in fixed increments (no rounding). Extension always adds from the current `endsAt`.
- Payment is always collected before the session starts.

---

## Recommended Production Setup

| Component | Recommendation |
|-----------|---------------|
| Database | MySQL 8.0 on dedicated VM or managed service |
| Server | Windows Server 2022 or Linux, behind reverse proxy (nginx/IIS) |
| TLS | HTTPS required in production (Let's Encrypt or corporate CA) |
| Admin auth | Set strong `AdminApiKey`; consider adding JWT auth in v2 |
| Scaling | Add Redis SignalR backplane for multi-instance deployments (400+ PCs) |
| Logging | Integrate Serilog + Seq/Loki for centralized log management |
| Monitoring | Add Prometheus metrics endpoint for fleet health |
| MeshCentral | Deploy on same LAN; open 443/TCP; use split-DNS for internal/external access |

