public class Payment
{
    public int PaymentId { get; set; }
    public int UserId { get; set; }
    public int ConsultantId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Completed, Failed
}
