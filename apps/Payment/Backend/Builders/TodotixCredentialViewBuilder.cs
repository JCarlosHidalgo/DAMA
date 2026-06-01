using Backend.Dtos.Todotix.Output;

namespace Backend.Builders;

public sealed class TodotixCredentialViewBuilder : ITodotixCredentialViewBuilder
{
    private const int VisibleSuffixLength = 4;

    public TodotixAppKeyStatusDto BuildStatus(bool hasCustomKey, string effectiveAppKey)
    {
        return new TodotixAppKeyStatusDto
        {
            HasCustomKey = hasCustomKey,
            MaskedAppKey = Mask(effectiveAppKey)
        };
    }

    private static string? Mask(string appKey)
    {
        if (string.IsNullOrEmpty(appKey))
        {
            return null;
        }

        string suffix = appKey.Length <= VisibleSuffixLength
            ? appKey
            : appKey[^VisibleSuffixLength..];
        return new string('•', 8) + suffix;
    }
}
