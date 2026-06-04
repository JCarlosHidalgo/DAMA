using Backend.Options;

namespace Test.Configuration;

[TestFixture]
public class CurrencyOptionsTests
{
    [Test]
    public void Validate_WithDefaultsBob_DoesNotThrow()
    {
        CurrencyOptions options = new CurrencyOptions();

        Assert.DoesNotThrow(() => options.Validate());
    }

    [Test]
    public void Validate_WhenDefaultNotInAllowed_Throws()
    {
        CurrencyOptions options = new CurrencyOptions
        {
            Default = "USD",
            Allowed = new[] { "BOB" }
        };

        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Test]
    public void Validate_WhenAllowedIsEmpty_Throws()
    {
        CurrencyOptions options = new CurrencyOptions
        {
            Default = "BOB",
            Allowed = Array.Empty<string>()
        };

        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [TestCase("bob")]
    [TestCase("BO")]
    [TestCase("BOLB")]
    [TestCase("B0B")]
    public void Validate_WithMalformedCode_Throws(string malformedCode)
    {
        CurrencyOptions options = new CurrencyOptions
        {
            Default = malformedCode,
            Allowed = new[] { malformedCode }
        };

        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [TestCase("BOB", ExpectedResult = true)]
    [TestCase("USD", ExpectedResult = true)]
    [TestCase("usd", ExpectedResult = false)]
    [TestCase("US", ExpectedResult = false)]
    [TestCase("USDD", ExpectedResult = false)]
    [TestCase(null, ExpectedResult = false)]
    public bool IsValidIso4217Code_ChecksThreeUppercaseLetters(string? code) =>
        CurrencyOptions.IsValidIso4217Code(code);
}
