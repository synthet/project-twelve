using NUnit.Framework;

/// <summary>
/// Known-answer vectors for <see cref="SandboxHash"/>. These exact values are
/// duplicated in the JavaScript port's test (<c>tools/world-viz/test/hash.test.js</c>);
/// the two suites together are the offline guarantee that the C# and JS hashes agree
/// bit-for-bit without needing Unity to author a fixture. If you change the hash mix,
/// regenerate the vectors and update BOTH suites (and the fluid sim's copy).
/// </summary>
public sealed class SandboxHashTests
{
    private static readonly object[] HashVectors =
    {
        new object[] { 0u, 0u, 0u, 3463101980u },
        new object[] { 1u, 2u, 3u, 1104645647u },
        new object[] { 1337u, 1u, 0u, 2329107406u },
        new object[] { 1337u, 1u, 1u, 1599356354u },
        new object[] { 1337u, 1u, 4294967295u, 4015006146u },
        new object[] { 4294967295u, 4294967295u, 4294967295u, 1646814764u },
    };

    [TestCaseSource(nameof(HashVectors))]
    public void HashMatchesKnownAnswer(uint a, uint b, uint c, uint expected)
    {
        Assert.AreEqual(expected, SandboxHash.Hash(a, b, c), $"Hash({a}, {b}, {c})");
    }

    [Test]
    public void HashIsDeterministic()
    {
        Assert.AreEqual(SandboxHash.Hash(42u, 7u, 99u), SandboxHash.Hash(42u, 7u, 99u));
    }

    [Test]
    public void UnitFloatMapsToUnitInterval()
    {
        Assert.AreEqual(0f, SandboxHash.UnitFloat(0u));
        Assert.AreEqual(0.5f, SandboxHash.UnitFloat(2147483648u));
        // uint.MaxValue / 2^32 narrows to exactly 1f.
        Assert.AreEqual(1f, SandboxHash.UnitFloat(4294967295u));
    }

    [Test]
    public void UnitFloatStaysInRangeForSpread()
    {
        for (uint i = 0; i < 64; i++)
        {
            float v = SandboxHash.UnitFloat(SandboxHash.Hash(i, i * 2654435761u, 0u));
            Assert.That(v, Is.GreaterThanOrEqualTo(0f).And.LessThanOrEqualTo(1f));
        }
    }
}
