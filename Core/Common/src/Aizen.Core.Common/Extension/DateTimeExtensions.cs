namespace Aizen.Core.Common.Extension;

public static class DateTimeExtensions
{
    public static DateTime GetStartOfWeek(this DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }
}