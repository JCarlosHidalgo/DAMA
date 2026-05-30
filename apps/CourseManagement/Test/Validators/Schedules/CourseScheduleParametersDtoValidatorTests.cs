using Backend.Dtos.Schedules.Input;
using Backend.Validators.Schedules;

using FluentValidation.Results;

namespace Test.Validators.Schedules;

[TestFixture]
public class CourseScheduleParametersDtoValidatorTests
{
    private const string InvalidCourseIdMessage = "CourseId es requerido";
    private const string InvalidIndexMessage = "WeekPaginationIndex fuera de rango razonable";

    private CourseScheduleParametersDtoValidator validator = null!;

    [SetUp]
    public void SetUp() => validator = new CourseScheduleParametersDtoValidator();

    [Test]
    public async Task Validate_WithValidPayload_IsValid()
    {
        var request = new CourseScheduleParametersDto
        {
            CourseId = Guid.NewGuid(),
            WeekPaginationIndex = 0
        };

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WithEmptyCourseId_FailsWithInvalidCourseIdMessage()
    {
        var request = new CourseScheduleParametersDto
        {
            CourseId = Guid.Empty,
            WeekPaginationIndex = 0
        };

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == InvalidCourseIdMessage), Is.True);
        });
    }

    [TestCase(-521)]
    [TestCase(521)]
    public async Task Validate_WithIndexOutsideRange_FailsWithInvalidIndexMessage(int weekPaginationIndex)
    {
        var request = new CourseScheduleParametersDto
        {
            CourseId = Guid.NewGuid(),
            WeekPaginationIndex = weekPaginationIndex
        };

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == InvalidIndexMessage), Is.True);
        });
    }

    [TestCase(-520)]
    [TestCase(520)]
    public async Task Validate_WithIndexAtBoundary_IsValid(int weekPaginationIndex)
    {
        var request = new CourseScheduleParametersDto
        {
            CourseId = Guid.NewGuid(),
            WeekPaginationIndex = weekPaginationIndex
        };

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.That(result.IsValid, Is.True);
    }
}
