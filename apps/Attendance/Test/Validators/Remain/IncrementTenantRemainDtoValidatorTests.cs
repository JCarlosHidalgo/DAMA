using Backend.Dtos.Remain.Input;
using Backend.Options;
using Backend.Validators.Remain;

using FluentValidation.Results;

using Microsoft.Extensions.Options;

namespace Test.Validators.Remain;

[TestFixture]
public class IncrementTenantRemainDtoValidatorTests
{
    private IncrementTenantRemainDtoValidator _sut = null!;
    private RemainLimits _limits = null!;

    [SetUp]
    public void SetUp()
    {
        _limits = new RemainLimits { MinIncrement = 1, MaxIncrement = 49, MaxStudentNameLength = 80 };
        _sut = new IncrementTenantRemainDtoValidator(Options.Create(_limits));
    }

    [Test]
    public async Task Validate_WithValidQuantity_IsValid()
    {
        IncrementTenantRemainDto request = new() { RequestId = Guid.NewGuid(), Quantity = 5 };

        ValidationResult result = await _sut.ValidateAsync(request);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WhenRequestIdIsEmpty_FailsWithIdempotencyMessage()
    {
        IncrementTenantRemainDto request = new() { RequestId = Guid.Empty, Quantity = 5 };

        ValidationResult result = await _sut.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Select(error => error.ErrorMessage), Does.Contain("RequestId es obligatorio para garantizar idempotencia"));
        });
    }

    [Test]
    public async Task Validate_WhenQuantityBelowMin_FailsWithBoundsMessage()
    {
        IncrementTenantRemainDto request = new() { RequestId = Guid.NewGuid(), Quantity = 0 };

        ValidationResult result = await _sut.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorMessage, Does.Contain("Quantity"));
        });
    }

    [Test]
    public async Task Validate_WhenQuantityAboveMax_FailsWithBoundsMessage()
    {
        IncrementTenantRemainDto request = new() { RequestId = Guid.NewGuid(), Quantity = 100 };

        ValidationResult result = await _sut.ValidateAsync(request);

        Assert.That(result.IsValid, Is.False);
    }
}
