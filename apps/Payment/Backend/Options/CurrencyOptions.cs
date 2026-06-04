namespace Backend.Options;

public sealed class CurrencyOptions
{
    public string Default { get; set; } = "BOB";

    public IReadOnlyList<string> Allowed { get; set; } = new[] { "BOB" };

    public void Validate()
    {
        if (!IsValidIso4217Code(Default))
        {
            throw new InvalidOperationException($"Currency default '{Default}' is not a valid ISO 4217 alphabetic code.");
        }

        if (Allowed.Count == 0)
        {
            throw new InvalidOperationException("Currency allow-list must contain at least one ISO 4217 code.");
        }

        foreach (string code in Allowed)
        {
            if (!IsValidIso4217Code(code))
            {
                throw new InvalidOperationException($"Currency allow-list entry '{code}' is not a valid ISO 4217 alphabetic code.");
            }
        }

        if (!Allowed.Contains(Default))
        {
            throw new InvalidOperationException($"Currency default '{Default}' is not within the allowed set.");
        }
    }

    public static bool IsValidIso4217Code(string? code) =>
        code is { Length: 3 } && code.All(char.IsAsciiLetterUpper);
}
