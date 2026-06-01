using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.PaymentCredentials;
using Backend.Dtos.Todotix.Input;
using Backend.Dtos.Todotix.Output;
using Backend.Entities.PaymentCredentials;
using Backend.Results.Todotix;
using Backend.Security;
using Backend.Services.Abstract.Todotix;

namespace Backend.Services.Concrete.Todotix;

public sealed class TodotixCredentialService : ITodotixCredentialService
{
    private readonly ITenantPaymentCredentialReader _credentialReader;
    private readonly ITenantPaymentCredentialWriter _credentialWriter;
    private readonly IAppKeyCipher _appKeyCipher;
    private readonly ITodotixAppKeyResolver _appKeyResolver;
    private readonly IClaimContext _claimContext;
    private readonly ITodotixCredentialViewBuilder _viewBuilder;

    public TodotixCredentialService(ITenantPaymentCredentialReader credentialReader,
                                    ITenantPaymentCredentialWriter credentialWriter,
                                    IAppKeyCipher appKeyCipher,
                                    ITodotixAppKeyResolver appKeyResolver,
                                    IClaimContext claimContext,
                                    ITodotixCredentialViewBuilder viewBuilder)
    {
        _credentialReader = credentialReader;
        _credentialWriter = credentialWriter;
        _appKeyCipher = appKeyCipher;
        _appKeyResolver = appKeyResolver;
        _claimContext = claimContext;
        _viewBuilder = viewBuilder;
    }

    public async Task<TodotixAppKeyStatusDto> GetStatusAsync()
    {
        Guid tenantId = _claimContext.TenantId;
        TenantPaymentCredential? credential = await _credentialReader.GetByTenantAsync(tenantId);
        string effectiveAppKey = await _appKeyResolver.ResolveAsync(tenantId);
        return _viewBuilder.BuildStatus(credential is not null, effectiveAppKey);
    }

    public async Task<PaymentAvailabilityDto> GetAvailabilityAsync()
    {
        TenantPaymentCredential? credential = await _credentialReader.GetByTenantAsync(_claimContext.TenantId);
        return new PaymentAvailabilityDto { HasPaymentCredentials = credential is not null };
    }

    public async Task<TodotixAppKeyRevealDto> RevealAsync()
    {
        string effectiveAppKey = await _appKeyResolver.ResolveAsync(_claimContext.TenantId);
        return new TodotixAppKeyRevealDto { AppKey = effectiveAppKey };
    }

    public async Task<UpdateTodotixAppKeyOutcome> UpdateAsync(UpdateTodotixAppKeyDto dto)
    {
        Guid tenantId = _claimContext.TenantId;
        string encryptedAppKey = _appKeyCipher.Encrypt(dto.AppKey);
        await _credentialWriter.UpsertAsync(tenantId, encryptedAppKey);
        return new UpdateTodotixAppKeyOutcome.Updated();
    }
}
