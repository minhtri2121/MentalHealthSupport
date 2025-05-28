namespace MentalHealthSupport.Models.ViewModel
{
    public class ManageViewModel
    {
        public int UserId { get; set; }
        public string? FullName { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public string? Phone { get; set; } = string.Empty;
        public string? Role { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }

        public int ConsultantId { get; set; }
        public string Specialization { get; set; } = string.Empty;
        public string Certificate { get; set; } = string.Empty;
        public string ApprovalStatus { get; set; } = "Pending";
        public int ExperienceYears { get; set; }
    }
}