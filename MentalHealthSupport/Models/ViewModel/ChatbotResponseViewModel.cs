namespace MentalHealthSupport.Models.ViewModel
{
    public class ChatbotResponseViewModel
    {
        public string Reply { get; set; } = "";
        public string Type { get; set; } = "text";
        public object? Items { get; set; }
    }
}