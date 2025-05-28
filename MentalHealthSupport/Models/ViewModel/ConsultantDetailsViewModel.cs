using System;
using System.Collections.Generic;

namespace MentalHealthSupport.Models.ViewModel
{
    public class ConsultantDetailsViewModel
    {
        // Thông tin người dùng
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsVerified { get; set; }

        // Hồ sơ chuyên gia
        public int ConsultantId { get; set; }
        public string Specialty { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ApprovalStatus { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public int ExperienceYears { get; set; }
        public string CertificateUrl { get; set; } = string.Empty;

        // Lịch làm việc
        public List<ConsultantScheduleViewModel> Schedules { get; set; } = new List<ConsultantScheduleViewModel>();

        // Đánh giá từ người dùng
        public List<RatingViewModel> Ratings { get; set; } = new List<RatingViewModel>();

        // Bài viết hoặc nội dung chia sẻ
        public List<ArticleViewModel> Articles { get; set; } = new List<ArticleViewModel>();
    }

    public class ConsultantScheduleViewModel
    {
        public DateTime DayOfWeek { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
    }

    public class RatingViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public int Score { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ArticleViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }
}
