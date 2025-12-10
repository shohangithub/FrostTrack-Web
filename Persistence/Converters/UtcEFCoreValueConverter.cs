using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Persistence.Converters;

/// <summary>
/// EF Core value converter to ensure DateTime is always stored and retrieved as UTC in the database
/// </summary>
public class UtcDateTimeValueConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeValueConverter()
        : base(
            v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}

/// <summary>
/// EF Core value converter for nullable DateTime
/// </summary>
public class UtcNullableDateTimeValueConverter : ValueConverter<DateTime?, DateTime?>
{
    public UtcNullableDateTimeValueConverter()
        : base(
            v => v.HasValue
                ? (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime())
                : (DateTime?)null,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : (DateTime?)null)
    {
    }
}
