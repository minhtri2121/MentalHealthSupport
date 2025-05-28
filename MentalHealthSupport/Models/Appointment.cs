public class Appointment
{
    public int AppointmentId { get; set; }
    public int UserId { get; set; }
    public int ConsultantId { get; set; }
    public DateTime ScheduledTime { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Completed, Canceled
}
