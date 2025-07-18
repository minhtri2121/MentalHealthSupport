namespace MentalHealthSupport.Models;

public class ConsultantProfile
{
    public int ConsultantId { get; set; }
    public string? Specialty { get; set; } = string.Empty;
    public int ExperienceYears { get; set; }
    public string? Description { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
}