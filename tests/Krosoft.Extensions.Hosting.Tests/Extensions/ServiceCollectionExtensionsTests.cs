using Krosoft.Extensions.Core.Models.Exceptions;
using Krosoft.Extensions.Hosting.Extensions;
using Krosoft.Extensions.Hosting.Models;
using Krosoft.Extensions.Hosting.Services;
using Krosoft.Extensions.Hosting.Tests.Core;
using Krosoft.Extensions.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Krosoft.Extensions.Hosting.Tests.Extensions;

[TestClass]
public class ServiceCollectionExtensionsTests : BaseTest
{
    private static readonly TimeSpan AttenteMax = TimeSpan.FromSeconds(5);

    private IConfiguration _configuration = null!;
    private Mock<ILogger<ScheduledHostedService>> _logger = null!;

    [TestInitialize]
    public void SetUp()
    {
        _logger = new Mock<ILogger<ScheduledHostedService>>();
    }

    protected override void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        _configuration = configuration;
        services.AddSingleton(_logger.Object);
    }

    [TestMethod]
    public void AjoutDUnJobPlanifie_EnregistreLaConfigurationEtLeService()
    {
        var serviceProvider = CreateServiceCollection(services => services.AddScheduledJob<FakeScheduledHostedService>(c => c.Interval = TimeSpan.FromMinutes(5)));

        Check.That(serviceProvider.GetRequiredService<ScheduleConfig>().Interval).IsEqualTo(TimeSpan.FromMinutes(5));

        var hostedServices = serviceProvider.GetServices<IHostedService>().ToList();
        Check.That(hostedServices).HasSize(1);
        Check.That(hostedServices.Single()).IsInstanceOf<FakeScheduledHostedService>();
    }

    [TestMethod]
    public void AjoutDUnJobPlanifie_RetourneLaMemeCollectionDeServices()
    {
        var services = new ServiceCollection();

        var resultat = services.AddScheduledJob<FakeScheduledHostedService>(c => c.Interval = TimeSpan.FromMinutes(5));

        Check.That(resultat).IsSameReferenceAs(services);
    }

    [TestMethod]
    public void AjoutDUnJobPlanifie_EnregistreLeServiceEnSingleton()
    {
        var serviceProvider = CreateServiceCollection(services => services.AddScheduledJob<FakeScheduledHostedService>(c => c.Interval = TimeSpan.FromMinutes(5)));

        var premiereResolution = serviceProvider.GetServices<IHostedService>().Single();
        var secondeResolution = serviceProvider.GetServices<IHostedService>().Single();

        Check.That(premiereResolution).IsSameReferenceAs(secondeResolution);
    }

    [TestMethod]
    public void AjoutDUnJobPlanifie_AvecUneConfigurationDerivee_EnregistreLeTypeDeConfigurationDerive()
    {
        var serviceProvider = CreateServiceCollection(services => services.AddScheduledJob<FakeCustomScheduledHostedService, CustomScheduleConfig>(c =>
        {
            c.Interval = TimeSpan.FromSeconds(30);
            c.Nom = "rafraichissement-cache";
        }));

        var config = serviceProvider.GetRequiredService<CustomScheduleConfig>();
        Check.That(config.Interval).IsEqualTo(TimeSpan.FromSeconds(30));
        Check.That(config.Nom).IsEqualTo("rafraichissement-cache");
        Check.That(serviceProvider.GetServices<IHostedService>().Single()).IsInstanceOf<FakeCustomScheduledHostedService>();
    }

    [TestMethod]
    public void AjoutDUnJobPlanifie_AvecUneConfigurationDerivee_NEnregistrePasLeTypeDeBase()
    {
        var serviceProvider = CreateServiceCollection(services => services.AddScheduledJob<FakeCustomScheduledHostedService, CustomScheduleConfig>(c => c.Interval = TimeSpan.FromSeconds(30)));

        Check.That(serviceProvider.GetService<ScheduleConfig>()).IsNull();
    }

    [TestMethod]
    public void AjoutDUnJobPlanifie_SansOptions_LeveUneErreur()
    {
        var services = new ServiceCollection();

        Check.ThatCode(() => services.AddScheduledJob<FakeCustomScheduledHostedService, CustomScheduleConfig>(null!))
             .Throws<KrosoftTechnicalException>()
             .WithMessage("La variable 'options' n'est pas renseignée.");
    }

    [TestMethod]
    public void AjoutDUnJobPlanifie_SansOptionsParDefaut_LeveUneErreur()
    {
        var services = new ServiceCollection();

        Check.ThatCode(() => services.AddScheduledJob<FakeScheduledHostedService>((Action<ScheduleConfig>)null!))
             .Throws<KrosoftTechnicalException>()
             .WithMessage("La variable 'options' n'est pas renseignée.");
    }

    [TestMethod]
    public void AjoutDUnJobPlanifie_DepuisLeFichierDeParametres_UtiliseLIntervalleDeRafraichissement()
    {
        var serviceProvider = CreateServiceCollection(services => services.AddScheduledJob<FakeScheduledHostedService>(_configuration));

        Check.That(serviceProvider.GetRequiredService<ScheduleConfig>().Interval).IsEqualTo(TimeSpan.FromMinutes(15));
    }

    [TestMethod]
    public void AjoutDUnJobPlanifie_DepuisUneConfigurationSansCle_UtiliseUnIntervalleNul()
    {
        var configuration = new ConfigurationBuilder().Build();

        var serviceProvider = CreateServiceCollection(services => services.AddScheduledJob<FakeScheduledHostedService>(configuration));

        Check.That(serviceProvider.GetRequiredService<ScheduleConfig>().Interval).IsEqualTo(TimeSpan.Zero);
    }

    [TestMethod]
    public async Task AjoutDUnJobPlanifie_LeServiceResoluDepuisLeConteneurEstDemarrable()
    {
        var serviceProvider = CreateServiceCollection(services => services.AddScheduledJob<FakeScheduledHostedService>(c => c.Interval = TimeSpan.FromMinutes(10)));
        var hostedService = (FakeScheduledHostedService)serviceProvider.GetServices<IHostedService>().Single();

        await hostedService.StartAsync(CancellationToken.None);

        Check.That(await hostedService.AttendreExecutionAsync(AttenteMax)).IsTrue();

        await hostedService.StopAsync(CancellationToken.None);
    }
}
