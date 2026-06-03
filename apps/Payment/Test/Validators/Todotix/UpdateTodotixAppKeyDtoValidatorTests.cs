using Backend.Dtos.Todotix.Input;
using Backend.Validators.Todotix;

namespace Test.Validators.Todotix;

[TestFixture]
public class UpdateTodotixAppKeyDtoValidatorTests
{
    private UpdateTodotixAppKeyDtoValidator _sut = null!;

    [SetUp]
    public void Setup() => _sut = new UpdateTodotixAppKeyDtoValidator();

    [Test]
    public async Task Validate_LowercaseGuid_Passes()
    {
        var result = await _sut.ValidateAsync(new UpdateTodotixAppKeyDto { AppKey = "51599bd3-eed3-2826-45a4-a16c2fcc2724" });

        Assert.That(result.IsValid, Is.True);
    }

    [TestCase("")]
    [TestCase("51599BD3-EED3-2826-45A4-A16C2FCC2724")]
    [TestCase("51599bd3eed3282645a4a16c2fcc2724")]
    [TestCase("51599bd3-eed3-2826-45a4-a16c2fcc272")]
    [TestCase("zzzzzzzz-eed3-2826-45a4-a16c2fcc2724")]
    [TestCase("not-a-guid")]
    public async Task Validate_InvalidFormats_Fail(string appKey)
    {
        var result = await _sut.ValidateAsync(new UpdateTodotixAppKeyDto { AppKey = appKey });

        Assert.That(result.IsValid, Is.False);
    }
}
