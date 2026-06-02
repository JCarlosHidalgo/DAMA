namespace Backend.Dtos.Users.Input;

public class RefreshTokenRequestDto
{
    public required string RefreshToken { get; set; } = string.Empty;
}
