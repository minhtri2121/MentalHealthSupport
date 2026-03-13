namespace MentalHealthSupport.Models.ViewModel
{
    public class ChatbotArticleItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string SourceType { get; set; } = "Article";
        public DateTime CreatedAt { get; set; }
    }
}