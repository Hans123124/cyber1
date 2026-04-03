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
    string? ImageGroup,
    Guid? ClubId);

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
    Guid? ClubId,
    string Name,
    string Type,
    decimal HourlyRateMdl,
    int? DurationMinutes,
    int? DurationDays,
    decimal Price,
    bool IsActive,
    int SortOrder);

public record CreateTariffPlanRequest(
    string Name,
    TariffType Type,
    decimal HourlyRateMdl,
    int? DurationMinutes,
    int? DurationDays,
    decimal Price = 0,
    bool IsActive = true,
    int SortOrder = 0,
    Guid? ClubId = null);

public record UpdateTariffPlanRequest(
    string? Name,
    decimal? HourlyRateMdl,
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
    int DurationHours,
    Guid? CustomerId,
    string? GuestName,
    PaymentMethod PaymentMethod = PaymentMethod.Cash,
    string OperatorName = "admin");

public record ExtendSessionRequest(
    Guid TariffPlanId,
    int DurationHours,
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

// ── Clubs ─────────────────────────────────────────────────────────────────────

public record ClubDto(
    Guid Id,
    string Name,
    DateTime CreatedAt);

public record CreateClubRequest(string Name);

public record UpdateClubRequest(string Name);

// ── Club Settings ─────────────────────────────────────────────────────────────

public record ClubSettingsDto(
    Guid ClubId,
    int? ShutdownIdlePcSeconds,
    int? AutoRestartAfterSessionSeconds,
    bool ShowGamerNameOnMap,
    string SinglePcActionMenuMode);

public record UpdateClubSettingsRequest(
    int? ShutdownIdlePcSeconds,
    int? AutoRestartAfterSessionSeconds,
    bool ShowGamerNameOnMap,
    string SinglePcActionMenuMode);

// ── Map Layouts ───────────────────────────────────────────────────────────────

public record MapLayoutDto(
    Guid Id,
    Guid ClubId,
    string Name,
    int GridWidth,
    int GridHeight,
    int GridCellSizePx,
    DateTime CreatedAt);

public record CreateMapLayoutRequest(
    string Name,
    int GridWidth = 30,
    int GridHeight = 20,
    int GridCellSizePx = 40);

public record UpdateMapLayoutRequest(
    string? Name,
    int? GridWidth,
    int? GridHeight,
    int? GridCellSizePx);

// ── Map Items ─────────────────────────────────────────────────────────────────

public record MapItemDto(
    Guid Id,
    Guid LayoutId,
    string Type,
    int X,
    int Y,
    int W,
    int H,
    int Rotation,
    string? Label,
    Guid? WorkstationId,
    Guid? ZoneId,
    string? MetaJson);

public record CreateMapItemRequest(
    string Type,
    int X,
    int Y,
    int W = 1,
    int H = 1,
    int Rotation = 0,
    string? Label = null,
    Guid? WorkstationId = null,
    Guid? ZoneId = null,
    string? MetaJson = null);

public record UpdateMapItemRequest(
    string? Type,
    int? X,
    int? Y,
    int? W,
    int? H,
    int? Rotation,
    string? Label,
    Guid? WorkstationId,
    Guid? ZoneId,
    string? MetaJson);

// ── Zones ─────────────────────────────────────────────────────────────────────

public record ZoneDto(
    Guid Id,
    Guid LayoutId,
    string Name,
    string Color,
    int X,
    int Y,
    int W,
    int H);

public record CreateZoneRequest(
    string Name,
    string Color = "#4A90D9",
    int X = 0,
    int Y = 0,
    int W = 4,
    int H = 3);

public record UpdateZoneRequest(
    string? Name,
    string? Color,
    int? X,
    int? Y,
    int? W,
    int? H);

// ── Workstation (extended) ────────────────────────────────────────────────────

public record AssignWorkstationClubRequest(Guid? ClubId);

// ── Auth ──────────────────────────────────────────────────────────────────────

public record LoginRequest(
    string Email,
    string Password,
    bool RememberMe = false);

// ── User Management ───────────────────────────────────────────────────────────

public record CreateUserRequest(
    string Email,
    string Password,
    string? Username,
    string? DisplayName,
    string? Role,
    List<Guid>? ClubIds);

public record SetRolesRequest(List<string> Roles);

public record SetClubsRequest(List<Guid> ClubIds);

