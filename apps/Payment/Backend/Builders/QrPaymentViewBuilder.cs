using Backend.Common;
using Backend.Dtos.QrPayments.Output;

namespace Backend.Builders;

public class QrPaymentViewBuilder : IQrPaymentViewBuilder
{
    public QrDebtStatusDto BuildReadyStatus(Guid identifier, string? qrSimpleUrl)
    {
        return new QrDebtStatusDto
        {
            IdentificadorDeuda = identifier,
            Status = "Ready",
            QrSimpleUrl = qrSimpleUrl
        };
    }

    public QrDebtStatusDto BuildFailedStatus(Guid identifier, string? error)
    {
        return new QrDebtStatusDto
        {
            IdentificadorDeuda = identifier,
            Status = "Failed",
            Error = error
        };
    }

    public QrDebtStatusDto BuildPendingStatus(Guid identifier)
    {
        return new QrDebtStatusDto
        {
            IdentificadorDeuda = identifier,
            Status = "Pending"
        };
    }

    public PageDto<TOutputDto> BuildPage<TOutputDto>(int currentIndex, int maxIndex, List<TOutputDto> items)
    {
        return new PageDto<TOutputDto>
        {
            CurrentIndex = currentIndex,
            MaxIndex = maxIndex,
            Items = items
        };
    }
}
