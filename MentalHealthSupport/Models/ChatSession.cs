public class ChatSession
{
    public int ChatSessionId { get; set; }
    public int UserId { get; set; }
    public int ConsultantId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}