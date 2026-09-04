using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// One entry in the viewer's document picker.
///
/// The bytes are behind a callback rather than held, because this is what let the pay stub
/// viewer stop rendering every employee at once: a hundred stubs are a hundred of these, and
/// only the selected one is ever produced. A field holding the bytes would quietly undo that.
/// </summary>
public class ViewerDocumentTests
{
    [Fact]
    public async Task ADocument_IsNotRenderedUntilItIsAskedFor()
    {
        int produced = 0;

        var document = new ViewerDocument
        {
            Name = "Dana Smith",
            FileName = "dana-smith.pdf",
            LoadAsync = () =>
            {
                produced++;
                return Task.FromResult(new byte[] { 1, 2, 3 });
            },
        };

        Assert.Equal(0, produced);

        Assert.Equal([1, 2, 3], await document.LoadAsync());
        Assert.Equal(1, produced);
    }

    [Fact]
    public void ADocument_ShowsItsNameInThePicker()
    {
        // The picker binds to the object, so ToString is what the employee's name comes from.
        var document = new ViewerDocument
        {
            Name = "Dana Smith",
            FileName = "dana-smith.pdf",
            LoadAsync = () => Task.FromResult(Array.Empty<byte>()),
        };

        Assert.Equal("Dana Smith", document.ToString());
        Assert.Equal("dana-smith.pdf", document.FileName);
    }
}
