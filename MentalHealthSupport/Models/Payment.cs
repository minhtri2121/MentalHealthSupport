namespace MentalHealthSupport.Models;

public class Payment
{
    public int PaymentId { get; set; }
    public int AppointmentId { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
}