using System.Globalization;

namespace Tellurian.Trains.ClockPulseApp.Service;

public static class TimeStringExtensions
{

    public static TimeOnly AsTimeOnly(this string? time, bool use12Hour = false) =>
        time is null ? throw new ArgumentNullException(nameof(time)) :
        time.Length == 5 && time[2] == ':' ?
        use12Hour ? TimeOnly.Parse(time, CultureInfo.InvariantCulture).As12Hour() : TimeOnly.Parse(time, CultureInfo.InvariantCulture) :
        throw new ArgumentOutOfRangeException(nameof(time), time);


    public static TimeOnly AddOneMinute(this TimeOnly time, bool is12Hour = false) =>
        is12Hour && time.Hour == 12 && time.Minute == 59 ? new TimeOnly(1, 0, 0) :
        time.Hour == 23 && time.Minute == 59 ? new TimeOnly(0, 0, 0) :
        time.AddMinutes(1);

    public static bool IsOneMinuteAfter(this TimeOnly time, TimeOnly other, bool is12Hour = false) =>
        IsMinutesAfter(time, other, 1, is12Hour);
    public static bool IsEqualTo(this TimeOnly time, TimeOnly other, bool is12Hour = false) =>
      IsMinutesAfter(time, other, 0, is12Hour);

    private static bool IsMinutesAfter(this TimeOnly time, TimeOnly other, int minutes, bool is12Hour = false) =>
        (is12Hour && time.Hour == 12 && time.Minute == 59 && other.Hour == 1 && other.Minute == 0) ||
        (time.Hour == 23 && time.Minute == 59 && other.Hour == 0 && other.Minute == 0) ||
        (other - time).TotalMinutes == minutes;

    public static string AsTime(this TimeOnly time, bool is12Hour = false) =>
        is12Hour && time.Hour == 0 ? $"12:{time.Minute:00}" :
        is12Hour && time.Hour > 12 ? $"{time.Hour - 12:00}:{time.Minute:00}" :
        $"{time.Hour:00}:{time.Minute:00}";

    private static TimeOnly As12Hour(this TimeOnly time) =>
        time.Hour == 0 ? time.Add(TimeSpan.FromHours(12)) :
        time.Hour > 12 ? time.Add(TimeSpan.FromHours(-12)) :
        time;
}
