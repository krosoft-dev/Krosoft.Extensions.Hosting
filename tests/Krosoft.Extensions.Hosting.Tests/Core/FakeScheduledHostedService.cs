using Krosoft.Extensions.Hosting.Models;
using Krosoft.Extensions.Hosting.Services;
using Microsoft.Extensions.Logging;

namespace Krosoft.Extensions.Hosting.Tests.Core;

internal sealed class FakeScheduledHostedService(ILogger<ScheduledHostedService> logger, ScheduleConfig config)
    : ScheduledHostedService(logger, config)
{
    private readonly SemaphoreSlim _signal = new(0);
    private int _nombreExecutions;

    public CancellationToken DernierJeton { get; private set; }

    public int NombreExecutions => Volatile.Read(ref _nombreExecutions);

    public Func<Task>? OnWork { get; init; }

    public Task<bool> AttendreExecutionAsync(TimeSpan attenteMax) => _signal.WaitAsync(attenteMax);

    protected override async Task DoWork(CancellationToken cancellationToken)
    {
        DernierJeton = cancellationToken;
        Interlocked.Increment(ref _nombreExecutions);
        _signal.Release();

        if (OnWork != null)
        {
            await OnWork();
        }
    }
}
