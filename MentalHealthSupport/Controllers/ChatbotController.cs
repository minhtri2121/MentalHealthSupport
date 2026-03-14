using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MentalHealthSupport.Models.ViewModel;
using MentalHealthSupport.Services;

namespace MentalHealthSupport.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly ChatbotService _chatbotService;
        private readonly IConfiguration _configuration;

        public ChatbotController(ChatbotService chatbotService, IConfiguration configuration)
        {
            _chatbotService = chatbotService;
            _configuration = configuration;
        }

        [HttpPost]
        public IActionResult Ask([FromBody] ChatbotRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return Json(new ChatbotResponseViewModel
                {
                    Reply = "Bạn hãy nhập câu hỏi để mình hỗ trợ nhé.",
                    Type = "text"
                });
            }

            int? userId = HttpContext.Session.GetInt32("UserId");

            string? conversationId = HttpContext.Session.GetString("ChatbotConversationId");
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                conversationId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("ChatbotConversationId", conversationId);
            }

            var response = _chatbotService.GetResponse(request.Message, userId, conversationId);
            _chatbotService.SaveChatHistory(userId, conversationId, request.Message, response.Reply);

            return Json(response);
        }

        [HttpGet]
        public IActionResult GetConversation()
        {
            string? conversationId = HttpContext.Session.GetString("ChatbotConversationId");

            if (string.IsNullOrWhiteSpace(conversationId))
            {
                return Json(new List<object>());
            }

            var history = new List<object>();

            try
            {
                using SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                string query = @"
                    SELECT UserMessage, BotReply, CreatedAt
                    FROM ChatbotMessages
                    WHERE ConversationId = @ConversationId
                    ORDER BY CreatedAt ASC";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ConversationId", conversationId);

                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string userMessage = reader.IsDBNull(0) ? "" : reader.GetString(0);
                    string botReply = reader.IsDBNull(1) ? "" : reader.GetString(1);

                    if (!string.IsNullOrWhiteSpace(userMessage))
                    {
                        history.Add(new
                        {
                            role = "user",
                            type = "text",
                            text = userMessage
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(botReply))
                    {
                        history.Add(new
                        {
                            role = "bot",
                            type = "text",
                            reply = botReply
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Chatbot GetConversation error: " + ex.Message);
            }

            return Json(history);
        }

        [HttpPost]
        public IActionResult ResetConversation()
        {
            HttpContext.Session.Remove("ChatbotConversationId");
            return Json(new { success = true });
        }
    }

    public class ChatbotRequest
    {
        public string Message { get; set; } = "";
    }
}