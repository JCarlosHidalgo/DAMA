using Backend.Common;
using Backend.Dtos.QrPayments.Output;

namespace Backend.Builders;

public interface IQrPaymentViewBuilder
{
    QrDebtStatusDto BuildReadyStatus(Guid identifier, string? qrSimpleUrl);

    QrDebtStatusDto BuildFailedStatus(Guid identifier, string? error);

    QrDebtStatusDto BuildPendingStatus(Guid identifier);

    PageDto<TOutputDto> BuildPage<TOutputDto>(int currentIndex, int maxIndex, List<TOutputDto> items);
}
