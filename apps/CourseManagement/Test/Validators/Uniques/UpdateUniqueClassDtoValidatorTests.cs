using Backend.Dtos;
using Backend.Dtos.Uniques.Input;
using Backend.Validators.Scheduleds;
using Backend.Validators.Uniques;

using FluentValidation.Results;

namespace Test.Validators.Uniques;

[TestFixture]
public class UpdateUniqueClassDtoValidatorTests
{
    private const string InvalidTimeRangeMessage = "StartTime debe ser menor que EndTime";
    private const string InvalidDateMessage = "Date fuera de rango razonable";
    private const string EmptyTeachersMessage = "Teachers debe contener al menos un docente";

    private UpdateUniqueClassDtoValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new UpdateUniqueClassDtoValidator();

    private static UpdateUniqueClassDto ValidPayload() => new()
    {
        Date = new DateOnly(2025, 6, 15),
        MaxStudentLimit = 0,
        StartTime = new TimeOnly(9, 0),
        EndTime = new TimeOnly(10, 0),
        Teachers =
        [
            new ClassTeacherDto { TeacherId = Guid.NewGuid(), TeacherName = "Profesor" }
        ]
    };

    [Test]
    public async Task Validate_WithValidPayload_IsValid()
    {
        ValidationResult result = await _validator.ValidateAsync(ValidPayload());

        Assert.That(result.IsValid, Is.True);
    }

    [TestCase(-1)]
    [TestCase(1001)]
    public async Task Validate_WithMaxStudentLimitOutsideRange_FailsWithInvalidMaxStudentLimitMessage(int maxStudentLimit)
    {
        UpdateUniqueClassDto request = ValidPayload();
        request.MaxStudentLimit = maxStudentLimit;

        ValidationResult result = await _validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == CreateScheduledClassDtoValidator.InvalidMaxStudentLimitMessage), Is.True);
        });
    }

    [TestCase(0)]
    [TestCase(1000)]
    public async Task Validate_WithMaxStudentLimitAtBoundary_IsValid(int maxStudentLimit)
    {
        UpdateUniqueClassDto request = ValidPayload();
        request.MaxStudentLimit = maxStudentLimit;

        ValidationResult result = await _validator.ValidateAsync(request);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WithStartTimeAfterEndTime_FailsWithInvalidTimeRangeMessage()
    {
        UpdateUniqueClassDto request = ValidPayload();
        request.StartTime = new TimeOnly(11, 0);
        request.EndTime = new TimeOnly(10, 0);

        ValidationResult result = await _validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == InvalidTimeRangeMessage), Is.True);
        });
    }

    [Test]
    public async Task Validate_WithWindowViolation_FailsWithInvalidTimeWindowMessage()
    {
        UpdateUniqueClassDto request = ValidPayload();
        request.StartTime = new TimeOnly(0, 30);
        request.EndTime = new TimeOnly(2, 0);

        ValidationResult result = await _validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == CreateScheduledClassDtoValidator.InvalidTimeWindowMessage), Is.True);
        });
    }

    [Test]
    public async Task Validate_WithDateOutsideRange_FailsWithInvalidDateMessage()
    {
        UpdateUniqueClassDto request = ValidPayload();
        request.Date = new DateOnly(1999, 1, 1);

        ValidationResult result = await _validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == InvalidDateMessage), Is.True);
        });
    }

    [Test]
    public async Task Validate_WithEmptyTeachersList_FailsWithEmptyTeachersMessage()
    {
        UpdateUniqueClassDto request = ValidPayload();
        request.Teachers = [];

        ValidationResult result = await _validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == EmptyTeachersMessage), Is.True);
        });
    }
}
