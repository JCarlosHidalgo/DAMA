using Backend.Dtos.Schedules.Input;
using Backend.Validators.Schedules;

using FluentValidation.Results;

namespace Test.Validators.Schedules;

[TestFixture]
public class WeekPointerDtoValidatorTests
{
    private const string InvalidIndexMessage = "WeekPaginationIndex fuera de rango razonable";

    private WeekPointerDtoValidator validator = null!;

    [SetUp]
    public void SetUp() => validator = new WeekPointerDtoValidator();

    [Test]
    public async Task Validate_WithZeroIndex_IsValid()
    {
        var request = new WeekPointerDto { WeekPaginationIndex = 0 };

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.That(result.IsValid, Is.True);
    }

    [TestCase(-520)]
    [TestCase(520)]
    public async Task Validate_WithIndexAtBoundary_IsValid(int weekPaginationIndex)
    {
        var request = new WeekPointerDto { WeekPaginationIndex = weekPaginationIndex };

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.That(result.IsValid, Is.True);
    }

    [TestCase(-521)]
    [TestCase(521)]
    public async Task Validate_WithIndexOutsideRange_FailsWithInvalidIndexMessage(int weekPaginationIndex)
    {
        var request = new WeekPointerDto { WeekPaginationIndex = weekPaginationIndex };

        ValidationResult result = await validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == InvalidIndexMessage), Is.True);
        });
    }
}
