using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace MentalHealthSupport.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IConfiguration _configuration;
        private static readonly ConcurrentDictionary<int, string> OnlineUsers = new ConcurrentDictionary<int, string>();

        public ChatHub(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserIdFromContext();
            if (userId.HasValue)
            {
                OnlineUsers.AddOrUpdate(userId.Value, Context.ConnectionId, (key, oldValue) => Context.ConnectionId);
                await SaveUserConnection(userId.Value, Context.ConnectionId);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserIdFromContext();
            if (userId.HasValue)
            {
                OnlineUsers.TryRemove(userId.Value, out _);
                await RemoveUserConnection(Context.ConnectionId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        private int? GetUserIdFromContext()
        {
            var httpContext = Context.GetHttpContext();
            if (httpContext == null)
            {
                Console.WriteLine("HttpContext is null in GetUserIdFromContext.");
                return null;
            }

            var userId = httpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                Console.WriteLine("UserId not found in session.");
            }
            else
            {
                Console.WriteLine($"UserId from session: {userId.Value}");
            }
            return userId;
        }

        public async Task StartChat(int userId, int consultantId)
        {
            Console.WriteLine($"Starting chat between userId: {userId} and consultantId: {consultantId}");
            try
            {
                var chatSessionId = await CreateChatSession(userId, consultantId);
                await Groups.AddToGroupAsync(Context.ConnectionId, $"ChatSession_{chatSessionId}");
                await Clients.Caller.SendAsync("ChatStarted", chatSessionId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"StartChat Error: {ex.Message}");
                await Clients.Caller.SendAsync("Error", $"Không thể bắt đầu chat: {ex.Message}");
            }
        }

        public async Task SendMessage(int chatSessionId, int senderId, string message)
        {
            try
            {
                // Kiểm tra trạng thái phiên chat
                string status = await GetChatSessionStatus(chatSessionId);
                if (status != "Active")
                {
                    throw new Exception("Phiên chat không còn hoạt động.");
                }

                await SaveMessage(chatSessionId, senderId, message);
                var chatInfo = await GetChatSessionInfo(chatSessionId);
                if (chatInfo.HasValue)
                {
                    var (userId, consultantId) = chatInfo.Value;
                    await Clients.Group($"ChatSession_{chatSessionId}")
                        .SendAsync("ReceiveMessage", chatSessionId, senderId, message, DateTime.Now);
                }
                else
                {
                    throw new Exception("Không tìm thấy thông tin phiên chat.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SendMessage Error: {ex.Message}");
                await Clients.Caller.SendAsync("Error", $"Lỗi gửi tin nhắn: {ex.Message}");
            }
        }

        public async Task EndChat(int chatSessionId)
        {
            try
            {
                await CloseChatSession(chatSessionId);
                await Clients.Group($"ChatSession_{chatSessionId}")
                    .SendAsync("ChatEnded");
                var chatInfo = await GetChatSessionInfo(chatSessionId);
                if (chatInfo.HasValue)
                {
                    var (userId, consultantId) = chatInfo.Value;
                    var userConnectionId = OnlineUsers.GetValueOrDefault(userId);
                    var consultantConnectionId = OnlineUsers.GetValueOrDefault(consultantId);
                    if (userConnectionId != null)
                    {
                        await Groups.RemoveFromGroupAsync(userConnectionId, $"ChatSession_{chatSessionId}");
                    }
                    if (consultantConnectionId != null)
                    {
                        await Groups.RemoveFromGroupAsync(consultantConnectionId, $"ChatSession_{chatSessionId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EndChat Error: {ex.Message}");
                await Clients.Caller.SendAsync("Error", $"Lỗi kết thúc chat: {ex.Message}");
            }
        }

        private async Task<int> CreateChatSession(int userId, int consultantId)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                var query = "INSERT INTO ChatSessions (UserId, ConsultantId, StartTime, Status) OUTPUT INSERTED.ChatSessionId VALUES (@UserId, @ConsultantId, @StartTime, @Status)";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@ConsultantId", consultantId);
                    command.Parameters.AddWithValue("@StartTime", DateTime.Now);
                    command.Parameters.AddWithValue("@Status", "Active");
                    var result = await command.ExecuteScalarAsync();
                    if (result == null)
                    {
                        throw new Exception("Không thể tạo phiên chat.");
                    }
                    return (int)result; // Sửa lỗi CS8605 bằng cách kiểm tra null trước khi ép kiểu
                }
            }
        }

        private async Task<(int UserId, int ConsultantId)?> GetChatSessionInfo(int chatSessionId)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                var query = "SELECT UserId, ConsultantId FROM ChatSessions WHERE ChatSessionId = @ChatSessionId";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ChatSessionId", chatSessionId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return (reader.GetInt32(0), reader.GetInt32(1));
                        }
                    }
                }
                return null;
            }
        }

        private async Task<string> GetChatSessionStatus(int chatSessionId)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                var query = "SELECT Status FROM ChatSessions WHERE ChatSessionId = @ChatSessionId";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ChatSessionId", chatSessionId);
                    var result = await command.ExecuteScalarAsync();
                    return result?.ToString() ?? "Closed";
                }
            }
        }

        private async Task SaveMessage(int chatSessionId, int senderId, string message)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                var query = "INSERT INTO ChatMessages (ChatSessionId, SenderId, Message, Timestamp, IsRead, MessageType) VALUES (@ChatSessionId, @SenderId, @Message, @Timestamp, @IsRead, @MessageType)";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ChatSessionId", chatSessionId);
                    command.Parameters.AddWithValue("@SenderId", senderId);
                    command.Parameters.AddWithValue("@Message", message);
                    command.Parameters.AddWithValue("@Timestamp", DateTime.Now);
                    command.Parameters.AddWithValue("@IsRead", false);
                    command.Parameters.AddWithValue("@MessageType", "Text");
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task CloseChatSession(int chatSessionId)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                var query = "UPDATE ChatSessions SET EndTime = @EndTime, Status = @Status WHERE ChatSessionId = @ChatSessionId";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@EndTime", DateTime.Now);
                    command.Parameters.AddWithValue("@Status", "Closed");
                    command.Parameters.AddWithValue("@ChatSessionId", chatSessionId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task SaveUserConnection(int userId, string connectionId)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                var query = "INSERT INTO UserConnections (ConnectionId, UserId, ConnectedAt, IsActive) VALUES (@ConnectionId, @UserId, @ConnectedAt, @IsActive)";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ConnectionId", connectionId);
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@ConnectedAt", DateTime.Now);
                    command.Parameters.AddWithValue("@IsActive", true);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task RemoveUserConnection(string connectionId)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                var query = "UPDATE UserConnections SET IsActive = @IsActive WHERE ConnectionId = @ConnectionId";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IsActive", false);
                    command.Parameters.AddWithValue("@ConnectionId", connectionId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}