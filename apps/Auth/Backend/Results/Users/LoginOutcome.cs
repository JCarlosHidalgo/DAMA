using Backend.Dtos.Users.Output;

namespace Backend.Results.Users;

public abstract record LoginOutcome
{
    private LoginOutcome() { }

    public sealed record Success(TokenResponseDto Tokens) : LoginOutcome;

    public sealed record InvalidCredentials : LoginOutcome;

    public sealed record AccountLocked : LoginOutcome;
}
