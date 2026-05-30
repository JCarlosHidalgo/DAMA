namespace Backend.Results.Tenants;

public abstract record UpdateTenantTimezoneOutcome
{
    private UpdateTenantTimezoneOutcome() { }

    public sealed record Updated : UpdateTenantTimezoneOutcome;

    public sealed record Forbidden : UpdateTenantTimezoneOutcome;

    public sealed record NotFound : UpdateTenantTimezoneOutcome;
}
