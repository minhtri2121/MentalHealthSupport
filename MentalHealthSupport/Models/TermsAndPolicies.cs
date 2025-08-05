namespace MentalHealthSupport.Models
{
    public class TermsAndPolicy
    {
        public int Id { get; set; }
        public string PolicyType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public bool IsActive { get; set; }
    }
}