namespace Backend.Dtos.QrPayments.Output;

public class StudentQrBreakdownDto
{
    public int PendingCount { get; set; }

    public int PendingAmount { get; set; }

    public int SuccessCount { get; set; }

    public int SuccessAmount { get; set; }

    public int ExpiredCount { get; set; }

    public int ExpiredAmount { get; set; }

    public int OtherFailedCount { get; set; }

    public int OtherFailedAmount { get; set; }

    public string Currency { get; set; } = "BOB";
}
