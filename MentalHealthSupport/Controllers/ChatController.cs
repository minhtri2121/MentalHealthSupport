using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;


[Route("api/[controller]")]
[ApiController]
public class ChatController : ControllerBase
{
    private readonly string? connectionString;

    public ChatController(IConfiguration config)
    {
        connectionString = config.GetConnectionString("DefaultConnection");
    }

    [HttpGet("GetOnlineConsultants")]
    public async Task<IActionResult> GetOnlineConsultants()
    {
        var consultants = new List<object>();
        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var query = @"
                SELECT u.UserId, u.FullName, cp.Specialty
                FROM Users u
                LEFT JOIN ConsultantProfiles cp ON u.UserId = cp.ConsultantId
                JOIN UserConnections uc ON u.UserId = uc.UserId
                WHERE u.Role = 'Consultant' AND uc.IsActive = 1";
            using (var command = new SqlCommand(query, connection))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        consultants.Add(new
                        {
                            userId = reader.GetInt32(0),
                            fullName = reader.GetString(1),
                            specialty = reader.IsDBNull(2) ? null : reader.GetString(2)
                        });
                    }
                }
            }
        }
        return Ok(consultants);
    }

    [HttpPost("StartChat")]
    public async Task<IActionResult> StartChat([FromBody] StartChatRequest request)
    {
        int chatSessionId;
        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var query = @"
                INSERT INTO ChatSessions (UserId, ConsultantId, StartTime, Status, CreatedAt)
                VALUES (@UserId, @ConsultantId, @StartTime, @Status, @CreatedAt);
                SELECT SCOPE_IDENTITY();";
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@UserId", request.UserId);
                command.Parameters.AddWithValue("@ConsultantId", request.ConsultantId);
                command.Parameters.AddWithValue("@StartTime", DateTime.Now);
                command.Parameters.AddWithValue("@Status", "Active");
                command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                chatSessionId = Convert.ToInt32(await command.ExecuteScalarAsync());
            }
        }
        return Ok(new { chatSessionId });
    }

    [HttpGet("GetMessages/{chatSessionId}")]
    public async Task<IActionResult> GetMessages(int chatSessionId)
    {
        var messages = new List<object>();
        using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var query = @"
                SELECT MessageId, SenderId, Message, Timestamp
                FROM ChatMessages
                WHERE ChatSessionId = @ChatSessionId
                ORDER BY Timestamp ASC";
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@ChatSessionId", chatSessionId);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        messages.Add(new
                        {
                            messageId = reader.GetInt32(0),
                            senderId = reader.GetInt32(1),
                            message = reader.GetString(2),
                            timestamp = reader.GetDateTime(3).ToString("o")
                        });
                    }
                }
            }
        }
        return Ok(messages);
    }
}

public class StartChatRequest
{
    public int UserId { get; set; }
    public int ConsultantId { get; set; }
}