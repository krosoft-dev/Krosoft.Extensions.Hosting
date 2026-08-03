using Krosoft.Extensions.Hosting.Services;
using Microsoft.Extensions.Logging;

namespace Krosoft.Extensions.Hosting.Tests.Core;

internal sealed class FakeInfiniteBackgroundService(ILogger<InfiniteBackgroundService> logger)
    : InfiniteBackgroundService(logger)
{
    private readonly TaskCompletionSource _annulation = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _execution = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _nombreExecutions;

    public Task Annulation => _annulation.Task;

    public Task Execution => _execution.Task;

    public int NombreExecutions => Volatile.Read(ref _nombreExecutions);

    public Func<Task>? OnRun { get; init; }

    protected override async Task RunAsync(CancellationToken stoppingToken)
    {
        Interlocked.Increment(ref _nombreExecutions);
        stoppingToken.Register(() => _annulation.TrySetResult());
        _execution.TrySetResult();

        if (OnRun != null)
        {
            await OnRun();
        }
    }
}
