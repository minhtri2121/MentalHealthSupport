using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;

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

        public async Task SendMessage(string userId, string message, int chatSessionId)
        {
            try
            {
                //Lưu tin nhắn vào DB
                int messageId;
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    string query = @"INSERT INTO ChatMessages (ChatSessionId, SenderId, Message, Timestamp, IsRead, MessageType)
                                    OUTPUT INSERTED.MessageId
                                    VALUES (@ChatSessionId, @SenderId, @Message, @Timestamp, @IsRead, @MessageType)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ChatSessionId", chatSessionId);
                        cmd.Parameters.AddWithValue("@SenderId", int.Parse(userId));
                        cmd.Parameters.AddWithValue("@Message", message);
                        cmd.Parameters.AddWithValue("@Timestamp", DateTime.UtcNow);
                        cmd.Parameters.AddWithValue("@IsRead", false);
                        cmd.Parameters.AddWithValue("@MessageType", "Text");

                        var result = await cmd.ExecuteScalarAsync();
                        messageId = Convert.ToInt32(result);
                    }
                }

                //Gửi tin nhắn đến group hiện tại (client đang mở chat)
                await Clients.Group($"session_{chatSessionId}")
                    .SendAsync("ReceiveMessage", userId, message, DateTime.UtcNow.ToString("HH:mm"));

                //Gửi thông báo riêng cho người nhận (tin nhắn popup/toast)
                int senderId = int.Parse(userId);
                int receiverUserId = 0;

                using (var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    var query = @"SELECT 
                                    CASE 
                                        WHEN UserId = @SenderId THEN ConsultantId 
                                        ELSE UserId 
                                    END AS ReceiverId
                                FROM ChatSessions
                                WHERE ChatSessionId = @ChatSessionId";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@SenderId", senderId);
                        cmd.Parameters.AddWithValue("@ChatSessionId", chatSessionId);

                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null)
                        {
                            receiverUserId = Convert.ToInt32(result);

                            await Clients.User(receiverUserId.ToString())
                                .SendAsync("NotifyNewMessage", userId, message, chatSessionId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ReceiveError", "Error sending message: " + ex.Message);
            }
        }
        public async Task JoinSession(int chatSessionId)
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"session_{chatSessionId}");

                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    string query = @"INSERT INTO UserConnections (ConnectionId, UserId, ConnectedAt, IsActive)
                                    VALUES (@ConnectionId, @UserId, @ConnectedAt, @IsActive)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ConnectionId", Context.ConnectionId);

                        var httpContext = Context.GetHttpContext();
                        var userId = httpContext?.Session.GetInt32("UserId");

                        if (userId == null)
                        {
                            await Clients.Caller.SendAsync("ReceiveError", "Không xác định được UserId từ session.");
                            return;
                        }
                        cmd.Parameters.AddWithValue("@UserId", userId.Value);
                        cmd.Parameters.AddWithValue("@ConnectedAt", DateTime.UtcNow);
                        cmd.Parameters.AddWithValue("@IsActive", true);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ReceiveError", $"JoinSession error: {ex.Message}");
                Console.WriteLine("JoinSession Error: " + ex);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await conn.OpenAsync();
                string query = @"UPDATE UserConnections SET IsActive = 0 WHERE ConnectionId = @ConnectionId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ConnectionId", Context.ConnectionId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
        
        public override Task OnConnectedAsync()
        {
            var userId = Context.GetHttpContext()?.Session.GetInt32("UserId");
            if (userId != null)
            {
                Context.Items["UserId"] = userId;
            }

            return base.OnConnectedAsync();
        }
    }
}