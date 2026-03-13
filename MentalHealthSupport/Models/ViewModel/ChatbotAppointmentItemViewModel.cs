namespace MentalHealthSupport.Models.ViewModel
{
    public class ChatbotAppointmentItemViewModel
    {
        public int AppointmentId { get; set; }
        public string ConsultantName { get; set; } = "";
        public DateTime AppointmentTime { get; set; }
        public string Status { get; set; } = "";
    }
}