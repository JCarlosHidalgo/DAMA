using Backend.Dtos.Users.Input;
using Backend.Validators.Users;

using FluentValidation.Results;

namespace Test.Validators.Users;

[TestFixture]
public class RegisterCredentialsDtoValidatorTests
{
    private RegisterCredentialsDtoValidator sut = null!;

    [SetUp]
    public void SetUp() => sut = new RegisterCredentialsDtoValidator();

    [Test]
    public async Task Validate_WithValidCredentials_IsValid()
    {
        RegisterCredentialsDto credentials = new() { Username = "validUserName", Password = "Strong#1" };

        ValidationResult result = await sut.ValidateAsync(credentials);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WhenUsernameBelowMinLength_FailsWithInvalidUsernameMessage()
    {
        RegisterCredentialsDto credentials = new() { Username = "abc", Password = "Strong#1" };

        ValidationResult result = await sut.ValidateAsync(credentials);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorMessage, Is.EqualTo(RegisterCredentialsDtoValidator.InvalidUsernameMessage));
        });
    }

    [Test]
    public async Task Validate_WhenUsernameAboveMaxLength_FailsWithInvalidUsernameMessage()
    {
        RegisterCredentialsDto credentials = new()
        {
            Username = new string('a', RegisterCredentialsDtoValidator.MaxUsernameLength + 1),
            Password = "Strong#1"
        };

        ValidationResult result = await sut.ValidateAsync(credentials);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorMessage, Is.EqualTo(RegisterCredentialsDtoValidator.InvalidUsernameMessage));
        });
    }

    [Test]
    public async Task Validate_WhenUsernameContainsInvalidCharacter_FailsWithInvalidUsernameMessage()
    {
        RegisterCredentialsDto credentials = new() { Username = "user_with_underscore", Password = "Strong#1" };

        ValidationResult result = await sut.ValidateAsync(credentials);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorMessage, Is.EqualTo(RegisterCredentialsDtoValidator.InvalidUsernameMessage));
        });
    }

    [Test]
    public async Task Validate_WhenPasswordBelowMinLength_FailsWithInvalidPasswordMessage()
    {
        RegisterCredentialsDto credentials = new() { Username = "validUserName", Password = "1234" };

        ValidationResult result = await sut.ValidateAsync(credentials);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorMessage, Is.EqualTo(RegisterCredentialsDtoValidator.InvalidPasswordMessage));
        });
    }

    [Test]
    public async Task Validate_WhenPasswordAboveMaxLength_FailsWithInvalidPasswordMessage()
    {
        RegisterCredentialsDto credentials = new()
        {
            Username = "validUserName",
            Password = new string('a', RegisterCredentialsDtoValidator.MaxPasswordLength + 1)
        };

        ValidationResult result = await sut.ValidateAsync(credentials);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorMessage, Is.EqualTo(RegisterCredentialsDtoValidator.InvalidPasswordMessage));
        });
    }

    [Test]
    public async Task Validate_WhenPasswordContainsForbiddenCharacter_FailsWithInvalidPasswordMessage()
    {
        RegisterCredentialsDto credentials = new() { Username = "validUserName", Password = "ánbidde<>" };

        ValidationResult result = await sut.ValidateAsync(credentials);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorMessage, Is.EqualTo(RegisterCredentialsDtoValidator.InvalidPasswordMessage));
        });
    }
}
