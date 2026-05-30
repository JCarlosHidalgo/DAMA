using Backend.Common;
using Backend.Validators;

using FluentValidation.Results;

namespace Test.Validators;

[TestFixture]
public class PaginationParamsDtoValidatorTests
{
    private PaginationParamsDtoValidator sut = null!;

    [SetUp]
    public void SetUp() => sut = new PaginationParamsDtoValidator();

    [Test]
    public async Task Validate_WithZeroIndex_IsValid()
    {
        ValidationResult result = await sut.ValidateAsync(new PaginationParamsDto { Index = 0 });

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WithPositiveIndex_IsValid()
    {
        ValidationResult result = await sut.ValidateAsync(new PaginationParamsDto { Index = 47 });

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WithNegativeIndex_FailsWithMessage()
    {
        ValidationResult result = await sut.ValidateAsync(new PaginationParamsDto { Index = -1 });

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorMessage, Is.EqualTo("El índice de página no puede ser negativo."));
        });
    }
}
