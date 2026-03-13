using Microsoft.Data.SqlClient;
using MentalHealthSupport.Models.ViewModel;

namespace MentalHealthSupport.Services
{
    public class ChatbotService
    {
        private readonly string? _connectionString;

        public ChatbotService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public ChatbotResponseViewModel GetResponse(string message, int? userId = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return new ChatbotResponseViewModel
                {
                    Reply = "Bạn hãy nhập nội dung để mình hỗ trợ nhé.",
                    Type = "text"
                };
            }

            string text = NormalizeText(message);

            if (IsGreeting(text))
            {
                return new ChatbotResponseViewModel
                {
                    Reply = "Xin chào, mình là chatbot của MentalHealthSupport. Mình có thể giúp bạn tìm chuyên gia, xem bài viết, kiểm tra lịch hẹn hoặc hướng dẫn đặt lịch.",
                    Type = "text"
                };
            }

            if (IsMyAppointmentQuestion(text))
            {
                if (!userId.HasValue)
                {
                    return new ChatbotResponseViewModel
                    {
                        Reply = "Bạn cần đăng nhập để mình kiểm tra lịch hẹn của bạn.",
                        Type = "text"
                    };
                }

                return GetMyAppointmentsReply(userId.Value);
            }

            if (text.Contains("đặt lịch") || text.Contains("đặt hẹn") || text.Contains("lịch tư vấn") || text.Contains("lịch hẹn"))
            {
                return new ChatbotResponseViewModel
                {
                    Reply = "Bạn có thể vào mục Chuyên gia, chọn hồ sơ phù hợp rồi bấm nút Đặt lịch tư vấn.",
                    Type = "actions",
                    Items = new[]
                    {
                        new { label = "Xem chuyên gia", url = "/Consultants/Index" },
                        new { label = "Lịch hẹn của tôi", url = "/Appointments/MyAppointments" }
                    }
                };
            }

            if (IsConsultantQuestion(text))
            {
                string? specialty = DetectSpecialty(text);
                bool topExperienced = text.Contains("giỏi") || text.Contains("nhiều kinh nghiệm") || text.Contains("tốt nhất") || text.Contains("nổi bật");
                return GetConsultantsReply(specialty, topExperienced);
            }

            if (IsArticleQuestion(text))
            {
                string? keyword = DetectArticleKeyword(text);
                bool latestOnly = text.Contains("mới") || text.Contains("gần đây") || text.Contains("mới nhất");
                return GetArticlesReply(keyword, latestOnly);
            }

            if (text.Contains("stress") || text.Contains("căng thẳng"))
            {
                return new ChatbotResponseViewModel
                {
                    Reply = "Stress kéo dài có thể ảnh hưởng đến giấc ngủ và tinh thần. Bạn có thể xem bài viết liên quan hoặc tìm chuyên gia phù hợp để được hỗ trợ.",
                    Type = "actions",
                    Items = new[]
                    {
                        new { label = "Xem bài viết", url = "/News/Index" },
                        new { label = "Tìm chuyên gia", url = "/Consultants/Index" }
                    }
                };
            }

            if (text.Contains("mất ngủ"))
            {
                return new ChatbotResponseViewModel
                {
                    Reply = "Mất ngủ có thể liên quan đến căng thẳng hoặc lo âu. Bạn nên nghỉ ngơi điều độ và cân nhắc trao đổi với chuyên gia nếu tình trạng kéo dài.",
                    Type = "actions",
                    Items = new[]
                    {
                        new { label = "Tìm chuyên gia", url = "/Consultants/Index" }
                    }
                };
            }

            if (text.Contains("lo âu") || text.Contains("lo lắng"))
            {
                return new ChatbotResponseViewModel
                {
                    Reply = "Lo âu kéo dài có thể ảnh hưởng đến sinh hoạt hằng ngày. Mình có thể gợi ý chuyên gia phù hợp hoặc bài viết liên quan.",
                    Type = "actions",
                    Items = new[]
                    {
                        new { label = "Xem chuyên gia", url = "/Consultants/Index" },
                        new { label = "Xem bài viết", url = "/News/Index" }
                    }
                };
            }

