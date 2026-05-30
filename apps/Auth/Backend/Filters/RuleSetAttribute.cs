namespace Backend.Filters;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class RuleSetAttribute : Attribute
{
    public string[] RuleSets { get; }

    public RuleSetAttribute(params string[] ruleSets)
    {
        RuleSets = ruleSets;
    }
}
