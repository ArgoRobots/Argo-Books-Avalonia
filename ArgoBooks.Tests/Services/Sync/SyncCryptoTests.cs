using System.Text;
using ArgoBooks.Core.Services.Sync;
using Xunit;

namespace ArgoBooks.Tests.Services.Sync;

public class SyncCryptoTests
{
    [Fact]
    public void Encrypt_then_Decrypt_round_trips()
    {
        var key = SyncCrypto.GenerateSyncKey();
        var data = Encoding.UTF8.GetBytes("{\"revenue\":123}");
        var payload = SyncCrypto.Encrypt(data, key);
        var back = SyncCrypto.Decrypt(payload, key);
        Assert.Equal(data, back);
    }

    [Fact]
    public void Decrypt_with_wrong_key_fails()
    {
        var payload = SyncCrypto.Encrypt(Encoding.UTF8.GetBytes("x"), SyncCrypto.GenerateSyncKey());
        Assert.ThrowsAny<System.Exception>(() => SyncCrypto.Decrypt(payload, SyncCrypto.GenerateSyncKey()));
    }

    [Fact]
    public void CompanyUid_is_hex_within_bounds()
    {
        var uid = SyncCrypto.GenerateCompanyUid();
        Assert.Matches("^[0-9a-f]{40,64}$", uid);
    }
}
