using Backend.Builders;
using Backend.Claims;
using Backend.DB.Daos.Abstract.Single.PaymentCredentials;
using Backend.Dtos.External.Todotix;
using Backend.Dtos.Todotix.Input;
using Backend.Dtos.Todotix.Output;
using Backend.Entities.PaymentCredentials;
using Backend.Logging;
using Backend.Results.Todotix;
using Backend.Security;
using Backend.Services.Abstract.Todotix;

using Microsoft.Extensions.Logging;

namespace Backend.Services.Concrete.Todotix;

public sealed class TodotixCredentialService : ITodotixCredentialService
{
    private readonly ITenantPaymentCredentialReader _credentialReader;
    private readonly ITenantPaymentCredentialWriter _credentialWriter;
    private readonly IAppKeyCipher _appKeyCipher;
    private readonly IClaimContext _claimContext;
    private readonly ITodotixCredentialViewBuilder _viewBuilder;
    private readonly ITodotixClient _todotixClient;
    private readonly ITodotixCredentialTestBuilder _testBuilder;
    private readonly ILogger<TodotixCredentialService> _logger;

    public TodotixCredentialService(ITenantPaymentCredentialReader credentialReader,
                                    ITenantPaymentCredentialWriter credentialWriter,
                                    IAppKeyCipher appKeyCipher,
                                    IClaimContext claimContext,
                                    ITodotixCredentialViewBuilder viewBuilder,
                                    ITodotixClient todotixClient,
                                    ITodotixCredentialTestBuilder testBuilder,
                                    ILogger<TodotixCredentialService> logger)
    {
        _credentialReader = credentialReader;
        _credentialWriter = credentialWriter;
        _appKeyCipher = appKeyCipher;
        _claimContext = claimContext;
        _viewBuilder = viewBuilder;
        _todotixClient = todotixClient;
        _testBuilder = testBuilder;
        _logger = logger;
    }

    public async Task<TodotixAppKeyStatusDto> GetStatusAsync()
    {
        TenantPaymentCredential? credential = await _credentialReader.GetByTenantAsync(_claimContext.TenantId);
        string? customAppKey = credential is null ? null : _appKeyCipher.Decrypt(credential.TodotixAppKey);
        return _viewBuilder.BuildStatus(credential is not null, customAppKey);
    }

    public async Task<PaymentAvailabilityDto> GetAvailabilityAsync()
    {
        TenantPaymentCredential? credential = await _credentialReader.GetByTenantAsync(_claimContext.TenantId);
        return new PaymentAvailabilityDto { HasPaymentCredentials = credential is not null };
    }

    public async Task<TodotixAppKeyRevealDto> RevealAsync()
    {
        TenantPaymentCredential? credential = await _credentialReader.GetByTenantAsync(_claimContext.TenantId);
        string appKey = credential is null ? string.Empty : _appKeyCipher.Decrypt(credential.TodotixAppKey);
        return new TodotixAppKeyRevealDto { AppKey = appKey };
    }

    public async Task<UpdateTodotixAppKeyOutcome> UpdateAsync(UpdateTodotixAppKeyDto dto)
    {
        Guid tenantId = _claimContext.TenantId;
        string encryptedAppKey = _appKeyCipher.Encrypt(dto.AppKey);
        await _credentialWriter.UpsertAsync(tenantId, encryptedAppKey);
        return new UpdateTodotixAppKeyOutcome.Updated();
    }

    public async Task<TestTodotixCredentialOutcome> TestAsync()
    {
        TenantPaymentCredential? credential = await _credentialReader.GetByTenantAsync(_claimContext.TenantId);
        if (credential is null)
        {
            return new TestTodotixCredentialOutcome.NotConfigured();
        }

        string appKey = _appKeyCipher.Decrypt(credential.TodotixAppKey);
        RegisterDebtRequest request = _testBuilder.BuildCredentialTestRequest(appKey, _claimContext.TenantTimezone);

        try
        {
            RegisterDebtResponse response = await _todotixClient.RegisterDebtAsync(request);
            if (response.Error == 0)
            {
                return new TestTodotixCredentialOutcome.Works();
            }

            LogEvents.TodotixCredentialTestFailed(_logger, _claimContext.TenantId, response.Error, response.Mensaje);
            return new TestTodotixCredentialOutcome.Failed();
        }
        catch (HttpRequestException httpRequestException)
        {
            LogEvents.TodotixCredentialTestHttpError(_logger, httpRequestException, _claimContext.TenantId);
            return new TestTodotixCredentialOutcome.Failed();
        }
    }
}
