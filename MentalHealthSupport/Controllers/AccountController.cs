using Microsoft.AspNetCore.Mvc;
using MentalHealthSupport.Models.ViewModel;
using Microsoft.Data.SqlClient;
using BCrypt.Net;
using System.Data;
using System.Text.Json.Serialization;
using System;
using System.IO;

namespace MentalHealthSupport.Controllers
{
    [Route("Account")]

    public class AccountController : Controller
    {
        private readonly string? connectionString;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public AccountController(IConfiguration config, IWebHostEnvironment hostingEnvironment)
        {
            connectionString = config.GetConnectionString("DefaultConnection");
            _hostingEnvironment = hostingEnvironment;
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
                            INSERT INTO Users (FullName, Email, PasswordHash, Role, Phone, Sex, SecurityQuestion, SecurityAnswer)
                            VALUES (@FullName, @Email, @PasswordHash, @Role, @Phone, @Sex, @SecurityQuestion, @SecurityAnswer)";

                        using (var command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@FullName", model.FullName);
                            command.Parameters.AddWithValue("@Email", model.Email);
                            command.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                            command.Parameters.AddWithValue("@Role", "User");
                            command.Parameters.AddWithValue("@Phone", (object?)model.Phone ?? DBNull.Value);
                            command.Parameters.AddWithValue("@Sex", model.Sex);
                            command.Parameters.AddWithValue("@SecurityQuestion", model.SecurityQuestion);
                            command.Parameters.AddWithValue("@SecurityAnswer", model.SecurityAnswer);
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

        [HttpGet("ForgotPassword")]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost("ForgotPassword")]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        string query = "SELECT SecurityQuestion FROM Users WHERE Email = @Email";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Email", model.Email);
                            var securityQuestion = command.ExecuteScalar() as string;
                            if (securityQuestion != null)
                            {
                                ViewData["SecurityQuestion"] = securityQuestion;
                                return View("VerifySecurityAnswer", new VerifySecurityAnswerViewModel { Email = model.Email });
                            }
                            else
                            {
                                ViewData["ErrorMessage"] = "Email không tồn tại hoặc chưa thiết lập câu hỏi bảo mật.";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewData["ErrorMessage"] = $"Lỗi: {ex.Message}";
                }
            }
            return View(model);
        }

