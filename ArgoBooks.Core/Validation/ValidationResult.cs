namespace ArgoBooks.Core.Validation;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Whether the validation passed.
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// List of validation errors.
    /// </summary>
    public List<ValidationError> Errors { get; } = [];

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success() => new();

    /// <summary>
    /// Creates a failed validation result with a single error.
    /// </summary>
    public static ValidationResult Failure(string propertyName, string message)
    {
        var result = new ValidationResult();
        result.AddError(propertyName, message);
        return result;
    }

    /// <summary>
    /// Adds an error to the result.
    /// </summary>
    public void AddError(string propertyName, string message)
    {
        Errors.Add(new ValidationError(propertyName, message));
    }

    /// <summary>
    /// Merges another validation result into this one.
    /// </summary>
    public void Merge(ValidationResult other)
    {
        Errors.AddRange(other.Errors);
    }

    /// <summary>
    /// Gets all error messages as a single string.
    /// </summary>
    public string GetErrorMessage(string separator = "\n")
    {
        return string.Join(separator, Errors.Select(e => e.Message));
    }

    /// <summary>
    /// Gets errors for a specific property.
    /// </summary>
    public IEnumerable<ValidationError> GetErrorsForProperty(string propertyName)
    {
        return Errors.Where(e => e.PropertyName == propertyName);
    }
}

/// <summary>
/// Represents a single validation error.
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Name of the property that failed validation.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Error message.
    /// </summary>
    public string Message { get; }

    public ValidationError(string propertyName, string message)
    {
        PropertyName = propertyName;
        Message = message;
    }
}
