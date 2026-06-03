using Backend.Dtos.Users.Input;
using Backend.Validators.Users;

using FluentValidation.Results;

namespace Test.Validators.Users;

[TestFixture]
public class LoginCredentialsDtoValidatorTests
{
    private LoginCredentialsDtoValidator _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new LoginCredentialsDtoValidator();

    [Test]
    public async Task Validate_WithValidCredentials_IsValid()
    {
        LoginCredentialsDto credentials = new() { Username = "validUserName", Password = "12345" };

        ValidationResult result = await _sut.ValidateAsync(credentials);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WhenUsernameBelowMinLength_FailsWithInvalidPayloadMessage()
    {
        LoginCredentialsDto credentials = new() { Username = "abc", Password = "12345" };

        ValidationResult result = await _sut.ValidateAsync(credentials);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorMessage, Is.EqualTo(LoginCredentialsDtoValidator.InvalidPayloadMessage));
        });
    }

    [Test]
    public async Task Validate_WhenPasswordAboveMaxLength_FailsWithInvalidPayloadMessage()
    {
        LoginCredentialsDto credentials = new()
        {
            Username = "validUserName",
            Password = new string('x', RegisterCredentialsDtoValidator.MaxPasswordLength + 1)
        };

        ValidationResult result = await _sut.ValidateAsync(credentials);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorMessage, Is.EqualTo(LoginCredentialsDtoValidator.InvalidPayloadMessage));
        });
    }

    [Test]
    public async Task Validate_DoesNotEnforceUsernameRegex()
    {
        LoginCredentialsDto credentials = new() { Username = "user_with_underscore", Password = "12345" };

        ValidationResult result = await _sut.ValidateAsync(credentials);

        Assert.That(result.IsValid, Is.True);
    }
}
