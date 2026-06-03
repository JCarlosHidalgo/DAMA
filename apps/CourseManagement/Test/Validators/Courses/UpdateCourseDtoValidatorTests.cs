using Backend.Dtos.Courses.Input;
using Backend.Validators.Courses;

using FluentValidation.Results;

namespace Test.Validators.Courses;

[TestFixture]
public class UpdateCourseDtoValidatorTests
{
    private const string EmptyNameMessage = "Name es requerido";
    private const string TooLongNameMessage = "Name no debe exceder 100 caracteres";

    private UpdateCourseDtoValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new UpdateCourseDtoValidator();

    [Test]
    public async Task Validate_WithValidName_IsValid()
    {
        var request = new UpdateCourseDto { Name = "Curso Renombrado" };

        ValidationResult result = await _validator.ValidateAsync(request);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WithEmptyName_FailsWithEmptyNameMessage()
    {
        var request = new UpdateCourseDto { Name = string.Empty };

        ValidationResult result = await _validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == EmptyNameMessage), Is.True);
        });
    }

    [Test]
    public async Task Validate_WithNameOver100Chars_FailsWithTooLongNameMessage()
    {
        var request = new UpdateCourseDto { Name = new string('a', 101) };

        ValidationResult result = await _validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == TooLongNameMessage), Is.True);
        });
    }
}
