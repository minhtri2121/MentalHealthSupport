using System.ComponentModel.DataAnnotations;

namespace MentalHealthSupport.Models.ViewModel
{
    public class CreateReviewViewModel
    {
        public int AppointmentId { get; set; }
        public int ConsultantId { get; set; }
        public string ConsultantName { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng chọn số sao")]
        [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5")]
        public int Rating { get; set; }

        [StringLength(1000, ErrorMessage = "Nhận xét tối đa 1000 ký tự")]
        public string? Comment { get; set; }
    }
}