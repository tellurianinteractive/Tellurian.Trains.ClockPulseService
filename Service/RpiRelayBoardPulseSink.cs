using System.Device.Gpio;

namespace Tellurian.Trains.ClockPulseApp.Service;
public sealed class RpiRelayBoardPulseSink : IPulseSink, IStatusSink, IDisposable
{
    public RpiRelayBoardPulseSink(ILogger logger)
    {
        Logger = logger;
    }
    private readonly ILogger Logger;
    private readonly GpioController Controller = new();

    private const int VoltageOnPin = 26;
    private const int PositivePin = 20;
    private const int NegativePin = 21;

    public async Task NegativeVoltageAsync()
    {
        Controller.Write(PositivePin, PinValue.Low);
        Controller.Write(NegativePin, PinValue.Low);
        await Task.Delay(100);
        Controller.Write(VoltageOnPin, PinValue.Low);
    }

    public async Task PositiveVoltageAsync()
    {
        Controller.Write(PositivePin, PinValue.High);
        Controller.Write(NegativePin, PinValue.High);
        await Task.Delay(100);
        Controller.Write(VoltageOnPin, PinValue.Low);
    }

    public Task ZeroVoltageAsync()
    {
        Controller.Write(VoltageOnPin, PinValue.High);
        return Task.CompletedTask;
    }

    public async Task InitializeAsync()
    {
        try
        {
            Controller.OpenPin(VoltageOnPin, PinMode.Output, PinValue.High);
            await Task.Delay(100);
            Controller.OpenPin(PositivePin, PinMode.Output, PinValue.High);
            Controller.OpenPin(NegativePin, PinMode.Output, PinValue.High);
            Logger.LogInformation("Started {sink}", nameof(RpiRelayBoardPulseSink));

        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error when starting {sink}", nameof(RpiRelayBoardPulseSink));
        }
    }

    public async Task CleanupAsync()
    {
        await ZeroVoltageAsync();
        Controller.ClosePin(VoltageOnPin);
        Controller.ClosePin(PositivePin);
        Controller.ClosePin(NegativePin);
        Logger.LogInformation("Stopped {sink}", nameof(RpiRelayBoardPulseSink));
    }

    public Task ClockIsStartedAsync()
    {
        return Task.CompletedTask;
    }

    public Task ClockIsStoppedAsync()
    {
        return Task.CompletedTask;
    }

    public void Dispose() => Controller.Dispose();
}
