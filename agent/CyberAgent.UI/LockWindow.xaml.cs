using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CyberAgent.UI.Config;
using Microsoft.AspNetCore.SignalR.Client;

namespace CyberAgent.UI;

public partial class LockWindow : Window
{
    private readonly AgentConfig _config = AgentConfig.Load();
    private HubConnection? _hub;
    private readonly DispatcherTimer _clockTimer;
    private readonly Thread _unlockListenerThread;
    private bool _unlocked;
    private DateTime? _sessionEndsAt;

    public LockWindow()
    {
        InitializeComponent();

        WorkstationLabel.Text = $"Workstation: {_config.WorkstationName}";

        // Clock timer — also drives the countdown
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClockAndCountdown();
        _clockTimer.Start();

        // Disable close button via Windows API
        DisableCloseButton();

        // Listen for unlock signal from the service via named event
        _unlockListenerThread = new Thread(ListenForUnlock) { IsBackground = true, Name = "UnlockListener" };
        _unlockListenerThread.Start();

        // Connect to SignalR hub
        _ = ConnectToHubAsync();
    }

    private void UpdateClockAndCountdown()
    {
        ClockText.Text = DateTime.Now.ToString("HH:mm:ss  dd.MM.yyyy");

        if (_unlocked && _sessionEndsAt.HasValue)
        {
            var remaining = _sessionEndsAt.Value - DateTime.UtcNow;
            if (remaining > TimeSpan.Zero)
            {
                CountdownText.Text = $"Time remaining: {remaining:hh\\:mm\\:ss}";
                CountdownText.Visibility = Visibility.Visible;
            }
            else
            {
                CountdownText.Text = "Session ending...";
            }
        }
        else
        {
            CountdownText.Visibility = Visibility.Collapsed;
        }
    }

    private void CallAdmin_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "An administrator has been notified.\nPlease wait.",
            "Admin Call",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    // ── SignalR ───────────────────────────────────────────────────────────────

    private async Task ConnectToHubAsync()
    {
        if (!_config.IsRegistered)
        {
            UpdateStatus("Not registered. Run the agent service first.");
            return;
        }

        _hub = new HubConnectionBuilder()
            .WithUrl($"{_config.ServerUrl}/hubs/agent")
            .WithAutomaticReconnect()
            .Build();

        _hub.On<object>("ReceiveCommand", payload =>
        {
            Dispatcher.Invoke(() => HandleCommand(payload?.ToString() ?? string.Empty));
        });

        _hub.On<object>("ReceiveSessionUpdate", payload =>
        {
            Dispatcher.Invoke(() => HandleSessionUpdate(payload?.ToString() ?? string.Empty));
        });

        _hub.Reconnected += _ => { UpdateStatus("Reconnected."); return Task.CompletedTask; };
        _hub.Reconnecting += _ => { UpdateStatus("Reconnecting..."); return Task.CompletedTask; };
        _hub.Closed += _ => { UpdateStatus("Disconnected."); return Task.CompletedTask; };

        while (true)
        {
            try
            {
                await _hub.StartAsync();
                await _hub.InvokeAsync("JoinAsync", _config.WorkstationId, _config.Secret);
                UpdateStatus("Connected to server.");
                return;
            }
            catch
            {
                UpdateStatus("Cannot connect to server. Retrying...");
                await Task.Delay(10_000);
            }
        }
    }

    private void HandleCommand(string json)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("Command", out var cmdProp)) return;
            var command = cmdProp.GetString();

            switch (command)
            {
                case "Unlock":
                    DoUnlock();
                    break;
                case "Lock":
                    DoLock();
                    break;
            }

            if (root.TryGetProperty("CommandLogId", out var idProp) && idProp.TryGetInt64(out var id))
                _ = _hub?.InvokeAsync("AcknowledgeCommandAsync", id);
        }
        catch { }
    }

    private void HandleSessionUpdate(string json)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("Type", out var typeProp)) return;
            var type = typeProp.GetString();

            switch (type)
            {
                case "SessionStarted":
                case "SessionExtended":
                    if (root.TryGetProperty("EndsAt", out var endsAtProp) &&
                        endsAtProp.TryGetDateTime(out var endsAt))
                    {
                        _sessionEndsAt = endsAt.ToUniversalTime();
                        CountdownText.Visibility = Visibility.Visible;
                    }
                    break;

                case "SessionEnded":
                    _sessionEndsAt = null;
                    CountdownText.Visibility = Visibility.Collapsed;
                    // Lock + Reboot will arrive as separate commands
                    break;
            }
        }
        catch { }
    }

    // ── Unlock / Lock ─────────────────────────────────────────────────────────

    private void ListenForUnlock()
    {
        using var evt = new EventWaitHandle(false, EventResetMode.AutoReset, "Global\\CyberClub_Unlock");
        while (!_unlocked)
        {
            if (evt.WaitOne(500))
            {
                Dispatcher.Invoke(DoUnlock);
            }
        }
    }

    private void DoUnlock()
    {
        _unlocked = true;
        Hide();

        // Set a named event so the service knows we're unlocked
        try
        {
            var evt = new EventWaitHandle(true, EventResetMode.ManualReset, "Global\\CyberClub_IsUnlocked");
            GC.KeepAlive(evt);
        }
        catch { }
    }

    private void DoLock()
    {
        _unlocked = false;
        _sessionEndsAt = null;
        CountdownText.Visibility = Visibility.Collapsed;

        // Reset the IsUnlocked event
        try
        {
            if (EventWaitHandle.TryOpenExisting("Global\\CyberClub_IsUnlocked", out var evt))
            {
                using (evt) evt.Reset();
            }
        }
        catch { }

        Show();
        Topmost = true;
    }

    // ── Prevent close ─────────────────────────────────────────────────────────

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_unlocked)
        {
            e.Cancel = true; // Block close while locked
        }
        else
        {
            base.OnClosing(e);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // Suppress Alt+F4, Win key, etc.
        if (e.Key == Key.System || e.Key == Key.LWin || e.Key == Key.RWin)
            e.Handled = true;
        base.OnKeyDown(e);
    }

    private void UpdateStatus(string message)
    {
        Dispatcher.Invoke(() => StatusBar.Text = message);
    }

    private void DisableCloseButton()
    {
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).EnsureHandle();
        var hMenu = GetSystemMenu(hwnd, false);
        if (hMenu != IntPtr.Zero)
            DeleteMenu(hMenu, SC_CLOSE, MF_BYCOMMAND);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

    [DllImport("user32.dll")]
    private static extern bool DeleteMenu(IntPtr hMenu, uint uPosition, uint uFlags);

    private const uint SC_CLOSE = 0xF060;
    private const uint MF_BYCOMMAND = 0x00000000;
}
