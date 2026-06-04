using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.QrPayments;
using Backend.Dtos.Summary.Output;
using Backend.Options;
using Backend.Services.Abstract.Summary;

using Microsoft.Extensions.Options;

namespace Backend.Services.Concrete.Summary;

public class SummaryService : ISummaryService
{
    private const int WindowDays = 30;

    private readonly ISuccessQrPaymentDao _successQrPaymentDao;
    private readonly IClaimContext _claimContext;
    private readonly IOptions<CurrencyOptions> _currencyOptions;

    public SummaryService(ISuccessQrPaymentDao successQrPaymentDao,
                          IClaimContext claimContext,
                          IOptions<CurrencyOptions> currencyOptions)
    {
        _successQrPaymentDao = successQrPaymentDao;
        _claimContext = claimContext;
        _currencyOptions = currencyOptions;
    }

    public async Task<PaymentSummaryDto> GetByTenantAsync()
    {
        Guid tenantId = _claimContext.TenantId;

        DateTime rangeEnd = DateTime.UtcNow;
        DateTime rangeStart = rangeEnd.AddDays(-WindowDays);

        (int totalEarnings, int monthEarnings, DateTime? firstPaymentDate) =
            await _successQrPaymentDao.GetSummaryAsync(tenantId, rangeStart);

        return new PaymentSummaryDto
        {
            TotalEarnings = totalEarnings,
            MonthEarnings = monthEarnings,
            Currency = _currencyOptions.Value.Default,
            FirstPaymentDate = firstPaymentDate,
            From = rangeStart,
            To = rangeEnd
        };
    }
}
