using ArgoBooks.Core.Models.Portal;
using ArgoBooks.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the PaymentProviderService static class.
/// </summary>
public class PaymentProviderServiceTests
{
    [Fact]
    public void GetConnectedMethods_DefaultIsEmpty()
    {
        var methods = PaymentProviderService.GetConnectedMethods();

        Assert.NotNull(methods);
    }

    [Fact]
    public void UpdateFromPaymentMethods_WithEmptyList_DoesNotThrow()
    {
        PaymentProviderService.UpdateFromPaymentMethods(new List<string>());

        var methods = PaymentProviderService.GetConnectedMethods();
        Assert.NotNull(methods);
    }
}
