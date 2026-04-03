using System.Windows;

namespace CyberAgent.UI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Prevent multiple instances
        var mutex = new System.Threading.Mutex(true, "CyberClub_LockUI", out bool created);
        if (!created)
        {
            Shutdown();
            return;
        }
        GC.KeepAlive(mutex);
    }
}
