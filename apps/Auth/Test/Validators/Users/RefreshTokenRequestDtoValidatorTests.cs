using Backend.Dtos.Users.Input;
using Backend.Validators.Users;

using FluentValidation.Results;

namespace Test.Validators.Users;

[TestFixture]
public class RefreshTokenRequestDtoValidatorTests
{
    private RefreshTokenRequestDtoValidator _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new RefreshTokenRequestDtoValidator();

    [Test]
    public async Task Validate_WithNonEmptyToken_IsValid()
    {
        RefreshTokenRequestDto request = new() { RefreshToken = "a-refresh-token" };

        ValidationResult result = await _sut.ValidateAsync(request);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WhenTokenEmpty_FailsWithInvalidPayloadMessage()
    {
        RefreshTokenRequestDto request = new() { RefreshToken = string.Empty };

        ValidationResult result = await _sut.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorMessage, Is.EqualTo(RefreshTokenRequestDtoValidator.InvalidPayloadMessage));
        });
    }
}
