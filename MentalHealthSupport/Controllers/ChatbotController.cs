using Microsoft.AspNetCore.Mvc;
using MentalHealthSupport.Models.ViewModel;
using MentalHealthSupport.Services;
using Microsoft.Data.SqlClient;

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
            ChatbotResponseViewModel response = _chatbotService.GetResponse(request.Message, userId);

            _chatbotService.SaveChatHistory(userId, request.Message, response.Reply);

            return Json(response);
        }

        [HttpGet]
        public IActionResult History()
        {
            string? role = HttpContext.Session.GetString("UserRole");
            if (role != "Admin")
                return RedirectToAction("Index", "Home");

            var list = new List<ChatbotHistoryViewModel>();

            try
            {
                string? connectionString = _configuration.GetConnectionString("DefaultConnection");

                using SqlConnection conn = new SqlConnection(connectionString);
                conn.Open();

                string query = @"
                    SELECT TOP 100 cm.Id, cm.UserId, ISNULL(u.FullName, N'Khách'), cm.UserMessage, cm.BotReply, cm.CreatedAt
                    FROM ChatbotMessages cm
                    LEFT JOIN Users u ON cm.UserId = u.UserId
                    ORDER BY cm.CreatedAt DESC";

                using SqlCommand cmd = new SqlCommand(query, conn);
                using SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new ChatbotHistoryViewModel
                    {
                        Id = reader.GetInt32(0),
                        UserId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                        UserName = reader.GetString(2),
                        UserMessage = reader.GetString(3),
                        BotReply = reader.GetString(4),
                        CreatedAt = reader.GetDateTime(5)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Chatbot History error: " + ex.Message);
            }

            return View(list);
        }
    }

    public class ChatbotRequest
    {
        public string Message { get; set; } = "";
    }
}