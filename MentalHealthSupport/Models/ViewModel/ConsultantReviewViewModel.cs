namespace MentalHealthSupport.Models.ViewModel
{
    public class ConsultantReviewViewModel
    {
        public int ReviewId { get; set; }
        public int AppointmentId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime RatedAt { get; set; }
        public string UserName { get; set; } = "";
        public string ConsultantName { get; set; } = "";
    }
}