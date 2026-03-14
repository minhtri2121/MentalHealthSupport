using Microsoft.Data.SqlClient;
using MentalHealthSupport.Models.ViewModel;
using MentalHealthSupport.Models;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MentalHealthSupport.Services
{
    public class ChatbotService
    {
        private readonly string? _connectionString;

        public ChatbotService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public ChatbotResponseViewModel GetResponse(string message, int? userId = null, string? conversationId = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return BuildTextResponse("Bạn hãy nhập nội dung để mình hỗ trợ nhé.");
            }

            string rawText = message.Trim();
            string text = NormalizeText(rawText);
            text = NormalizeSemanticText(text);
            string textNoAccent = RemoveVietnameseAccents(text);

            var context = LoadConversationContext(conversationId, userId);

            // 1. Ưu tiên tình huống khẩn cấp
            if (IsEmergency(text, textNoAccent))
            {
                SaveConversationContext(
                    context.ConversationId,
                    userId,
                    "emergency",
                    "crisis",
                    "",
                    "",
                    "text",
                    null
                );

                return new ChatbotResponseViewModel
                {
                    Reply = "Mình rất tiếc khi bạn đang ở trạng thái nghiêm trọng. Hãy liên hệ ngay người thân đáng tin cậy, cơ sở y tế gần nhất hoặc gọi cấp cứu tại địa phương để được hỗ trợ ngay lúc này. Bạn không nên ở một mình lúc này.",
                    Type = "text"
                };
            }

            // 2. Follow-up theo ngữ cảnh trước đó
            if (IsFollowUp(text, textNoAccent, context))
            {
                return HandleFollowUp(text, textNoAccent, userId, context);
            }

            // 3. Xác định intent
            string intent = DetectIntent(text, textNoAccent);

            switch (intent)
            {
                case "greeting":
                    SaveConversationContext(
                        context.ConversationId,
                        userId,
                        "greeting",
                        "general",
                        "",
                        "",
                        "text",
                        null
                    );

                    return BuildTextResponse("Xin chào, mình là trợ lý của MentalHealthSupport. Mình có thể giúp bạn tìm chuyên gia, gợi ý bài viết, kiểm tra lịch hẹn hoặc hướng dẫn đặt lịch.");

                case "appointment_check":
                    if (!userId.HasValue)
                    {
                        SaveConversationContext(
                            context.ConversationId,
                            userId,
                            "appointment_check",
                            "appointments",
                            "",
                            "",
                            "text",
                            null
                        );

                        return BuildTextResponse("Bạn cần đăng nhập để mình kiểm tra lịch hẹn của bạn.");
                    }

                    return GetMyAppointmentsReply(userId.Value, context.ConversationId);

                case "booking_help":
                    SaveConversationContext(
                        context.ConversationId,
                        userId,
                        "booking_help",
                        "booking",
                        "",
                        "",
                        "actions",
                        null
                    );

                    return new ChatbotResponseViewModel
                    {
                        Reply = "Bạn có thể vào mục Chuyên gia, chọn hồ sơ phù hợp rồi bấm nút Đặt lịch tư vấn. Nếu muốn, mình cũng có thể gợi ý chuyên gia theo vấn đề bạn đang gặp.",
                        Type = "actions",
                        Items = new[]
                        {
                            new { label = "Xem chuyên gia", url = "/Consultants/Index" },
                            new { label = "Lịch hẹn của tôi", url = "/Appointments/MyAppointments" }
                        }
                    };

                case "consultant_search":
                    {
                        string? specialty = DetectSpecialty(text, textNoAccent, context);
                        bool topExperienced = IsTopExperiencedRequest(text, textNoAccent);
                        return GetConsultantsReply(specialty, topExperienced, context.ConversationId, userId);
                    }

                case "article_search":
                    {
                        string? keyword = DetectArticleKeyword(text, textNoAccent, context);
                        bool latestOnly = IsLatestRequest(text, textNoAccent);
                        return GetArticlesReply(keyword, latestOnly, context.ConversationId, userId);
                    }

                case "symptom_support":
                    {
                        string? specialty = DetectSpecialty(text, textNoAccent, context);

                        SaveConversationContext(
                            context.ConversationId,
                            userId,
                            "symptom_support",
                            specialty ?? "mental_health",
                            specialty ?? "",
                            specialty ?? "",
                            "actions",
                            null
                        );

                        return new ChatbotResponseViewModel
                        {
                            Reply = BuildSymptomReply(specialty),
                            Type = "actions",
                            Items = new[]
                            {
                                new { label = "Xem bài viết", url = "/News/Index" },
                                new { label = "Tìm chuyên gia", url = "/Consultants/Index" }
                            }
                        };
                    }

                default:
                    SaveConversationContext(
                        context.ConversationId,
                        userId,
                        "unknown",
                        "general",
                        "",
                        "",
                        "quickReplies",
                        null
                    );

                    return new ChatbotResponseViewModel
                    {
                        Reply = "Mình hiểu bạn đang cần hỗ trợ, nhưng câu hỏi này chưa đủ rõ để mình trả lời chính xác. Bạn có thể chọn một chủ đề bên dưới nhé.",
                        Type = "quickReplies",
                        Items = new[]
                        {
                            "Tìm chuyên gia",
                            "Bài viết mới",
                            "Lịch hẹn của tôi",
                            "Cách đặt lịch",
                            "Tôi đang bị lo âu",
                            "Tôi bị stress"
                        }
                    };
            }
        }

        public void SaveChatHistory(int? userId, string conversationId, string userMessage, string botReply)
        {
            try
            {
                using SqlConnection conn = new SqlConnection(_connectionString);
                conn.Open();

                string query = @"
                    INSERT INTO ChatbotMessages (UserId, ConversationId, UserMessage, BotReply, CreatedAt)
                    VALUES (@UserId, @ConversationId, @UserMessage, @BotReply, GETDATE())";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ConversationId", conversationId);
                cmd.Parameters.AddWithValue("@UserMessage", userMessage);
                cmd.Parameters.AddWithValue("@BotReply", botReply);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Chatbot SaveChatHistory error: " + ex.Message);
            }
        }

        private ChatbotResponseViewModel BuildTextResponse(string reply)
        {
            return new ChatbotResponseViewModel
            {
                Reply = reply,
                Type = "text"
            };
        }

        private string NormalizeText(string text)
        {
            text = text.Trim().ToLower();
            text = Regex.Replace(text, @"\s+", " ");

            var replacements = new Dictionary<string, string>
            {
                { "ko", "không" },
                { "k ", "không " },
                { "khum", "không" },
                { "hong", "không" },
                { "mk", "mình" },
                { "mik", "mình" },
                { "bn", "bao nhiêu" },
                { "trlcam", "trầm cảm" },
                { "cx", "cũng" },
                { "bt", "bình thường" },
                { "dc", "được" },
                { "đc", "được" },
                { "j", "gì" },
                { "vs", "với" },
                { "nt", "như thế" },
                { "r", "rồi" }
            };

            foreach (var item in replacements)
            {
                text = text.Replace(item.Key, item.Value);
            }

            return text;
        }

        private string RemoveVietnameseAccents(string text)
        {
            string normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (char c in normalized)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString()
                .Replace('đ', 'd')
                .Replace('Đ', 'D')
                .Normalize(NormalizationForm.FormC)
                .ToLower();
        }

        private bool IsEmergency(string text, string textNoAccent)
        {
            string[] patterns =
            {
                "tu tu", "muon chet", "khong muon song", "ket thuc tat ca",
                "tu sat", "nghi quach", "nghi den viec chet",
                "tự tử", "muốn chết", "không muốn sống", "tự sát"
            };

            return patterns.Any(p => text.Contains(p) || textNoAccent.Contains(p));
        }

        private bool IsFollowUp(string text, string textNoAccent, ChatbotConversationContext context)
        {
            if (string.IsNullOrWhiteSpace(context.LastIntent)) return false;

            string[] followUps =
            {
                "cai dau tien", "nguoi dau tien", "nguoi thu 2", "nguoi thu hai", "nguoi thu 3", "nguoi thu ba",
                "bai dau tien", "bai thu 2", "bai thu hai", "bai thu 3", "bai thu ba",
                "bai moi nhat", "con ai khac", "con bai nao khac", "chi tiet hon",
                "dat lich nguoi do", "dat lich voi nguoi do", "dat lich voi nguoi dau tien", "dat lich voi nguoi thu 2",
                "cái đầu tiên", "người đầu tiên", "người thứ 2", "người thứ 3",
                "bài đầu tiên", "bài thứ 2", "bài thứ 3", "bài mới nhất",
                "còn ai khác không", "còn bài nào khác không", "đặt lịch với người đó", "đặt lịch với người đầu tiên"
            };

            return followUps.Any(x => text.Contains(x) || textNoAccent.Contains(x));
        }

        private ChatbotResponseViewModel HandleFollowUp(string text, string textNoAccent, int? userId, ChatbotConversationContext context)
        {
            int? requestedIndex = ExtractRequestedIndex(text, textNoAccent);

            if (context.LastIntent == "consultant_search")
            {
                var consultants = DeserializeItems<List<ChatbotConsultantItemViewModel>>(context.LastItemsJson);

                if (consultants != null && consultants.Count > 0)
                {
                    if (IsMoreRequest(text, textNoAccent))
                    {
                        return new ChatbotResponseViewModel
                        {
                            Reply = "Bạn có thể vào danh sách chuyên gia để xem thêm nhiều chuyên gia khác phù hợp hơn.",
                            Type = "actions",
                            Items = new[]
                            {
                                new { label = "Xem tất cả chuyên gia", url = "/Consultants/Index" }
                            }
                        };
                    }

                    if (requestedIndex.HasValue && requestedIndex.Value >= 0 && requestedIndex.Value < consultants.Count)
                    {
                        var item = consultants[requestedIndex.Value];

                        SaveConversationContext(
                            context.ConversationId,
                            userId,
                            "consultant_followup",
                            item.Specialty,
                            item.Specialty,
                            "",
                            "consultants",
                            context.LastItemsJson
                        );

                        if (IsBookingFollowUp(text, textNoAccent))
                        {
                            return new ChatbotResponseViewModel
                            {
                                Reply = $"Bạn có thể đặt lịch với {item.FullName} ngay bây giờ.",
                                Type = "actions",
                                Items = new[]
                                {
                                    new { label = "Đặt lịch ngay", url = $"/Appointments/Create?consultantId={item.ConsultantId}" },
                                    new { label = "Xem hồ sơ chuyên gia", url = $"/Consultants/Details/{item.ConsultantId}" }
                                }
                            };
                        }

                        return new ChatbotResponseViewModel
                        {
                            Reply = $"Chuyên gia bạn chọn là {item.FullName}, chuyên môn {item.Specialty}, kinh nghiệm {item.ExperienceYears} năm.",
                            Type = "actions",
                            Items = new[]
                            {
                                new { label = "Xem hồ sơ", url = $"/Consultants/Details/{item.ConsultantId}" },
                                new { label = "Đặt lịch", url = $"/Appointments/Create?consultantId={item.ConsultantId}" }
                            }
                        };
                    }
                }
            }

            if (context.LastIntent == "article_search")
            {
                var articles = DeserializeItems<List<ChatbotArticleItemViewModel>>(context.LastItemsJson);

                if (articles != null && articles.Count > 0)
                {
                    if (IsMoreRequest(text, textNoAccent))
                    {
                        return new ChatbotResponseViewModel
                        {
                            Reply = "Bạn có thể xem thêm toàn bộ bài viết trong mục bài viết của hệ thống.",
                            Type = "actions",
                            Items = new[]
                            {
                                new { label = "Xem tất cả bài viết", url = "/News/Index" }
                            }
                        };
                    }

                    if ((text.Contains("mới nhất") || textNoAccent.Contains("moi nhat")) && articles.Count > 0)
                    {
                        var newest = articles[0];

                        return new ChatbotResponseViewModel
                        {
                            Reply = $"Bài viết mới nhất là: {newest.Title}.",
                            Type = "actions",
                            Items = new[]
                            {
                                new { label = "Xem chi tiết", url = $"/News/Detail/{newest.Id}" }
                            }
                        };
                    }

                    if (requestedIndex.HasValue && requestedIndex.Value >= 0 && requestedIndex.Value < articles.Count)
                    {
                        var article = articles[requestedIndex.Value];

                        return new ChatbotResponseViewModel
                        {
                            Reply = $"Bài bạn chọn là: {article.Title}.",
                            Type = "actions",
                            Items = new[]
                            {
                                new { label = "Xem chi tiết", url = $"/News/Detail/{article.Id}" },
                                new { label = "Xem tất cả bài viết", url = "/News/Index" }
                            }
                        };
                    }
                }
            }

            return BuildTextResponse("Mình hiểu bạn đang hỏi tiếp nội dung trước đó, nhưng chưa xác định chính xác lựa chọn. Bạn có thể nói rõ hơn như: 'người thứ 2', 'bài thứ 3', hoặc 'đặt lịch với người đầu tiên'.");
        }

        private string DetectIntent(string text, string textNoAccent)
        {
            var scores = new Dictionary<string, int>
            {
                { "greeting", 0 },
                { "appointment_check", 0 },
                { "booking_help", 0 },
                { "consultant_search", 0 },
                { "article_search", 0 },
                { "symptom_support", 0 },
                { "unknown", 0 }
            };

            AddScore(scores, "greeting", text, textNoAccent,
                "xin chào", "chào", "hello", "hi", "hey");

            AddScore(scores, "appointment_check", text, textNoAccent,
                "lịch hẹn", "lịch của tôi", "tôi có lịch", "đã đặt lịch", "xem lịch hẹn");

            AddScore(scores, "booking_help", text, textNoAccent,
                "đặt lịch", "đặt hẹn", "cách đặt lịch", "đăng ký tư vấn", "book lịch");

            AddScore(scores, "consultant_search", text, textNoAccent,
                "chuyên gia", "tư vấn viên", "bác sĩ", "nhà tâm lý", "ai phù hợp",
                "người phù hợp", "chuyên viên", "tìm chuyên gia", "tư vấn", "hỗ trợ");

            AddScore(scores, "article_search", text, textNoAccent,
                "bài viết", "tin tức", "bài báo", "đọc thêm", "kiến thức",
                "xem bài", "bài mới", "tin mới", "bài liên quan", "bài đọc");

            AddScore(scores, "symptom_support", text, textNoAccent,
                "stress", "căng thẳng", "lo âu", "lo lắng", "trầm cảm",
                "mất ngủ", "kiệt sức", "áp lực", "khó ngủ", "hồi hộp",
                "bất an", "chán nản", "trống rỗng", "mệt mỏi", "bí bách");

            int maxScore = scores.Max(x => x.Value);
            if (maxScore == 0) return "unknown";

            return scores.OrderByDescending(x => x.Value).First().Key;
        }

        private void AddScore(Dictionary<string, int> scores, string intent, string text, string textNoAccent, params string[] patterns)
        {
            foreach (var pattern in patterns)
            {
                string p = pattern.ToLower();
                string pNoAccent = RemoveVietnameseAccents(p);

                if (text.Contains(p) || textNoAccent.Contains(pNoAccent))
                {
                    scores[intent] += 2;
                }
            }
        }

        private bool IsTopExperiencedRequest(string text, string textNoAccent)
        {
            string[] patterns =
            {
                "giỏi", "nhiều kinh nghiệm", "tốt nhất", "nổi bật",
                "gioi", "nhieu kinh nghiem", "tot nhat", "noi bat"
            };

            return patterns.Any(p => text.Contains(p) || textNoAccent.Contains(p));
        }

        private bool IsLatestRequest(string text, string textNoAccent)
        {
            string[] patterns =
            {
                "mới", "gần đây", "mới nhất",
                "moi", "gan day", "moi nhat"
            };

            return patterns.Any(p => text.Contains(p) || textNoAccent.Contains(p));
        }

       private string? DetectSpecialty(string text, string textNoAccent, ChatbotConversationContext? context = null)
        {
            if (text.Contains("lo âu") || text.Contains("lo lắng") || text.Contains("hồi hộp") || text.Contains("bất an")
                || textNoAccent.Contains("lo au") || textNoAccent.Contains("lo lang") || textNoAccent.Contains("hoi hop") || textNoAccent.Contains("bat an"))
                return "lo âu";

            if (text.Contains("stress") || text.Contains("căng thẳng") || text.Contains("áp lực") || text.Contains("bí bách")
                || textNoAccent.Contains("cang thang") || textNoAccent.Contains("ap luc") || textNoAccent.Contains("bi bach"))
                return "stress";

            if (text.Contains("trầm cảm") || text.Contains("chán nản") || text.Contains("trống rỗng") || text.Contains("mất động lực")
                || textNoAccent.Contains("tram cam") || textNoAccent.Contains("chan nan") || textNoAccent.Contains("trong rong") || textNoAccent.Contains("mat dong luc"))
                return "trầm cảm";

            if (text.Contains("mất ngủ") || text.Contains("khó ngủ") || text.Contains("ngủ không được") || text.Contains("thức đêm")
                || textNoAccent.Contains("mat ngu") || textNoAccent.Contains("kho ngu") || textNoAccent.Contains("ngu khong duoc") || textNoAccent.Contains("thuc dem"))
                return "mất ngủ";

            if (text.Contains("học đường") || textNoAccent.Contains("hoc duong")) return "học đường";
            if (text.Contains("gia đình") || textNoAccent.Contains("gia dinh")) return "gia đình";
            if (text.Contains("hôn nhân") || textNoAccent.Contains("hon nhan")) return "hôn nhân";
            if (text.Contains("trẻ em") || textNoAccent.Contains("tre em")) return "trẻ em";

            if (context != null && !string.IsNullOrWhiteSpace(context.LastSpecialty))
                return context.LastSpecialty;

            return null;
        }

        private string? DetectArticleKeyword(string text, string textNoAccent, ChatbotConversationContext? context = null)
        {
            string? keyword = DetectSpecialty(text, textNoAccent, context);
            if (!string.IsNullOrWhiteSpace(keyword)) return keyword;

            if (text.Contains("tâm lý") || textNoAccent.Contains("tam ly")) return "tâm lý";
            if (text.Contains("cảm xúc") || textNoAccent.Contains("cam xuc")) return "cảm xúc";
            if (text.Contains("sức khỏe tinh thần") || textNoAccent.Contains("suc khoe tinh than")) return "tâm lý";

            if (context != null && !string.IsNullOrWhiteSpace(context.LastKeyword))
                return context.LastKeyword;

            return null;
        }

        private string BuildSymptomReply(string? specialty)
        {
            return specialty switch
            {
                "stress" => "Có vẻ bạn đang chịu nhiều áp lực hoặc căng thẳng. Mình có thể gợi ý bài viết phù hợp hoặc giúp bạn tìm chuyên gia để trao đổi thêm.",
                "lo âu" => "Có vẻ bạn đang có dấu hiệu lo âu hoặc bất an. Mình có thể gợi ý bài viết liên quan hoặc tìm chuyên gia phù hợp cho bạn.",
                "trầm cảm" => "Nếu bạn đang buồn chán, trống rỗng hoặc mất động lực trong thời gian dài, bạn nên tìm thêm sự hỗ trợ. Mình có thể giúp bạn tìm chuyên gia hoặc bài viết phù hợp.",
                "mất ngủ" => "Tình trạng khó ngủ hoặc mất ngủ kéo dài có thể ảnh hưởng khá nhiều đến tinh thần. Mình có thể gợi ý bài viết liên quan hoặc tìm chuyên gia phù hợp.",
                _ => "Mình hiểu bạn đang gặp vấn đề về tinh thần hoặc cảm xúc. Mình có thể gợi ý bài viết hoặc chuyên gia phù hợp để bạn tham khảo."
            };
        }

        private ChatbotResponseViewModel GetConsultantsReply(string? specialty, bool topExperienced, string conversationId, int? userId)
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
                    return BuildTextResponse(
                        specialty == null
                            ? "Hiện chưa có chuyên gia phù hợp để gợi ý."
                            : $"Hiện chưa tìm thấy chuyên gia phù hợp với chủ đề '{specialty}'."
                    );
                }

                string reply = specialty != null
                    ? $"Mình tìm được một số chuyên gia liên quan đến '{specialty}':"
                    : topExperienced
                        ? "Đây là một số chuyên gia có nhiều kinh nghiệm:"
                        : "Đây là một số chuyên gia nổi bật:";

                string itemsJson = JsonSerializer.Serialize(consultants);

                SaveConversationContext(
                    conversationId,
                    userId,
                    "consultant_search",
                    specialty ?? "consultants",
                    specialty ?? "",
                    specialty ?? "",
                    "consultants",
                    itemsJson
                );

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

                return BuildTextResponse("Hiện tại mình chưa lấy được danh sách chuyên gia. Bạn có thể vào trực tiếp mục Chuyên gia để xem thêm.");
            }
        }

        private ChatbotResponseViewModel GetArticlesReply(string? keyword, bool latestOnly, string conversationId, int? userId)
        {
            try
            {
                using SqlConnection conn = new SqlConnection(_connectionString);
                conn.Open();

                var articles = new List<ChatbotArticleItemViewModel>();

                string query;
                SqlCommand cmd;

                if (!string.IsNullOrEmpty(keyword))
                {
                    query = @"
                        SELECT TOP 5 Id, Title, CreatedDate
                        FROM News
                        WHERE Title LIKE @Keyword OR Content LIKE @Keyword
                        ORDER BY CreatedDate DESC";

                    cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Keyword", "%" + keyword + "%");
                }
                else
                {
                    query = @"
                        SELECT TOP 5 Id, Title, CreatedDate
                        FROM News
                        ORDER BY CreatedDate DESC";

                    cmd = new SqlCommand(query, conn);
                }

                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    articles.Add(new ChatbotArticleItemViewModel
                    {
                        Id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                        Title = reader.IsDBNull(1) ? "Tin tức" : reader.GetString(1),
                        CreatedAt = reader.IsDBNull(2) ? DateTime.Now : reader.GetDateTime(2),
                        SourceType = "News"
                    });
                }

                if (articles.Count == 0)
                {
                    return BuildTextResponse(
                        keyword == null
                            ? "Hiện chưa có bài viết hoặc tin tức nào."
                            : $"Hiện chưa có bài viết phù hợp với từ khóa '{keyword}'."
                    );
                }

                string reply = keyword != null
                    ? $"Đây là một số bài viết liên quan đến '{keyword}':"
                    : latestOnly
                        ? "Đây là một số bài viết hoặc tin tức mới:"
                        : "Đây là một số bài viết gần đây:";

                string itemsJson = JsonSerializer.Serialize(articles);

                SaveConversationContext(
                    conversationId,
                    userId,
                    "article_search",
                    keyword ?? "articles",
                    "",
                    keyword ?? "",
                    "articles",
                    itemsJson
                );

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
                return BuildTextResponse("Hiện tại mình chưa lấy được danh sách bài viết.");
            }
        }

        private ChatbotResponseViewModel GetMyAppointmentsReply(int userId, string conversationId)
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
                    SaveConversationContext(
                        conversationId,
                        userId,
                        "appointment_check",
                        "appointments",
                        "",
                        "",
                        "text",
                        null
                    );

                    return BuildTextResponse("Bạn hiện chưa có lịch hẹn nào.");
                }

                SaveConversationContext(
                    conversationId,
                    userId,
                    "appointment_check",
                    "appointments",
                    "",
                    "",
                    "appointments",
                    JsonSerializer.Serialize(appointments)
                );

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
                return BuildTextResponse("Mình chưa lấy được thông tin lịch hẹn của bạn lúc này.");
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

        private ChatbotConversationContext LoadConversationContext(string? conversationId, int? userId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                return new ChatbotConversationContext
                {
                    ConversationId = Guid.NewGuid().ToString(),
                    UserId = userId
                };
            }

            try
            {
                using SqlConnection conn = new SqlConnection(_connectionString);
                conn.Open();

                string query = @"
                    SELECT TOP 1 ConversationId, UserId, LastIntent, LastTopic, LastSpecialty,
                           LastKeyword, LastResponseType, LastItemsJson, UpdatedAt
                    FROM ChatbotConversationContexts
                    WHERE ConversationId = @ConversationId";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ConversationId", conversationId);

                using SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new ChatbotConversationContext
                    {
                        ConversationId = reader.IsDBNull(0) ? conversationId : reader.GetString(0),
                        UserId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                        LastIntent = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        LastTopic = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        LastSpecialty = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        LastKeyword = reader.IsDBNull(5) ? "" : reader.GetString(5),
                        LastResponseType = reader.IsDBNull(6) ? "" : reader.GetString(6),
                        LastItemsJson = reader.IsDBNull(7) ? "" : reader.GetString(7),
                        UpdatedAt = reader.IsDBNull(8) ? DateTime.Now : reader.GetDateTime(8)
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadConversationContext error: " + ex.Message);
            }

            return new ChatbotConversationContext
            {
                ConversationId = conversationId!,
                UserId = userId
            };
        }

        private void SaveConversationContext(
            string conversationId,
            int? userId,
            string lastIntent,
            string lastTopic,
            string lastSpecialty,
            string lastKeyword,
            string lastResponseType,
            string? lastItemsJson)
        {
            try
            {
                using SqlConnection conn = new SqlConnection(_connectionString);
                conn.Open();

                string query = @"
                    MERGE ChatbotConversationContexts AS target
                    USING (SELECT @ConversationId AS ConversationId) AS source
                    ON target.ConversationId = source.ConversationId
                    WHEN MATCHED THEN
                        UPDATE SET
                            UserId = @UserId,
                            LastIntent = @LastIntent,
                            LastTopic = @LastTopic,
                            LastSpecialty = @LastSpecialty,
                            LastKeyword = @LastKeyword,
                            LastResponseType = @LastResponseType,
                            LastItemsJson = @LastItemsJson,
                            UpdatedAt = GETDATE()
                    WHEN NOT MATCHED THEN
                        INSERT (ConversationId, UserId, LastIntent, LastTopic, LastSpecialty, LastKeyword, LastResponseType, LastItemsJson, UpdatedAt)
                        VALUES (@ConversationId, @UserId, @LastIntent, @LastTopic, @LastSpecialty, @LastKeyword, @LastResponseType, @LastItemsJson, GETDATE());";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ConversationId", conversationId);
                cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@LastIntent", lastIntent ?? "");
                cmd.Parameters.AddWithValue("@LastTopic", lastTopic ?? "");
                cmd.Parameters.AddWithValue("@LastSpecialty", lastSpecialty ?? "");
                cmd.Parameters.AddWithValue("@LastKeyword", lastKeyword ?? "");
                cmd.Parameters.AddWithValue("@LastResponseType", lastResponseType ?? "");
                cmd.Parameters.AddWithValue("@LastItemsJson", (object?)lastItemsJson ?? DBNull.Value);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine("SaveConversationContext error: " + ex.Message);
            }
        }

        private T? DeserializeItems<T>(string json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json)) return default;
                return JsonSerializer.Deserialize<T>(json);
            }
            catch
            {
                return default;
            }
        }

        private int? ExtractRequestedIndex(string text, string textNoAccent)
        {
            if (text.Contains("đầu tiên") || textNoAccent.Contains("dau tien"))
                return 0;

            if (text.Contains("thứ 2") || text.Contains("thứ hai") || textNoAccent.Contains("thu 2") || textNoAccent.Contains("thu hai"))
                return 1;

            if (text.Contains("thứ 3") || text.Contains("thứ ba") || textNoAccent.Contains("thu 3") || textNoAccent.Contains("thu ba"))
                return 2;

            var match = Regex.Match(textNoAccent, @"thu\s+(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int number) && number > 0)
                return number - 1;

            return null;
        }

        private bool IsBookingFollowUp(string text, string textNoAccent)
        {
            string[] patterns =
            {
                "đặt lịch", "đặt hẹn", "book lịch",
                "dat lich", "dat hen", "book lich"
            };

            return patterns.Any(x => text.Contains(x) || textNoAccent.Contains(x));
        }

        private bool IsMoreRequest(string text, string textNoAccent)
        {
            string[] patterns =
            {
                "còn ai khác", "còn bài khác", "xem thêm", "thêm nữa",
                "con ai khac", "con bai khac", "xem them", "them nua"
            };

            return patterns.Any(x => text.Contains(x) || textNoAccent.Contains(x));
        }

        private string NormalizeSemanticText(string text)
        {
            var normalized = " " + text.ToLower().Trim() + " ";

            var phraseMap = new Dictionary<string, string>
            {
                { " áp lực ", " stress " },
                { " căng não ", " stress " },
                { " mệt mỏi ", " kiệt sức " },
                { " bí bách ", " stress " },
                { " ngộp thở ", " lo âu " },
                { " hồi hộp ", " lo âu " },
                { " bất an ", " lo âu " },
                { " lo lắm ", " lo âu " },
                { " buồn chán ", " trầm cảm " },
                { " chán nản ", " trầm cảm " },
                { " trống rỗng ", " trầm cảm " },
                { " mất động lực ", " trầm cảm " },
                { " khó ngủ ", " mất ngủ " },
                { " ngủ không được ", " mất ngủ " },
                { " ngủ không ngon ", " mất ngủ " },
                { " thức đêm ", " mất ngủ " },
                { " muốn tìm người nói chuyện ", " tìm chuyên gia " },
                { " muốn được tư vấn ", " tìm chuyên gia " },
                { " ai tư vấn ổn ", " tìm chuyên gia " },
                { " ai hỗ trợ ", " tìm chuyên gia " },
                { " bài đọc ", " bài viết " },
                { " bài liên quan ", " bài viết " },
                { " tin liên quan ", " bài viết " }
            };

            foreach (var pair in phraseMap)
            {
                normalized = normalized.Replace(pair.Key, " " + pair.Value.Trim() + " ");
            }

            return Regex.Replace(normalized.Trim(), @"\s+", " ");
        }
    }
}