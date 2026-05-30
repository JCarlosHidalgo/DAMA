using Backend.Dtos;
using Backend.Dtos.Uniques.Input;
using Backend.Validators.Scheduleds;
using Backend.Validators.Uniques;

using FluentValidation.Results;

namespace Test.Validators.Uniques;

[TestFixture]
public class CreateUniqueClassDtoValidatorTests
{
    private const string InvalidTimeRangeMessage = "StartTime debe ser menor que EndTime";
    private const string InvalidCourseIdMessage = "CourseId es requerido";
    private const string InvalidDateMessage = "Date fuera de rango razonable";
    private const string TooLongExternalReferenceMessage = "ExternalReference no debe exceder 128 caracteres";
    private const string EmptyTeachersMessage = "Teachers debe contener al menos un docente";
    private const string DuplicateTeacherMessage = "Teachers contiene TeacherId duplicados";

    private CreateUniqueClassDtoValidator validator = null!;

    [SetUp]
    public void SetUp() => validator = new CreateUniqueClassDtoValidator();

    private static CreateUniqueClassDto ValidPayload() => new()
    {
        Date = new DateOnly(2025, 6, 15),
        MaxStudentLimit = 0,
        StartTime = new TimeOnly(9, 0),
        EndTime = new TimeOnly(10, 0),
        CourseId = Guid.NewGuid(),
        Teachers =
        [
            new ClassTeacherDto { TeacherId = Guid.NewGuid(), TeacherName = "Profesor" }
        ],
        ExternalReference = null
    };

    [Test]
    public async Task Validate_WithValidPayload_IsValid()
    {
        ValidationResult result = await validator.ValidateAsync(ValidPayload());

        Assert.That(result.IsValid, Is.True);
    }

    [TestCase(-1)]
    [TestCase(1001)]
    public async Task Validate_WithMaxStudentLimitOutsideRange_FailsWithInvalidMaxStudentLimitMessage(int maxStudentLimit)
    {
        CreateUniqueClassDto request = ValidPayload();
        request.MaxStudentLimit = maxStudentLimit;

        ValidationResult result = await validator.ValidateAsync(request);

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
        CreateUniqueClassDto request = ValidPayload();
        request.MaxStudentLimit = maxStudentLimit;

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WithStartTimeAfterEndTime_FailsWithInvalidTimeRangeMessage()
    {
        CreateUniqueClassDto request = ValidPayload();
        request.StartTime = new TimeOnly(11, 0);
        request.EndTime = new TimeOnly(10, 0);

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == InvalidTimeRangeMessage), Is.True);
        });
    }

    [Test]
    public async Task Validate_WithStartTimeBeforeWindow_FailsWithInvalidTimeWindowMessage()
    {
        CreateUniqueClassDto request = ValidPayload();
        request.StartTime = new TimeOnly(0, 30);
        request.EndTime = new TimeOnly(2, 0);

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == CreateScheduledClassDtoValidator.InvalidTimeWindowMessage), Is.True);
        });
    }

    [Test]
    public async Task Validate_WithEndTimeAfterWindow_FailsWithInvalidTimeWindowMessage()
    {
        CreateUniqueClassDto request = ValidPayload();
        request.StartTime = new TimeOnly(22, 0);
        request.EndTime = new TimeOnly(23, 30);

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == CreateScheduledClassDtoValidator.InvalidTimeWindowMessage), Is.True);
        });
    }

    [Test]
    public async Task Validate_WithEmptyCourseId_FailsWithInvalidCourseIdMessage()
    {
        CreateUniqueClassDto request = ValidPayload();
        request.CourseId = Guid.Empty;

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == InvalidCourseIdMessage), Is.True);
        });
    }

    [TestCase(1999, 12, 31)]
    [TestCase(2101, 1, 1)]
    public async Task Validate_WithDateOutsideRange_FailsWithInvalidDateMessage(int year, int month, int day)
    {
        CreateUniqueClassDto request = ValidPayload();
        request.Date = new DateOnly(year, month, day);

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == InvalidDateMessage), Is.True);
        });
    }

    [TestCase(2000, 1, 1)]
    [TestCase(2100, 12, 31)]
    public async Task Validate_WithDateAtBoundary_IsValid(int year, int month, int day)
    {
        CreateUniqueClassDto request = ValidPayload();
        request.Date = new DateOnly(year, month, day);

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WithEmptyTeachersList_FailsWithEmptyTeachersMessage()
    {
        CreateUniqueClassDto request = ValidPayload();
        request.Teachers = [];

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == EmptyTeachersMessage), Is.True);
        });
    }

    [Test]
    public async Task Validate_WithDuplicateTeacherIds_FailsWithDuplicateTeacherMessage()
    {
        var teacherId = Guid.NewGuid();
        CreateUniqueClassDto request = ValidPayload();
        request.Teachers =
        [
            new ClassTeacherDto { TeacherId = teacherId, TeacherName = "A" },
            new ClassTeacherDto { TeacherId = teacherId, TeacherName = "B" }
        ];

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == DuplicateTeacherMessage), Is.True);
        });
    }

    [Test]
    public async Task Validate_WithExternalReferenceOver128Chars_FailsWithTooLongExternalReferenceMessage()
    {
        CreateUniqueClassDto request = ValidPayload();
        request.ExternalReference = new string('x', 129);

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == TooLongExternalReferenceMessage), Is.True);
        });
    }
}
