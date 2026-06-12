using Backend.Pagination;
using Backend.Validators;

using FluentValidation.Results;

namespace Test.Validators;

[TestFixture]
public class PaginationQueryDtoValidatorTests
{
    private const string InvalidPageIndexMessage = "pageIndex no válido";

    private PaginationQueryDtoValidator _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new PaginationQueryDtoValidator();

    [Test]
    public async Task Validate_WithZeroPageIndex_IsValid()
    {
        PaginationQueryDto query = new() { PageIndex = 0 };

        ValidationResult result = await _sut.ValidateAsync(query);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WithPositivePageIndex_IsValid()
    {
        PaginationQueryDto query = new() { PageIndex = 47 };

        ValidationResult result = await _sut.ValidateAsync(query);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WithNegativePageIndex_FailsWithInvalidPageIndexMessage()
    {
        PaginationQueryDto query = new() { PageIndex = -1 };

        ValidationResult result = await _sut.ValidateAsync(query);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].ErrorMessage, Is.EqualTo(InvalidPageIndexMessage));
        });
    }

    [Test]
    public async Task Validate_WithPageIndexAboveMaximum_Fails()
    {
        PaginationQueryDto query = new() { PageIndex = int.MaxValue };

        ValidationResult result = await _sut.ValidateAsync(query);

        Assert.That(result.IsValid, Is.False);
    }
}
