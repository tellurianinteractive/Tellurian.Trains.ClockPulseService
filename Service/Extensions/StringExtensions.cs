namespace Tellurian.Trains.ClockPulseApp.Service.Extensions;
internal static class StringExtensions
{
    public static bool IsAnyOf(this string? value, string[] values) =>
        value is not null && values.Length > 0 && values.Any(arg => Equals(value, StringComparison.OrdinalIgnoreCase));
}
