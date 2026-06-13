using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class ReferenceResolverTests
{
    [Fact]
    public void Resolve_NormalizedExactMatch_ReturnsId()
    {
        var index = ReferenceResolver.BuildIndex(new[] { ("CUS-1", "Acme Ltd."), ("CUS-2", "Globex") });
        Assert.Equal("CUS-1", ReferenceResolver.Resolve("  acme  ltd ", index).MatchedId);
    }

    [Fact]
    public void Resolve_AmbiguousFuzzy_ReturnsUnresolved()
    {
        var index = ReferenceResolver.BuildIndex(new[] { ("S-1", "Smith Plumbing"), ("S-2", "Smith Electrical") });
        var r = ReferenceResolver.Resolve("Smith", index);
        Assert.Null(r.MatchedId);
        Assert.True(r.IsAmbiguous);
    }

    [Fact]
    public void Resolve_HighConfidenceFuzzy_ReturnsId()
    {
        // "Globx" is very close to "Globex" and the other entry ("Acme Ltd") is distant
        var index = ReferenceResolver.BuildIndex(new[] { ("CUS-1", "Acme Ltd."), ("CUS-2", "Globex") });
        var r = ReferenceResolver.Resolve("Globx", index);
        Assert.Equal("CUS-2", r.MatchedId);
        Assert.False(r.IsAmbiguous);
    }

    [Fact]
    public void Resolve_NoMatch_ReturnsNullNotAmbiguous()
    {
        var index = ReferenceResolver.BuildIndex(new[] { ("CUS-1", "Acme Ltd."), ("CUS-2", "Globex") });
        var r = ReferenceResolver.Resolve("Totally Different", index);
        Assert.Null(r.MatchedId);
        Assert.False(r.IsAmbiguous);
    }

    [Fact]
    public void BuildIndex_NormalizesDuplicateNames_KeepsFirst()
    {
        // Two entities with names that normalize to the same key: only the first survives
        var index = ReferenceResolver.BuildIndex(new[] { ("A-1", "Alpha Corp."), ("A-2", "Alpha Corp") });
        Assert.Single(index);
        Assert.True(index.ContainsKey("alpha corp"));
        Assert.Equal("A-1", index["alpha corp"]);
    }

    [Fact]
    public void Resolve_ExactMatchWithTrailingPunctuation_ReturnsId()
    {
        // The name "Globex" is stored; query "Globex," (trailing comma) normalizes to "globex"
        var index = ReferenceResolver.BuildIndex(new[] { ("CUS-2", "Globex") });
        Assert.Equal("CUS-2", ReferenceResolver.Resolve("Globex,", index).MatchedId);
    }
}
