using Backend.Dtos.Tenants.Input;
using Backend.Validators.Tenants;

using FluentValidation.Results;

namespace Test.Validators.Tenants;

[TestFixture]
public class UpdateTenantNameDtoValidatorTests
{
    private UpdateTenantNameDtoValidator _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new UpdateTenantNameDtoValidator();

    [Test]
    public async Task Validate_WithValidName_IsValid()
    {
        UpdateTenantNameDto tenant = new() { Name = "Sample Tenant" };

        ValidationResult result = await _sut.ValidateAsync(tenant);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WhenNameEmpty_Fails()
    {
        UpdateTenantNameDto tenant = new() { Name = string.Empty };

        ValidationResult result = await _sut.ValidateAsync(tenant);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorMessage, Is.EqualTo("El nombre del tenant es obligatorio."));
        });
    }

    [Test]
    public async Task Validate_WhenNameAboveMaxLength_Fails()
    {
        UpdateTenantNameDto tenant = new() { Name = new string('a', 201) };

        ValidationResult result = await _sut.ValidateAsync(tenant);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorMessage, Is.EqualTo("El nombre del tenant no puede exceder 200 caracteres."));
        });
    }
}