            if (text.Contains("trầm cảm"))
            {
                return new ChatbotResponseViewModel
                {
                    Reply = "Nếu bạn cảm thấy buồn bã, mất động lực hoặc kiệt sức trong thời gian dài, bạn nên tìm sự hỗ trợ từ chuyên gia tâm lý.",
                    Type = "actions",
                    Items = new[]
                    {
                        new { label = "Tìm chuyên gia", url = "/Consultants/Index" }
                    }
                };
            }

            if (text.Contains("khẩn cấp") || text.Contains("tự tử") || text.Contains("muốn chết") || text.Contains("không muốn sống"))
            {
                return new ChatbotResponseViewModel
                {
                    Reply = "Mình rất tiếc khi bạn đang trong trạng thái nghiêm trọng. Hãy liên hệ ngay người thân, bạn bè đáng tin cậy hoặc cơ sở y tế gần nhất để được giúp đỡ ngay.",
                    Type = "text"
                };
            }

            return new ChatbotResponseViewModel
            {
                Reply = "Mình đã ghi nhận câu hỏi của bạn. Bạn có thể hỏi về chuyên gia, bài viết, lịch hẹn hoặc cách đặt lịch để mình hỗ trợ cụ thể hơn.",
                Type = "quickReplies",
                Items = new[]
                {
                    "Tìm chuyên gia",
                    "Bài viết mới",
                    "Lịch hẹn của tôi",
                    "Cách đặt lịch"
                }
            };
        }

