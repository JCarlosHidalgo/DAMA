using Backend.Application.Commands;
using Backend.Application.Mediator;
using Backend.Application.Results;
using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.Subscriptions;
using Backend.DB.Daos.Abstract.Single.Todotix;
using Backend.Dtos.External.Todotix;
using Backend.Entities.Subscriptions;
using Backend.Entities.Todotix;
using Backend.Options;

using DAMA.Software.MySqlUnitOfWork;

using Microsoft.Extensions.Options;

namespace Backend.Application.Handlers;

public sealed class CreateSubscriptionQrDebtCommandHandler
    : ICommandHandler<CreateSubscriptionQrDebtCommand, CreateSubscriptionDebtOutcome>
{
    private static readonly TimeSpan DebtExpiration = TimeSpan.FromDays(3);

    private readonly ISubscriptionPlanDao _subscriptionPlanDao;
    private readonly IPendingSubscriptionPaymentDao _pendingSubscriptionPaymentDao;
    private readonly ITodotixOutboxDao _todotixOutboxDao;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimContext _claimContext;
    private readonly ISubscriptionCreationBuilder _creationBuilder;
    private readonly IOptions<TodotixOptions> _todotixOptions;

    public CreateSubscriptionQrDebtCommandHandler(ISubscriptionPlanDao subscriptionPlanDao,
                                                  IPendingSubscriptionPaymentDao pendingSubscriptionPaymentDao,
                                                  ITodotixOutboxDao todotixOutboxDao,
                                                  IUnitOfWork unitOfWork,
                                                  IClaimContext claimContext,
                                                  ISubscriptionCreationBuilder creationBuilder,
                                                  IOptions<TodotixOptions> todotixOptions)
    {
        _subscriptionPlanDao = subscriptionPlanDao;
        _pendingSubscriptionPaymentDao = pendingSubscriptionPaymentDao;
        _todotixOutboxDao = todotixOutboxDao;
        _unitOfWork = unitOfWork;
        _claimContext = claimContext;
        _creationBuilder = creationBuilder;
        _todotixOptions = todotixOptions;
    }

    public async Task<CreateSubscriptionDebtOutcome> Handle(CreateSubscriptionQrDebtCommand command)
    {
        Guid tenantId = _claimContext.TenantId;
        string tenantTimezone = _claimContext.TenantTimezone;
        string description = $"Suscripción DAMA nivel {command.Level}: {_claimContext.TenantName}";

        SubscriptionPlan? plan = await _subscriptionPlanDao.GetByLevelAsync(command.Level);
        if (plan is null)
        {
            return new CreateSubscriptionDebtOutcome.PlanNotFound();
        }

        if (await _pendingSubscriptionPaymentDao.CountActiveForTenantAsync(tenantId, DateTime.UtcNow) > 0)
        {
            return new CreateSubscriptionDebtOutcome.ActiveSubscriptionDebt();
        }

        string platformAppKey = _todotixOptions.Value.PlatformAppKey;
        if (string.IsNullOrWhiteSpace(platformAppKey))
        {
            return new CreateSubscriptionDebtOutcome.PaymentNotConfigured();
        }

        Guid debtIdentifier = Guid.NewGuid();
        DateTime expiresAtUtc = DateTime.UtcNow.Add(DebtExpiration);
        PendingSubscriptionPayment pending = _creationBuilder.BuildPendingPayment(debtIdentifier, tenantId, plan, expiresAtUtc);
        RegisterDebtRequest todotixRequest = _creationBuilder.BuildTodotixRequest(
            debtIdentifier, command.Email, plan, tenantTimezone, description, expiresAtUtc, platformAppKey);
        TodotixOutboxEvent outboxEvent = _creationBuilder.BuildOutboxEvent(debtIdentifier, tenantId, todotixRequest);

        await using ITransactionScope scope = await _unitOfWork.BeginAsync();
        await _pendingSubscriptionPaymentDao.CreateAsync(pending, scope);
        await _todotixOutboxDao.InsertAsync(outboxEvent, scope);
        await scope.CommitAsync();

        return new CreateSubscriptionDebtOutcome.Success(_creationBuilder.BuildPendingDebtDto(debtIdentifier));
    }
}
