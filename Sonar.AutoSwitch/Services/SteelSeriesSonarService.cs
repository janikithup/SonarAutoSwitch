using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Sonar.AutoSwitch.Services.Win32;
using Sonar.AutoSwitch.ViewModels;

namespace Sonar.AutoSwitch.Services;

public class SteelSeriesSonarService
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private readonly string _connectionString;
    private int? _lastWorkingPort;
    private IReadOnlyList<SonarGamingConfiguration>? _cachedConfigs;

    // Source of the config list. Defaults to the real SQLite read; swappable in tests.
    internal Func<IEnumerable<SonarGamingConfiguration>> ConfigQuery { get; set; }

    public SteelSeriesSonarService()
    {
        ConfigQuery = GetGamingConfigurations;
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                @"SteelSeries\GG\apps\sonar\db\database.db")
        }.ToString();
    }

    public static SteelSeriesSonarService Instance { get; } = new();

    // Cached: the bound ItemsSource in Home.axaml is evaluated once per profile card,
    // and Sonar's SQLite DB is a single-writer file we must not hammer. One read per
    // session; RefreshGamingConfigurations() invalidates when configs may have changed.
    public IReadOnlyList<SonarGamingConfiguration> AvailableGamingConfigurations
    {
        get
        {
            if (_cachedConfigs != null) return _cachedConfigs;
            try { return _cachedConfigs = ConfigQuery().OrderBy(s => s.Name).ToList(); }
            catch { return []; }  // degrade gracefully; don't cache a transient failure
        }
    }

    public void RefreshGamingConfigurations() => _cachedConfigs = null;

    public IEnumerable<SonarGamingConfiguration> GetGamingConfigurations()
    {
        // Get all the available profiles from SQLite
        using var sqliteConnection = new SqliteConnection(_connectionString);
        sqliteConnection.Open();

        using SqliteCommand sqliteCommand = sqliteConnection.CreateCommand();
        sqliteCommand.CommandText = "select id, name, vad from configs where vad == 1";
        using SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader();
        while (sqliteDataReader.Read())
        {
            string id = sqliteDataReader.GetString(0);
            string name = sqliteDataReader.GetString(1);
            yield return new SonarGamingConfiguration(id, name);
        }
    }

    /// <returns>true if Sonar acknowledged the switch (HTTP 200); false if it was unreachable.</returns>
    public async Task<bool> ChangeSelectedGamingConfiguration(SonarGamingConfiguration sonarGamingConfiguration,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(sonarGamingConfiguration.Id))
            return false;

        var sw = Stopwatch.StartNew();

        // Fast path: try cached port first — skip the expensive TCP scan entirely.
        if (_lastWorkingPort != null)
        {
            AutoSwitchService.Log($"PortScan: 0ms, cachedPort={_lastWorkingPort.Value}");
            if (cancellationToken.IsCancellationRequested) return false;
            var t0 = sw.ElapsedMilliseconds;
            HttpResponseMessage? resp = null;
            try { resp = await _httpClient.PutAsync($"http://localhost:{_lastWorkingPort.Value}/configs/{sonarGamingConfiguration.Id}/select", new StringContent(""), cancellationToken); }
            catch (Exception) { }
            using (resp)
            {
                AutoSwitchService.Log($"PUT :{_lastWorkingPort.Value} → {resp?.StatusCode.ToString() ?? "null"} [{sw.ElapsedMilliseconds - t0}ms]");
                if (resp?.StatusCode == HttpStatusCode.OK)
                {
                    AutoSwitchService.Log($"ChangeConfig: ok [{sw.ElapsedMilliseconds}ms total]");
                    return true;
                }
            }
            if (cancellationToken.IsCancellationRequested) return false;
            // Cached port is stale (Sonar restarted). Clear and fall through to full scan.
            _lastWorkingPort = null;
        }

        // Slow path: full TCP scan — only on first call or after cache miss.
        Process[] procs = Process.GetProcessesByName("SteelSeriesSonar");
        if (procs.Length <= 0 || cancellationToken.IsCancellationRequested)
        {
            foreach (var p in procs) p.Dispose();
            return false;
        }

        IEnumerable<int> potentialPorts = procs.SelectMany(p => NetworkHelper.GetPortById(p.Id, false));
        AutoSwitchService.Log($"PortScan: {sw.ElapsedMilliseconds}ms, cachedPort=none");

        bool switched = false;
        foreach (int port in potentialPorts.Distinct())
        {
            if (cancellationToken.IsCancellationRequested) break;
            var t0 = sw.ElapsedMilliseconds;
            HttpResponseMessage? resp = null;
            try { resp = await _httpClient.PutAsync($"http://localhost:{port}/configs/{sonarGamingConfiguration.Id}/select", new StringContent(""), cancellationToken); }
            catch (Exception) { }
            using (resp)
            {
                AutoSwitchService.Log($"PUT :{port} → {resp?.StatusCode.ToString() ?? "null"} [{sw.ElapsedMilliseconds - t0}ms]");
                if (resp?.StatusCode == HttpStatusCode.OK)
                {
                    _lastWorkingPort = port;
                    switched = true;
                    break;
                }
            }
        }

        foreach (var p in procs) p.Dispose();
        if (!switched) _lastWorkingPort = null;
        AutoSwitchService.Log($"ChangeConfig: {(switched ? "ok" : "failed")} [{sw.ElapsedMilliseconds}ms total]");
        return switched;
    }

    public string GetSelectedGamingConfiguration()
    {
        // Get all the available profiles from SQLite
        using var sqliteConnection = new SqliteConnection(_connectionString);
        sqliteConnection.Open();

        using SqliteCommand sqliteCommand = sqliteConnection.CreateCommand();
        sqliteCommand.CommandText = "select config_id, vad from selected_config where vad == 1";
        using SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader();
        if (!sqliteDataReader.Read())
            throw new InvalidOperationException("Unable to check for selected gaming profile");
        return sqliteDataReader.GetString(0);
    }
}