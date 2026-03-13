namespace MentalHealthSupport.Models.ViewModel
{
    public class ChatbotHistoryViewModel
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string UserName { get; set; } = "";
        public string UserMessage { get; set; } = "";
        public string BotReply { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}