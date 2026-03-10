namespace MentalHealthSupport.Models
{
    public class Appointment
    {
        public int AppointmentId { get; set; }
        public int UserId { get; set; }
        public int ConsultantId { get; set; }
        public DateTime AppointmentTime { get; set; }
        public string Status { get; set; } = "Pending";
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? MeetingType { get; set; }
        public int DurationMinutes { get; set; } = 60;
        public string? ConsultantNote { get; set; }
    }
}