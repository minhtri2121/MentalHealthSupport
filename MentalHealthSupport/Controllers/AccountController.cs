using Microsoft.AspNetCore.Mvc;
using MentalHealthSupport.Models.ViewModel;
using Microsoft.Data.SqlClient;
using BCrypt.Net;
using System.Data;

namespace MentalHealthSupport.Controllers
{
    public class AccountController : Controller
    {
        private readonly string? connectionString;

        public AccountController(IConfiguration config)
        {
            connectionString = config.GetConnectionString("DefaultConnection");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            
            if (ModelState.IsValid)
            {
                try            
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        // Thêm UserId vào truy vấn
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

                                // Lưu thông tin vào session, bao gồm UserId
                                HttpContext.Session.SetInt32("UserId", userId);
                                HttpContext.Session.SetString("UserEmail", model.Email);
                                HttpContext.Session.SetString("FullName", fullName);
                                HttpContext.Session.SetString("UserRole", role);

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

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
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

                        // Kiểm tra email đã tồn tại
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

                        // Mã hóa mật khẩu
                        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.PasswordHash);

                        // Thêm người dùng vào bảng Users với vai trò mặc định là "User"
                        var query = @"
                            INSERT INTO Users (FullName, Email, PasswordHash, Role, Phone, Sex)
                            VALUES (@FullName, @Email, @PasswordHash, @Role, @Phone, @Sex)";

                        using (var command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@FullName", model.FullName);
                            command.Parameters.AddWithValue("@Email", model.Email);
                            command.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                            command.Parameters.AddWithValue("@Role", "User"); // Mặc định là User
                            command.Parameters.AddWithValue("@Phone", (object?)model.Phone ?? DBNull.Value);
                            command.Parameters.AddWithValue("@Sex", model.Sex); // Lưu true (Nam) hoặc false (Nữ) vào cột bit

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

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

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

                // Nếu là Consultant thì lấy thêm thông tin
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

            return View("Manage",model);
        }

        [HttpPost]
        public IActionResult UpdateAccount(ManageViewModel model)
        {
            if (!ModelState.IsValid)
                return View("ManageAccount", model);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Cập nhật bảng Users
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

                // Nếu là Consultant thì cập nhật thêm
                if (model.Role == "Consultant")
                {
                    string updateConsultant = @"UPDATE ConsultantProfiles 
                                                SET Specialty = @Specialty, CertificateUrl = @CertificateUrl, ExperienceYears = @ExperienceYears 
                                                WHERE ConsultantId = @ConsultantId";
                    using (SqlCommand cmd = new SqlCommand(updateConsultant, connection))
                    {
                        cmd.Parameters.AddWithValue("@Specialty", model.Specialty);
                        cmd.Parameters.AddWithValue("@Certificate", model.CertificateUrl);
                        cmd.Parameters.AddWithValue("@ExperienceYears", model.ExperienceYears);
                        cmd.Parameters.AddWithValue("@ConsultantId", model.ConsultantId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            TempData["SuccessMessage"] = "Cập nhật thông tin thành công.";
            return RedirectToAction("ManageAccount");
        }
    }
}