using Backend.Options;

namespace Backend.Modules;

public sealed class OptionsModule : IServiceModule
{
    public int Order => 10;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AttendanceOptions>(options =>
        {
            AttendanceOptions defaults = new AttendanceOptions();
            options.PageSize = configuration.GetValue<int?>("Attendance:PageSize") ?? defaults.PageSize;
            options.AllowedWindowStart = ParseTimeOrDefault(
                configuration["Attendance:AllowedWindowStart"],
                defaults.AllowedWindowStart);
            options.AllowedWindowEnd = ParseTimeOrDefault(
                configuration["Attendance:AllowedWindowEnd"],
                defaults.AllowedWindowEnd);
        });

        services.Configure<RabbitMqOptions>(options =>
        {
            options.Host = configuration["RABBITMQ_HOST"]
                ?? throw new InvalidOperationException("RABBITMQ_HOST not set.");
            string rawPort = configuration["RABBITMQ_PORT"]
                ?? throw new InvalidOperationException("RABBITMQ_PORT not set.");
            if (!int.TryParse(rawPort, out int parsedPort))
            {
                throw new InvalidOperationException("RABBITMQ_PORT is not a valid integer.");
            }
            options.Port = parsedPort;
            options.User = configuration["RABBITMQ_USER"]
                ?? throw new InvalidOperationException("RABBITMQ_USER not set.");
            options.Password = configuration["RABBITMQ_PASSWORD"]
                ?? throw new InvalidOperationException("RABBITMQ_PASSWORD not set.");
        });

        services.Configure<RemainLimits>(options =>
        {
            RemainLimits defaults = new RemainLimits();
            options.MinIncrement = configuration.GetValue<int?>("RemainLimits:MinIncrement")
                                   ?? defaults.MinIncrement;
            options.MaxIncrement = configuration.GetValue<int?>("RemainLimits:MaxIncrement")
                                   ?? defaults.MaxIncrement;
            options.MaxStudentNameLength = configuration.GetValue<int?>("RemainLimits:MaxStudentNameLength")
                                           ?? defaults.MaxStudentNameLength;
        });

        services.Configure<CallbackOptions>(options =>
        {
            options.Secret = configuration["PAYMENT_CALLBACK_SECRET"];
        });
    }

    private static TimeOnly ParseTimeOrDefault(string? rawTime, TimeOnly defaultValue)
    {
        return TimeOnly.TryParse(rawTime, out TimeOnly parsedTime) ? parsedTime : defaultValue;
    }
}
