using ClickstreamProcessorFunction.Models;

namespace ClickstreamProcessorFunction.Services;

/// <summary>
/// Validates clickstream events for required fields and data quality
/// </summary>
public interface IEventValidator
{
    /// <summary>
    /// Validates event schema and required fields
    /// </summary>
    /// <param name="clickEvent">Event to validate</param>
    /// <exception cref="ValidationException">Thrown when validation fails</exception>
    void Validate(ClickstreamEvent clickEvent);
}

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }
}
