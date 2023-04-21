namespace Tellurian.Trains.ClockPulseApp.Service;

public static class TimeStringExtensions
{
    public static TimeSpan AsTimespan(this string? time, bool use12Hour = false) =>
        time is null ? throw new ArgumentNullException(nameof(time)) :
        time.Length == 5 && time[2] == ':' ?
        use12Hour ? TimeSpan.Parse(time).As12Hour() : TimeSpan.Parse(time) :
        throw new ArgumentOutOfRangeException(nameof(time), time);

    private static TimeSpan As12Hour(this TimeSpan time) =>
        time.Hours == 0 ? time.Add(TimeSpan.FromHours(12)) :
        time.Hours > 12 ? time.Add(TimeSpan.FromHours(-12)) :
        time;

    public static string AsTime(this TimeSpan time, bool is12Hour = false) =>
        is12Hour && time.Hours == 0 ? $"12:{time.Minutes:00}" :
        is12Hour && time.Hours > 12 ? $"{time.Hours - 12:00}:{time.Minutes:00}" :
        $"{time.Hours:00}:{time.Minutes:00}";

    public static TimeSpan AddOneMinute(this TimeSpan time, bool is12Hour = false) =>
        is12Hour && time.Hours == 12 && time.Minutes == 59 ? new TimeSpan(1, 0, 0) :
        time.Hours == 23 && time.Minutes == 59 ? TimeSpan.Zero :
        time.Add(TimeSpan.FromMinutes(1));

    public static bool IsOneMinuteAfter(this TimeSpan time, TimeSpan other, bool is12Hour = false) =>
        IsMinutesAfter(time, other, 1, is12Hour);

    public static bool IsTwoMinuteAfter(this TimeSpan time, TimeSpan other, bool is12Hour = false) =>
       IsMinutesAfter(time, other, 2, is12Hour);
    public static bool IsEqualTo(this TimeSpan time, TimeSpan other, bool is12Hour = false) =>
      IsMinutesAfter(time, other, 0, is12Hour);
    private static bool IsMinutesAfter(this TimeSpan time, TimeSpan other, int minutes, bool is12Hour = false) =>
        (is12Hour && time.Hours == 12 && time.Minutes == 59 && other.Hours == 1 && other.Minutes == 0) ||
        (time.Hours == 23 && time.Minutes == 59 && other.Hours == 0 && other.Minutes == 0) ||
        (other - time).TotalMinutes == minutes;

}
