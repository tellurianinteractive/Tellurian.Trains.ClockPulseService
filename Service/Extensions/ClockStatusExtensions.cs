using Tellurian.Trains.MeetingApp.Contracts;

namespace Tellurian.Trains.ClockPulseApp.Service.Extensions;
internal static class ClockStatusExtensions
{
    public static bool WasStopped(this ClockStatus status, ClockStatus? previous) =>
        previous is not null && previous.IsRunning && !status.IsRunning || status.IsRealtime || status.IsPaused || status.IsUnavailable;

    public static bool WasStarted(this ClockStatus status, ClockStatus? previous) =>
        (previous is null || !previous.IsRunning) && status.IsRunning && !status.IsRealtime && !status.IsPaused && !status.IsUnavailable;

}

