using Backend.Dtos.Remain.Input;
using Backend.Options;
using Backend.Validators.Remain;

using FluentValidation.Results;

using Microsoft.Extensions.Options;

namespace Test.Validators.Remain;

[TestFixture]
public class IncrementStudentRemainDtoValidatorTests
{
    private IncrementStudentRemainDtoValidator sut = null!;
    private RemainLimits limits = null!;

    [SetUp]
    public void SetUp()
    {
        limits = new RemainLimits { MinIncrement = 1, MaxIncrement = 49, MaxStudentNameLength = 80 };
        sut = new IncrementStudentRemainDtoValidator(Options.Create(limits));
    }

    [Test]
    public async Task Validate_WithValidQuantityAndName_IsValid()
    {
        IncrementStudentRemainDto request = new() { RequestId = Guid.NewGuid(), Quantity = 5, StudentName = "Some Student" };

        ValidationResult result = await sut.ValidateAsync(request);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WithNullStudentName_IsValid()
    {
        IncrementStudentRemainDto request = new() { RequestId = Guid.NewGuid(), Quantity = 5, StudentName = null };

        ValidationResult result = await sut.ValidateAsync(request);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WhenRequestIdIsEmpty_FailsWithIdempotencyMessage()
    {
        IncrementStudentRemainDto request = new() { RequestId = Guid.Empty, Quantity = 5 };

        ValidationResult result = await sut.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Select(error => error.ErrorMessage), Does.Contain("RequestId es obligatorio para garantizar idempotencia"));
        });
    }

    [Test]
    public async Task Validate_WhenQuantityBelowMin_FailsWithBoundsMessage()
    {
        IncrementStudentRemainDto request = new() { RequestId = Guid.NewGuid(), Quantity = 0 };

        ValidationResult result = await sut.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorMessage, Does.Contain("Quantity"));
        });
    }

    [Test]
    public async Task Validate_WhenQuantityAboveMax_FailsWithBoundsMessage()
    {
        IncrementStudentRemainDto request = new() { RequestId = Guid.NewGuid(), Quantity = 100 };

        ValidationResult result = await sut.ValidateAsync(request);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task Validate_WhenStudentNameTooLong_FailsWithLengthMessage()
    {
        IncrementStudentRemainDto request = new() { RequestId = Guid.NewGuid(), Quantity = 5, StudentName = new string('x', 81) };

        ValidationResult result = await sut.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Select(error => error.ErrorMessage), Does.Contain("StudentName no debe exceder 80 caracteres"));
        });
    }
}
