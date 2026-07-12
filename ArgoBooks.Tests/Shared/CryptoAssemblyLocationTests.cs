using ArgoBooks.Core.Services;
using ArgoBooks.Core.Security;
using Xunit;

namespace ArgoBooks.Tests.Shared;

public class CryptoAssemblyLocationTests
{
    [Fact]
    public void EncryptionService_lives_in_the_shared_assembly()
    {
        Assert.Equal("ArgoBooks.Shared", typeof(EncryptionService).Assembly.GetName().Name);
    }

    [Fact]
    public void KeyDerivation_lives_in_the_shared_assembly()
    {
        Assert.Equal("ArgoBooks.Shared", typeof(KeyDerivation).Assembly.GetName().Name);
    }
}
