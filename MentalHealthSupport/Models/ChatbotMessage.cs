namespace MentalHealthSupport.Models
{
    public class ChatbotMessage
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string UserMessage { get; set; } = "";
        public string BotReply { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}