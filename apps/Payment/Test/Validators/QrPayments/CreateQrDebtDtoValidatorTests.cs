using Backend.Dtos.QrPayments.Input;
using Backend.Validators.QrPayments;

namespace Test.Validators.QrPayments;

[TestFixture]
public class CreateQrDebtDtoValidatorTests
{
    private CreateQrDebtDtoValidator _sut = null!;

    [SetUp]
    public void Setup() => _sut = new CreateQrDebtDtoValidator();

    [Test]
    public async Task Validate_EmptyDto_Passes()
    {
        FluentValidation.Results.ValidationResult result = await _sut.ValidateAsync(new CreateQrDebtDto());

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_InvalidEmail_Fails()
    {
        FluentValidation.Results.ValidationResult result = await _sut.ValidateAsync(new CreateQrDebtDto { Email = "not-an-email" });

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task Validate_ValidEmail_Passes()
    {
        FluentValidation.Results.ValidationResult result = await _sut.ValidateAsync(new CreateQrDebtDto { Email = "a@b.com" });

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_ExternalReferenceTooLong_Fails()
    {
        FluentValidation.Results.ValidationResult result = await _sut.ValidateAsync(new CreateQrDebtDto { ExternalReference = new string('x', 129) });

        Assert.That(result.IsValid, Is.False);
    }
}
