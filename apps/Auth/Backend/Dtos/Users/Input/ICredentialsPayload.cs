namespace Backend.Dtos.Users.Input;

public interface ICredentialsPayload
{
    string Username { get; }

    string Password { get; }
}
