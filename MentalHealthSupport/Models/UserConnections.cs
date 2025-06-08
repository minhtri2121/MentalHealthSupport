public class UserConnections
{
    public string ConnectionId { get; set; } = string.Empty;
    public int UserId { get; set; }
    public DateTime ConnectedAt { get; set; }
    public bool IsActive { get; set; }
}