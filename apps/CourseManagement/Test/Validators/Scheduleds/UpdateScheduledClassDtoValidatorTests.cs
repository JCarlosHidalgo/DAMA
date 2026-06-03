using Backend.Dtos;
using Backend.Dtos.Scheduleds.Input;
using Backend.Validators.Scheduleds;

using FluentValidation.Results;

namespace Test.Validators.Scheduleds;

[TestFixture]
public class UpdateScheduledClassDtoValidatorTests
{
    private const string InvalidDayOfWeekMessage = "DayOfWeekIndex debe estar entre 1 y 7";
    private const string InvalidTimeRangeMessage = "StartTime debe ser menor que EndTime";
    private const string EmptyTeachersMessage = "Teachers debe contener al menos un docente";

    private UpdateScheduledClassDtoValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new UpdateScheduledClassDtoValidator();

    private static UpdateScheduledClassDto ValidPayload() => new()
    {
        DayOfWeekIndex = 3,
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
        UpdateScheduledClassDto request = ValidPayload();
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
        UpdateScheduledClassDto request = ValidPayload();
        request.MaxStudentLimit = maxStudentLimit;

        ValidationResult result = await _validator.ValidateAsync(request);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WithDayOfWeekIndexOutsideRange_FailsWithInvalidDayOfWeekMessage()
    {
        UpdateScheduledClassDto request = ValidPayload();
        request.DayOfWeekIndex = 0;

        ValidationResult result = await _validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == InvalidDayOfWeekMessage), Is.True);
        });
    }

    [Test]
    public async Task Validate_WithStartTimeAfterEndTime_FailsWithInvalidTimeRangeMessage()
    {
        UpdateScheduledClassDto request = ValidPayload();
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
    public async Task Validate_WithStartTimeBeforeWindow_FailsWithInvalidTimeWindowMessage()
    {
        UpdateScheduledClassDto request = ValidPayload();
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
    public async Task Validate_WithEndTimeAfterWindow_FailsWithInvalidTimeWindowMessage()
    {
        UpdateScheduledClassDto request = ValidPayload();
        request.StartTime = new TimeOnly(22, 0);
        request.EndTime = new TimeOnly(23, 30);

        ValidationResult result = await _validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == CreateScheduledClassDtoValidator.InvalidTimeWindowMessage), Is.True);
        });
    }

    [Test]
    public async Task Validate_WithEmptyTeachersList_FailsWithEmptyTeachersMessage()
    {
        UpdateScheduledClassDto request = ValidPayload();
        request.Teachers = [];

        ValidationResult result = await _validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == EmptyTeachersMessage), Is.True);
        });
    }
}
