namespace Backend.Results.Tenants;

public abstract record UpdateTenantNameOutcome
{
    private UpdateTenantNameOutcome() { }

    public sealed record Updated : UpdateTenantNameOutcome;

    public sealed record NotFound : UpdateTenantNameOutcome;
}