        public void SaveChatHistory(int? userId, string userMessage, string botReply)
        {
            try
            {
                using SqlConnection conn = new SqlConnection(_connectionString);
                conn.Open();

                string query = @"
                    INSERT INTO ChatbotMessages (UserId, UserMessage, BotReply, CreatedAt)
                    VALUES (@UserId, @UserMessage, @BotReply, GETDATE())";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UserMessage", userMessage);
                cmd.Parameters.AddWithValue("@BotReply", botReply);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Chatbot SaveChatHistory error: " + ex.Message);
            }
        }

        private string NormalizeText(string text)
        {
            return text.Trim().ToLower();
        }

        private bool IsGreeting(string text)
        {
            return text.Contains("xin chào")
                || text == "chào"
                || text == "hi"
                || text == "hello"
                || text == "hey";
        }

        private bool IsConsultantQuestion(string text)
        {
            return text.Contains("chuyên gia")
                || text.Contains("tư vấn")
                || text.Contains("bác sĩ")
                || text.Contains("nhà tâm lý")
                || text.Contains("tâm lý");
        }

        private bool IsArticleQuestion(string text)
        {
            return text.Contains("bài viết")
                || text.Contains("tin tức")
                || text.Contains("bài báo")
                || text.Contains("bài tư vấn")
                || text.Contains("đọc thêm");
        }

        private bool IsMyAppointmentQuestion(string text)
        {
            return text.Contains("lịch hẹn của tôi")
                || text.Contains("tôi có lịch")
                || text.Contains("lịch của tôi")
                || text.Contains("lịch tư vấn của tôi")
                || text.Contains("tôi đã đặt lịch");
        }

        private string? DetectSpecialty(string text)
        {
            if (text.Contains("lo âu") || text.Contains("lo lắng")) return "lo âu";
            if (text.Contains("stress") || text.Contains("căng thẳng")) return "stress";
            if (text.Contains("trầm cảm")) return "trầm cảm";
            if (text.Contains("mất ngủ")) return "mất ngủ";
            if (text.Contains("học đường")) return "học đường";
            if (text.Contains("tâm lý")) return "tâm lý";
            if (text.Contains("gia đình")) return "gia đình";
            if (text.Contains("hôn nhân")) return "hôn nhân";
            if (text.Contains("trẻ em")) return "trẻ em";
            return null;
        }

        private string? DetectArticleKeyword(string text)
        {
            if (text.Contains("stress")) return "stress";
            if (text.Contains("lo âu")) return "lo âu";
            if (text.Contains("trầm cảm")) return "trầm cảm";
            if (text.Contains("mất ngủ")) return "mất ngủ";
            if (text.Contains("tâm lý")) return "tâm lý";
            if (text.Contains("gia đình")) return "gia đình";
            return null;
        }

        private ChatbotResponseViewModel GetConsultantsReply(string? specialty, bool topExperienced = false)
        {
            try
            {
                using SqlConnection conn = new SqlConnection(_connectionString);
                conn.Open();

                string query;
                SqlCommand cmd;

                if (!string.IsNullOrEmpty(specialty))
                {
                    query = @"
                        SELECT TOP 5 cp.ConsultantId, u.FullName, cp.Specialty, cp.ExperienceYears
                        FROM ConsultantProfiles cp
                        INNER JOIN Users u ON cp.ConsultantId = u.UserId
                        WHERE cp.ApprovalStatus = 'Approved'
                          AND cp.Specialty LIKE @Keyword
                        ORDER BY cp.ExperienceYears DESC, u.FullName ASC";
                    cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Keyword", "%" + specialty + "%");
                }
                else
                {
                    query = @"
                        SELECT TOP 5 cp.ConsultantId, u.FullName, cp.Specialty, cp.ExperienceYears
                        FROM ConsultantProfiles cp
                        INNER JOIN Users u ON cp.ConsultantId = u.UserId
                        WHERE cp.ApprovalStatus = 'Approved'
                        ORDER BY cp.ExperienceYears DESC, u.FullName ASC";
                    cmd = new SqlCommand(query, conn);
                }

                using SqlDataReader reader = cmd.ExecuteReader();
                var consultants = new List<ChatbotConsultantItemViewModel>();

                while (reader.Read())
                {
                    consultants.Add(new ChatbotConsultantItemViewModel
                    {
                        ConsultantId = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                        FullName = reader.IsDBNull(1) ? "Chuyên gia" : reader.GetString(1),
                        Specialty = reader.IsDBNull(2) ? "Chưa cập nhật" : reader.GetString(2),
                        ExperienceYears = reader.IsDBNull(3) ? 0 : reader.GetInt32(3)
                    });
                }

                if (consultants.Count == 0)
                {
                    return new ChatbotResponseViewModel
                    {
                        Reply = specialty == null
                            ? "Hiện chưa có chuyên gia phù hợp để gợi ý."
                            : $"Hiện chưa tìm thấy chuyên gia phù hợp với chủ đề '{specialty}'.",
                        Type = "text"
                    };
                }

                string reply;
                if (specialty != null)
                    reply = $"Mình tìm được một số chuyên gia liên quan đến '{specialty}':";
                else if (topExperienced)
                    reply = "Đây là một số chuyên gia có nhiều kinh nghiệm:";
                else
                    reply = "Đây là một số chuyên gia nổi bật:";

                return new ChatbotResponseViewModel
                {
                    Reply = reply,
                    Type = "consultants",
                    Items = consultants
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("Chatbot GetConsultantsReply error: " + ex.Message);
                return new ChatbotResponseViewModel
                {
                    Reply = "Hiện tại mình chưa lấy được danh sách chuyên gia. Bạn có thể vào trực tiếp mục Chuyên gia để xem thêm.",
                    Type = "text"
                };
            }
        }

        private ChatbotResponseViewModel GetArticlesReply(string? keyword, bool latestOnly = false)
        {
            try
            {
                using SqlConnection conn = new SqlConnection(_connectionString);
                conn.Open();

                var articles = new List<ChatbotArticleItemViewModel>();

                string articleQuery;
                SqlCommand articleCmd;

                if (!string.IsNullOrEmpty(keyword))
                {
                    articleQuery = @"
                        SELECT TOP 5 ArticleId, Title, CreatedAt
                        FROM Articles
                        WHERE Title LIKE @Keyword OR Content LIKE @Keyword OR Category LIKE @Keyword
                        ORDER BY CreatedAt DESC";
                    articleCmd = new SqlCommand(articleQuery, conn);
                    articleCmd.Parameters.AddWithValue("@Keyword", "%" + keyword + "%");
                }
                else
                {
                    articleQuery = @"
                        SELECT TOP 5 ArticleId, Title, CreatedAt
                        FROM Articles
                        ORDER BY CreatedAt DESC";
                    articleCmd = new SqlCommand(articleQuery, conn);
                }

                using (SqlDataReader reader = articleCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        articles.Add(new ChatbotArticleItemViewModel
                        {
                            Id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                            Title = reader.IsDBNull(1) ? "Bài viết" : reader.GetString(1),
                            CreatedAt = reader.IsDBNull(2) ? DateTime.Now : reader.GetDateTime(2),
                            SourceType = "Article"
                        });
                    }
                }

                if (articles.Count == 0)
                {
                    string newsQuery;
                    SqlCommand newsCmd;

                    if (!string.IsNullOrEmpty(keyword))
                    {
                        newsQuery = @"
                            SELECT TOP 5 Id, Title, CreatedDate
                            FROM News
                            WHERE Title LIKE @Keyword OR Content LIKE @Keyword
                            ORDER BY CreatedDate DESC";
                        newsCmd = new SqlCommand(newsQuery, conn);
                        newsCmd.Parameters.AddWithValue("@Keyword", "%" + keyword + "%");
                    }
                    else
                    {
                        newsQuery = @"
                            SELECT TOP 5 Id, Title, CreatedDate
                            FROM News
                            ORDER BY CreatedDate DESC";
                        newsCmd = new SqlCommand(newsQuery, conn);
                    }

                    using SqlDataReader newsReader = newsCmd.ExecuteReader();
                    while (newsReader.Read())
                    {
                        articles.Add(new ChatbotArticleItemViewModel
                        {
                            Id = newsReader.IsDBNull(0) ? 0 : newsReader.GetInt32(0),
                            Title = newsReader.IsDBNull(1) ? "Tin tức" : newsReader.GetString(1),
                            CreatedAt = newsReader.IsDBNull(2) ? DateTime.Now : newsReader.GetDateTime(2),
                            SourceType = "News"
                        });
                    }
                }

                if (articles.Count == 0)
                {
                    return new ChatbotResponseViewModel
                    {
                        Reply = keyword == null
                            ? "Hiện chưa có bài viết hoặc tin tức nào."
                            : $"Hiện chưa có bài viết phù hợp với từ khóa '{keyword}'.",
                        Type = "text"
                    };
                }

                string reply;
                if (keyword != null)
                    reply = $"Đây là một số bài viết liên quan đến '{keyword}':";
                else if (latestOnly)
                    reply = "Đây là một số bài viết hoặc tin tức mới:";
                else
                    reply = "Đây là một số bài viết gần đây:";

                return new ChatbotResponseViewModel
                {
                    Reply = reply,
                    Type = "articles",
                    Items = articles
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("Chatbot GetArticlesReply error: " + ex.Message);
                return new ChatbotResponseViewModel
                {
                    Reply = "Hiện tại mình chưa lấy được danh sách bài viết.",
                    Type = "text"
                };
            }
        }

        private ChatbotResponseViewModel GetMyAppointmentsReply(int userId)
        {
            try
            {
                using SqlConnection conn = new SqlConnection(_connectionString);
                conn.Open();

                string query = @"
                    SELECT TOP 5 a.AppointmentId, a.AppointmentTime, a.Status, u.FullName
                    FROM Appointments a
                    INNER JOIN Users u ON a.ConsultantId = u.UserId
                    WHERE a.UserId = @UserId
                    ORDER BY a.AppointmentTime DESC";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserId", userId);

                using SqlDataReader reader = cmd.ExecuteReader();
                var appointments = new List<ChatbotAppointmentItemViewModel>();

                while (reader.Read())
                {
                    appointments.Add(new ChatbotAppointmentItemViewModel
                    {
                        AppointmentId = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                        AppointmentTime = reader.IsDBNull(1) ? DateTime.Now : reader.GetDateTime(1),
                        Status = reader.IsDBNull(2) ? "" : TranslateStatus(reader.GetString(2)),
                        ConsultantName = reader.IsDBNull(3) ? "Chuyên gia" : reader.GetString(3)
                    });
                }

                if (appointments.Count == 0)
                {
                    return new ChatbotResponseViewModel
                    {
                        Reply = "Bạn hiện chưa có lịch hẹn nào.",
                        Type = "text"
                    };
                }

                return new ChatbotResponseViewModel
                {
                    Reply = "Đây là một số lịch hẹn gần đây của bạn:",
                    Type = "appointments",
                    Items = appointments
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("Chatbot GetMyAppointmentsReply error: " + ex.Message);
                return new ChatbotResponseViewModel
                {
                    Reply = "Mình chưa lấy được thông tin lịch hẹn của bạn lúc này.",
                    Type = "text"
                };
            }
        }

        private string TranslateStatus(string status)
        {
            return status switch
            {
                "Pending" => "Chờ xác nhận",
                "Confirmed" => "Đã xác nhận",
                "Completed" => "Đã hoàn thành",
                "Cancelled" => "Đã hủy",
                _ => status
            };
        }
    }
}