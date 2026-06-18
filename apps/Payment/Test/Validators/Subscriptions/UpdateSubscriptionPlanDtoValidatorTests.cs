using Backend.Dtos.Subscriptions.Input;
using Backend.Validators.Subscriptions;

namespace Test.Validators.Subscriptions;

[TestFixture]
public class UpdateSubscriptionPlanDtoValidatorTests
{
    private UpdateSubscriptionPlanDtoValidator _sut = null!;

    [SetUp]
    public void Setup() => _sut = new UpdateSubscriptionPlanDtoValidator();

    private static UpdateSubscriptionPlanDto ValidDto()
    {
        return new UpdateSubscriptionPlanDto
        {
            Price = 180,
            DurationAmount = 1,
            DurationUnit = "Month"
        };
    }

    [Test]
    public async Task Validate_ValidDto_Passes()
    {
        FluentValidation.Results.ValidationResult result = await _sut.ValidateAsync(ValidDto());

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_PriceZero_Fails()
    {
        UpdateSubscriptionPlanDto dto = ValidDto();
        dto.Price = 0;

        FluentValidation.Results.ValidationResult result = await _sut.ValidateAsync(dto);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task Validate_UnknownDurationUnit_Fails()
    {
        UpdateSubscriptionPlanDto dto = ValidDto();
        dto.DurationUnit = "Fortnight";

        FluentValidation.Results.ValidationResult result = await _sut.ValidateAsync(dto);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task Validate_DurationAmountBelowOne_Fails()
    {
        UpdateSubscriptionPlanDto dto = ValidDto();
        dto.DurationAmount = 0;

        FluentValidation.Results.ValidationResult result = await _sut.ValidateAsync(dto);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task Validate_DaysAtUpperBound_Passes()
    {
        UpdateSubscriptionPlanDto dto = ValidDto();
        dto.DurationUnit = "Day";
        dto.DurationAmount = 365;

        FluentValidation.Results.ValidationResult result = await _sut.ValidateAsync(dto);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_DaysAboveUpperBound_Fails()
    {
        UpdateSubscriptionPlanDto dto = ValidDto();
        dto.DurationUnit = "Day";
        dto.DurationAmount = 366;

        FluentValidation.Results.ValidationResult result = await _sut.ValidateAsync(dto);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task Validate_WeeksAtUpperBound_Passes()
    {
        UpdateSubscriptionPlanDto dto = ValidDto();
        dto.DurationUnit = "Week";
        dto.DurationAmount = 52;

        FluentValidation.Results.ValidationResult result = await _sut.ValidateAsync(dto);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_WeeksAboveUpperBound_Fails()
    {
        UpdateSubscriptionPlanDto dto = ValidDto();
        dto.DurationUnit = "Week";
        dto.DurationAmount = 53;

        FluentValidation.Results.ValidationResult result = await _sut.ValidateAsync(dto);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task Validate_MonthsAtUpperBound_Passes()
    {
        UpdateSubscriptionPlanDto dto = ValidDto();
        dto.DurationUnit = "month";
        dto.DurationAmount = 12;

        FluentValidation.Results.ValidationResult result = await _sut.ValidateAsync(dto);

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public async Task Validate_MonthsAboveUpperBound_Fails()
    {
        UpdateSubscriptionPlanDto dto = ValidDto();
        dto.DurationUnit = "Month";
        dto.DurationAmount = 13;

        FluentValidation.Results.ValidationResult result = await _sut.ValidateAsync(dto);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task Validate_NumericOutOfRangeDurationUnit_Fails()
    {
        UpdateSubscriptionPlanDto dto = ValidDto();
        dto.DurationUnit = "99";
        dto.DurationAmount = 1;

        FluentValidation.Results.ValidationResult result = await _sut.ValidateAsync(dto);

        Assert.That(result.IsValid, Is.False);
    }
}
