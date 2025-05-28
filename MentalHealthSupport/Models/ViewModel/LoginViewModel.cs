using System.ComponentModel.DataAnnotations;

namespace MentalHealthSupport.Models.ViewModel
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [DataType(DataType.Password)]
        public string PasswordHash { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}