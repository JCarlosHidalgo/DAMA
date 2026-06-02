using Backend.Entities.Tokens;

namespace Backend.Transporters.Entities;

public sealed record IssuedRefreshToken(string RawToken, RefreshToken Entity);
