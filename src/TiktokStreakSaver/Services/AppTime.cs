namespace TiktokStreakSaver.Services;

/// <summary>
/// Centralized application clock and timezone helper.
/// Uses the device's local timezone when distinguishable, and falls back to
/// Philippine Time (PHT / Asia/Manila, UTC+8) when the device timezone cannot
/// be reliably determined (e.g., indeterminate, empty, or generic UTC/GMT fallback).
/// </summary>
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public static class AppTime
{
    private const string ManilaIanaId = "Asia/Manila";
    private const string ManilaWindowsId = "Singapore Standard Time";

    private static readonly Lazy<TimeZoneInfo> CachedFallbackManilaZone = new(CreateAsiaManilaTimeZone);

    /// <summary>
    /// Current wall-clock time in the effective application timezone.
    /// </summary>
    public static DateTime Now => ToAppTime(DateTime.UtcNow);

    /// <summary>
    /// Current calendar date in the effective application timezone.
    /// </summary>
    public static DateTime Today => Now.Date;

    /// <summary>
    /// Effective timezone ID used by the application.
    /// </summary>
    public static string EffectiveTimeZoneId => GetEffectiveTimeZone().Id;

    /// <summary>
    /// Resolves the device's local timezone, or falls back to PHT (Asia/Manila, UTC+8)
    /// when the device timezone cannot be distinguished.
    /// </summary>
    public static TimeZoneInfo GetEffectiveTimeZone()
    {
        try
        {
            var local = TimeZoneInfo.Local;
            if (IsDistinguishableTimeZone(local))
                return local;
        }
        catch
        {
            // Fall through to platform/PHT fallback.
        }

#if ANDROID
        try
        {
            var javaTz = Java.Util.TimeZone.Default;
            var javaId = javaTz?.ID;
            if (!string.IsNullOrWhiteSpace(javaId) && !IsGenericUtcOrIndeterminateId(javaId))
            {
                if (TimeZoneInfo.TryFindSystemTimeZoneById(javaId, out var resolved))
                    return resolved;

                var offsetMs = javaTz!.GetOffset(Java.Lang.JavaSystem.CurrentTimeMillis());
                return TimeZoneInfo.CreateCustomTimeZone(
                    javaId,
                    TimeSpan.FromMilliseconds(offsetMs),
                    javaTz.DisplayName ?? javaId,
                    javaTz.DisplayName ?? javaId);
            }
        }
        catch
        {
            // Fall back to Asia/Manila below.
        }
#endif

        return CachedFallbackManilaZone.Value;
    }

    /// <summary>
    /// Converts a <see cref="DateTime"/> to wall-clock time in <see cref="GetEffectiveTimeZone"/>.
    /// UTC values are converted from UTC; Local values are converted from system local via UTC;
    /// Unspecified values are assumed to already be in the effective app timezone.
    /// </summary>
    public static DateTime ToAppTime(DateTime dateTime)
    {
        var tz = GetEffectiveTimeZone();
        DateTime converted = dateTime.Kind switch
        {
            DateTimeKind.Utc => TimeZoneInfo.ConvertTimeFromUtc(dateTime, tz),
            DateTimeKind.Local => TimeZoneInfo.ConvertTimeFromUtc(dateTime.ToUniversalTime(), tz),
            _ => dateTime
        };

        return DateTime.SpecifyKind(converted, DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Converts a <see cref="DateTime"/> (where Unspecified is interpreted as <see cref="GetEffectiveTimeZone"/>)
    /// into a UTC <see cref="DateTime"/>.
    /// </summary>
    public static DateTime ToUtc(DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Utc)
            return dateTime;

        if (dateTime.Kind == DateTimeKind.Local)
            return dateTime.ToUniversalTime();

        var tz = GetEffectiveTimeZone();
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified), tz);
    }

    /// <summary>
    /// Converts a <see cref="DateTime"/> to a <see cref="DateTimeOffset"/> anchored in
    /// <see cref="GetEffectiveTimeZone"/> (used for AlarmManager Unix epoch scheduling).
    /// </summary>
    public static DateTimeOffset ToAppDateTimeOffset(DateTime dateTime)
    {
        var utc = ToUtc(dateTime);
        return new DateTimeOffset(utc, TimeSpan.Zero);
    }

    private static bool IsDistinguishableTimeZone(TimeZoneInfo? tz)
    {
        if (tz == null || string.IsNullOrWhiteSpace(tz.Id))
            return false;

        if (IsGenericUtcOrIndeterminateId(tz.Id))
            return false;

        // If a runtime failed to load tzdata and mapped Local to a 0-offset generic zone named "Local",
        // double-check on Android via Java.Util.TimeZone.Default before trusting it.
#if ANDROID
        if (tz.Id.Equals("Local", StringComparison.OrdinalIgnoreCase) && tz.BaseUtcOffset == TimeSpan.Zero)
        {
            var javaId = Java.Util.TimeZone.Default?.ID;
            if (string.IsNullOrWhiteSpace(javaId) || IsGenericUtcOrIndeterminateId(javaId))
                return false;
        }
#endif

        return true;
    }

    private static bool IsGenericUtcOrIndeterminateId(string id)
    {
        var trimmed = id.Trim();
        return trimmed.Equals("UTC", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("Etc/UTC", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("Etc/GMT", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("GMT", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("GMT+00:00", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("GMT-00:00", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("Z", StringComparison.OrdinalIgnoreCase);
    }

    private static TimeZoneInfo CreateAsiaManilaTimeZone()
    {
        if (TimeZoneInfo.TryFindSystemTimeZoneById(ManilaIanaId, out var iana))
            return iana;

        if (TimeZoneInfo.TryFindSystemTimeZoneById(ManilaWindowsId, out var win))
            return win;

        return TimeZoneInfo.CreateCustomTimeZone(
            ManilaIanaId,
            TimeSpan.FromHours(8),
            "(UTC+08:00) Manila",
            "Philippine Standard Time");
    }
}
