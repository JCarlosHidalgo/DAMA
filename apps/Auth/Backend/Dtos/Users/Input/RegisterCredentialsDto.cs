namespace Backend.Dtos.Users.Input;

public class RegisterCredentialsDto : ICredentialsPayload
{
    public required string Username { get; set; } = string.Empty;

    public required string Password { get; set; } = string.Empty;
}
