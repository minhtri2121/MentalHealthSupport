using Microsoft.AspNetCore.Mvc;
using MentalHealthSupport.Models.ViewModel;
using Microsoft.Data.SqlClient;
using BCrypt.Net;
using System.Data;
using System.Text.Json.Serialization;

namespace MentalHealthSupport.Controllers
{
    [Route("Account")] // Định nghĩa prefix route cho toàn bộ controller
    public class AccountController : Controller
    {
        private readonly string? connectionString;

        public AccountController(IConfiguration config)
        {
            connectionString = config.GetConnectionString("DefaultConnection");
        }

        [HttpGet("Login")] // Route cụ thể cho Login
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost("Login")]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        string query = "SELECT UserId, PasswordHash, FullName, Role FROM Users WHERE Email = @Email";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Email", model.Email);
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (!reader.Read())
                                {
                                    ViewData["ErrorMessage"] = "Email không tồn tại.";
                                    return View(model);
                                }

                                int userId = reader.GetInt32(0);
                                string storedHash = reader.GetString(1);
                                string fullName = reader.GetString(2);
                                string role = reader.GetString(3);

                                bool isValidPassword = BCrypt.Net.BCrypt.Verify(model.PasswordHash, storedHash);
                                if (!isValidPassword)
                                {
                                    ViewData["ErrorMessage"] = "Mật khẩu không đúng.";
                                    return View(model);
                                }

