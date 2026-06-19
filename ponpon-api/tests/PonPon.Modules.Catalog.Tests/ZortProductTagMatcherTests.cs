using System.Text.Json;
using PonPon.Modules.Catalog.Infrastructure.ExternalServices.Zort;

namespace PonPon.Modules.Catalog.Tests;

public sealed class ZortProductTagMatcherTests
{
    public void ArrayContainingLineliffMatches()
    {
        AssertMatches("""{"tag":["Lineliff"]}""", expected: true);
    }

    public void ArrayWithoutLineliffDoesNotMatch()
    {
        AssertMatches("""{"tag":["Other"]}""", expected: false);
    }

    public void StringLineliffMatches()
    {
        AssertMatches("""{"tag":"Lineliff"}""", expected: true);
    }

    public void NullTagDoesNotMatch()
    {
        AssertMatches("""{"tag":null}""", expected: false);
    }

    public void LowercaseLineliffMatches()
    {
        AssertMatches("""{"tag":["lineliff"]}""", expected: true);
    }

    private static void AssertMatches(string json, bool expected)
    {
        using var document = JsonDocument.Parse(json);
        var actual = ZortProductTagMatcher.HasTag(document.RootElement.GetProperty("tag"), ZortProductTagMatcher.LiffTag);
        if (actual != expected)
        {
            throw new InvalidOperationException($"Expected tag match to be {expected}, but was {actual}.");
        }
    }
}
