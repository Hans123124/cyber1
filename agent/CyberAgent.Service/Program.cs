using CyberAgent.Service;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "CyberClub Agent";
});

builder.Services.AddHostedService<AgentWorker>();

var host = builder.Build();
host.Run();
