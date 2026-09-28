namespace OSCU.Application.Common.Interfaces;

/// <summary>
/// The system clock, behind an interface so that time is injectable.
/// </summary>
/// <remarks>
/// A direct call to <c>DateTime.UtcNow</c> inside a service makes any
/// assertion about <c>CreatedDate</c> untestable without sleeping or
/// asserting a tolerance window. Injecting the clock lets the tests pin it to
/// a fixed instant and assert exact equality.
/// </remarks>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
