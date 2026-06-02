namespace Backend.Dtos.Users.Output;

public class TokenResponseDto
{
    public required string AccessToken { get; set; }

    public required string RefreshToken { get; set; }
}
