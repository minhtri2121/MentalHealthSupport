namespace MentalHealthSupport.Models.ViewModel
{
    public class NewsDetailViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public string? ImageUrl { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}