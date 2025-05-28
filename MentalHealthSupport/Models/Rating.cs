public class Rating
{
    public int RatingId { get; set; }
    public int UserId { get; set; }
    public int ConsultantId { get; set; }
    public int Stars { get; set; } // 1 - 5
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
