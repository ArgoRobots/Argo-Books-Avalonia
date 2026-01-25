namespace ArgoBooks.Converters;

/// <summary>
/// Shared utility methods for converters.
/// </summary>
internal static class ConverterUtils
{
    /// <summary>
    /// Checks if two objects are equal using reference equality or Equals.
    /// </summary>
    public static bool AreEqual(object? value1, object? value2)
    {
        if (value1 == null && value2 == null)
            return true;
        if (value1 == null || value2 == null)
            return false;
        return ReferenceEquals(value1, value2) || value1.Equals(value2);
    }
}
