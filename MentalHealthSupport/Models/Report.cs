public class Report
{
    public int ReportId { get; set; }
    public int ReporterId { get; set; }
    public int ReportedUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
