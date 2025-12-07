namespace ClickstreamProcessorFunction.Models;

/// <summary>
/// Processing outcome for event records
/// </summary>
public enum ProcessingResult
{
    Success,
    ValidationError,
    TransientError,
    PermanentError
}
