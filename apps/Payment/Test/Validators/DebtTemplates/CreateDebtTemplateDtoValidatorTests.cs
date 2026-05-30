using Backend.Dtos.DebtTemplates.Input;
using Backend.Validators.DebtTemplates;

using FluentValidation.Results;

namespace Test.Validators.DebtTemplates;

[TestFixture]
public class CreateDebtTemplateDtoValidatorTests
{
    private CreateDebtTemplateDtoValidator sut = null!;

    [SetUp]
    public void Setup() => sut = new CreateDebtTemplateDtoValidator();

    private static CreateDebtTemplateDto ValidDto() => new CreateDebtTemplateDto { Description = "cuota", ClassQuantity = 1, Cost = 1 };

    [Test]
    public async Task Validate_HappyPath_NoErrors()
    {
        ValidationResult result = await sut.ValidateAsync(ValidDto());

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_EmptyDescription_Fails()
    {
        CreateDebtTemplateDto request = ValidDto();
        request.Description = string.Empty;

        ValidationResult result = await sut.ValidateAsync(request);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task Validate_DescriptionTooLong_Fails()
    {
        CreateDebtTemplateDto request = ValidDto();
        request.Description = new string('x', 257);

        ValidationResult result = await sut.ValidateAsync(request);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task Validate_ClassQuantityZero_Fails()
    {
        CreateDebtTemplateDto request = ValidDto();
        request.ClassQuantity = 0;

        ValidationResult result = await sut.ValidateAsync(request);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task Validate_CostZero_Fails()
    {
        CreateDebtTemplateDto request = ValidDto();
        request.Cost = 0;

        ValidationResult result = await sut.ValidateAsync(request);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task Validate_ExternalReferenceTooLong_Fails()
    {
        CreateDebtTemplateDto request = ValidDto();
        request.ExternalReference = new string('x', 129);

        ValidationResult result = await sut.ValidateAsync(request);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task Validate_ExternalReferenceNullOrEmpty_Passes()
    {
        CreateDebtTemplateDto request = ValidDto();
        request.ExternalReference = null;

        ValidationResult result = await sut.ValidateAsync(request);

        Assert.That(result.IsValid, Is.True);
    }
}
