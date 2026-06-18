using Backend.Dtos.Subscriptions.Input;
using Backend.Validators.Subscriptions;

namespace Test.Validators.Subscriptions;

[TestFixture]
public class CreateSubscriptionDebtDtoValidatorTests
{
    private CreateSubscriptionDebtDtoValidator _sut = null!;

    [SetUp]
    public void Setup() => _sut = new CreateSubscriptionDebtDtoValidator();

    [Test]
    public async Task Validate_ValidDtoWithoutEmail_Passes()
    {
        FluentValidation.Results.ValidationResult result = await _sut.ValidateAsync(new CreateSubscriptionDebtDto { Level = 2, Method = "QR" });

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_ValidDtoWithEmail_Passes()
    {
        FluentValidation.Results.ValidationResult result = await _sut.ValidateAsync(new CreateSubscriptionDebtDto { Level = 1, Method = "QR", Email = "a@b.com" });

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_LevelBelowRange_Fails()
    {
        FluentValidation.Results.ValidationResult result = await _sut.ValidateAsync(new CreateSubscriptionDebtDto { Level = 0, Method = "QR" });

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task Validate_LevelAboveRange_Fails()
    {
        FluentValidation.Results.ValidationResult result = await _sut.ValidateAsync(new CreateSubscriptionDebtDto { Level = 4, Method = "QR" });

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task Validate_MethodNotQr_Fails()
    {
        FluentValidation.Results.ValidationResult result = await _sut.ValidateAsync(new CreateSubscriptionDebtDto { Level = 2, Method = "Cash" });

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task Validate_InvalidEmail_Fails()
    {
        FluentValidation.Results.ValidationResult result = await _sut.ValidateAsync(new CreateSubscriptionDebtDto { Level = 2, Method = "QR", Email = "not-an-email" });

        Assert.That(result.IsValid, Is.False);
    }
}
