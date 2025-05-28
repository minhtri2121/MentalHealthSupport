public class ConsultantProfile
{
    public int ConsultantId { get; set; }
    public int UserId { get; set; }
    public string Specialization { get; set; } = string.Empty;
    public string Certificate { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = "Pending"; // Pending, Approved, Rejected
    public int ExperienceYears { get; set; }
}
