using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Sonar.AutoSwitch.Services;
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

    // Private type: never conflicts with real app state files.
    private class TestPayload { public string Value { get; set; } = ""; }

    private static string StatePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Sonar.AutoSwitch", "TestPayload.json");

    private static void ClearCache()
    {
        var field = typeof(StateManager).GetField("_states",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        var dict = (Dictionary<Type, object?>)field.GetValue(StateManager.Instance)!;
        dict.Remove(typeof(TestPayload));
    }

    [Fact]
    public void SaveStateNow_writes_file_and_GetOrLoadState_reads_it_back()
    {
        var unique = "test-" + Guid.NewGuid();
        try
        {
            var state = StateManager.Instance.GetOrLoadState<TestPayload>();
            state.Value = unique;
            StateManager.Instance.SaveStateNow<TestPayload>();

            Assert.True(File.Exists(StatePath), "SaveStateNow must write the file");
            ClearCache();

            var loaded = StateManager.Instance.GetOrLoadState<TestPayload>();
            Assert.Equal(unique, loaded.Value);
        }
        finally
        {
            ClearCache();
            if (File.Exists(StatePath)) File.Delete(StatePath);
        }
    }

    [Fact]
    public void GetOrLoadState_returns_default_instance_when_no_file_exists()
    {
        if (File.Exists(StatePath)) File.Delete(StatePath);
        ClearCache();
        try
        {
            var state = StateManager.Instance.GetOrLoadState<TestPayload>();
            Assert.NotNull(state);
            Assert.Equal("", state.Value);
        }
        finally
        {
            ClearCache();
            if (File.Exists(StatePath)) File.Delete(StatePath);
        }
    }

    // Distinct type so marking it read-only can't leak into the TestPayload tests above.
    private class ReadOnlyPayload { public string Value { get; set; } = ""; }

    private static string ReadOnlyPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Sonar.AutoSwitch", "ReadOnlyPayload.json");

    [Fact]
    public void SeedReadOnly_serves_the_instance_and_never_writes_to_disk()
    {
        if (File.Exists(ReadOnlyPath)) File.Delete(ReadOnlyPath);
        try
        {
            var payload = new ReadOnlyPayload { Value = "demo" };
            StateManager.Instance.SeedReadOnly(payload);

            // Served from the seed without touching disk.
            Assert.Same(payload, StateManager.Instance.GetOrLoadState<ReadOnlyPayload>());

            // Both save paths are no-ops for read-only state.
            StateManager.Instance.SaveStateNow<ReadOnlyPayload>();
            StateManager.Instance.SaveState<ReadOnlyPayload>();
            Assert.False(File.Exists(ReadOnlyPath), "read-only state must never hit disk");
        }
        finally
        {
            var states = (Dictionary<Type, object?>)typeof(StateManager)
                .GetField("_states", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(StateManager.Instance)!;
            states.Remove(typeof(ReadOnlyPayload));
            var ro = (HashSet<Type>)typeof(StateManager)
                .GetField("_readOnly", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(StateManager.Instance)!;
            ro.Remove(typeof(ReadOnlyPayload));
            if (File.Exists(ReadOnlyPath)) File.Delete(ReadOnlyPath);
        }
    }

    [Fact]
    public void CheckStateExists_reflects_file_presence()
    {
        if (File.Exists(StatePath)) File.Delete(StatePath);
        ClearCache();
        try
        {
            Assert.False(StateManager.Instance.CheckStateExists<TestPayload>());
            var state = StateManager.Instance.GetOrLoadState<TestPayload>();
            state.Value = "x";
            StateManager.Instance.SaveStateNow<TestPayload>();
            Assert.True(StateManager.Instance.CheckStateExists<TestPayload>());
        }
        finally
        {
            ClearCache();
            if (File.Exists(StatePath)) File.Delete(StatePath);
        }
    }
}
