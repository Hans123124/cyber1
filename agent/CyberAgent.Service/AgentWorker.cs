using CyberAgent.Service.Config;
using CyberAgent.Service.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CyberAgent.Service;

/// <summary>
/// Main agent background service.
/// Handles: registration, heartbeat, SignalR command channel.
/// </summary>
public class AgentWorker(ILogger<AgentWorker> logger) : BackgroundService
{
    private const string AgentVersion = "1.0.0";
    private readonly AgentConfig _config = AgentConfig.Load();
    private HubConnection? _hub;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var http = new HttpClient { BaseAddress = new Uri(_config.ServerUrl) };
        var api = new ServerApiClient(http);

        // ── Registration ────────────────────────────────────────────────────
        if (!_config.IsRegistered)
        {
            logger.LogInformation("Registering workstation with server...");
            try
            {
                var fingerprint = FingerprintService.GetFingerprint();
                var osVersion = Environment.OSVersion.ToString();
                var result = await api.RegisterAsync(fingerprint, _config.WorkstationName, AgentVersion, osVersion, stoppingToken);
                if (result is not null)
                {
                    _config.WorkstationId = result.WorkstationId;
                    _config.Secret = result.Secret;
                    _config.Save();
                    logger.LogInformation("Registered as {WorkstationId}", result.WorkstationId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Registration failed, will retry next cycle.");
            }
        }

        // ── SignalR Hub connection ──────────────────────────────────────────
        if (_config.IsRegistered)
        {
            _hub = new HubConnectionBuilder()
                .WithUrl($"{_config.ServerUrl}/hubs/agent")
                .WithAutomaticReconnect()
                .Build();

            _hub.On<object>("ReceiveCommand", HandleCommand);

            _hub.Reconnected += async _ =>
            {
                logger.LogInformation("Reconnected to hub, re-joining group...");
                await JoinHubAsync();
            };

            await StartHubAsync(stoppingToken);
        }

        // ── Heartbeat loop ─────────────────────────────────────────────────
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (!_config.IsRegistered) continue;

            try
            {
                var (cpu, ram) = GetMetrics();
                var state = GetCurrentState();
                await api.HeartbeatAsync(_config, AgentVersion, state, cpu, ram, stoppingToken);
                logger.LogDebug("Heartbeat sent (state={State}, cpu={Cpu:F1}%, ram={Ram:F0}MB)", state, cpu, ram);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Heartbeat failed");
            }
        }
    }

    private async Task StartHubAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await _hub!.StartAsync(ct);
                await JoinHubAsync();
                logger.LogInformation("Connected to SignalR hub.");
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Hub connection failed, retrying in 10s...");
                await Task.Delay(10_000, ct);
            }
        }
    }

    private async Task JoinHubAsync()
    {
        try
        {
            await _hub!.InvokeAsync("JoinAsync", _config.WorkstationId, _config.Secret);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to join hub group.");
        }
    }

    private void HandleCommand(object commandPayload)
    {
        logger.LogInformation("Received command: {Payload}", commandPayload);

        // Parse JSON payload
        var json = commandPayload?.ToString() ?? string.Empty;
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("Command", out var cmdProp)) return;
        var command = cmdProp.GetString();

        long commandLogId = 0;
        if (root.TryGetProperty("CommandLogId", out var idProp))
            idProp.TryGetInt64(out commandLogId);

        try
        {
            switch (command)
            {
                case "Lock":
                    SystemCommandExecutor.Lock();
                    break;
                case "Unlock":
                    SystemCommandExecutor.Unlock();
                    break;
                case "Reboot":
                    SystemCommandExecutor.Reboot();
                    break;
                case "Shutdown":
                    SystemCommandExecutor.Shutdown();
                    break;
                default:
                    logger.LogWarning("Unknown command: {Command}", command);
                    break;
            }

            if (_hub?.State == HubConnectionState.Connected && commandLogId > 0)
            {
                _ = _hub.InvokeAsync("AcknowledgeCommandAsync", commandLogId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing command {Command}", command);
        }
    }

    private static string GetCurrentState()
    {
        // Check the named event to determine if the UI lock screen is showing
        // The UI sets this flag; if not accessible, assume Locked
        try
        {
            if (System.Threading.EventWaitHandle.TryOpenExisting("Global\\CyberClub_IsUnlocked", out var evt))
            {
                using (evt)
                    return "Unlocked";
            }
        }
        catch { }
        return "Locked";
    }

    private static (double cpu, double ram) GetMetrics()
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();
        var ramMb = process.WorkingSet64 / 1024.0 / 1024.0;

        // Simple CPU approximation
        double cpu = 0;
        try
        {
            using var counter = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total");
            counter.NextValue();
            System.Threading.Thread.Sleep(100);
            cpu = counter.NextValue();
        }
        catch { }

        return (cpu, ramMb);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_hub is not null)
            await _hub.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
