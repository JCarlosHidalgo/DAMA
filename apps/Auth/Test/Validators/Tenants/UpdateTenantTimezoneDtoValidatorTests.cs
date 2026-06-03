using Backend.Dtos.Tenants.Input;
using Backend.Validators.Tenants;

using FluentValidation.Results;

namespace Test.Validators.Tenants;

[TestFixture]
public class UpdateTenantTimezoneDtoValidatorTests
{
    private const string InvalidTimezoneMessage = "Timezone IANA inválido";

    private UpdateTenantTimezoneDtoValidator _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new UpdateTenantTimezoneDtoValidator();

    [Test]
    public async Task Validate_WithValidIanaIdentifier_IsValid()
    {
        UpdateTenantTimezoneDto request = new() { Timezone = "America/La_Paz" };

        ValidationResult result = await _sut.ValidateAsync(request);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WhenTimezoneIsEmpty_FailsWithInvalidTimezoneMessage()
    {
        UpdateTenantTimezoneDto request = new() { Timezone = string.Empty };

        ValidationResult result = await _sut.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorMessage, Is.EqualTo(InvalidTimezoneMessage));
        });
    }

    [Test]
    public async Task Validate_WhenTimezoneIsWhitespace_FailsWithInvalidTimezoneMessage()
    {
        UpdateTenantTimezoneDto request = new() { Timezone = "   " };

        ValidationResult result = await _sut.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorMessage, Is.EqualTo(InvalidTimezoneMessage));
        });
    }

    [Test]
    public async Task Validate_WhenTimezoneIsUnknownIanaIdentifier_FailsWithInvalidTimezoneMessage()
    {
        UpdateTenantTimezoneDto request = new() { Timezone = "Continent/NonExistent" };

        ValidationResult result = await _sut.ValidateAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorMessage, Is.EqualTo(InvalidTimezoneMessage));
        });
    }
}
