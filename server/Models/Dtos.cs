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
    string IpAddress,
    string? MeshCentralDeviceId,
    string? FogHostId,
    string? ImageGroup);

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

public record UpdateIntegrationRequest(
    string? MeshCentralDeviceId,
    string? FogHostId,
    string? ImageGroup);

public record RemoteLinkResponse(
    Guid WorkstationId,
    string? MeshCentralDeviceId,
    string? RemoteUrl,
    string Note);

public record MarkForReimageRequest(
    string IssuedBy = "admin",
    string? Notes = null);

public record LinkExternalReceiptRequest(
    string Source,
    string ReceiptNo,
    decimal Amount,
    string Currency,
    Guid? WorkstationId,
    string? SessionId,
    string? RawJson);

public record ExternalReceiptDto(
    Guid Id,
    string Source,
    string ReceiptNo,
    decimal Amount,
    string Currency,
    DateTime CreatedAt,
    Guid? WorkstationId,
    string? SessionId);

// ── Tariff Plans ──────────────────────────────────────────────────────────────

public record TariffPlanDto(
    Guid Id,
    string Name,
    string Type,
    int? DurationMinutes,
    int? DurationDays,
    decimal Price,
    bool IsActive,
    int SortOrder);

public record CreateTariffPlanRequest(
    string Name,
    TariffType Type,
    int? DurationMinutes,
    int? DurationDays,
    decimal Price,
    bool IsActive = true,
    int SortOrder = 0);

public record UpdateTariffPlanRequest(
    string? Name,
    int? DurationMinutes,
    int? DurationDays,
    decimal? Price,
    bool? IsActive,
    int? SortOrder);

// ── Customers ─────────────────────────────────────────────────────────────────

public record CustomerDto(
    Guid Id,
    string? Username,
    string? Phone,
    DateTime CreatedAt,
    bool IsActive);

public record CreateCustomerRequest(
    string? Username,
    string? Phone);

// ── Sessions ──────────────────────────────────────────────────────────────────

public record StartSessionRequest(
    Guid WorkstationId,
    Guid TariffPlanId,
    Guid? CustomerId,
    string? GuestName,
    decimal Amount,
    string Currency = "KZT",
    PaymentMethod PaymentMethod = PaymentMethod.Cash,
    string OperatorName = "admin");

public record ExtendSessionRequest(
    Guid TariffPlanId,
    decimal Amount,
    string Currency = "KZT",
    PaymentMethod PaymentMethod = PaymentMethod.Cash,
    string OperatorName = "admin");

public record EndSessionRequest(bool Reboot = true);

public record SessionDto(
    Guid Id,
    Guid WorkstationId,
    Guid? CustomerId,
    string? GuestName,
    Guid TariffPlanId,
    string TariffPlanName,
    Guid SaleId,
    DateTime StartedAt,
    DateTime EndsAt,
    string Status,
    DateTime? EndedAt);

public record SaleDto(
    Guid Id,
    DateTime CreatedAt,
    decimal Amount,
    string Currency,
    string Method,
    string? OperatorName);

