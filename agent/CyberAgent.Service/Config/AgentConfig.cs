using System.Text.Json;

namespace CyberAgent.Service.Config;

/// <summary>
/// Stores workstationId and shared secret on disk.
/// File is created after successful registration with the server.
/// Recommended: restrict file ACLs to SYSTEM/Administrators only.
/// </summary>
public class AgentConfig
{
    private static readonly string ConfigDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "CyberClub", "Agent");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "agent.json");

    public Guid WorkstationId { get; set; }
    public string Secret { get; set; } = string.Empty;
    public string ServerUrl { get; set; } = "http://localhost:5000";
    public string WorkstationName { get; set; } = Environment.MachineName;

    public static AgentConfig Load()
    {
        if (!File.Exists(ConfigPath))
            return new AgentConfig();

        var json = File.ReadAllText(ConfigPath);
        return JsonSerializer.Deserialize<AgentConfig>(json) ?? new AgentConfig();
    }

    public void Save()
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);

        // Restrict permissions: only SYSTEM and Administrators can read/write
        try
        {
            var info = new System.IO.FileInfo(ConfigPath);
            var security = info.GetAccessControl();
            security.SetAccessRuleProtection(true, false);

            var systemRule = new System.Security.AccessControl.FileSystemAccessRule(
                "SYSTEM",
                System.Security.AccessControl.FileSystemRights.FullControl,
                System.Security.AccessControl.AccessControlType.Allow);
            var adminRule = new System.Security.AccessControl.FileSystemAccessRule(
                "Administrators",
                System.Security.AccessControl.FileSystemRights.FullControl,
                System.Security.AccessControl.AccessControlType.Allow);

            security.AddAccessRule(systemRule);
            security.AddAccessRule(adminRule);
            info.SetAccessControl(security);
        }
        catch
        {
            // Non-fatal: best-effort permission restriction
        }
    }

    public bool IsRegistered => WorkstationId != Guid.Empty && !string.IsNullOrEmpty(Secret);
}
