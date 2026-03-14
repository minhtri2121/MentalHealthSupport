namespace MentalHealthSupport.Models.ViewModel
{
    public class ChatbotResponseViewModel
    {
        public string Reply { get; set; } = "";
        public string Type { get; set; } = "text";
        public object? Items { get; set; }
        public List<string> Suggestions { get; set; } = new();
        public string? Intent { get; set; }
        public bool IsEmergency { get; set; } = false;
        public string? ConversationId { get; set; }
        public Dictionary<string, string> Meta { get; set; } = new();
    }
}