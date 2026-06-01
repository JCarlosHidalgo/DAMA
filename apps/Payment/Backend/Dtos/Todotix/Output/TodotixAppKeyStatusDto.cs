namespace Backend.Dtos.Todotix.Output;

public class TodotixAppKeyStatusDto
{
    public bool HasCustomKey { get; set; }

    public string? MaskedAppKey { get; set; }
}
