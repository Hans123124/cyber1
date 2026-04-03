using CyberServer.Domain;

namespace CyberServer.Models;

public record RegisterRequest(
    string MachineFingerprint,
    string WorkstationName,
    string? AgentVersion = null,
    string? OsVersion = null);

public record RegisterResponse(
    Guid WorkstationId,
    string Secret,
    string WorkstationName);

public record HeartbeatRequest(
    Guid WorkstationId,
    string Secret,
    string AgentVersion,
    WorkstationState State,
    double CpuUsage,
    double RamUsageMb);

public record HeartbeatResponse(bool Ok, DateTime ServerTime);

public record WorkstationDto(
    Guid Id,
    string Name,
    WorkstationState State,
    bool IsOnline,
    DateTime LastSeenAt,
    string AgentVersion,
    string IpAddress);

public record SendCommandRequest(
    CommandType Command,
    string IssuedBy = "admin",
    string? Notes = null);

public record CommandLogDto(
    long Id,
    Guid WorkstationId,
    string Command,
    string IssuedBy,
    string Status,
    string? Notes,
    DateTime IssuedAt,
    DateTime? DeliveredAt);
