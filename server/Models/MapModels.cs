namespace CyberServer.Models;

// ── Settings DTOs ─────────────────────────────────────────────────────────────

public record ClubSettingsDto(
    int ShutdownIdlePcSeconds,
    int AutoRestartAfterSessionSeconds,
    bool AutoRestartEnabled,
    bool ShowGamerNameOnMap,
    string ActionMenuMode,
    DateTime UpdatedAt
);

public record UpdateClubSettingsRequest(
    int ShutdownIdlePcSeconds,
    int AutoRestartAfterSessionSeconds,
    bool AutoRestartEnabled,
    bool ShowGamerNameOnMap,
    string ActionMenuMode
);

// ── Map Layout DTOs ───────────────────────────────────────────────────────────

public record MapLayoutDto(
    Guid Id,
    string Name,
    int Width,
    int Height,
    int GridSize,
    List<MapItemDto> Items,
    List<ZoneDto> Zones
);

public record UpdateMapLayoutRequest(
    string Name,
    int Width,
    int Height,
    int GridSize
);

// ── Map Item DTOs ─────────────────────────────────────────────────────────────

public record MapItemDto(
    Guid Id,
    Guid LayoutId,
    string Type,
    int X,
    int Y,
    int W,
    int H,
    int Rotation,
    string Label,
    Guid? WorkstationId,
    Guid? ZoneId,
    string? MetaJson
);

public record CreateMapItemRequest(
    Guid LayoutId,
    string Type,
    int X,
    int Y,
    int W,
    int H,
    int Rotation,
    string Label,
    Guid? WorkstationId,
    Guid? ZoneId,
    string? MetaJson
);

public record UpdateMapItemRequest(
    string Type,
    int X,
    int Y,
    int W,
    int H,
    int Rotation,
    string Label,
    Guid? WorkstationId,
    Guid? ZoneId,
    string? MetaJson
);

// ── Zone DTOs ─────────────────────────────────────────────────────────────────

public record ZoneDto(
    Guid Id,
    Guid LayoutId,
    string Name,
    string Color,
    int X,
    int Y,
    int W,
    int H,
    string? MetaJson
);

public record CreateZoneRequest(
    Guid LayoutId,
    string Name,
    string Color,
    int X,
    int Y,
    int W,
    int H,
    string? MetaJson
);

public record UpdateZoneRequest(
    string Name,
    string Color,
    int X,
    int Y,
    int W,
    int H,
    string? MetaJson
);
