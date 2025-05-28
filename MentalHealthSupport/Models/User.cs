public class User
{
    public int UserId { get; set; }

    public string? FullName { get; set; } = string.Empty;

    public string? Email { get; set; } = string.Empty;

    public string? Phone { get; set; } = string.Empty;

    public string? Role { get; set; } = string.Empty;

    public bool IsVerified { get; set; }

    public DateTime CreatedAt { get; set; }
}
