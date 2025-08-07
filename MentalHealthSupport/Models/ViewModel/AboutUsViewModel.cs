namespace MentalHealthSupport.Models.ViewModel
{
    public class AboutUsViewModel
    {
        public int? Id { get; set; } = 1; // Giả sử chỉ có 1 bản ghi cho AboutUs
        public string? Title { get; set; } = "Về Chúng Tôi";
        public string? HeroHeading { get; set; } = "Về Chúng Tôi";
        public string? HeroDescription { get; set; } = "Chúng tôi tin rằng mọi người đều xứng đáng có được sức khỏe tinh thần tốt và sự hỗ trợ chuyên nghiệp khi cần thiết";
        public string? HeroImageUrl { get; set; }
        public string? MissionHeading { get; set; } = "Sứ Mệnh Của Chúng Tôi";
        public string? ValuesHeading { get; set; } = "Giá Trị Cốt Lõi";
        public string? CallToActionHeading { get; set; } = "Bắt Đầu Hành Trình Chăm Sóc Tâm Lý";
        public string? CallToActionDescription { get; set; } = "Đừng để những khó khăn tâm lý cản trở cuộc sống của bạn. Hãy để chúng tôi đồng hành cùng bạn trên con đường tìm lại sự cân bằng và hạnh phúc.";
        public IFormFile? HeroImageFile { get; set; } // Để upload ảnh cho Hero
    }
}