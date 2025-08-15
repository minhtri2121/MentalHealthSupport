using System.ComponentModel.DataAnnotations;

namespace MentalHealthSupport.ViewModels
{
    public class ConsultantCreate
    {
        [Required]
        public int UserId { get; set; }
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
        [Phone]
        [StringLength(20)]
        public string? Phone { get; set; } = string.Empty;
        [Required]
        public string Role { get; set; } = "Consultant";
        public bool IsVerified { get; set; } = false;
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public bool Sex { get; set; }
        public string? SecurityQuestion { get; set; } = string.Empty;
        public string? SecurityAnswer { get; set; } = string.Empty;
        [Required(ErrorMessage = "Chuyên ngành không được để trống")]
        [StringLength(100)]
        public string? Specialty { get; set; } = string.Empty;
        public int? ExperienceYears { get; set; }
        [DataType(DataType.MultilineText)]
        public string? Description { get; set; } = string.Empty;
        public string? ApprovalStatus { get; set; } = "Pending";
        public string? CertificateUrl { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; } = string.Empty;
        public IFormFile? AvatarFile { get; set; }
    }
}
