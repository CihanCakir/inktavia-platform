using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Aizen.Core.EFCore.Converter;
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter() : base(
        v => v,
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}