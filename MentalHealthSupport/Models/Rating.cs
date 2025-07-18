namespace MentalHealthSupport.Models;

public class Rating
{
    public int RatingId { get; set; }
    public int AppointmentId { get; set; }
    public int Score { get; set; }
    public string? Comment { get; set; } = string.Empty;
    public DateTime RatedAt { get; set; }
}