using Backend.Dtos.Groups.Input;
using Backend.Validators.Groups;

using FluentValidation.Results;

namespace Test.Validators.Groups;

[TestFixture]
public class UpdateClassGroupDtoValidatorTests
{
    private UpdateClassGroupDtoValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new UpdateClassGroupDtoValidator();

    [Test]
    public async Task Validate_WithValidPayload_IsValid()
    {
        var request = new UpdateClassGroupDto { Name = "Grupo Demo" };

        ValidationResult result = await _validator.ValidateAsync(request);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WithEmptyName_FailsWithInvalidNameMessage()
    {
        var request = new UpdateClassGroupDto { Name = string.Empty };

        ValidationResult result = await _validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == CreateClassGroupDtoValidator.InvalidNameMessage), Is.True);
        });
    }

    [Test]
    public async Task Validate_WithNameOver200Chars_FailsWithInvalidNameMessage()
    {
        var request = new UpdateClassGroupDto { Name = new string('a', 201) };

        ValidationResult result = await _validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == CreateClassGroupDtoValidator.InvalidNameMessage), Is.True);
        });
    }
}