                                HttpContext.Session.SetInt32("UserId", userId);
                                HttpContext.Session.SetString("UserEmail", model.Email);
                                HttpContext.Session.SetString("FullName", fullName);
                                HttpContext.Session.SetString("UserRole", role);
                                Console.WriteLine($"Login successful for UserId: {userId}, Email: {model.Email}");
                                return RedirectToAction("Index", "Home");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewData["ErrorMessage"] = "Lỗi khi đăng nhập: " + ex.Message;
                    return View(model);
                }
            }

            ViewData["ErrorMessage"] = "Vui lòng nhập đầy đủ thông tin hợp lệ.";
            return View(model);
        }

        [HttpGet("Register")] // Route cụ thể cho Register
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost("Register")] // Route cụ thể cho Register POST
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        var checkEmailQuery = "SELECT COUNT(*) FROM Users WHERE Email = @Email";
                        using (var checkCommand = new SqlCommand(checkEmailQuery, connection))
                        {
                            checkCommand.Parameters.AddWithValue("@Email", model.Email);
                            int emailCount = (int)checkCommand.ExecuteScalar();
                            if (emailCount > 0)
                            {
                                ViewData["ErrorMessage"] = "Email đã tồn tại. Vui lòng sử dụng email khác.";
                                return View(model);
                            }
                        }

                        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.PasswordHash);
                        var query = @"
                            INSERT INTO Users (FullName, Email, PasswordHash, Role, Phone, Sex)
                            VALUES (@FullName, @Email, @PasswordHash, @Role, @Phone, @Sex)";

                        using (var command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@FullName", model.FullName);
                            command.Parameters.AddWithValue("@Email", model.Email);
                            command.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                            command.Parameters.AddWithValue("@Role", "User");
                            command.Parameters.AddWithValue("@Phone", (object?)model.Phone ?? DBNull.Value);
                            command.Parameters.AddWithValue("@Sex", model.Sex);
                            command.ExecuteNonQuery();
                        }
                    }

                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    ViewData["ErrorMessage"] = "Đã có lỗi xảy ra khi đăng ký: " + ex.Message;
                    return View(model);
                }
            }

            ViewData["ErrorMessage"] = ModelState.Values
                .SelectMany(v => v.Errors)
                .FirstOrDefault()?.ErrorMessage;
            return View(model);
        }

        [HttpGet("Logout")] // Route cụ thể cho Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet("Manage")] // Route cụ thể cho Manage
        public IActionResult Manage()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            ManageViewModel model = new ManageViewModel();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string userQuery = @"SELECT UserId, FullName, Email, Phone, Role, IsVerified, CreatedAt 
                                    FROM Users WHERE UserId = @UserId";
                using (SqlCommand cmd = new SqlCommand(userQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.UserId = reader.GetInt32(0);
                            model.FullName = reader.GetString(1);
                            model.Email = reader.GetString(2);
                            model.Phone = reader.IsDBNull(3) ? "" : reader.GetString(3);
                            model.Role = reader.GetString(4);
                            model.IsVerified = reader.GetBoolean(5);
                            model.CreatedAt = reader.GetDateTime(6);
                        }
                    }
                }

                if (model.Role == "Consultant")
                {
                    string consultantQuery = @"SELECT ConsultantId, Specialty, CertificateUrl, ApprovalStatus, ExperienceYears 
                                            FROM ConsultantProfiles WHERE ConsultantId = @UserId";
                    using (SqlCommand cmd = new SqlCommand(consultantQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                model.ConsultantId = reader.GetInt32(0);
                                model.Specialty = reader.GetString(1);
                                model.CertificateUrl = reader.GetString(2);
                                model.ApprovalStatus = reader.GetString(3);
                                model.ExperienceYears = reader.GetInt32(4);
                            }
                        }
                    }
                }
            }

            return View("Manage", model);
        }

        [HttpPost("UpdateAccount")] // Route cụ thể cho UpdateAccount
        public IActionResult UpdateAccount(ManageViewModel model)
        {
            if (!ModelState.IsValid)
                return View("ManageAccount", model);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string updateUser = @"UPDATE Users 
                                    SET FullName = @FullName, Phone = @Phone 
                                    WHERE UserId = @UserId";
                using (SqlCommand cmd = new SqlCommand(updateUser, connection))
                {
                    cmd.Parameters.AddWithValue("@FullName", model.FullName);
                    cmd.Parameters.AddWithValue("@Phone", (object?)model.Phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UserId", model.UserId);
                    cmd.ExecuteNonQuery();
                }

                if (model.Role == "Consultant")
                {
                    string updateConsultant = @"UPDATE ConsultantProfiles 
                                                SET Specialty = @Specialty, CertificateUrl = @CertificateUrl, ExperienceYears = @ExperienceYears 
                                                WHERE ConsultantId = @ConsultantId";
                    using (SqlCommand cmd = new SqlCommand(updateConsultant, connection))
                    {
                        cmd.Parameters.AddWithValue("@Specialty", model.Specialty);
                        cmd.Parameters.AddWithValue("@CertificateUrl", model.CertificateUrl);
                        cmd.Parameters.AddWithValue("@ExperienceYears", model.ExperienceYears);
                        cmd.Parameters.AddWithValue("@ConsultantId", model.ConsultantId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            TempData["SuccessMessage"] = "Cập nhật thông tin thành công.";
            return RedirectToAction("ManageAccount");
        }

        [HttpPost("AssignConsultant")]
        public IActionResult AssignConsultant([FromBody] AssignConsultantRequest request)
        {
            Console.WriteLine($"AssignConsultant called with request: {System.Text.Json.JsonSerializer.Serialize(request)}");
            try
            {
                if (request == null || request.UserId <= 0)
                {
                    Console.WriteLine("Validation failed: Invalid userId");
                    return BadRequest(new { error = "Invalid userId provided." });
                }

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT TOP 1 ConsultantId FROM ConsultantProfiles WHERE ApprovalStatus = @ApprovalStatus ORDER BY NEWID()";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ApprovalStatus", "Approved");
                        object result = command.ExecuteScalar();
                        if (result == null)
                        {
                            string countQuery = "SELECT COUNT(*) FROM ConsultantProfiles WHERE ApprovalStatus = @ApprovalStatus";
                            using (SqlCommand countCommand = new SqlCommand(countQuery, connection))
                            {
                                countCommand.Parameters.AddWithValue("@ApprovalStatus", "Approved");
                                int totalConsultants = (int)countCommand.ExecuteScalar();
                                Console.WriteLine($"No approved consultants found. Total approved consultants: {totalConsultants}");
                            }
                            return BadRequest(new { error = "No consultants available." });
                        }

                        int consultantId = Convert.ToInt32(result);
                        Console.WriteLine($"Assigned consultantId: {consultantId} from available consultants: {GetAvailableConsultants(connection)}");
                        return Ok(new { ConsultantId = consultantId });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AssignConsultant: {ex.Message}");
                return StatusCode(500, new { error = $"Error: {ex.Message}" });
            }
        }

        private string GetAvailableConsultants(SqlConnection connection)
        {
            string query = "SELECT ConsultantId FROM ConsultantProfiles WHERE ApprovalStatus = @ApprovalStatus";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@ApprovalStatus", "Approved");
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    var ids = new List<int>();
                    while (reader.Read())
                    {
                        ids.Add(reader.GetInt32(0));
                    }
                    return string.Join(", ", ids);
                }
            }
        }
    }

    public class AssignConsultantRequest
    {
        [JsonPropertyName("userId")]
        public int UserId { get; set; }
    }
}