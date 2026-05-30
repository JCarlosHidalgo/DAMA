using Backend.DB.Daos.Abstract.Single.Events;
using Backend.DB.Daos.Abstract.Single.Remain;
using Backend.Events;
using Backend.Results.Events;
using Backend.Services.Abstract.Events;

using DAMA.Software.MySqlOutbox;
using DAMA.Software.MySqlUnitOfWork;

namespace Backend.Services.Concrete.Events;

public sealed class PaymentCapturedHandler : IPaymentCapturedHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProcessedEventDao _processedEventDao;
    private readonly IStudentRemainClassesDao _remainClassesDao;
    private readonly IPaymentCreditLedgerDao _paymentCreditLedgerDao;
    private readonly ILogger<PaymentCapturedHandler> _logger;

    public PaymentCapturedHandler(IUnitOfWork unitOfWork,
                                  IProcessedEventDao processedEventDao,
                                  IStudentRemainClassesDao remainClassesDao,
                                  IPaymentCreditLedgerDao paymentCreditLedgerDao,
                                  ILogger<PaymentCapturedHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _processedEventDao = processedEventDao;
        _remainClassesDao = remainClassesDao;
        _paymentCreditLedgerDao = paymentCreditLedgerDao;
        _logger = logger;
    }

    public async Task<PaymentCapturedOutcome> HandleAsync(PaymentCapturedEvent paymentCapturedEvent, CancellationToken cancellationToken)
    {
        try
        {
            return await IdempotentTransaction.RunAsync<PaymentCapturedOutcome>(
                _unitOfWork,
                _processedEventDao,
                paymentCapturedEvent.EventId,
                new PaymentCapturedOutcome.AlreadyProcessed(),
                async scope =>
                {
                    await _remainClassesDao.IncrementAsync(
                        paymentCapturedEvent.Data.TenantId,
                        paymentCapturedEvent.Data.StudentId,
                        delta: paymentCapturedEvent.Data.Quantity,
                        studentName: null,
                        transaction: scope);

                    await _paymentCreditLedgerDao.RecordAsync(
                        paymentCapturedEvent.EventId,
                        paymentCapturedEvent.Data.TenantId,
                        paymentCapturedEvent.Data.StudentId,
                        paymentCapturedEvent.Data.Quantity,
                        paymentCapturedEvent.Data.ExternalReference,
                        paymentCapturedEvent.OccurredAt,
                        scope);

                    return new PaymentCapturedOutcome.RemainCredited();
                });
        }
        catch (Exception handlerException)
        {
            _logger.LogError(
                handlerException,
                "Handle PaymentCaptured falló para {EventId}",
                paymentCapturedEvent.EventId);
            return new PaymentCapturedOutcome.Failed();
        }
    }
}
