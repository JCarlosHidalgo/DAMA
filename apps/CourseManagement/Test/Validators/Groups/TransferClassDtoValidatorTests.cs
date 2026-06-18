using Backend.Dtos.Groups.Input;
using Backend.Validators.Groups;

using FluentValidation.Results;

namespace Test.Validators.Groups;

[TestFixture]
public class TransferClassDtoValidatorTests
{
    private const string InvalidTargetGroupIdMessage = "TargetGroupId es requerido";

    private TransferClassDtoValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new TransferClassDtoValidator();

    [Test]
    public async Task Validate_WithValidTargetGroupId_IsValid()
    {
        var request = new TransferClassDto { TargetGroupId = Guid.NewGuid() };

        ValidationResult result = await _validator.ValidateAsync(request);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WithEmptyTargetGroupId_FailsWithInvalidMessage()
    {
        var request = new TransferClassDto { TargetGroupId = Guid.Empty };

        ValidationResult result = await _validator.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(error => error.ErrorMessage == InvalidTargetGroupIdMessage), Is.True);
        });
    }
}
