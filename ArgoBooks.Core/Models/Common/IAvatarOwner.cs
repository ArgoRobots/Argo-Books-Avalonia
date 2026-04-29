namespace ArgoBooks.Core.Models.Common;

/// <summary>
/// Marks an entity that can carry an optional avatar image stored under the
/// company temp directory. Implemented by Customer and Supplier so CompanyManager
/// can share a single set of avatar storage helpers.
/// </summary>
public interface IAvatarOwner
{
    string Id { get; set; }

    /// <summary>
    /// Relative path (within the company temp directory) to the avatar image,
    /// or null if no avatar is set.
    /// </summary>
    string? AvatarFileName { get; set; }

    DateTime UpdatedAt { get; set; }
}
