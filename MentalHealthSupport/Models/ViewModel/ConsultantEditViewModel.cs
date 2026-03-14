using System.ComponentModel.DataAnnotations;

namespace MentalHealthSupport.ViewModels
{
    public class ConsultantEditViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; } = string.Empty;
        public string Role { get; set; } = "Consultant";
        public bool IsVerified { get; set; } = false;
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public bool Sex { get; set; }
        public string? SecurityQuestion { get; set; } = string.Empty;
        public string? SecurityAnswer { get; set; } = string.Empty;
        public string? Specialty { get; set; } = string.Empty;
        public int? ExperienceYears { get; set; }
        public string? Description { get; set; } = string.Empty;
        public string? ApprovalStatus { get; set; } = "Pending";
        public string? CertificateUrl { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; } = string.Empty;

        public IFormFile? AvatarFile { get; set; }
    }
}
