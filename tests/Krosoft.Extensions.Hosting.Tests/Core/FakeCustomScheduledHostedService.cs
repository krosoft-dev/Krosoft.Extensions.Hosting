using Krosoft.Extensions.Hosting.Models;
using Krosoft.Extensions.Hosting.Services;
using Microsoft.Extensions.Logging;

namespace Krosoft.Extensions.Hosting.Tests.Core;

internal record CustomScheduleConfig : ScheduleConfig
{
    public string? Nom { get; set; }
}

internal sealed class FakeCustomScheduledHostedService(ILogger<ScheduledHostedService> logger, CustomScheduleConfig config)
    : ScheduledHostedService(logger, config)
{
    protected override Task DoWork(CancellationToken cancellationToken) => Task.CompletedTask;
}
