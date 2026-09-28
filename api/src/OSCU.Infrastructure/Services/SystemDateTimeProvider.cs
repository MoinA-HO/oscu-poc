using OSCU.Application.Common.Interfaces;

namespace OSCU.Infrastructure.Services;

/// <inheritdoc cref="IDateTimeProvider"/>
public class SystemDateTimeProvider : IDateTimeProvider
{
    // UtcNow, never Now. DateTime.Now carries Kind = Local, which the Referral
    // guard rejects and which Npgsql refuses to write to a
    // 'timestamp with time zone' column.
    public DateTime UtcNow => DateTime.UtcNow;
}
