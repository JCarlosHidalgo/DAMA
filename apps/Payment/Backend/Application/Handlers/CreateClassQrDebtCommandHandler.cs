using Backend.Application.Commands;
using Backend.Application.Mediator;
using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single;
using Backend.DB.Daos.Abstract.Single.DebtTemplates;
using Backend.DB.Daos.Abstract.Single.QrPayments;
using Backend.DB.Daos.Abstract.Single.Todotix;
using Backend.Dtos.External.Todotix;
using Backend.Entities;
using Backend.Entities.DebtTemplates;
using Backend.Entities.QrPayments;
using Backend.Entities.Todotix;
using Backend.Results.QrPayments;
using Backend.Services.Abstract.Todotix;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.Application.Handlers;

public sealed class CreateClassQrDebtCommandHandler
    : ICommandHandler<CreateClassQrDebtCommand, CreateQrDebtOutcome>
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

    public CreateClassQrDebtCommandHandler(IDebtTemplateDao debtTemplateDao,
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

    public async Task<CreateQrDebtOutcome> Handle(CreateClassQrDebtCommand command)
    {
        Guid tenantId = _claimContext.TenantId;
        Guid studentId = _claimContext.UserId;
        string tenantTimezone = _claimContext.TenantTimezone;

        string description = string.IsNullOrWhiteSpace(command.Dto.Descripcion)
            ? $"Pago: {_claimContext.TenantName}"
            : command.Dto.Descripcion;

        DebtTemplate? template = await _debtTemplateDao.GetByIdForTenantAsync(tenantId, command.TemplateId);
        if (template == null)
        {
            return new CreateQrDebtOutcome.TemplateNotFound();
        }

        if (await HasActiveDebtForTemplateAsync(tenantId, studentId, command.TemplateId))
        {
            return new CreateQrDebtOutcome.ActiveDebtForTemplate();
        }

        string? appKey = await _appKeyResolver.ResolveAsync(tenantId);
        if (appKey is null)
        {
            return new CreateQrDebtOutcome.PaymentNotConfigured();
        }

        Guid debtIdentifier = Guid.NewGuid();
        DateTime expiresAtUtc = DateTime.UtcNow.Add(DebtExpiration);
        DateTime expirationDueAtUtc = expiresAtUtc.Add(LatePaymentGrace);
        PendingQrPayment pending = _creationBuilder.BuildPendingPayment(
            debtIdentifier, tenantId, studentId, command.TemplateId, template, expiresAtUtc);
        RegisterDebtRequest todotixRequest = _creationBuilder.BuildTodotixRequest(
            debtIdentifier, command.Email, template, tenantTimezone, description, expiresAtUtc, appKey);
        TodotixOutboxEvent outboxEvent = _creationBuilder.BuildOutboxEvent(debtIdentifier, tenantId, todotixRequest);
        ExpirationOutboxEvent expirationEvent = _creationBuilder.BuildExpirationOutboxEvent(
            debtIdentifier, tenantId, studentId, expirationDueAtUtc);

        await using ITransactionScope scope = await _unitOfWork.BeginAsync();
        await _pendingQrPaymentDao.CreateAsync(pending, scope);
        await _todotixOutboxDao.InsertAsync(outboxEvent, scope);
        await _expirationOutboxDao.InsertAsync(expirationEvent, scope);
        await scope.CommitAsync();

        return new CreateQrDebtOutcome.Success(_creationBuilder.BuildPendingDebtDto(debtIdentifier));
    }

    private async Task<bool> HasActiveDebtForTemplateAsync(Guid tenantId, Guid studentId, Guid templateId)
    {
        int activeDebtCount = await _pendingQrPaymentDao.CountActiveForTemplateAsync(
            tenantId, studentId, templateId, DateTime.UtcNow);
        return activeDebtCount > 0;
    }
}
