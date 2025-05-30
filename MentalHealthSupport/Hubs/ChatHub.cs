using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;

namespace MentalHealthSupport.Hubs
{
    public class ChatHub : Hub
    {
        private readonly string? connectionString;

        public ChatHub(IConfiguration config)
        {
            connectionString = config.GetConnectionString("DefaultConnection");
        }

        public async Task SendMessage(int chatSessionId, int senderId, string message)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "INSERT INTO ChatMessages (ChatSessionId, SenderId, Message, Timestamp) VALUES (@ChatSessionId, @SenderId, @Message, @Timestamp)";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ChatSessionId", chatSessionId);
                    command.Parameters.AddWithValue("@SenderId", senderId);
                    command.Parameters.AddWithValue("@Message", message);
                    command.Parameters.AddWithValue("@Timestamp", DateTime.Now);
                    await command.ExecuteNonQueryAsync();
                }
            }

            await Clients.Group(chatSessionId.ToString()).SendAsync("ReceiveMessage", senderId, message);
        }

        public async Task StartChat(int userId, int consultantId)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "INSERT INTO ChatSessions (UserId, ConsultantId, StartTime, Status) VALUES (@UserId, @ConsultantId, @StartTime, @Status); SELECT SCOPE_IDENTITY();";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@ConsultantId", consultantId);
                    command.Parameters.AddWithValue("@StartTime", DateTime.Now);
                    command.Parameters.AddWithValue("@Status", "Active");
                    int chatSessionId = Convert.ToInt32(await command.ExecuteScalarAsync());

                    await Groups.AddToGroupAsync(Context.ConnectionId, chatSessionId.ToString());
                    await Clients.Group(chatSessionId.ToString()).SendAsync("ChatStarted", chatSessionId);
                }
            }
        }

        public async Task EndChat(int chatSessionId)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "UPDATE ChatSessions SET Status = @Status WHERE ChatSessionId = @ChatSessionId";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Status", "Closed");
                    command.Parameters.AddWithValue("@ChatSessionId", chatSessionId);
                    await command.ExecuteNonQueryAsync();
                }

                await Clients.Group(chatSessionId.ToString()).SendAsync("ChatEnded");
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatSessionId.ToString());
            }
        }
    }
}