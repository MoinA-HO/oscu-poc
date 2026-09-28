namespace OSCU.Application.Common.Extensions;

public static class DateTimeExtensions
{
    /// <summary>
    /// Normalises a <see cref="DateTime"/> arriving from the transport layer
    /// into UTC, ready for a Postgres <c>timestamp with time zone</c> column.
    /// </summary>
    /// <remarks>
    /// System.Text.Json produces:
    /// <list type="bullet">
    ///   <item><c>"...Z"</c> or <c>"...+00:00"</c> — Kind Utc, passed through.</item>
    ///   <item><c>"...+01:00"</c> — Kind Local, converted.</item>
    ///   <item><c>"2026-08-01T09:30:00"</c> — Kind Unspecified.</item>
    /// </list>
    /// An Unspecified value is treated as already being UTC rather than
    /// converted from server-local time. Converting would make the stored
    /// instant depend on the timezone of whichever App Service happened to
    /// handle the request, which is not something we want to be true.
    /// </remarks>
    public static DateTime ToUtc(this DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
