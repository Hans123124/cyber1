using CyberServer.Services;
using Microsoft.AspNetCore.SignalR;

namespace CyberServer.Hubs;

/// <summary>
/// SignalR hub that Windows agents connect to.
/// Agents join a group named by their workstationId so that
/// the server can send targeted commands.
/// </summary>
public class AgentHub(IWorkstationService workstationService, ICommandService commandService) : Hub
{
    /// <summary>
    /// Called by the agent after connecting.
    /// Header "X-Workstation-Id" + "X-Agent-Secret" are used for auth.
    /// </summary>
    public async Task JoinAsync(Guid workstationId, string secret)
    {
        var workstation = await workstationService.AuthenticateAsync(workstationId, secret);
        if (workstation is null)
        {
            throw new HubException("Authentication failed.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, workstationId.ToString());
        await Clients.Caller.SendAsync("Joined", workstationId);
    }

    /// <summary>
    /// Agent calls this to acknowledge a command was received.
    /// </summary>
    public async Task AcknowledgeCommandAsync(long commandLogId)
    {
        await commandService.MarkDeliveredAsync(commandLogId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
