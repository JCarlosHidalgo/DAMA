using System.Reflection;

using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Results;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Backend.Filters;

public sealed class FluentValidationActionFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public FluentValidationActionFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext actionContext, ActionExecutionDelegate next)
    {
        string[]? ruleSetNames = ResolveRuleSets(actionContext);

        foreach (object? argument in actionContext.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            Type validatorInterfaceType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            IValidator? validator = (IValidator?)_serviceProvider.GetService(validatorInterfaceType);
            if (validator is null)
            {
                continue;
            }

            IValidationContext validationContext = BuildValidationContext(argument, ruleSetNames);
            ValidationResult validationResult = await validator.ValidateAsync(validationContext);
            if (!validationResult.IsValid)
            {
                actionContext.Result = new BadRequestObjectResult(validationResult.Errors[0].ErrorMessage);
                return;
            }
        }

        await next();
    }

    private static string[]? ResolveRuleSets(ActionExecutingContext actionContext)
    {
        ControllerActionDescriptor? actionDescriptor = actionContext.ActionDescriptor as ControllerActionDescriptor;
        RuleSetAttribute? ruleSetAttribute = actionDescriptor?.MethodInfo.GetCustomAttribute<RuleSetAttribute>();
        return ruleSetAttribute?.RuleSets;
    }

    private static IValidationContext BuildValidationContext(object argument, string[]? ruleSetNames)
    {
        if (ruleSetNames is null || ruleSetNames.Length == 0)
        {
            return new ValidationContext<object>(argument);
        }
        return new ValidationContext<object>(
            argument,
            new PropertyChain(),
            new RulesetValidatorSelector(ruleSetNames));
    }
}
