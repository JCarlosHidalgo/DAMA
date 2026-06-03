using Backend.Dtos.DebtTemplates.Input;
using Backend.Validators.DebtTemplates;

namespace Test.Validators.DebtTemplates;

[TestFixture]
public class UpdateDebtTemplateDtoValidatorTests
{
    private UpdateDebtTemplateDtoValidator _sut = null!;

    [SetUp]
    public void Setup() => _sut = new UpdateDebtTemplateDtoValidator();

    [Test]
    public async Task Validate_HappyPath_Passes()
    {
        FluentValidation.Results.ValidationResult result = await _sut.ValidateAsync(new UpdateDebtTemplateDto { Description = "ok", ClassQuantity = 1, Cost = 1 });

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_EmptyDescription_Fails()
    {
        FluentValidation.Results.ValidationResult result = await _sut.ValidateAsync(new UpdateDebtTemplateDto { Description = string.Empty, ClassQuantity = 1, Cost = 1 });

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task Validate_DescriptionTooLong_Fails()
    {
        FluentValidation.Results.ValidationResult result = await _sut.ValidateAsync(new UpdateDebtTemplateDto { Description = new string('x', 257), ClassQuantity = 1, Cost = 1 });

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task Validate_NegativeQuantityOrCost_Fails()
    {
        FluentValidation.Results.ValidationResult quantityFail = await _sut.ValidateAsync(new UpdateDebtTemplateDto { Description = "x", ClassQuantity = 0, Cost = 1 });
        FluentValidation.Results.ValidationResult costFail = await _sut.ValidateAsync(new UpdateDebtTemplateDto { Description = "x", ClassQuantity = 1, Cost = 0 });

        Assert.Multiple(() =>
        {
            Assert.That(quantityFail.IsValid, Is.False);
            Assert.That(costFail.IsValid, Is.False);
        });
    }
}
