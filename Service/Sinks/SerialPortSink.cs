﻿using System.IO.Ports;

namespace Tellurian.Trains.ClockPulseApp.Service.Sinks;

/// <summary>
/// Signals pulses by the handshaking wires DTR and RTS.
/// While RTS remains high a positive clock voltage should be on.
/// While DTR remains high a negative clock voltage should be on.
/// </summary>
public sealed class SerialPortSink(string portName, ILogger logger) : IControlSink, IPulseSink, IDisposable
{
    private SerialPort? Port;
    private readonly ILogger Logger = logger;
    private readonly string PortName = portName;

    public Task NegativeVoltageAsync()
    {
        try
        {
            if (Port is not null) Port.RtsEnable = true;
        }
        catch (IOException ex)
        {
            Logger.LogError(ex, "Serial port error.");
        }
        return Task.CompletedTask;
    }

    public Task PositiveVoltageAsync()
    {
        try
        {
            if (Port is not null) Port.DtrEnable = true;
        }
        catch (IOException ex)
        {
            Logger.LogError(ex, "Serial port error.");
        }
        return Task.CompletedTask;
    }

    public Task ZeroVoltageAsync()
    {
        try
        {
            if (Port is not null) { Port.RtsEnable = false; Port.DtrEnable = false; }
        }
        catch (IOException ex)
        {
            Logger.LogError(ex, "Serial port error.");
        }
        return Task.CompletedTask;
    }

    public Task InitializeAsync(TimeOnly analogueStartTime)
    {
        try
        {
            Port = new(PortName)
            {
                Handshake = Handshake.None,
                RtsEnable = false,
                DtrEnable = false
            };
            Logger.LogInformation("Serial port pulse sink started.");
        }
        catch (IOException ex)
        {
            Logger.LogCritical(ex, "Serial port {portName} failed:", PortName);
        }
        return Task.CompletedTask;
    }

    public Task CleanupAsync()
    {
        Port?.Dispose();
        Port = null;
        Logger.LogInformation("Serial port pulse sink stopped.");
        return Task.CompletedTask;
    }

    public void Dispose() => Port?.Dispose();
}

public static class SerialPulseSinkExtensions
{
    public static bool IsValidSerialPortName(this string? name) =>
        !string.IsNullOrWhiteSpace(name) && name.ToUpperInvariant().StartsWith("COM", StringComparison.OrdinalIgnoreCase);
}

