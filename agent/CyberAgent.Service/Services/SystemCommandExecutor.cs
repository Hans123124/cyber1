using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CyberAgent.Service.Services;

/// <summary>
/// Executes system-level commands: Lock, Unlock, Reboot, Shutdown.
/// </summary>
public static class SystemCommandExecutor
{
    public static void Lock()
    {
        LockWorkStation();
    }

    public static void Unlock()
    {
        // Unlock is handled by the UI process: tell it to hide the lock screen.
        // We use a named event to signal the UI process.
        using var evt = new System.Threading.EventWaitHandle(false,
            System.Threading.EventResetMode.AutoReset,
            "Global\\CyberClub_Unlock");
        evt.Set();
    }

    public static void Reboot()
    {
        Process.Start(new ProcessStartInfo("shutdown", "/r /t 5 /c \"CyberClub: reboot command received\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        });
    }

    public static void Shutdown()
    {
        Process.Start(new ProcessStartInfo("shutdown", "/s /t 5 /c \"CyberClub: shutdown command received\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        });
    }

    [DllImport("user32.dll")]
    private static extern bool LockWorkStation();
}
