using AutoMapper;

using Backend.Builders;
using Backend.Claims;
using Backend.Common;
using Backend.DB.Daos.Abstract.Single.QrPayments;
using Backend.DB.Daos.Abstract.Single.Todotix;
using Backend.Dtos.QrPayments.Output;
using Backend.Entities.QrPayments;
using Backend.Entities.Todotix;
using Backend.Results.QrPayments;
using Backend.Services.Abstract.QrPayments;

namespace Backend.Services.Concrete.QrPayments;

public class QrPaymentQueryService : IQrPaymentQueryService
{
    private const int PageSize = 10;

    private readonly IPendingQrPaymentDao _pendingQrPaymentDao;
    private readonly ISuccessQrPaymentDao _successQrPaymentDao;
    private readonly IFailedQrPaymentDao _failedQrPaymentDao;
    private readonly ITodotixOutboxDao _todotixOutboxDao;
    private readonly IMapper _autoMapper;
    private readonly IClaimContext _claimContext;
    private readonly IQrPaymentViewBuilder _viewBuilder;

    public QrPaymentQueryService(IPendingQrPaymentDao pendingQrPaymentDao,
                                 ISuccessQrPaymentDao successQrPaymentDao,
                                 IFailedQrPaymentDao failedQrPaymentDao,
                                 ITodotixOutboxDao todotixOutboxDao,
                                 IMapper autoMapper,
                                 IClaimContext claimContext,
                                 IQrPaymentViewBuilder viewBuilder)
    {
        _pendingQrPaymentDao = pendingQrPaymentDao;
        _successQrPaymentDao = successQrPaymentDao;
        _failedQrPaymentDao = failedQrPaymentDao;
        _todotixOutboxDao = todotixOutboxDao;
        _autoMapper = autoMapper;
        _claimContext = claimContext;
        _viewBuilder = viewBuilder;
    }

    public async Task<GetQrDebtStatusOutcome> GetDebtStatusAsync(Guid paymentId)
    {
        Guid tenantId = _claimContext.TenantId;
        PendingQrPayment? pending = await _pendingQrPaymentDao.GetByIdForTenantAsync(tenantId, paymentId);
        if (pending != null)
        {
            if (!string.IsNullOrEmpty(pending.QrImageUrl))
            {
                return new GetQrDebtStatusOutcome.Found(_viewBuilder.BuildReadyStatus(pending.Id, pending.QrImageUrl));
            }

            TodotixOutboxEvent? outboxEvent = await _todotixOutboxDao.GetByPendingIdAsync(paymentId);
            if (outboxEvent != null && outboxEvent.Status == "Failed")
            {
                return new GetQrDebtStatusOutcome.Found(_viewBuilder.BuildFailedStatus(pending.Id, outboxEvent.LastError));
            }

            return new GetQrDebtStatusOutcome.Found(_viewBuilder.BuildPendingStatus(pending.Id));
        }

        SuccessQrPayment? success = await _successQrPaymentDao.GetByIdAsync(paymentId);
        if (success != null && success.TenantId == tenantId)
        {
            return new GetQrDebtStatusOutcome.Found(_viewBuilder.BuildReadyStatus(success.Id, null));
        }

        return new GetQrDebtStatusOutcome.NotFound();
    }

    public async Task<PageDto<PendingQrDebtDto>> ListPendingAsync(int pageIndex)
    {
        Guid tenantId = _claimContext.TenantId;
        Guid studentId = _claimContext.UserId;

        return await BuildPageAsync<PendingQrPayment, PendingQrDebtDto>(
            tenantId,
            studentId,
            pageIndex,
            _pendingQrPaymentDao.CountByStudentForTenantAsync,
            _pendingQrPaymentDao.GetPageByStudentForTenantAsync);
    }

    public async Task<PageDto<SuccessQrPaymentDto>> ListSuccessAsync(int pageIndex)
    {
        Guid tenantId = _claimContext.TenantId;
        Guid studentId = _claimContext.UserId;

        return await BuildPageAsync<SuccessQrPayment, SuccessQrPaymentDto>(
            tenantId,
            studentId,
            pageIndex,
            _successQrPaymentDao.CountByStudentForTenantAsync,
            _successQrPaymentDao.GetPageByStudentForTenantAsync);
    }

    public async Task<PageDto<FailedQrPaymentDto>> ListFailedAsync(int pageIndex)
    {
        Guid tenantId = _claimContext.TenantId;
        Guid studentId = _claimContext.UserId;

        return await BuildPageAsync<FailedQrPayment, FailedQrPaymentDto>(
            tenantId,
            studentId,
            pageIndex,
            _failedQrPaymentDao.CountByStudentForTenantAsync,
            _failedQrPaymentDao.GetPageByStudentForTenantAsync);
    }

    private async Task<PageDto<TOutputDto>> BuildPageAsync<TEntity, TOutputDto>(
        Guid tenantId,
        Guid studentId,
        int pageIndex,
        Func<Guid, Guid, Task<int>> countAsync,
        Func<Guid, Guid, int, int, Task<List<TEntity>>> pageAsync) where TOutputDto : class
    {
        int totalCount = await countAsync(tenantId, studentId);
        int maxIndex = ComputeMaxIndex(totalCount);

        List<TEntity> entities = pageIndex > maxIndex
            ? new List<TEntity>()
            : await pageAsync(tenantId, studentId, pageIndex * PageSize, PageSize);

        return _viewBuilder.BuildPage(
            pageIndex,
            maxIndex,
            _autoMapper.Map<List<TEntity>, List<TOutputDto>>(entities));
    }

    private static int ComputeMaxIndex(int totalCount)
    {
        if (totalCount <= 0)
        {
            return 0;
        }

        return (totalCount - 1) / PageSize;
    }
}
