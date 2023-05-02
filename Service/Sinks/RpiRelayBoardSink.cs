using System.Device.Gpio;

namespace Tellurian.Trains.ClockPulseApp.Service.Sinks;
public sealed class RpiRelayBoardSink : IPulseSink, IStatusSink, IControlSink, IDisposable
{
    const int VoltagePinDelay = 250; // milliseconds
    public RpiRelayBoardSink(ILogger logger)
    {
        Logger = logger;
    }
    private readonly ILogger Logger;
    private readonly GpioController Controller = new();
    private bool PinsAreNotOpenShouldBeLogged = true;

    private const int VoltageOnPin = 26;
    private const int PositivePin = 20;
    private const int NegativePin = 21;

    enum Polarity
    {
        Negative = -1,
        Zero = 0,
        Positive = 1,
    }
    private Polarity LastKnownPolarity;

    public async Task NegativeVoltageAsync()
    {
        if (ArePinsOpen)
        {
            Controller.Write(PositivePin, PinValue.Low);
            Controller.Write(NegativePin, PinValue.Low);
            await Task.Delay(VoltagePinDelay);
            Controller.Write(VoltageOnPin, PinValue.Low);
            LastKnownPolarity = Polarity.Negative;
        }
        else if (PinsAreNotOpenShouldBeLogged)
            LoggingOncePinsNotOpened();
    }

    public async Task PositiveVoltageAsync()
    {
        if (ArePinsOpen)
        {
            Controller.Write(PositivePin, PinValue.High);
            Controller.Write(NegativePin, PinValue.High);
            await Task.Delay(VoltagePinDelay);
            Controller.Write(VoltageOnPin, PinValue.Low);
            LastKnownPolarity = Polarity.Positive;
        }
        else if (PinsAreNotOpenShouldBeLogged)
            LoggingOncePinsNotOpened();
    }

    public Task ZeroVoltageAsync()
    {
        if (ArePinsOpen)
        {
            ShortcutClock();
            Controller.Write(VoltageOnPin, PinValue.High);
        }
        else if (PinsAreNotOpenShouldBeLogged)
            LoggingOncePinsNotOpened();
        LastKnownPolarity = Polarity.Zero;
        return Task.CompletedTask;
    }

    private void ShortcutClock()
    {
            if (LastKnownPolarity == Polarity.Negative) Controller.Write(PositivePin, PinValue.High);
            else if (LastKnownPolarity == Polarity.Positive) Controller.Write(PositivePin, PinValue.Low);
    }

    public async Task InitializeAsync(TimeOnly analogueTime)
    {
        try
        {
            Controller.OpenPin(VoltageOnPin, PinMode.Output, PinValue.High);
            await Task.Delay(VoltagePinDelay);
            Controller.OpenPin(PositivePin, PinMode.Output, PinValue.High);
            Controller.OpenPin(NegativePin, PinMode.Output, PinValue.High);
            PinsAreNotOpenShouldBeLogged = false;
            Logger.LogInformation("Started {sink}", nameof(RpiRelayBoardSink));

        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error when starting {sink}", nameof(RpiRelayBoardSink));
        }
    }

    public async Task CleanupAsync()
    {
        await ZeroVoltageAsync();
        Controller.ClosePin(VoltageOnPin);
        Controller.ClosePin(PositivePin);
        Controller.ClosePin(NegativePin);
        Logger.LogInformation("Stopped {sink}", nameof(RpiRelayBoardSink));
    }

    public Task ClockIsStartedAsync()
    {
        return Task.CompletedTask;
    }

    public Task ClockIsStoppedAsync()
    {
        return Task.CompletedTask;
    }

    bool IsDisposed = false;
    public void Dispose()
    {
        IsDisposed = true;
        Controller.Dispose();
    }

    private bool ArePinsOpen =>
        !IsDisposed && Controller.IsPinOpen(VoltageOnPin) && Controller.IsPinOpen(PositivePin) && Controller.IsPinOpen(NegativePin);

    private void LoggingOncePinsNotOpened()
    {
        PinsAreNotOpenShouldBeLogged = false;
        Logger.LogError("Pins are not opened on {sink}.", nameof(RpiRelayBoardSink));
    }
}
