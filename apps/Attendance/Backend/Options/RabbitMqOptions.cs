namespace Backend.Options;

public sealed class RabbitMqOptions
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 5672;

    public string User { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string ExchangeName { get; set; } = "dama.events";

    public string StudentRegisteredQueueName { get; set; } = "attendance.student-registered";

    public string StudentRegisteredRoutingKey { get; set; } = "student.registered";

    public string CourseDeletedQueueName { get; set; } = "attendance.course-deleted";

    public string CourseDeletedRoutingKey { get; set; } = "course.deleted";

    public string ClassDeletedQueueName { get; set; } = "attendance.class-deleted";

    public string ClassDeletedRoutingKey { get; set; } = "class.deleted";

    public string PaymentCapturedQueueName { get; set; } = "attendance.payment-captured";

    public string PaymentCapturedRoutingKey { get; set; } = "payment.captured";

    public ushort PrefetchCount { get; set; } = 10;

    public int ReconnectDelaySeconds { get; set; } = 5;
}
