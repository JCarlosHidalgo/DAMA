using Backend.Common;
using Backend.Dtos.QrPayments.Output;
using Backend.Results.QrPayments;

namespace Backend.Services.Abstract.QrPayments;

public interface IQrPaymentQueryService
{
    Task<GetQrDebtStatusOutcome> GetDebtStatusAsync(Guid paymentId);

    Task<PageDto<PendingQrDebtDto>> ListPendingAsync(int pageIndex);

    Task<PageDto<SuccessQrPaymentDto>> ListSuccessAsync(int pageIndex);

    Task<PageDto<FailedQrPaymentDto>> ListFailedAsync(int pageIndex);

    Task<StudentQrBreakdownDto> GetStatusBreakdownAsync();

    Task<List<StudentSpendPointDto>> GetSpendByMonthAsync(DateTime fromDate, DateTime toDate);
}
