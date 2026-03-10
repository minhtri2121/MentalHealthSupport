namespace MentalHealthSupport.Models.ViewModel
{
    public class ConsultantAppointmentViewModel
    {
        public int AppointmentId { get; set; }
        public string UserName { get; set; } = "";
        public DateTime AppointmentTime { get; set; }
        public string Status { get; set; } = "";
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}