using Krosoft.Extensions.Hosting.Models;
using Krosoft.Extensions.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Krosoft.Extensions.Hosting.Tests.Models;

[TestClass]
public class ScheduleConfigTests : BaseTest
{
    private IConfiguration _configuration = null!;

    [TestInitialize]
    public void SetUp()
    {
        var serviceProvider = CreateServiceCollection();
        _configuration = serviceProvider.GetRequiredService<IConfiguration>();
    }

    private static IConfiguration CreerConfiguration(string? interval) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "Schedule:Interval", interval } })
            .Build();

    [TestMethod]
    public void ParDefaut_LIntervalleEstNul()
    {
        var config = new ScheduleConfig();

        Check.That(config.Interval).IsEqualTo(TimeSpan.Zero);
    }

    [TestMethod]
    public void Liaison_DepuisLeFichierDeParametres_RenseigneLIntervalle()
    {
        var config = _configuration.GetSection("Schedule").Get<ScheduleConfig>();

        Check.That(config).IsNotNull();
        Check.That(config!.Interval).IsEqualTo(TimeSpan.FromMinutes(5));
    }

    [TestMethod]
    public void Liaison_DUnIntervalleEnSecondes_RenseigneLIntervalle()
    {
        var config = CreerConfiguration("00:00:30").GetSection("Schedule").Get<ScheduleConfig>();

        Check.That(config).IsNotNull();
        Check.That(config!.Interval).IsEqualTo(TimeSpan.FromSeconds(30));
    }

    [TestMethod]
    public void Liaison_DUneSectionAbsente_NeRetourneAucuneConfiguration()
    {
        var config = _configuration.GetSection("SectionInconnue").Get<ScheduleConfig>();

        Check.That(config).IsNull();
    }

    [TestMethod]
    public void Liaison_DUnIntervalleInvalide_LeveUneErreur()
    {
        var configuration = CreerConfiguration("valeur-invalide");

        Check.ThatCode(() => configuration.GetSection("Schedule").Get<ScheduleConfig>())
             .Throws<InvalidOperationException>();
    }

    [TestMethod]
    public void Egalite_DeuxConfigurationsDeMemeIntervalle_SontEquivalentes()
    {
        var config = new ScheduleConfig { Interval = TimeSpan.FromMinutes(1) };
        var identique = new ScheduleConfig { Interval = TimeSpan.FromMinutes(1) };
        var different = new ScheduleConfig { Interval = TimeSpan.FromMinutes(2) };

        Check.That(config).IsEqualTo(identique);
        Check.That(config.GetHashCode()).IsEqualTo(identique.GetHashCode());
        Check.That(config).IsNotEqualTo(different);
    }
}
