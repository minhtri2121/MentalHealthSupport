namespace MentalHealthSupport.Models.ViewModel
{
    public class ChatbotArticleItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string SourceType { get; set; } = "News";

        public string Url => $"/News/Detail/{Id}";
    }
}