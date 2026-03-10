namespace MentalHealthSupport.Models.ViewModel
{
    public class MyAppointmentViewModel
    {
        public int AppointmentId { get; set; }

        public DateTime AppointmentTime { get; set; }

        public string Status { get; set; } = "";

        public string? Note { get; set; }

        public string ConsultantName { get; set; } = "";
    }
}