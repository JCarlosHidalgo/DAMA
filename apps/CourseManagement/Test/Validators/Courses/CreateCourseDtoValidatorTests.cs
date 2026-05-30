using Backend.Dtos.Courses.Input;
using Backend.Validators.Courses;

using FluentValidation.Results;

namespace Test.Validators.Courses;

[TestFixture]
public class CreateCourseDtoValidatorTests
{
    private const string EmptyNameMessage = "Name es requerido";
    private const string TooLongNameMessage = "Name no debe exceder 100 caracteres";
    private const string TooLongExternalReferenceMessage = "ExternalReference no debe exceder 128 caracteres";

    private CreateCourseDtoValidator validator = null!;

    [SetUp]
    public void SetUp() => validator = new CreateCourseDtoValidator();

    [Test]
    public async Task Validate_WithValidPayload_IsValid()
    {
        var request = new CreateCourseDto
        {
            Name = "Curso Demo",
            ExternalReference = "ref-001"
        };

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WithNullExternalReference_IsValid()
    {
        var request = new CreateCourseDto
        {
            Name = "Curso Demo",
            ExternalReference = null
        };

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WithEmptyName_FailsWithEmptyNameMessage()
    {
        var request = new CreateCourseDto
        {
            Name = string.Empty
        };

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == EmptyNameMessage), Is.True);
        });
    }

    [Test]
    public async Task Validate_WithNameOver100Chars_FailsWithTooLongNameMessage()
    {
        var request = new CreateCourseDto
        {
            Name = new string('a', 101)
        };

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == TooLongNameMessage), Is.True);
        });
    }

    [Test]
    public async Task Validate_WithExternalReferenceOver128Chars_FailsWithTooLongExternalReferenceMessage()
    {
        var request = new CreateCourseDto
        {
            Name = "Curso Demo",
            ExternalReference = new string('x', 129)
        };

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == TooLongExternalReferenceMessage), Is.True);
        });
    }
}
