using System.Net.Http.Json;
using CyberAgent.Service.Config;

namespace CyberAgent.Service.Services;

public class ServerApiClient(HttpClient http)
{
    public async Task<RegisterResponse?> RegisterAsync(
        string fingerprint, string name, string agentVersion, string osVersion, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("/api/agents/register", new
        {
            MachineFingerprint = fingerprint,
            WorkstationName = name,
            AgentVersion = agentVersion,
            OsVersion = osVersion
        }, ct);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RegisterResponse>(ct);
    }

    public async Task<bool> HeartbeatAsync(AgentConfig config, string agentVersion,
        string state, double cpu, double ram, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("/api/agents/heartbeat", new
        {
            WorkstationId = config.WorkstationId,
            config.Secret,
            AgentVersion = agentVersion,
            State = state,
            CpuUsage = cpu,
            RamUsageMb = ram
        }, ct);

        return response.IsSuccessStatusCode;
    }
}

public record RegisterResponse(Guid WorkstationId, string Secret, string WorkstationName);
