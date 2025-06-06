namespace MentalHealthSupport.Models.ViewModel
{
    public class ConsultantViewModel
    {
        // Thông tin User
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsVerified { get; set; }

        // Thông tin ConsultantProfile
        public int ConsultantId { get; set; }
        public string Specialty { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ApprovalStatus { get; set; } = "Pending";
        public int ExperienceYears { get; set; }
        public string AvatarUrl { get; set; } = string.Empty;
        public string CertificateUrl { get; set; } = string.Empty;
    }
}