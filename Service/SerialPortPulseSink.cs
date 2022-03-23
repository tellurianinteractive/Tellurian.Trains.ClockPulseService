using System.IO.Ports;

namespace Tellurian.Trains.ClockPulseApp.Service;

/// <summary>
/// Signals pulses by the handshaking wires DTR and RTS.
/// While RTS remains high a positive clock voltage should be on.
/// While DTR remains high a negative clock voltage should be on.
/// </summary>
public sealed class SerialPortPulseSink : IPulseSink, IDisposable
{
    private readonly SerialPort? Port;
    private readonly ILogger Logger;
    private readonly bool UseDtrOnly;
    public SerialPortPulseSink(string portName, ILogger logger, bool useDtrOnly = false)
    {
        Logger = logger;
        UseDtrOnly = useDtrOnly;
        try
        {
            Port = new(portName);
            Port.RtsEnable = false;
            Port.DtrEnable = false;
        }
        catch (IOException ex)
        {
            Logger.LogCritical(ex, "Serial port {portName} failed:", portName);
        }
    }
    public Task NegativeVoltageAsync()
    {
        try
        {
            if (Port is not null)
                if (UseDtrOnly) 
                    Port.DtrEnable = true;
                else 
                    Port.RtsEnable = true;
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

    public void Dispose() => Port?.Dispose();
    public Task StartAsync()
    {
        Logger.LogInformation("Serial port pulse sink started.");
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        Logger.LogInformation("Serial port pulse sink stopped.");
        return Task.CompletedTask;
    }
}
