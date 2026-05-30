using Backend.Common;
using Backend.Validators;

namespace Test.Validators;

[TestFixture]
public class PaginationParamsDtoValidatorTests
{
    private PaginationParamsDtoValidator sut = null!;

    [SetUp]
    public void Setup() => sut = new PaginationParamsDtoValidator();

    [Test]
    public async Task Validate_ZeroOrPositive_Passes()
    {
        FluentValidation.Results.ValidationResult resultZero = await sut.ValidateAsync(new PaginationParamsDto { Index = 0 });
        FluentValidation.Results.ValidationResult resultPositive = await sut.ValidateAsync(new PaginationParamsDto { Index = 7 });

        Assert.Multiple(() =>
        {
            Assert.That(resultZero.IsValid, Is.True);
            Assert.That(resultPositive.IsValid, Is.True);
        });
    }

    [Test]
    public async Task Validate_Negative_Fails()
    {
        FluentValidation.Results.ValidationResult result = await sut.ValidateAsync(new PaginationParamsDto { Index = -1 });

        Assert.That(result.IsValid, Is.False);
    }
}
