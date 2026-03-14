namespace MentalHealthSupport.Models
{
    public class ChatbotConversationContext
    {
        public string ConversationId { get; set; } = Guid.NewGuid().ToString();
        public int? UserId { get; set; }

        public string LastIntent { get; set; } = "";
        public string LastTopic { get; set; } = "";
        public string LastSpecialty { get; set; } = "";
        public string LastKeyword { get; set; } = "";
        public string LastResponseType { get; set; } = "";
        public string LastItemsJson { get; set; } = "";

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}