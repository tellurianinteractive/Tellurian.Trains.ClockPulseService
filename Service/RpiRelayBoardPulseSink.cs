using System.Device.Gpio;

namespace Tellurian.Trains.ClockPulseApp.Service;
public sealed class RpiRelayBoardPulseSink : IPulseSink, IStatusSink, IDisposable
{
    public RpiRelayBoardPulseSink(ILogger logger, bool useRelay1AsClockStatus = false)
    {
        Logger = logger;
        UseRelay1AsClockStatus = useRelay1AsClockStatus;
    }
    private readonly ILogger Logger;
    private readonly bool UseRelay1AsClockStatus;
    private readonly GpioController Controller = new();
    private const int GeneralPulsePin = 26;
    private const int PositivePulsePin = 20;
    private const int NegativePulsePin = 21;

    public Task NegativeVoltageAsync()
    {
        if (!UseRelay1AsClockStatus) Controller.Write(GeneralPulsePin, PinValue.Low);
        Controller.Write(NegativePulsePin, PinValue.Low);
        return Task.CompletedTask;
    }

    public Task PositiveVoltageAsync()
    {
        if (!UseRelay1AsClockStatus) Controller.Write(GeneralPulsePin, PinValue.Low);
        Controller.Write(PositivePulsePin, PinValue.Low);
        return Task.CompletedTask;
    }

    public Task ZeroVoltageAsync()
    {
        if (!UseRelay1AsClockStatus) Controller.Write(GeneralPulsePin, PinValue.High);
        Controller.Write(PositivePulsePin, PinValue.High);
        Controller.Write(NegativePulsePin, PinValue.High);
        return Task.CompletedTask;
    }

    public Task InitializeAsync()
    {
        try
        {
            Controller.OpenPin(GeneralPulsePin, PinMode.Output, PinValue.High);
            Controller.OpenPin(PositivePulsePin, PinMode.Output, PinValue.High);
            Controller.OpenPin(NegativePulsePin, PinMode.Output, PinValue.High);
            Logger.LogInformation("Started {sink}", nameof(RpiRelayBoardPulseSink));

        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error when starting {sink}", nameof(RpiRelayBoardPulseSink));
        }
        return Task.CompletedTask;
    }

    public async Task CleanupAsync()
    {
        await ZeroVoltageAsync();
        Controller.ClosePin(GeneralPulsePin);
        Controller.ClosePin(PositivePulsePin);
        Controller.ClosePin(NegativePulsePin);
        Logger.LogInformation("Stopped {sink}", nameof(RpiRelayBoardPulseSink));
    }

    public Task ClockIsStartedAsync()
    {
        if (UseRelay1AsClockStatus)
        {
            Controller.Write(GeneralPulsePin, PinValue.High);
        }
        return Task.CompletedTask;
    }

    public Task ClockIsStoppedAsync()
    {
        if (UseRelay1AsClockStatus)
        {
            Controller.Write(GeneralPulsePin, PinValue.Low);
        }
        return Task.CompletedTask;
    }

    public void Dispose() => Controller.Dispose();
}
