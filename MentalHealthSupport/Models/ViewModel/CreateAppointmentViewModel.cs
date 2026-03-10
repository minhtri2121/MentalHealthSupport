using System.ComponentModel.DataAnnotations;

namespace MentalHealthSupport.Models.ViewModel
{
    public class CreateAppointmentViewModel
    {
        public int ConsultantId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thời gian")]
        public DateTime AppointmentTime { get; set; }

        public string? Note { get; set; }
    }
}