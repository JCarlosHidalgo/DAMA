using Backend.Dtos.Users.Input;
using Backend.Validators.Users;

using FluentValidation.Results;

namespace Test.Validators.Users;

[TestFixture]
public class UserSearchQueryDtoValidatorTests
{
    private UserSearchQueryDtoValidator _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new UserSearchQueryDtoValidator();

    [Test]
    public async Task Validate_WithValidName_IsValid()
    {
        UserSearchQueryDto query = new() { Name = "validQuery" };

        ValidationResult result = await _sut.ValidateAsync(query);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WhenNameBelowMinLength_FailsWithInvalidUsernameMessage()
    {
        UserSearchQueryDto query = new() { Name = "ab" };

        ValidationResult result = await _sut.ValidateAsync(query);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorMessage, Is.EqualTo(RegisterCredentialsDtoValidator.InvalidUsernameMessage));
        });
    }

    [Test]
    public async Task Validate_WhenNameAboveMaxLength_FailsWithInvalidUsernameMessage()
    {
        UserSearchQueryDto query = new()
        {
            Name = new string('a', RegisterCredentialsDtoValidator.MaxUsernameLength + 1)
        };

        ValidationResult result = await _sut.ValidateAsync(query);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorMessage, Is.EqualTo(RegisterCredentialsDtoValidator.InvalidUsernameMessage));
        });
    }

    [Test]
    public async Task Validate_WhenNameContainsInvalidCharacter_FailsWithInvalidUsernameMessage()
    {
        UserSearchQueryDto query = new() { Name = "bad_query!" };

        ValidationResult result = await _sut.ValidateAsync(query);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorMessage, Is.EqualTo(RegisterCredentialsDtoValidator.InvalidUsernameMessage));
        });
    }
}
