using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using MentalHealthSupport.Hubs;

public class ChatSessionViewModel
{
    public int ChatSessionId { get; set; }
    public int UserId { get; set; }
    public int ConsultantId { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public int UnreadCount { get; set; }
}
public class ChatController : Controller
{
    private readonly string? connectionString;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatController(IConfiguration config, IHubContext<ChatHub> hubContext)
    {
        connectionString = config.GetConnectionString("DefaultConnection");
        _hubContext = hubContext;
    }

    // Hiển thị danh sách chat sessions
    public async Task<IActionResult> Index()
    {
        var sessions = new List<ChatSessionViewModel>();
        var userId = HttpContext.Session.GetInt32("UserId") ?? 0;

        if (string.IsNullOrEmpty(connectionString))
        {
            ViewBag.ErrorMessage = "Lỗi: Chuỗi kết nối cơ sở dữ liệu không được cấu hình.";
            return View(sessions);
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                string query = @"
                    SELECT cs.ChatSessionId, cs.UserId, cs.ConsultantId, cs.StartTime, cs.EndTime, cs.Status, cs.CreatedAt, cs.IsActive,
                           CASE 
                               WHEN cs.UserId = @UserId THEN u2.FullName
                               ELSE u1.FullName
                           END AS OtherUserName,
                           (SELECT COUNT(*) 
                            FROM ChatMessages cm 
                            WHERE cm.ChatSessionId = cs.ChatSessionId 
                            AND cm.SenderId != @UserId 
                            AND cm.IsRead = 0) AS UnreadCount
                    FROM ChatSessions cs
                    INNER JOIN Users u1 ON cs.UserId = u1.UserId
                    INNER JOIN Users u2 ON cs.ConsultantId = u2.UserId
                    WHERE (cs.UserId = @UserId OR cs.ConsultantId = @UserId) AND cs.IsActive = 1";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            sessions.Add(new ChatSessionViewModel
                            {
                                ChatSessionId = reader.GetInt32(0),
                                UserId = reader.GetInt32(1),
                                ConsultantId = reader.GetInt32(2),
                                StartTime = reader.GetDateTime(3),
                                EndTime = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                                Status = reader.GetString(5),
                                CreatedAt = reader.GetDateTime(6),
                                IsActive = reader.GetBoolean(7),
                                OtherUserName = reader.GetString(8),
                                UnreadCount = reader.GetInt32(9)
                            });
                        }
                    }
                }
            }
        }
        catch (SqlException ex)
        {
            ViewBag.ErrorMessage = $"Lỗi cơ sở dữ liệu: {ex.Message}";
        }

        return View(sessions);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var messages = new List<ChatMessageViewModel>();
        var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        string otherUserName = "";

        if (string.IsNullOrEmpty(connectionString))
        {
            ViewBag.ErrorMessage = "Lỗi: Chuỗi kết nối cơ sở dữ liệu không được cấu hình.";
            return View(messages);
        }

        try
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                // Lấy danh sách tin nhắn
                string query = @"
                    SELECT cm.MessageId, cm.ChatSessionId, cm.SenderId, cm.Message, 
                        cm.Timestamp, cm.IsRead, cm.MessageType, u.FullName as SenderName
                    FROM ChatMessages cm
                    INNER JOIN Users u ON cm.SenderId = u.UserId
                    WHERE cm.ChatSessionId = @ChatSessionId
                    ORDER BY cm.Timestamp";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ChatSessionId", id);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            messages.Add(new ChatMessageViewModel
                            {
                                MessageId = reader.GetInt32(0),
                                ChatSessionId = reader.GetInt32(1),
                                SenderId = reader.GetInt32(2),
                                Message = reader.GetString(3),
                                Timestamp = DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc),
                                IsRead = reader.GetBoolean(5),
                                MessageType = reader.GetString(6),
                                SenderName = reader.GetString(7)
                            });
                        }
                    }
                }

                // Cập nhật trạng thái IsRead
                string updateQuery = @"UPDATE ChatMessages SET IsRead = 1 
                                      WHERE ChatSessionId = @ChatSessionId AND SenderId != @CurrentUserId";
                using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@ChatSessionId", id);
                    cmd.Parameters.AddWithValue("@CurrentUserId", userId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Truy vấn tên người dùng khác
                string nameQuery = @"
                    SELECT 
                        CASE 
                            WHEN cs.UserId = @CurrentUserId THEN u2.FullName
                            ELSE u1.FullName
                        END AS OtherUserName
                    FROM ChatSessions cs
                    INNER JOIN Users u1 ON cs.UserId = u1.UserId
                    INNER JOIN Users u2 ON cs.ConsultantId = u2.UserId
                    WHERE cs.ChatSessionId = @ChatSessionId";

                using (SqlCommand nameCmd = new SqlCommand(nameQuery, conn))
                {
                    nameCmd.Parameters.AddWithValue("@CurrentUserId", userId);
                    nameCmd.Parameters.AddWithValue("@ChatSessionId", id);

                    var result = await nameCmd.ExecuteScalarAsync();
                    if (result != null)
                    {
                        otherUserName = result.ToString() ?? "";
                    }
                }
            }

            // Gửi sự kiện SignalR để cập nhật badge
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ClearBadge", id);
        }
        catch (SqlException ex)
        {
            ViewBag.ErrorMessage = $"Lỗi cơ sở dữ liệu: {ex.Message}";
        }

        ViewBag.ChatSessionId = id;
        ViewBag.OtherUserName = otherUserName;
        return View("Detail", messages);
    }
    
    [HttpPost]
    public async Task<IActionResult> StartChat(int consultantId)
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            TempData["ErrorMessage"] = "Vui lòng đăng nhập để bắt đầu trò chuyện.";
            return RedirectToAction("Login", "Account"); // hoặc quay lại Index
        }

        if (userId == consultantId)
        {
            TempData["ErrorMessage"] = "Bạn không thể trò chuyện với chính mình.";
            return RedirectToAction("Index", "Consultants");
        }

        int chatSessionId;

        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();

            // ✅ Kiểm tra nếu đã có phiên trò chuyện đang hoạt động
            var checkQuery = @"SELECT TOP 1 ChatSessionId 
                            FROM ChatSessions 
                            WHERE ((UserId = @UserId AND ConsultantId = @ConsultantId) 
                                    OR (UserId = @ConsultantId AND ConsultantId = @UserId))
                                AND IsActive = 1";

            using (var checkCmd = new SqlCommand(checkQuery, connection))
            {
                checkCmd.Parameters.AddWithValue("@UserId", userId);
                checkCmd.Parameters.AddWithValue("@ConsultantId", consultantId);

                var result = await checkCmd.ExecuteScalarAsync();
                if (result != null)
                {
                    chatSessionId = Convert.ToInt32(result);
                    return RedirectToAction("Detail", "Chat", new { id = chatSessionId });
                }
            }

            // ✅ Nếu chưa có, tạo phiên chat mới
            var insertQuery = @"INSERT INTO ChatSessions (UserId, ConsultantId, StartTime, Status, CreatedAt, IsActive)
                                OUTPUT INSERTED.ChatSessionId
                                VALUES (@UserId, @ConsultantId, @StartTime, @Status, @CreatedAt, 1)";

            using (var insertCmd = new SqlCommand(insertQuery, connection))
            {
                insertCmd.Parameters.AddWithValue("@UserId", userId);
                insertCmd.Parameters.AddWithValue("@ConsultantId", consultantId);
                insertCmd.Parameters.AddWithValue("@StartTime", DateTime.UtcNow);
                insertCmd.Parameters.AddWithValue("@Status", "Active");
                insertCmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

                chatSessionId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());
            }
        }

        return RedirectToAction("Detail", "Chat", new { id = chatSessionId });
    }

}