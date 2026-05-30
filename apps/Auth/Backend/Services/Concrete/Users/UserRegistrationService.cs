using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single;
using Backend.DB.Daos.Abstract.Single.Users;
using Backend.DB.Daos.Abstract.TwoForeign.Tenants;
using Backend.Dtos.Users.Input;
using Backend.Entities;
using Backend.Entities.Tenants;
using Backend.Entities.Users;
using Backend.Results.Users;
using Backend.Services.Abstract.Users;

using DAMA.Software.MySqlUnitOfWork;

namespace Backend.Services.Concrete.Users;

public class UserRegistrationService : IUserRegistrationService
{
    private readonly IUserRegistrationDao _userDao;
    private readonly ITenantDomainDao _tenantDomainDao;
    private readonly IOutboxEventDao _outboxDao;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimContext _claimContext;
    private readonly IStudentRegisteredEventBuilder _studentRegisteredEventBuilder;
    private readonly IUserEntityBuilder _entityBuilder;

    public UserRegistrationService(IUserRegistrationDao userDao,
                                   ITenantDomainDao tenantDomainDao,
                                   IOutboxEventDao outboxDao,
                                   IUnitOfWork unitOfWork,
                                   IClaimContext claimContext,
                                   IStudentRegisteredEventBuilder studentRegisteredEventBuilder,
                                   IUserEntityBuilder entityBuilder)
    {
        _userDao = userDao;
        _tenantDomainDao = tenantDomainDao;
        _outboxDao = outboxDao;
        _unitOfWork = unitOfWork;
        _claimContext = claimContext;
        _studentRegisteredEventBuilder = studentRegisteredEventBuilder;
        _entityBuilder = entityBuilder;
    }

    public async Task<RegisterUserOutcome> RegisterAsync(RegisterCredentialsDto request, UserRole role)
    {
        User user = _entityBuilder.BuildUser(request, role);
        Guid tenantId = _claimContext.TenantId;

        await using ITransactionScope scope = await _unitOfWork.BeginAsync();

        bool created = await _userDao.TryCreateAsync(user, scope);
        if (!created)
        {
            return new RegisterUserOutcome.DuplicateName();
        }

        TenantDomain tenantDomain = _entityBuilder.BuildTenantDomain(user.Id, tenantId);
        await _tenantDomainDao.CreateAsync(tenantDomain, scope);

        if (role == UserRole.Student)
        {
            OutboxEvent outboxEvent = _studentRegisteredEventBuilder.Build(user, tenantId);
            await _outboxDao.InsertAsync(outboxEvent, scope);
        }

        await scope.CommitAsync();

        return new RegisterUserOutcome.Created();
    }
}
