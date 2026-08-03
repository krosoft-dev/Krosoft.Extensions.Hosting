using Krosoft.Extensions.Hosting.Models;
using Krosoft.Extensions.Hosting.Services;
using Krosoft.Extensions.Hosting.Tests.Core;
using Krosoft.Extensions.Testing;
using Krosoft.Extensions.Testing.Extensions;
using Microsoft.Extensions.Logging;

namespace Krosoft.Extensions.Hosting.Tests.Services;

[TestClass]
public class ScheduledHostedServiceTests : BaseTest
{
    private static readonly TimeSpan AttenteMax = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan IntervalleCourt = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan IntervalleLong = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan Marge = TimeSpan.FromMilliseconds(300);

    private Mock<ILogger<ScheduledHostedService>> _logger = null!;

    [TestInitialize]
    public void SetUp()
    {
        _logger = new Mock<ILogger<ScheduledHostedService>>();
    }

    private FakeScheduledHostedService CreerService(TimeSpan interval, Func<Task>? onWork = null) =>
        new(_logger.Object, new ScheduleConfig { Interval = interval }) { OnWork = onWork };

    [TestMethod]
    public async Task Demarrage_DeclencheLeTraitementImmediatement()
    {
        using var service = CreerService(IntervalleLong);

        await service.StartAsync(CancellationToken.None);

        Check.That(await service.AttendreExecutionAsync(AttenteMax)).IsTrue();
        Check.That(service.NombreExecutions).IsEqualTo(1);
        _logger.VerifyWasCalled(LogLevel.Information, "ScheduledHostedService is starting.", Times.Once());

        await service.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task Demarrage_AvecUnIntervalleLong_NeRedeclenchePasAvantEcheance()
    {
        using var service = CreerService(IntervalleLong);
        await service.StartAsync(CancellationToken.None);
        Check.That(await service.AttendreExecutionAsync(AttenteMax)).IsTrue();

        await Task.Delay(Marge);

        Check.That(service.NombreExecutions).IsEqualTo(1);

        await service.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task Demarrage_AvecUnIntervalleCourt_DeclencheLeTraitementAPlusieursReprises()
    {
        using var service = CreerService(IntervalleCourt);

        await service.StartAsync(CancellationToken.None);

        Check.That(await service.AttendreExecutionAsync(AttenteMax)).IsTrue();
        Check.That(await service.AttendreExecutionAsync(AttenteMax)).IsTrue();
        Check.That(await service.AttendreExecutionAsync(AttenteMax)).IsTrue();
        Check.That(service.NombreExecutions).IsStrictlyGreaterThan(2);

        await service.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task Demarrage_SansIntervalle_DeclencheLeTraitementUneSeuleFois()
    {
        using var service = CreerService(TimeSpan.Zero);
        await service.StartAsync(CancellationToken.None);
        Check.That(await service.AttendreExecutionAsync(AttenteMax)).IsTrue();

        await Task.Delay(Marge);

        Check.That(service.NombreExecutions).IsEqualTo(1);

        await service.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task Traitement_RecoitLeJetonFourniAuDemarrage()
    {
        using var service = CreerService(IntervalleLong);
        using var cancellationTokenSource = new CancellationTokenSource();

        await service.StartAsync(cancellationTokenSource.Token);

        Check.That(await service.AttendreExecutionAsync(AttenteMax)).IsTrue();
        Check.That(service.DernierJeton).IsEqualTo(cancellationTokenSource.Token);

        await service.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task Traitement_EnErreur_EstLoggeSansInterrompreLaPlanification()
    {
        using var service = CreerService(IntervalleCourt, () => throw new InvalidOperationException("Erreur de traitement."));
        await service.StartAsync(CancellationToken.None);

        Check.That(await service.AttendreExecutionAsync(AttenteMax)).IsTrue();
        Check.That(await service.AttendreExecutionAsync(AttenteMax)).IsTrue();

        await service.StopAsync(CancellationToken.None);
        await Task.Delay(Marge);

        _logger.VerifyWasCalled(LogLevel.Error, "An error occurred while executing the scheduled task.", Times.AtLeastOnce());
    }

    [TestMethod]
    public async Task Arret_StoppeLesDeclenchementsSuivants()
    {
        using var service = CreerService(IntervalleCourt);
        await service.StartAsync(CancellationToken.None);
        Check.That(await service.AttendreExecutionAsync(AttenteMax)).IsTrue();

        await service.StopAsync(CancellationToken.None);
        await Task.Delay(Marge);
        var nombreExecutionsApresArret = service.NombreExecutions;
        await Task.Delay(Marge);

        Check.That(service.NombreExecutions).IsEqualTo(nombreExecutionsApresArret);
        _logger.VerifyWasCalled(LogLevel.Debug, "ScheduledHostedService is stopping.", Times.Once());
    }

    [TestMethod]
    public async Task Arret_SansDemarrage_NeLevePasDErreur()
    {
        using var service = CreerService(IntervalleLong);

        await service.StopAsync(CancellationToken.None);

        Check.That(service.NombreExecutions).IsEqualTo(0);
    }

    [TestMethod]
    public async Task Annulation_DuJetonDeDemarrage_SuspendLeTraitement()
    {
        using var service = CreerService(IntervalleCourt);
        using var cancellationTokenSource = new CancellationTokenSource();
        await service.StartAsync(cancellationTokenSource.Token);
        Check.That(await service.AttendreExecutionAsync(AttenteMax)).IsTrue();

        await cancellationTokenSource.CancelAsync();
        await Task.Delay(Marge);
        var nombreExecutionsApresAnnulation = service.NombreExecutions;
        await Task.Delay(Marge);

        Check.That(service.NombreExecutions).IsEqualTo(nombreExecutionsApresAnnulation);

        await service.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task Liberation_AppeleePlusieursFois_EstSansEffet()
    {
        var service = CreerService(IntervalleLong);
        await service.StartAsync(CancellationToken.None);
        Check.That(await service.AttendreExecutionAsync(AttenteMax)).IsTrue();
        await service.StopAsync(CancellationToken.None);

        Check.ThatCode(() =>
             {
                 service.Dispose();
                 service.Dispose();
             })
             .DoesNotThrow();
    }
}
