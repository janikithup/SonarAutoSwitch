using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Sonar.AutoSwitch.Services;

[ComImport]
[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDeviceEnumerator
{
    // Slot 0: enumerate all endpoints of a given data-flow and state
    void EnumAudioEndpoints(int dataFlow, int dwStateMask, out IMMDeviceCollection ppDevices);
    // Slot 1: default endpoint for a given role
    void GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppEndpoint);
}

[ComImport]
[Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDeviceCollection
{
    void GetCount(out uint pcDevices);
    void Item(uint nDevice, out IMMDevice ppDevice);
}

[ComImport]
[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDevice
{
    void Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams,
        [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
}

[ComImport]
[Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioMeterInformation
{
    void GetPeak(out float pfPeak);
}

public sealed class AudioMeterService
{
    private static readonly Lazy<AudioMeterService> _lazy = new(() => new AudioMeterService());
    public static AudioMeterService Instance => _lazy.Value;

    private readonly List<IAudioMeterInformation> _meters = [];
    private readonly Timer _timer;
    // Deferred init log — emitted on first Poll() to avoid circular static init:
    // Log() inside the Lazy factory → AutoSwitchService type init → new AutoSwitchService()
    // → GetOrLoadState<HomeViewModel>() → new HomeViewModel() → AudioMeterService.Instance → exception.
    private string? _pendingInitLog;
    private int _diagTick;

    public event EventHandler<float>? PeakChanged;

    private AudioMeterService()
    {
        TryInitMeters();
        _timer = new Timer(_ => Poll(), null, 0, 33);
    }

    private void TryInitMeters()
    {
        try
        {
            var enumeratorType = Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"));
            if (enumeratorType is null) { _pendingInitLog = "AudioMeter: CLSID not found"; return; }

            var enumeratorObj = Activator.CreateInstance(enumeratorType);
            if (enumeratorObj is not IMMDeviceEnumerator enumerator)
            {
                _pendingInitLog = "AudioMeter: enumerator cast failed";
                return;
            }

            var meterIid = new Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064");

            // Enumerate ALL active render endpoints — not just the three role-based defaults.
            // Sonar virtual devices (the "default" endpoints) return 0 from GetPeak() because
            // they are virtual sinks. The physical hardware card IS in this list and does report
            // real peak data. DEVICE_STATE_ACTIVE = 1.
            enumerator.EnumAudioEndpoints(0, 1, out var collection);
            collection.GetCount(out var count);

            for (uint i = 0; i < count; i++)
            {
                try
                {
                    collection.Item(i, out var device);
                    var id = meterIid; // local copy — ref param can't use a field
                    device.Activate(ref id, 7, IntPtr.Zero, out var obj);
                    if (obj is IAudioMeterInformation meter)
                        _meters.Add(meter);
                }
                catch { }
            }

            _pendingInitLog = $"AudioMeter init: {_meters.Count}/{count} endpoints have peak meters";
        }
        catch (Exception ex)
        {
            _pendingInitLog = $"AudioMeter init failed: {ex.GetType().Name} {ex.Message}";
        }
    }

    private void Poll()
    {
        if (_pendingInitLog != null)
        {
            AutoSwitchService.Log(_pendingInitLog);
            _pendingInitLog = null;
        }

        var peak = 0f;
        foreach (var meter in _meters)
        {
            try
            {
                meter.GetPeak(out var p);
                if (p > peak) peak = p;
            }
            catch { }
        }

        // Log peak every ~3 s to verify we're seeing real data
        if (Interlocked.Increment(ref _diagTick) % 90 == 0)
            AutoSwitchService.Log($"AudioMeter: peak={peak:F3} across {_meters.Count} endpoints");

        PeakChanged?.Invoke(this, peak);
    }
}
