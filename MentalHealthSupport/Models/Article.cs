public class Article
{
    public int ArticleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? AuthorId { get; set; } // Có thể là chuyên gia, hoặc admin
}