        [HttpPost("VerifySecurityAnswer")]
        [ValidateAntiForgeryToken]
        public IActionResult VerifySecurityAnswer(VerifySecurityAnswerViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        string query = "SELECT SecurityAnswer FROM Users WHERE Email = @Email";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Email", model.Email);
                            var dbAnswer = command.ExecuteScalar() as string;
                            if (dbAnswer != null && dbAnswer.ToLower() == model.Answer.ToLower()) // So sánh không phân biệt hoa thường
                            {
                                var token = Guid.NewGuid().ToString();
                                string updateQuery = "UPDATE Users SET ResetToken = @ResetToken, ResetTokenExpiry = @ResetTokenExpiry WHERE Email = @Email";
                                using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                                {
                                    updateCommand.Parameters.AddWithValue("@ResetToken", token);
                                    updateCommand.Parameters.AddWithValue("@ResetTokenExpiry", DateTime.Now.AddHours(1));
                                    updateCommand.Parameters.AddWithValue("@Email", model.Email);
                                    updateCommand.ExecuteNonQuery();
                                }
                                return RedirectToAction("ResetPassword", new { email = model.Email, token = token });
                            }
                            else
                            {
                                ViewData["ErrorMessage"] = "Câu trả lời không đúng.";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewData["ErrorMessage"] = $"Lỗi: {ex.Message}";
                }
            }
            ViewData["SecurityQuestion"] = ViewData["SecurityQuestion"] ?? "Câu hỏi bảo mật của bạn";
            return View(model);
        }

        [HttpGet("ResetPassword")]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                return RedirectToAction("ForgotPassword");
            }
            return View(new ResetPasswordViewModel { Email = email, Token = token });
        }

        [HttpPost("ResetPassword")]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        string query = "SELECT ResetToken, ResetTokenExpiry FROM Users WHERE Email = @Email";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Email", model.Email);
                            using (SqlDataReader reader = command.ExecuteReader()) // Đóng reader bằng using
                            {
                                if (reader.Read())
                                {
                                    var dbToken = reader["ResetToken"] as string;
                                    var expiry = reader["ResetTokenExpiry"] as DateTime?;
                                    if (dbToken == model.Token && expiry.HasValue && expiry.Value > DateTime.Now)
                                    {
                                        reader.Close(); // Đóng reader trước khi thực hiện lệnh UPDATE
                                        string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                                        string updateQuery = "UPDATE Users SET PasswordHash = @PasswordHash, ResetToken = NULL, ResetTokenExpiry = NULL WHERE Email = @Email";
                                        using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                                        {
                                            updateCommand.Parameters.AddWithValue("@PasswordHash", passwordHash);
                                            updateCommand.Parameters.AddWithValue("@Email", model.Email);
                                            updateCommand.ExecuteNonQuery();
                                        }
                                        ViewData["SuccessMessage"] = "Mật khẩu đã được đặt lại. Vui lòng đăng nhập.";
                                        return RedirectToAction("Login");
                                    }
                                    else
                                    {
                                        ViewData["ErrorMessage"] = "Token không hợp lệ hoặc đã hết hạn.";
                                    }
                                }
                                else
                                {
                                    ViewData["ErrorMessage"] = "Email không tồn tại.";
                                }
                            } // reader tự động đóng khi thoát using
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewData["ErrorMessage"] = $"Lỗi: {ex.Message}";
                }
            }
            return View(model);
        }

        [HttpGet]
        [Route("Edit")]
        public IActionResult Edit()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            ManageViewModel? model = null;
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string userQuery = "SELECT UserId, FullName, Email, Phone, Role, IsVerified, CreatedAt FROM Users WHERE UserId = @UserId";
                    using (SqlCommand userCommand = new SqlCommand(userQuery, connection))
                    {
                        userCommand.Parameters.AddWithValue("@UserId", userId);
                        using (SqlDataReader userReader = userCommand.ExecuteReader())
                        {
                            if (userReader.Read())
                            {
                                model = new ManageViewModel
                                {
                                    UserId = userReader.GetInt32(0),
                                    FullName = userReader.IsDBNull(1) ? null : userReader.GetString(1),
                                    Email = userReader.IsDBNull(2) ? null : userReader.GetString(2),
                                    Phone = userReader.IsDBNull(3) ? null : userReader.GetString(3),
                                    Role = userReader.IsDBNull(4) ? null : userReader.GetString(4),
                                    IsVerified = userReader.GetBoolean(5),
                                    CreatedAt = userReader.GetDateTime(6)
                                };
                            }
                        }
                    }

                    if (model != null && model.Role == "Consultant")
                    {
                        string consultantQuery = "SELECT ConsultantId, Specialty, CertificateUrl, ApprovalStatus, ExperienceYears, AvatarUrl FROM ConsultantProfiles WHERE ConsultantId = @ConsultantId";
                        using (SqlCommand consultantCommand = new SqlCommand(consultantQuery, connection))
                        {
                            consultantCommand.Parameters.AddWithValue("@ConsultantId", userId);
                            using (SqlDataReader consultantReader = consultantCommand.ExecuteReader())
                            {
                                if (consultantReader.Read())
                                {
                                    model.ConsultantId = consultantReader.GetInt32(0);
                                    model.Specialty = consultantReader.IsDBNull(1) ? null : consultantReader.GetString(1);
                                    model.CertificateUrl = consultantReader.IsDBNull(2) ? null : consultantReader.GetString(2);
                                    model.ApprovalStatus = consultantReader.IsDBNull(3) ? null : consultantReader.GetString(3);
                                    model.ExperienceYears = consultantReader.IsDBNull(4) ? 0 : consultantReader.GetInt32(4);
                                    model.AvatarUrl = consultantReader.IsDBNull(5) ? null : consultantReader.GetString(5);
                                }
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError("", $"Lỗi cơ sở dữ liệu: {ex.Message}");
                return RedirectToAction("Index", "Home");
            }

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        [HttpPost]
        [Route("Edit")]
        public async Task<IActionResult> Edit(ManageViewModel model)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        // Cập nhật bảng Users
                        string userQuery = @"
                            UPDATE Users 
                            SET FullName = @FullName, Email = @Email, Phone = @Phone
                            WHERE UserId = @UserId";
                        using (SqlCommand userCommand = new SqlCommand(userQuery, connection))
                        {
                            userCommand.Parameters.AddWithValue("@UserId", userId);
                            userCommand.Parameters.AddWithValue("@FullName", model.FullName);
                            userCommand.Parameters.AddWithValue("@Email", model.Email);
                            userCommand.Parameters.AddWithValue("@Phone", model.Phone ?? (object)DBNull.Value);
                            userCommand.ExecuteNonQuery();
                        }

                        // Xử lý upload file và lưu tên file
                        string? avatarFileName = model.AvatarUrl; // Giữ nguyên nếu không upload
                        if (model.AvatarFile != null && model.Role == "Consultant")
                        {
                            // Tạo thư mục images (nếu chưa có)
                            string uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "images");
                            if (!Directory.Exists(uploadsFolder))
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }

                            // Lấy tên file gốc và tạo tên duy nhất
                            string fileName = Path.GetFileName(model.AvatarFile.FileName);
                            avatarFileName = Guid.NewGuid().ToString() + "_" + fileName; // Thêm GUID để tránh trùng lặp
                            string filePath = Path.Combine(uploadsFolder, avatarFileName);

                            // Lưu file lên server
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await model.AvatarFile.CopyToAsync(stream);
                            }
                        }

                        // Cập nhật hoặc thêm ConsultantProfiles
                        if (model.Role == "Consultant")
                        {
                            string checkConsultantQuery = "SELECT COUNT(*) FROM ConsultantProfiles WHERE ConsultantId = @ConsultantId";
                            using (SqlCommand checkCommand = new SqlCommand(checkConsultantQuery, connection))
                            {
                                checkCommand.Parameters.AddWithValue("@ConsultantId", userId);
                                int count = (int)checkCommand.ExecuteScalar();
                                if (count == 0)
                                {
                                    string insertQuery = "INSERT INTO ConsultantProfiles (ConsultantId, Specialty, CertificateUrl, ApprovalStatus, ExperienceYears, AvatarUrl) VALUES (@ConsultantId, @Specialty, @CertificateUrl, @ApprovalStatus, @ExperienceYears, @AvatarUrl)";
                                    using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                                    {
                                        insertCommand.Parameters.AddWithValue("@ConsultantId", userId);
                                        insertCommand.Parameters.AddWithValue("@Specialty", model.Specialty);
                                        insertCommand.Parameters.AddWithValue("@CertificateUrl", model.CertificateUrl ?? (object)DBNull.Value);
                                        insertCommand.Parameters.AddWithValue("@ApprovalStatus", model.ApprovalStatus);
                                        insertCommand.Parameters.AddWithValue("@ExperienceYears", model.ExperienceYears);
                                        insertCommand.Parameters.AddWithValue("@AvatarUrl", avatarFileName ?? (object)DBNull.Value);
                                        insertCommand.ExecuteNonQuery();
                                    }
                                }
                                else
                                {
                                    string updateQuery = @"
                                        UPDATE ConsultantProfiles 
                                        SET Specialty = @Specialty, CertificateUrl = @CertificateUrl, 
                                            ApprovalStatus = @ApprovalStatus, ExperienceYears = @ExperienceYears, AvatarUrl = @AvatarUrl
                                        WHERE ConsultantId = @ConsultantId";
                                    using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                                    {
                                        updateCommand.Parameters.AddWithValue("@ConsultantId", userId);
                                        updateCommand.Parameters.AddWithValue("@Specialty", model.Specialty);
                                        updateCommand.Parameters.AddWithValue("@CertificateUrl", model.CertificateUrl ?? (object)DBNull.Value);
                                        updateCommand.Parameters.AddWithValue("@ApprovalStatus", model.ApprovalStatus);
                                        updateCommand.Parameters.AddWithValue("@ExperienceYears", model.ExperienceYears);
                                        updateCommand.Parameters.AddWithValue("@AvatarUrl", avatarFileName ?? (object)DBNull.Value);
                                        updateCommand.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                    }
                    return RedirectToAction("Manage", "Account");
                }
                catch (SqlException ex)
                {
                    ModelState.AddModelError("", $"Lỗi cơ sở dữ liệu: {ex.Message}");
                }
                catch (IOException ex)
                {
                    ModelState.AddModelError("", $"Lỗi khi lưu file: {ex.Message}");
                }
            }
            return View(model);
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