namespace Backend.Dtos.Summary.Output;

public class PaymentSummaryDto
{
    public int TotalEarnings { get; set; }
    public int MonthEarnings { get; set; }
    public string Currency { get; set; } = "BOB";
    public DateTime? FirstPaymentDate { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}
