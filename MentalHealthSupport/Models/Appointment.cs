namespace MentalHealthSupport.Models;

public class Appointment
{
    public int AppointmentId { get; set; }
    public int UserId { get; set; }
    public int ConsultantId { get; set; }
    public DateTime AppointmentTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}