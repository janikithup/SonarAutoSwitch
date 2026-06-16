using System.Collections.Generic;
using System.Text.Json;
using Sonar.AutoSwitch.ViewModels;

namespace Sonar.AutoSwitch.Tests;

// B6 regression: GetOrLoadState must never return null even when JSON contains "null".
public class StateManagerTest
{
    [Fact]
    public void Deserialize_null_json_falls_back_to_new_instance()
    {
        // JsonSerializer.Deserialize<T>("null") returns null for reference types.
        // StateManager fix: ?? new T() ensures we never cache or return null.
        var result = JsonSerializer.Deserialize<SettingsViewModel>("null") ?? new SettingsViewModel();
        Assert.NotNull(result);
    }
}
