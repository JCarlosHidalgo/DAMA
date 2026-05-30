using Backend.Dtos.Users.Input;
using Backend.Validators.Users;

using FluentValidation.Results;

namespace Test.Validators.Users;

[TestFixture]
public class UpdateUsernameDtoValidatorTests
{
    private UpdateUsernameDtoValidator sut = null!;

    [SetUp]
    public void SetUp() => sut = new UpdateUsernameDtoValidator();

    [Test]
    public async Task Validate_WithValidUsername_IsValid()
    {
        UpdateUsernameDto request = new() { Username = "renamedUserName" };

        ValidationResult result = await sut.ValidateAsync(request);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WhenUsernameBelowMinLength_FailsWithInvalidUsernameMessage()
    {
        UpdateUsernameDto request = new() { Username = "ab" };

        ValidationResult result = await sut.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorMessage, Is.EqualTo(RegisterCredentialsDtoValidator.InvalidUsernameMessage));
        });
    }

    [Test]
    public async Task Validate_WhenUsernameAboveMaxLength_FailsWithInvalidUsernameMessage()
    {
        UpdateUsernameDto request = new()
        {
            Username = new string('a', RegisterCredentialsDtoValidator.MaxUsernameLength + 1)
        };

        ValidationResult result = await sut.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorMessage, Is.EqualTo(RegisterCredentialsDtoValidator.InvalidUsernameMessage));
        });
    }

    [Test]
    public async Task Validate_WhenUsernameContainsInvalidCharacter_FailsWithInvalidUsernameMessage()
    {
        UpdateUsernameDto request = new() { Username = "bad_name_with_$" };

        ValidationResult result = await sut.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorMessage, Is.EqualTo(RegisterCredentialsDtoValidator.InvalidUsernameMessage));
        });
    }
}
