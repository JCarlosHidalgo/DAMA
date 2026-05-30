using Backend.Dtos.Summary.Output;

namespace Backend.Services.Abstract.Summary;

public interface ISummaryService
{
    Task<PaymentSummaryDto> GetByTenantAsync();
}
