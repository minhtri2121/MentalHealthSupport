using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MentalHealthSupport.Models;

namespace MentalHealthSupport.Controllers
{
    [Route("Admin")]
    public class AdminController : Controller
    {
        private readonly string? connectionString;

        public AdminController(IConfiguration config)
        {
            connectionString = config.GetConnectionString("DefaultConnection") ?? throw new ArgumentNullException(nameof(config), "Connection string not found.");
        }

        [Route("Index")]
        public IActionResult Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")) || HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        // Quản lý Tin tức
        [Route("News/Create")]
        [HttpGet]
        public IActionResult CreateNews()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")) || HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [Route("News/Create")]
        [HttpPost]
        public IActionResult CreateNews(News news)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")) || HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            if (ModelState.IsValid)
            {
                news.CreatedDate = DateTime.Now;
                news.Author = HttpContext.Session.GetString("FullName") ?? "Admin";

                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        string query = "INSERT INTO News (Title, Content, CreatedDate, Author) VALUES (@Title, @Content, @CreatedDate, @Author)";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Title", news.Title);
                            command.Parameters.AddWithValue("@Content", news.Content);
                            command.Parameters.AddWithValue("@CreatedDate", news.CreatedDate);
                            command.Parameters.AddWithValue("@Author", news.Author);
                            command.ExecuteNonQuery();
                        }
                    }
                    return RedirectToAction("NewsList");
                }
                catch (SqlException ex)
                {
                    ModelState.AddModelError("", $"Lỗi cơ sở dữ liệu: {ex.Message}");
                }
            }
            return View(news);
        }

        [Route("News/List")]
        [HttpGet]
        public IActionResult NewsList()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")) || HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            List<News> newsList = new List<News>();
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT Id, Title, Content, CreatedDate, Author FROM News ORDER BY CreatedDate DESC";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                newsList.Add(new News
                                {
                                    Id = reader.GetInt32(0),
                                    Title = reader.GetString(1),
                                    Content = reader.GetString(2),
                                    CreatedDate = reader.GetDateTime(3),
                                    Author = reader.GetString(4)
                                });
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError("", $"Lỗi cơ sở dữ liệu: {ex.Message}");
            }
            return View(newsList);
        }

        [Route("News/Edit/{id}")]
        [HttpGet]
        public IActionResult EditNews(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")) || HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            News? news = null;
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT Id, Title, Content, CreatedDate, Author FROM News WHERE Id = @Id";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                news = new News
                                {
                                    Id = reader.GetInt32(0),
                                    Title = reader.GetString(1),
                                    Content = reader.GetString(2),
                                    CreatedDate = reader.GetDateTime(3),
                                    Author = reader.GetString(4)
                                };
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError("", $"Lỗi cơ sở dữ liệu: {ex.Message}");
                return RedirectToAction("NewsList");
            }
            if (news == null)
            {
                return NotFound();
            }
            return View(news);
        }

        [Route("News/Edit/{id}")]
        [HttpPost]
        public IActionResult EditNews(News news)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")) || HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            if (ModelState.IsValid)
            {
                news.Author = HttpContext.Session.GetString("FullName") ?? "Admin";
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        DateTime validCreatedDate = news.CreatedDate;
                        if (validCreatedDate < new DateTime(1753, 1, 1))
                        {
                            string getDateQuery = "SELECT CreatedDate FROM News WHERE Id = @Id";
                            using (SqlCommand getCommand = new SqlCommand(getDateQuery, connection))
                            {
                                getCommand.Parameters.AddWithValue("@Id", news.Id);
                                var existingDate = getCommand.ExecuteScalar();
                                validCreatedDate = existingDate != DBNull.Value ? (DateTime)existingDate : DateTime.Now;
                            }
                        }

                        string query = "UPDATE News SET Title = @Title, Content = @Content, CreatedDate = @CreatedDate, Author = @Author WHERE Id = @Id";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Id", news.Id);
                            command.Parameters.AddWithValue("@Title", news.Title);
                            command.Parameters.AddWithValue("@Content", news.Content);
                            command.Parameters.AddWithValue("@CreatedDate", validCreatedDate);
                            command.Parameters.AddWithValue("@Author", news.Author);
                            command.ExecuteNonQuery();
                        }
                    }
                    return RedirectToAction("NewsList");
                }
                catch (SqlException ex)
                {
                    ModelState.AddModelError("", $"Lỗi cơ sở dữ liệu: {ex.Message}");
                }
            }
            return View(news);
        }

        [Route("News/Delete/{id}")]
        [HttpGet]
        public IActionResult DeleteNews(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")) || HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "DELETE FROM News WHERE Id = @Id";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError("", $"Lỗi cơ sở dữ liệu: {ex.Message}");
            }
            return RedirectToAction("NewsList");
        }

        // Quản lý Người dùng
        [Route("Users/Create")]
        [HttpGet]
        public IActionResult CreateUser()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")) || HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [Route("Users/Create")]
        [HttpPost]
        public IActionResult CreateUser(User user)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")) || HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        string query = "INSERT INTO Users (FullName, Email, Phone, Role, IsVerified, PasswordHash) VALUES (@FullName, @Email, @Phone, @Role, @IsVerified, @PasswordHash)";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@FullName", user.FullName);
                            command.Parameters.AddWithValue("@Email", user.Email);
                            if (user.Phone == null)
                            {
                                command.Parameters.Add("@Phone", System.Data.SqlDbType.NVarChar).Value = DBNull.Value;
                            }
                            else
                            {
                                command.Parameters.Add("@Phone", System.Data.SqlDbType.NVarChar).Value = user.Phone;
                            }
                            command.Parameters.AddWithValue("@Role", user.Role);
                            command.Parameters.AddWithValue("@IsVerified", user.IsVerified);
                            // Hash password trước khi lưu
                            string passwordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash); // Cần package BCrypt.Net-Next
                            command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                            command.ExecuteNonQuery();
                        }
                    }
                    return RedirectToAction("UserList");
                }
                catch (SqlException ex)
                {
                    ModelState.AddModelError("", $"Lỗi cơ sở dữ liệu: {ex.Message}");
                }
            }
            return View(user);
        }

        [Route("Users/List")]
        [HttpGet]
        public IActionResult UserList()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")) || HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            List<User> userList = new List<User>();
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT UserId, FullName, Email, Phone, Role, IsVerified FROM Users";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                userList.Add(new User
                                {
                                    UserId = reader.GetInt32(0),
                                    FullName = reader.GetString(1),
                                    Email = reader.GetString(2),
                                    Phone = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    Role = reader.GetString(4),
                                    IsVerified = reader.GetBoolean(5)
                                });
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError("", $"Lỗi cơ sở dữ liệu: {ex.Message}");
            }
            return View(userList);
        }

        [Route("Users/Edit/{id}")]
        [HttpGet]
        public IActionResult EditUser(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")) || HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            User? user = null;
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT UserId, FullName, Email, Phone, Role, IsVerified FROM Users WHERE UserId = @Id";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                user = new User
                                {
                                    UserId = reader.GetInt32(0),
                                    FullName = reader.GetString(1),
                                    Email = reader.GetString(2),
                                    Phone = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    Role = reader.GetString(4),
                                    IsVerified = reader.GetBoolean(5)
                                };
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError("", $"Lỗi cơ sở dữ liệu: {ex.Message}");
                return RedirectToAction("UserList");
            }
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [Route("Users/Edit/{id}")]
        [HttpPost]
        public IActionResult EditUser(User user)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")) || HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        string query = "UPDATE Users SET FullName = @FullName, Email = @Email, Phone = @Phone, Role = @Role, IsVerified = @IsVerified, PasswordHash = @PasswordHash WHERE UserId = @UserId";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@UserId", user.UserId);
                            command.Parameters.AddWithValue("@FullName", user.FullName);
                            command.Parameters.AddWithValue("@Email", user.Email);
                            if (user.Phone == null)
                            {
                                command.Parameters.Add("@Phone", System.Data.SqlDbType.NVarChar).Value = DBNull.Value;
                            }
                            else
                            {
                                command.Parameters.Add("@Phone", System.Data.SqlDbType.NVarChar).Value = user.Phone;
                            }
                            command.Parameters.AddWithValue("@Role", user.Role);
                            command.Parameters.AddWithValue("@IsVerified", user.IsVerified);
                            // Hash password nếu có thay đổi
                            string passwordHash = string.IsNullOrEmpty(user.PasswordHash) ? (string)command.Parameters["@PasswordHash"].Value : BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                            command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                            command.ExecuteNonQuery();
                        }
                    }
                    return RedirectToAction("UserList");
                }
                catch (SqlException ex)
                {
                    ModelState.AddModelError("", $"Lỗi cơ sở dữ liệu: {ex.Message}");
                }
            }
            return View(user);
        }

        [Route("Users/Delete/{id}")]
        [HttpGet]
        public IActionResult DeleteUser(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")) || HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "DELETE FROM Users WHERE UserId = @Id";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError("", $"Lỗi cơ sở dữ liệu: {ex.Message}");
            }
            return RedirectToAction("UserList");
        }

        // Quản lý Chuyên gia
        [Route("Consultants/Create")]
        [HttpGet]
        public IActionResult CreateConsultant()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")) || HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [Route("Consultants/Create")]
        [HttpPost]
        public IActionResult CreateConsultant(ConsultantProfile consultant)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")) || HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        string query = "INSERT INTO ConsultantProfiles (ConsultantId, Specialty, ExperienceYears, Description, ApprovalStatus) VALUES (@ConsultantId, @Specialty, @ExperienceYears, @Description, @ApprovalStatus)";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@ConsultantId", consultant.ConsultantId);
                            if (consultant.Specialty == null)
                            {
                                command.Parameters.Add("@Specialty", System.Data.SqlDbType.NVarChar).Value = DBNull.Value;
                            }
                            else
                            {
                                command.Parameters.Add("@Specialty", System.Data.SqlDbType.NVarChar).Value = consultant.Specialty;
                            }

                            if (consultant.Description == null)
                            {
                                command.Parameters.Add("@Description", System.Data.SqlDbType.NVarChar).Value = DBNull.Value;
                            }
                            else
                            {
                                command.Parameters.Add("@Description", System.Data.SqlDbType.NVarChar).Value = consultant.Description;
                            }
                            command.Parameters.AddWithValue("@ExperienceYears", (object)consultant.ExperienceYears ?? DBNull.Value);
                            command.Parameters.AddWithValue("@ApprovalStatus", consultant.ApprovalStatus);
                            command.ExecuteNonQuery();
                        }
                    }
                    return RedirectToAction("ConsultantList");
                }
                catch (SqlException ex)
                {
                    ModelState.AddModelError("", $"Lỗi cơ sở dữ liệu: {ex.Message}");
                }
            }
            return View(consultant);
        }

        [Route("Consultants/List")]
        [HttpGet]
        public IActionResult ConsultantList()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")) || HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            List<ConsultantProfile> consultantList = new List<ConsultantProfile>();
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT ConsultantId, Specialty, ExperienceYears, Description, ApprovalStatus FROM ConsultantProfiles";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                consultantList.Add(new ConsultantProfile
                                {
                                    ConsultantId = reader.GetInt32(0),
                                    Specialty = reader.IsDBNull(1) ? null : reader.GetString(1),
                                    ExperienceYears = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                                    Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    ApprovalStatus = reader.GetString(4)
                                });
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError("", $"Lỗi cơ sở dữ liệu: {ex.Message}");
            }
            return View(consultantList);
        }

        [Route("Consultants/Edit/{id}")]
        [HttpGet]
        public IActionResult EditConsultant(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")) || HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            ConsultantProfile? consultant = null;
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT ConsultantId, Specialty, ExperienceYears, Description, ApprovalStatus FROM ConsultantProfiles WHERE ConsultantId = @Id";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                consultant = new ConsultantProfile
                                {
                                    ConsultantId = reader.GetInt32(0),
                                    Specialty = reader.IsDBNull(1) ? null : reader.GetString(1),
                                    ExperienceYears = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                                    Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    ApprovalStatus = reader.GetString(4)
                                };
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError("", $"Lỗi cơ sở dữ liệu: {ex.Message}");
                return RedirectToAction("ConsultantList");
            }
            if (consultant == null)
            {
                return NotFound();
            }
            return View(consultant);
        }

        [Route("Consultants/Edit/{id}")]
        [HttpPost]
        public IActionResult EditConsultant(ConsultantProfile consultant)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")) || HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        string query = "UPDATE ConsultantProfiles SET Specialty = @Specialty, ExperienceYears = @ExperienceYears, Description = @Description, ApprovalStatus = @ApprovalStatus WHERE ConsultantId = @ConsultantId";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@ConsultantId", consultant.ConsultantId);
                            if (consultant.Specialty == null)
                            {
                                command.Parameters.Add("@Specialty", System.Data.SqlDbType.NVarChar).Value = DBNull.Value;
                            }
                            else
                            {
                                command.Parameters.Add("@Specialty", System.Data.SqlDbType.NVarChar).Value = consultant.Specialty;
                            }

                            if (consultant.Description == null)
                            {
                                command.Parameters.Add("@Description", System.Data.SqlDbType.NVarChar).Value = DBNull.Value;
                            }
                            else
                            {
                                command.Parameters.Add("@Description", System.Data.SqlDbType.NVarChar).Value = consultant.Description;
                            }
                            command.Parameters.AddWithValue("@ExperienceYears", (object)consultant.ExperienceYears ?? DBNull.Value);
                            command.Parameters.AddWithValue("@ApprovalStatus", consultant.ApprovalStatus);
                            command.ExecuteNonQuery();
                        }
                    }
                    return RedirectToAction("ConsultantList");
                }
                catch (SqlException ex)
                {
                    ModelState.AddModelError("", $"Lỗi cơ sở dữ liệu: {ex.Message}");
                }
            }
            return View(consultant);
        }

        [Route("Consultants/Delete/{id}")]
        [HttpGet]
        public IActionResult DeleteConsultant(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")) || HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "DELETE FROM ConsultantProfiles WHERE ConsultantId = @Id";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError("", $"Lỗi cơ sở dữ liệu: {ex.Message}");
            }
            return RedirectToAction("ConsultantList");
        }
    }
}