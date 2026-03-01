using ArgoBooks.Converters;
using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.Converters;

/// <summary>
/// Tests for the ChangeTypeToVisibilityConverter class.
/// </summary>
public class ChangeTypeToVisibilityConverterTests
{
    #region Convert Tests

    [Fact]
    public void Convert_MatchingChangeType_ReturnsTrue()
    {
        var converter = new ChangeTypeToVisibilityConverter { TargetType = ChangeType.Modified };

        var result = converter.Convert(ChangeType.Modified, typeof(bool), null, null!);

        Assert.Equal(true, result);
    }

    [Fact]
    public void Convert_NonMatchingChangeType_ReturnsFalse()
    {
        var converter = new ChangeTypeToVisibilityConverter { TargetType = ChangeType.Modified };

        var result = converter.Convert(ChangeType.Added, typeof(bool), null, null!);

        Assert.Equal(false, result);
    }

    [Fact]
    public void Convert_DeletedTarget_MatchesDeleted()
    {
        var converter = new ChangeTypeToVisibilityConverter { TargetType = ChangeType.Deleted };

        var result = converter.Convert(ChangeType.Deleted, typeof(bool), null, null!);

        Assert.Equal(true, result);
    }

    [Fact]
    public void Convert_AddedTarget_MatchesAdded()
    {
        var converter = new ChangeTypeToVisibilityConverter { TargetType = ChangeType.Added };

        var result = converter.Convert(ChangeType.Added, typeof(bool), null, null!);

        Assert.Equal(true, result);
    }

    [Fact]
    public void Convert_NullValue_ReturnsFalse()
    {
        var converter = new ChangeTypeToVisibilityConverter { TargetType = ChangeType.Modified };

        var result = converter.Convert(null, typeof(bool), null, null!);

        Assert.Equal(false, result);
    }

    [Fact]
    public void Convert_NonEnumValue_ReturnsFalse()
    {
        var converter = new ChangeTypeToVisibilityConverter { TargetType = ChangeType.Modified };

        var result = converter.Convert("not an enum", typeof(bool), null, null!);

        Assert.Equal(false, result);
    }

    #endregion

    #region Default Value Tests

    [Fact]
    public void DefaultTargetType_IsModified()
    {
        var converter = new ChangeTypeToVisibilityConverter();

        Assert.Equal(ChangeType.Modified, converter.TargetType);
    }

    #endregion
}
