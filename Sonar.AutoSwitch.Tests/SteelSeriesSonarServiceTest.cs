using System.Collections.Generic;
using System.Linq;
using Sonar.AutoSwitch.Services;
using Xunit;

namespace Sonar.AutoSwitch.Tests;

public class SteelSeriesSonarServiceTest
{
    private static List<SonarGamingConfiguration> FakeConfigs() =>
    [
        new("2", "Counter-Strike 2"),
        new("1", "Crimson Desert"),
    ];

    [Fact]
    public void AvailableGamingConfigurations_reads_once_then_serves_from_cache()
    {
        var svc = new SteelSeriesSonarService();
        int reads = 0;
        svc.ConfigQuery = () => { reads++; return FakeConfigs(); };

        _ = svc.AvailableGamingConfigurations.ToList();
        _ = svc.AvailableGamingConfigurations.ToList();
        _ = svc.AvailableGamingConfigurations.ToList();

        Assert.Equal(1, reads); // three accesses, one DB read
    }

    [Fact]
    public void AvailableGamingConfigurations_is_sorted_by_name()
    {
        var svc = new SteelSeriesSonarService();
        svc.ConfigQuery = FakeConfigs;

        var names = svc.AvailableGamingConfigurations.Select(c => c.Name).ToList();

        Assert.Equal(new[] { "Counter-Strike 2", "Crimson Desert" }, names);
    }

    [Fact]
    public void RefreshGamingConfigurations_forces_a_fresh_read()
    {
        var svc = new SteelSeriesSonarService();
        int reads = 0;
        svc.ConfigQuery = () => { reads++; return FakeConfigs(); };

        _ = svc.AvailableGamingConfigurations.ToList();
        svc.RefreshGamingConfigurations();
        _ = svc.AvailableGamingConfigurations.ToList();

        Assert.Equal(2, reads); // refresh invalidates the cache
    }

    [Fact]
    public void AvailableGamingConfigurations_returns_empty_and_does_not_cache_on_query_failure()
    {
        var svc = new SteelSeriesSonarService();
        int reads = 0;
        svc.ConfigQuery = () =>
        {
            reads++;
            if (reads == 1) throw new System.InvalidOperationException("DB locked");
            return FakeConfigs();
        };

        var first = svc.AvailableGamingConfigurations.ToList();
        var second = svc.AvailableGamingConfigurations.ToList();

        Assert.Empty(first);                 // failure degrades gracefully
        Assert.Equal(2, second.Count);       // transient failure not cached — retried
        Assert.Equal(2, reads);
    }
}
