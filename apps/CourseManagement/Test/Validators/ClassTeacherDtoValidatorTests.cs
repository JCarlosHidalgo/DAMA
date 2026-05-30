using Backend.Dtos;
using Backend.Validators;

using FluentValidation.Results;

namespace Test.Validators;

[TestFixture]
public class ClassTeacherDtoValidatorTests
{
    private const string InvalidTeacherIdMessage = "TeacherId es requerido";
    private const string EmptyTeacherNameMessage = "TeacherName es requerido";
    private const string TooLongTeacherNameMessage = "TeacherName no debe exceder 200 caracteres";

    private ClassTeacherDtoValidator validator = null!;

    [SetUp]
    public void SetUp() => validator = new ClassTeacherDtoValidator();

    [Test]
    public async Task Validate_WithValidPayload_IsValid()
    {
        var request = new ClassTeacherDto
        {
            TeacherId = Guid.NewGuid(),
            TeacherName = "Profesor Demo"
        };

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WithEmptyTeacherId_FailsWithInvalidTeacherIdMessage()
    {
        var request = new ClassTeacherDto
        {
            TeacherId = Guid.Empty,
            TeacherName = "Profesor Demo"
        };

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == InvalidTeacherIdMessage), Is.True);
        });
    }

    [Test]
    public async Task Validate_WithEmptyTeacherName_FailsWithEmptyTeacherNameMessage()
    {
        var request = new ClassTeacherDto
        {
            TeacherId = Guid.NewGuid(),
            TeacherName = string.Empty
        };

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == EmptyTeacherNameMessage), Is.True);
        });
    }

    [Test]
    public async Task Validate_WithTeacherNameAt200Chars_IsValid()
    {
        var request = new ClassTeacherDto
        {
            TeacherId = Guid.NewGuid(),
            TeacherName = new string('a', 200)
        };

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WithTeacherNameOver200Chars_FailsWithTooLongTeacherNameMessage()
    {
        var request = new ClassTeacherDto
        {
            TeacherId = Guid.NewGuid(),
            TeacherName = new string('a', 201)
        };

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == TooLongTeacherNameMessage), Is.True);
        });
    }
}
