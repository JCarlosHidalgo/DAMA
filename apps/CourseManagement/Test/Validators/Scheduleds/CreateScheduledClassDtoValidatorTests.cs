using Backend.Dtos;
using Backend.Dtos.Scheduleds.Input;
using Backend.Validators.Scheduleds;

using FluentValidation.Results;

namespace Test.Validators.Scheduleds;

[TestFixture]
public class CreateScheduledClassDtoValidatorTests
{
    private const string InvalidDayOfWeekMessage = "DayOfWeekIndex debe estar entre 1 y 7";
    private const string InvalidTimeRangeMessage = "StartTime debe ser menor que EndTime";
    private const string InvalidCourseIdMessage = "CourseId es requerido";
    private const string InvalidGroupIdMessage = "GroupId es requerido";
    private const string TooLongExternalReferenceMessage = "ExternalReference no debe exceder 128 caracteres";
    private const string EmptyTeachersMessage = "Teachers debe contener al menos un docente";
    private const string DuplicateTeacherMessage = "Teachers contiene TeacherId duplicados";

    private CreateScheduledClassDtoValidator validator = null!;

    [SetUp]
    public void SetUp() => validator = new CreateScheduledClassDtoValidator();

    private static CreateScheduledClassDto ValidPayload() => new()
    {
        DayOfWeekIndex = 1,
        MaxStudentLimit = 0,
        StartTime = new TimeOnly(9, 0),
        EndTime = new TimeOnly(10, 0),
        CourseId = Guid.NewGuid(),
        GroupId = Guid.NewGuid(),
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

    [TestCase(0)]
    [TestCase(8)]
    public async Task Validate_WithDayOfWeekIndexOutsideRange_FailsWithInvalidDayOfWeekMessage(int dayOfWeekIndex)
    {
        CreateScheduledClassDto request = ValidPayload();
        request.DayOfWeekIndex = dayOfWeekIndex;

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == InvalidDayOfWeekMessage), Is.True);
        });
    }

    [TestCase(-1)]
    [TestCase(1001)]
    public async Task Validate_WithMaxStudentLimitOutsideRange_FailsWithInvalidMaxStudentLimitMessage(int maxStudentLimit)
    {
        CreateScheduledClassDto request = ValidPayload();
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
        CreateScheduledClassDto request = ValidPayload();
        request.MaxStudentLimit = maxStudentLimit;

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.That(result.IsValid, Is.True);
    }

    [TestCase(1)]
    [TestCase(7)]
    public async Task Validate_WithDayOfWeekIndexAtBoundary_IsValid(int dayOfWeekIndex)
    {
        CreateScheduledClassDto request = ValidPayload();
        request.DayOfWeekIndex = dayOfWeekIndex;

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WithStartTimeEqualToEndTime_FailsWithInvalidTimeRangeMessage()
    {
        CreateScheduledClassDto request = ValidPayload();
        request.StartTime = new TimeOnly(10, 0);
        request.EndTime = new TimeOnly(10, 0);

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == InvalidTimeRangeMessage), Is.True);
        });
    }

    [Test]
    public async Task Validate_WithStartTimeBefore0100_FailsWithInvalidTimeWindowMessage()
    {
        CreateScheduledClassDto request = ValidPayload();
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
    public async Task Validate_WithEndTimeAfter2300_FailsWithInvalidTimeWindowMessage()
    {
        CreateScheduledClassDto request = ValidPayload();
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
    public async Task Validate_WithWindowExactlyAtBoundary_IsValid()
    {
        CreateScheduledClassDto request = ValidPayload();
        request.StartTime = new TimeOnly(1, 0);
        request.EndTime = new TimeOnly(23, 0);

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WithEmptyCourseId_FailsWithInvalidCourseIdMessage()
    {
        CreateScheduledClassDto request = ValidPayload();
        request.CourseId = Guid.Empty;

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == InvalidCourseIdMessage), Is.True);
        });
    }

    [Test]
    public async Task Validate_WithEmptyGroupId_FailsWithInvalidGroupIdMessage()
    {
        CreateScheduledClassDto request = ValidPayload();
        request.GroupId = Guid.Empty;

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == InvalidGroupIdMessage), Is.True);
        });
    }

    [Test]
    public async Task Validate_WithNullTeachers_FailsWithEmptyTeachersMessage()
    {
        CreateScheduledClassDto request = ValidPayload();
        request.Teachers = null!;

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == EmptyTeachersMessage), Is.True);
        });
    }

    [Test]
    public async Task Validate_WithEmptyTeachersList_FailsWithEmptyTeachersMessage()
    {
        CreateScheduledClassDto request = ValidPayload();
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
        CreateScheduledClassDto request = ValidPayload();
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
    public async Task Validate_WithInvalidTeacherEntry_FailsViaChildValidator()
    {
        CreateScheduledClassDto request = ValidPayload();
        request.Teachers =
        [
            new ClassTeacherDto { TeacherId = Guid.Empty, TeacherName = "Sin Id" }
        ];

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == "TeacherId es requerido"), Is.True);
        });
    }

    [Test]
    public async Task Validate_WithNullExternalReference_IsValid()
    {
        CreateScheduledClassDto request = ValidPayload();
        request.ExternalReference = null;

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WithExternalReferenceOver128Chars_FailsWithTooLongExternalReferenceMessage()
    {
        CreateScheduledClassDto request = ValidPayload();
        request.ExternalReference = new string('x', 129);

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == TooLongExternalReferenceMessage), Is.True);
        });
    }
}
