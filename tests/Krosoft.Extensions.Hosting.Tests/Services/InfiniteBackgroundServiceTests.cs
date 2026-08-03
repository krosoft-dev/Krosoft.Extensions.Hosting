using Krosoft.Extensions.Hosting.Services;
using Krosoft.Extensions.Hosting.Tests.Core;
using Krosoft.Extensions.Testing;
using Krosoft.Extensions.Testing.Extensions;
using Microsoft.Extensions.Logging;

namespace Krosoft.Extensions.Hosting.Tests.Services;

[TestClass]
public class InfiniteBackgroundServiceTests : BaseTest
{
    private static readonly TimeSpan AttenteMax = TimeSpan.FromSeconds(5);

    private Mock<ILogger<InfiniteBackgroundService>> _logger = null!;

    [TestInitialize]
    public void SetUp()
    {
        _logger = new Mock<ILogger<InfiniteBackgroundService>>();
    }

    [TestMethod]
    public async Task Demarrage_ExecuteLeTraitement()
    {
        using var service = new FakeInfiniteBackgroundService(_logger.Object);

        await service.StartAsync(CancellationToken.None);

        await service.Execution.WaitAsync(AttenteMax);
        Check.That(service.NombreExecutions).IsEqualTo(1);
        _logger.VerifyWasCalled(LogLevel.Information, "InfiniteBackgroundService is starting.", Times.Once());

        await service.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task Demarrage_LaBoucleResteActiveApresLeTraitement()
    {
        using var service = new FakeInfiniteBackgroundService(_logger.Object);

        await service.StartAsync(CancellationToken.None);
        await service.Execution.WaitAsync(AttenteMax);

        Check.That(service.ExecuteTask).IsNotNull();
        Check.That(service.ExecuteTask!.IsCompleted).IsFalse();

        await service.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task Arret_TermineLaBoucleProprement()
    {
        using var service = new FakeInfiniteBackgroundService(_logger.Object);
        await service.StartAsync(CancellationToken.None);
        await service.Execution.WaitAsync(AttenteMax);

        await service.StopAsync(CancellationToken.None);

        Check.That(service.ExecuteTask!.IsCompleted).IsTrue();
        _logger.VerifyWasCalled(LogLevel.Information, "InfiniteBackgroundService background task is stopping.", Times.Once());
    }

    [TestMethod]
    public async Task Arret_AnnuleLeJetonTransmisAuTraitement()
    {
        using var service = new FakeInfiniteBackgroundService(_logger.Object);
        await service.StartAsync(CancellationToken.None);
        await service.Execution.WaitAsync(AttenteMax);
        Check.That(service.Annulation.IsCompleted).IsFalse();

        await service.StopAsync(CancellationToken.None);

        await service.Annulation.WaitAsync(AttenteMax);
        Check.That(service.Annulation.IsCompletedSuccessfully).IsTrue();
    }

    [TestMethod]
    public async Task Arret_SansDemarrage_NeLevePasDErreur()
    {
        using var service = new FakeInfiniteBackgroundService(_logger.Object);

        await service.StopAsync(CancellationToken.None);

        Check.That(service.NombreExecutions).IsEqualTo(0);
        Check.That(service.ExecuteTask).IsNull();
    }

    [TestMethod]
    public async Task Demarrage_AvecUnJetonDejaAnnule_NExecutePasLeTraitement()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        using var service = new FakeInfiniteBackgroundService(_logger.Object);

        await service.StartAsync(cancellationTokenSource.Token);

        Check.ThatCode(() => service.ExecuteTask!.WaitAsync(AttenteMax).GetAwaiter().GetResult())
             .Throws<TaskCanceledException>();
        Check.That(service.NombreExecutions).IsEqualTo(0);
        Check.That(service.Execution.IsCompleted).IsFalse();
        _logger.VerifyWasCalled(LogLevel.Information, "InfiniteBackgroundService is starting.", Times.Never());
    }

    [TestMethod]
    public async Task Traitement_EnErreur_RemonteLErreurDansLaTacheDExecution()
    {
        using var service = new FakeInfiniteBackgroundService(_logger.Object)
        {
            OnRun = () => throw new InvalidOperationException("Erreur de traitement.")
        };

        await service.StartAsync(CancellationToken.None);

        Check.ThatCode(() => service.ExecuteTask!.WaitAsync(AttenteMax).GetAwaiter().GetResult())
             .Throws<InvalidOperationException>()
             .WithMessage("Erreur de traitement.");
        Check.That(service.NombreExecutions).IsEqualTo(1);
    }
}
