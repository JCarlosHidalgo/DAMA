using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single;
using Backend.DB.Daos.Abstract.Single.DebtTemplates;
using Backend.DB.Daos.Abstract.Single.QrPayments;
using Backend.DB.Daos.Abstract.Single.Todotix;
using Backend.Dtos.External.Todotix;
using Backend.Dtos.QrPayments.Input;
using Backend.Entities;
using Backend.Entities.DebtTemplates;
using Backend.Entities.QrPayments;
using Backend.Entities.Todotix;
using Backend.Results.QrPayments;
using Backend.Services.Abstract.QrPayments;
using Backend.Services.Abstract.Todotix;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.Services.Concrete.QrPayments;

public class QrDebtCreationService : IQrDebtCreationService
{
    private static readonly TimeSpan DebtExpiration = TimeSpan.FromDays(3);
    private static readonly TimeSpan LatePaymentGrace = TimeSpan.FromMinutes(10);

    private readonly IDebtTemplateDao _debtTemplateDao;
    private readonly IPendingQrPaymentDao _pendingQrPaymentDao;
    private readonly ITodotixOutboxDao _todotixOutboxDao;
    private readonly IExpirationOutboxDao _expirationOutboxDao;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimContext _claimContext;
    private readonly IQrPaymentCreationBuilder _creationBuilder;
    private readonly ITodotixAppKeyResolver _appKeyResolver;

    public QrDebtCreationService(IDebtTemplateDao debtTemplateDao,
                                 IPendingQrPaymentDao pendingQrPaymentDao,
                                 ITodotixOutboxDao todotixOutboxDao,
                                 IExpirationOutboxDao expirationOutboxDao,
                                 IUnitOfWork unitOfWork,
                                 IClaimContext claimContext,
                                 IQrPaymentCreationBuilder creationBuilder,
                                 ITodotixAppKeyResolver appKeyResolver)
    {
        _debtTemplateDao = debtTemplateDao;
        _pendingQrPaymentDao = pendingQrPaymentDao;
        _todotixOutboxDao = todotixOutboxDao;
        _expirationOutboxDao = expirationOutboxDao;
        _unitOfWork = unitOfWork;
        _claimContext = claimContext;
        _creationBuilder = creationBuilder;
        _appKeyResolver = appKeyResolver;
    }

    public async Task<CreateQrDebtOutcome> CreateDebtAsync(Guid templateId, string? email, CreateQrDebtDto dto)
    {
        Guid tenantId = _claimContext.TenantId;
        Guid studentId = _claimContext.UserId;
        string tenantTimezone = _claimContext.TenantTimezone;

        string description = string.IsNullOrWhiteSpace(dto.Descripcion)
            ? $"Pago: {_claimContext.TenantName}"
            : dto.Descripcion;

        DebtTemplate? template = await _debtTemplateDao.GetByIdForTenantAsync(tenantId, templateId);
        if (template == null)
        {
            return new CreateQrDebtOutcome.TemplateNotFound();
        }

        if (await HasActiveDebtForTemplateAsync(tenantId, studentId, templateId))
        {
            return new CreateQrDebtOutcome.ActiveDebtForTemplate();
        }

        Guid debtIdentifier = Guid.NewGuid();
        DateTime expiresAtUtc = DateTime.UtcNow.Add(DebtExpiration);
        DateTime expirationDueAtUtc = expiresAtUtc.Add(LatePaymentGrace);
        string appKey = await _appKeyResolver.ResolveAsync(tenantId);
        PendingQrPayment pending = _creationBuilder.BuildPendingPayment(debtIdentifier, tenantId, studentId, templateId, template, expiresAtUtc);
        RegisterDebtRequest todotixRequest = _creationBuilder.BuildTodotixRequest(debtIdentifier, email, template, tenantTimezone, description, expiresAtUtc, appKey);
        TodotixOutboxEvent outboxEvent = _creationBuilder.BuildOutboxEvent(debtIdentifier, tenantId, todotixRequest);
        ExpirationOutboxEvent expirationEvent = _creationBuilder.BuildExpirationOutboxEvent(debtIdentifier, tenantId, studentId, expirationDueAtUtc);

        await using ITransactionScope scope = await _unitOfWork.BeginAsync();
        await _pendingQrPaymentDao.CreateAsync(pending, scope);
        await _todotixOutboxDao.InsertAsync(outboxEvent, scope);
        await _expirationOutboxDao.InsertAsync(expirationEvent, scope);
        await scope.CommitAsync();

        return new CreateQrDebtOutcome.Success(_creationBuilder.BuildPendingDebtDto(debtIdentifier));
    }

    private async Task<bool> HasActiveDebtForTemplateAsync(Guid tenantId, Guid studentId, Guid templateId)
    {
        int activeDebtCount = await _pendingQrPaymentDao.CountActiveForTemplateAsync(tenantId, studentId, templateId, DateTime.UtcNow);
        return activeDebtCount > 0;
    }
}
