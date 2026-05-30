using AutoMapper;

using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.DebtTemplates;
using Backend.DB.Daos.Abstract.Single.QrPayments;
using Backend.Dtos.DebtTemplates.Input;
using Backend.Dtos.DebtTemplates.Output;
using Backend.Entities.DebtTemplates;
using Backend.Entities.QrPayments;
using Backend.Results.DebtTemplates;
using Backend.Services.Abstract.DebtTemplates;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.Services.Concrete.DebtTemplates;

public class DebtTemplateService : IDebtTemplateService
{
    private readonly IDebtTemplateDao _debtTemplateDao;
    private readonly IQrPaymentIdempotencyDao _qrPaymentIdempotencyDao;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _autoMapper;
    private readonly IClaimContext _claimContext;
    private readonly IDebtTemplateBuilder _debtTemplateBuilder;

    public DebtTemplateService(IDebtTemplateDao debtTemplateDao,
                               IQrPaymentIdempotencyDao qrPaymentIdempotencyDao,
                               IUnitOfWork unitOfWork,
                               IMapper autoMapper,
                               IClaimContext claimContext,
                               IDebtTemplateBuilder debtTemplateBuilder)
    {
        _debtTemplateDao = debtTemplateDao;
        _qrPaymentIdempotencyDao = qrPaymentIdempotencyDao;
        _unitOfWork = unitOfWork;
        _autoMapper = autoMapper;
        _claimContext = claimContext;
        _debtTemplateBuilder = debtTemplateBuilder;
    }

    public async Task<CreateDebtTemplateOutcome> CreateAsync(CreateDebtTemplateDto dto)
    {
        Guid tenantId = _claimContext.TenantId;

        DebtTemplate candidate = _debtTemplateBuilder.BuildDebtTemplate(tenantId, dto);

        if (string.IsNullOrEmpty(dto.ExternalReference))
        {
            await _debtTemplateDao.CreateAsync(candidate);
            return new CreateDebtTemplateOutcome.Success(_autoMapper.Map<DebtTemplateDto>(candidate));
        }

        return await _unitOfWork.RunInTransactionAsync<CreateDebtTemplateOutcome>(async transaction =>
        {
            QrPaymentIdempotency idempotencyRecord = _debtTemplateBuilder.BuildIdempotencyRecord(tenantId, dto.ExternalReference, candidate.Id);

            if (!await _qrPaymentIdempotencyDao.TryRecordAsync(idempotencyRecord, transaction))
            {
                QrPaymentIdempotency? existingIdempotency = await _qrPaymentIdempotencyDao.GetByExternalReferenceAsync(tenantId, dto.ExternalReference);
                if (existingIdempotency == null)
                {
                    throw new InvalidOperationException("Idempotency record vanished after duplicate detection.");
                }

                DebtTemplate? previousTemplate = await _debtTemplateDao.GetByIdForTenantAsync(tenantId, existingIdempotency.EntityId);
                if (previousTemplate == null)
                {
                    throw new InvalidOperationException("Idempotency referenced template not found.");
                }

                CreateDebtTemplateOutcome replayedOutcome = new CreateDebtTemplateOutcome.Replayed(_autoMapper.Map<DebtTemplateDto>(previousTemplate));
                return (replayedOutcome, ShouldCommit: false);
            }

            await _debtTemplateDao.CreateAsync(candidate, transaction);
            CreateDebtTemplateOutcome successOutcome = new CreateDebtTemplateOutcome.Success(_autoMapper.Map<DebtTemplateDto>(candidate));
            return (successOutcome, ShouldCommit: true);
        });
    }

    public async Task<List<DebtTemplateDto>> GetByTenantAsync()
    {
        Guid tenantId = _claimContext.TenantId;
        List<DebtTemplate> templates = await _debtTemplateDao.GetByTenantAsync(tenantId);
        return _autoMapper.Map<List<DebtTemplate>, List<DebtTemplateDto>>(templates);
    }

    public async Task<GetDebtTemplateOutcome> GetByIdAsync(Guid templateId)
    {
        Guid tenantId = _claimContext.TenantId;
        DebtTemplate? template = await _debtTemplateDao.GetByIdForTenantAsync(tenantId, templateId);
        if (template == null)
        {
            return new GetDebtTemplateOutcome.NotFound();
        }

        return new GetDebtTemplateOutcome.Found(_autoMapper.Map<DebtTemplateDto>(template));
    }

    public async Task<UpdateDebtTemplateOutcome> UpdateAsync(Guid templateId, UpdateDebtTemplateDto dto)
    {
        Guid tenantId = _claimContext.TenantId;
        bool updated = await _debtTemplateDao.UpdateForTenantAsync(tenantId, templateId, dto.Description, dto.ClassQuantity, dto.Cost);
        if (!updated)
        {
            return new UpdateDebtTemplateOutcome.NotFound();
        }

        return new UpdateDebtTemplateOutcome.Updated();
    }

    public async Task<DeleteDebtTemplateOutcome> DeleteAsync(Guid templateId)
    {
        Guid tenantId = _claimContext.TenantId;
        bool deleted = await _debtTemplateDao.DeleteForTenantAsync(tenantId, templateId);
        if (!deleted)
        {
            return new DeleteDebtTemplateOutcome.NotFound();
        }

        return new DeleteDebtTemplateOutcome.Deleted();
    }
}
