public class ChatMessage
{
    public int MessageId { get; set; }
    public int ChatSessionId { get; set; }
    public int SenderId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool IsRead { get; set; }
    public string MessageType { get; set; } = "Text";
}
