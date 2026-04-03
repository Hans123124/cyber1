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

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/admin/workstations` | List all workstations with online status |
| `GET` | `/api/admin/workstations/{id}` | Get single workstation |
| `POST` | `/api/admin/workstations/{id}/commands` | Send command to workstation |
| `GET` | `/api/admin/workstations/{id}/commands` | Get command/audit log for workstation |
| `GET` | `/api/admin/commands` | Get global command audit log |

**Send command request:**
```json
{
  "command": "Lock",
  "issuedBy": "admin",
  "notes": "End of session"
}
```

Commands: `Lock`, `Unlock`, `Reboot`, `Shutdown`, `Message`

### SignalR Hub

**Endpoint:** `/hubs/agent`

**Agent → Server:**
- `JoinAsync(workstationId, secret)` – authenticate and join group
- `AcknowledgeCommandAsync(commandLogId)` – confirm command received

**Server → Agent:**
- `ReceiveCommand({ CommandLogId, Command, Notes })` – command delivery
- `Joined(workstationId)` – join confirmation

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
  "AdminApiKey": "change-me-in-production"
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

## Database Schema

Tables (managed by EF Core migrations):

- **Workstations** – registered workstations, state, lastSeen
- **AgentHeartbeats** – periodic heartbeat records (CPU, RAM, state)
- **CommandLogs** – audit log of all commands issued

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

The UI should be launched at Windows startup for the target user (via GPO or registry autorun). It will show a full-screen lock screen when the station is locked.

---

## Architecture Notes

- Workstations are online if `lastSeenAt` is within the last 60 seconds.
- SignalR agents join a group named by their `workstationId` for targeted commands.
- The lock/unlock signal between service and UI uses a named Windows Event (`Global\CyberClub_Unlock`).
- The locker UI disables the close button and suppresses Alt+F4 / Win key.

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
