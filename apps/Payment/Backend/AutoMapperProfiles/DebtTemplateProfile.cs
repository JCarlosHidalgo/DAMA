using AutoMapper;

using Backend.Dtos.DebtTemplates.Output;
using Backend.Dtos.QrPayments.Output;
using Backend.Entities.DebtTemplates;
using Backend.Entities.QrPayments;

namespace Backend.AutoMapperProfiles;

public class DebtTemplateProfile : Profile
{
    public DebtTemplateProfile()
    {
        CreateMap<DebtTemplate, DebtTemplateDto>();
        CreateMap<PendingQrPayment, PendingQrDebtDto>();
        CreateMap<SuccessQrPayment, SuccessQrPaymentDto>();
        CreateMap<FailedQrPayment, FailedQrPaymentDto>();
    }
}
