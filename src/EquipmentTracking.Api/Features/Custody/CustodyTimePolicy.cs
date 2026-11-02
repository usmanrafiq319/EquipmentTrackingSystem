namespace EquipmentTracking.Api.Features.Custody;

public static class CustodyTimePolicy
{
    private static readonly TimeZoneInfo KarachiTimeZone = ResolveKarachiTimeZone();

    public static DateTimeOffset DefaultDueAt(DateTimeOffset now)
    {
        var localTomorrow = TimeZoneInfo.ConvertTime(now, KarachiTimeZone).AddDays(1);
        var utc = TimeZoneInfo.ConvertTimeToUtc(localTomorrow.DateTime, KarachiTimeZone);
        return new DateTimeOffset(utc);
    }

    public static DateTimeOffset Normalize(DateTimeOffset value) => value.ToUniversalTime();

    public static string? GetDueStatus(DateTimeOffset? dueAt, DateTimeOffset now)
    {
        if (!dueAt.HasValue)
            return null;

        var dueUtc = dueAt.Value.UtcDateTime;
        var nowUtc = now.UtcDateTime;
        if (dueUtc <= nowUtc)
            return "Overdue";
        return dueUtc <= nowUtc.AddDays(1) ? "DueSoon" : "Active";
    }

    private static TimeZoneInfo ResolveKarachiTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Karachi");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time");
        }
    }
}
