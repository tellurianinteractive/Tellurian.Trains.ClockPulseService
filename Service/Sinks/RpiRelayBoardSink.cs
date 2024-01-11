using System.Device.Gpio;

namespace Tellurian.Trains.ClockPulseApp.Service.Sinks;

public sealed class RpiRelayBoardSink(GpioController controller, ClockStoppedPinUse clockStoppedPinUse, ILogger logger) : IPulseSink, IStatusSink, IControlSink, IDisposable
{
    const int VoltagePinDelay = 250; // milliseconds
    private readonly ILogger Logger = logger;
    private readonly GpioController Controller = controller;
    private readonly ClockStoppedPinUse ClockStoppedPinUse = clockStoppedPinUse;

    private bool PinsAreNotOpenShouldBeLogged = true;

    private const int ClockStoppedPin = 26;
    private const int PositivePin = 20;
    private const int NegativePin = 21;

    private const int ClockStoppedAlarmMilliseconds = 3000;
    private const int ClockStartedAlarmMilliseconds = 1000;

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
            Controller.Write(PositivePin, PinValue.High);
            Controller.Write(NegativePin, PinValue.Low);
            await Task.Delay(VoltagePinDelay);
            LastKnownPolarity = Polarity.Negative;
        }
        else
            LoggingOncePinsNotOpened();
    }

    public async Task PositiveVoltageAsync()
    {
        if (ArePinsOpen)
        {
            Controller.Write(PositivePin, PinValue.Low);
            Controller.Write(NegativePin, PinValue.High);
            await Task.Delay(VoltagePinDelay);
            LastKnownPolarity = Polarity.Positive;
        }
        else
            LoggingOncePinsNotOpened();
    }

    public Task ZeroVoltageAsync()
    {
        if (ArePinsOpen)
        {
            ShortcutClock();
        }
        else
            LoggingOncePinsNotOpened();
        LastKnownPolarity = Polarity.Zero;
        return Task.CompletedTask;
    }

    private void ShortcutClock()
    {
        if (LastKnownPolarity == Polarity.Negative) Controller.Write(PositivePin, PinValue.Low);
        else if (LastKnownPolarity == Polarity.Positive) Controller.Write(PositivePin, PinValue.High);
    }

    public Task InitializeAsync(TimeOnly analogueTime)
    {
        try
        {
            Controller.OpenPin(ClockStoppedPin, PinMode.Output, PinValue.High);
            Controller.OpenPin(PositivePin, PinMode.Output, PinValue.High);
            Controller.OpenPin(NegativePin, PinMode.Output, PinValue.High);
            PinsAreNotOpenShouldBeLogged = false;
            Logger.LogInformation("Started {sink}", nameof(RpiRelayBoardSink));

        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error when starting {sink}", nameof(RpiRelayBoardSink));
        }
        return Task.CompletedTask;
    }

    public async Task CleanupAsync()
    {
        await ZeroVoltageAsync();
        Controller.ClosePin(ClockStoppedPin);
        Controller.ClosePin(PositivePin);
        Controller.ClosePin(NegativePin);
        Logger.LogInformation("Stopped {sink}", nameof(RpiRelayBoardSink));
    }

    public async Task ClockIsStartedAsync()
    {
        if (ArePinsOpen)
        {
            switch (ClockStoppedPinUse)
            {
                case ClockStoppedPinUse.Alarm:
                    Controller.Write(ClockStoppedPin, PinValue.High);
                    await Task.Delay(ClockStartedAlarmMilliseconds);
                    Controller.Write(ClockStoppedPin, PinValue.Low);
                    break;
                default:
                    Controller.Write(ClockStoppedPin, PinValue.High);
                    break;
            }
        }
        else
            LoggingOncePinsNotOpened();
        return;
    }

    public async Task ClockIsStoppedAsync()
    {
        if (ArePinsOpen)
        {
            switch (ClockStoppedPinUse)
            {
                case ClockStoppedPinUse.Alarm:
                    Controller.Write(ClockStoppedPin, PinValue.High);
                    await Task.Delay(ClockStoppedAlarmMilliseconds);
                    Controller.Write(ClockStoppedPin, PinValue.Low);
                    break;
                default:
                    Controller.Write(ClockStoppedPin, PinValue.Low);
                    break;
            }
        }
        else
            LoggingOncePinsNotOpened();
        return;
    }

    public Task SessionIsCompletedAsync()
    {
        if (ArePinsOpen)
        {
            Controller.Write(ClockStoppedPin, PinValue.High);
        }
        else
            LoggingOncePinsNotOpened();
        return Task.CompletedTask;
    }

    bool IsDisposed = false;
    public void Dispose()
    {
        IsDisposed = true;
        Controller.Dispose();
    }

    private bool ArePinsOpen =>
        !IsDisposed && Controller.IsPinOpen(ClockStoppedPin) && Controller.IsPinOpen(PositivePin) && Controller.IsPinOpen(NegativePin);

    private void LoggingOncePinsNotOpened()
    {
        if (!PinsAreNotOpenShouldBeLogged) return;
        Logger.LogError("Pins are not opened on {sink}.", nameof(RpiRelayBoardSink));
        PinsAreNotOpenShouldBeLogged = false;
    }
}

public interface IGpioController : IDisposable
{
    void OpenPin(int pinNumber, PinMode pinMode, PinValue pinValue);
    void Write(int pinNumber, PinValue pinValue);
    void ClosePin(int pinNumber);
    bool IsPinOpen(int pinNumber);
}

public enum ClockStoppedPinUse
{
    RedGreen = 0, // Relay pin is Low = Red: clock is stopped, High = Green: clock is running
    Alarm = 1 // Pin is low for a few seconds when clock is stopped or started.
}


