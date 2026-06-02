using Backend.Entities.Tokens;

namespace Backend.Transporters.Entities;

public sealed record RefreshTokenWithOwner(RefreshToken Token, UserWithTenant Owner);
