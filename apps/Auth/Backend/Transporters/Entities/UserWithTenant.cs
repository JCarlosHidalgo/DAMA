using Backend.Entities.Tenants;
using Backend.Entities.Users;

namespace Backend.Transporters.Entities;

public sealed record UserWithTenant(User User, Tenant Tenant);
