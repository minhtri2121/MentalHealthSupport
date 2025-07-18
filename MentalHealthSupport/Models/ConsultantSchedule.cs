namespace MentalHealthSupport.Models;

public class ConsultantSchedule
{
    public int ScheduleId { get; set; }
    public int ConsultantId { get; set; }
    public int DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}