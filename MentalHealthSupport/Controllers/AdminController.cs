using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MentalHealthSupport.Models;
using MentalHealthSupport.ViewModels;
using MentalHealthSupport.Models.ViewModel;
using System.IO;

namespace MentalHealthSupport.Controllers
{
    [Route("Admin")]
    public class AdminController : Controller
    {
        private readonly string? connectionString;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public AdminController(IConfiguration config, IWebHostEnvironment hostingEnvironment)
        {
            connectionString = config.GetConnectionString("DefaultConnection") ?? throw new ArgumentNullException(nameof(config), "Connection string not found.");
            _hostingEnvironment = hostingEnvironment;
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
                            string passwordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash); 
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
                        string query = "UPDATE Users SET FullName = @FullName, Email = @Email, Phone = @Phone, Role = @Role, IsVerified = @IsVerified WHERE UserId = @UserId";
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
            return View(new ConsultantCreate());
        }

        [Route("Consultants/Create")]
    [HttpPost]
    public async Task<IActionResult> CreateConsultant(ConsultantCreate model)
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

                    // Kiểm tra email đã tồn tại chưa
                    string checkEmailQuery = "SELECT COUNT(*) FROM Users WHERE Email = @Email";
                    using (SqlCommand checkCommand = new SqlCommand(checkEmailQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Email", model.Email);
                        int emailCount = (int)checkCommand.ExecuteScalar();
                        if (emailCount > 0)
                        {
                            ModelState.AddModelError("Email", "Email đã tồn tại.");
                            return View(model);
                        }
                    }

                    // Xử lý upload file avatar
                    string? avatarFileName = null; 
                    if (model.AvatarFile != null) 
                    {
                        string uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "images");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }
                        avatarFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.AvatarFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, avatarFileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.AvatarFile.CopyToAsync(stream);
                        }
                    }

                    // Mã hóa mật khảu
                    string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

                    // Thêm user vào bảng Users
                    string userQuery = "INSERT INTO Users (FullName, Email, Phone, Role, IsVerified, PasswordHash, Sex, SecurityQuestion, SecurityAnswer, CreatedAt) VALUES (@FullName, @Email, @Phone, @Role, @IsVerified, @PasswordHash, @Sex, @SecurityQuestion, @SecurityAnswer, @CreatedAt); SELECT SCOPE_IDENTITY();";
                    int userId;
                    using (SqlCommand userCommand = new SqlCommand(userQuery, connection))
                    {
                        userCommand.Parameters.AddWithValue("@FullName", model.FullName);
                        userCommand.Parameters.AddWithValue("@Email", model.Email);
                        userCommand.Parameters.AddWithValue("@Phone", model.Phone != null ? (object)model.Phone : DBNull.Value);
                        userCommand.Parameters.AddWithValue("@Role", model.Role);
                        userCommand.Parameters.AddWithValue("@IsVerified", model.IsVerified);
                        userCommand.Parameters.AddWithValue("@PasswordHash", passwordHash);
                        userCommand.Parameters.AddWithValue("@Sex", (object)model.Sex ?? DBNull.Value);
                        userCommand.Parameters.AddWithValue("@SecurityQuestion", model.SecurityQuestion);
                        userCommand.Parameters.AddWithValue("@SecurityAnswer", model.SecurityAnswer);
                        userCommand.Parameters.AddWithValue("@CreatedAt", model.CreatedAt);
                        userId = Convert.ToInt32(userCommand.ExecuteScalar());
                    }

                    // Thêm vào ConsultantProfiles với avatar
                    string consultantQuery = "INSERT INTO ConsultantProfiles (ConsultantId, Specialty, ExperienceYears, Description, ApprovalStatus, CertificateUrl, AvatarUrl) VALUES (@ConsultantId, @Specialty, @ExperienceYears, @Description, @ApprovalStatus, @CertificateUrl, @AvatarUrl)";
                    using (SqlCommand consultantCommand = new SqlCommand(consultantQuery, connection))
                    {
                        consultantCommand.Parameters.AddWithValue("@ConsultantId", userId);
                        consultantCommand.Parameters.AddWithValue("@Specialty", model.Specialty);
                        consultantCommand.Parameters.AddWithValue("@ExperienceYears", model.ExperienceYears.HasValue ? (object)model.ExperienceYears.Value : DBNull.Value);
                        consultantCommand.Parameters.AddWithValue("@Description", model.Description);
                        consultantCommand.Parameters.AddWithValue("@ApprovalStatus", model.ApprovalStatus);
                        consultantCommand.Parameters.AddWithValue("@CertificateUrl", model.CertificateUrl != null ? (object)model.CertificateUrl : DBNull.Value);
                        consultantCommand.Parameters.AddWithValue("@AvatarUrl", avatarFileName ?? (object)DBNull.Value); // Lưu tên file avatar
                        consultantCommand.ExecuteNonQuery();
                    }
                }
                return RedirectToAction("ConsultantList");
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
                    string query = "SELECT ConsultantId, Specialty, ExperienceYears, Description, ApprovalStatus, Users.FullName FROM ConsultantProfiles JOIN Users ON ConsultantProfiles.ConsultantId = Users.UserId WHERE Users.Role = 'Consultant'";
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
                                    ApprovalStatus = reader.GetString(4),
                                    FullName = reader.IsDBNull(5) ? null : reader.GetString(5)
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

        [HttpGet]
        [Route("Consultants/Edit/{id}")]
        public IActionResult EditConsultant(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")) || HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            ConsultantEditViewModel? model = null;
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Lấy thông tin từ bảng Users
                    string userQuery = "SELECT UserId, FullName, Email, Phone, Role, IsVerified, Sex, SecurityQuestion, SecurityAnswer, CreatedAt FROM Users WHERE UserId = @UserId";
                    using (SqlCommand userCommand = new SqlCommand(userQuery, connection))
                    {
                        userCommand.Parameters.AddWithValue("@UserId", id);
                        using (SqlDataReader userReader = userCommand.ExecuteReader())
                        {
                            if (userReader.Read())
                            {
                                model = new ConsultantEditViewModel
                                {
                                    UserId = userReader.GetInt32(0),
                                    FullName = userReader.GetString(1),
                                    Email = userReader.GetString(2),
                                    Phone = userReader.IsDBNull(3) ? null : userReader.GetString(3),
                                    Role = userReader.GetString(4),
                                    IsVerified = userReader.GetBoolean(5),
                                    Sex = userReader.IsDBNull(6) ? false : Convert.ToBoolean(userReader.GetValue(6)),
                                    SecurityQuestion = userReader.IsDBNull(7) ? null : userReader.GetString(7),
                                    SecurityAnswer = userReader.IsDBNull(8) ? null : userReader.GetString(8),
                                    CreatedAt = userReader.GetDateTime(9)
                                };
                            }
                        }
                    }

                    if (model != null)
                    {
                        // Lấy thông tin từ ConsultantProfiles
                        string consultantQuery = "SELECT Specialty, ExperienceYears, Description, ApprovalStatus, CertificateUrl FROM ConsultantProfiles WHERE ConsultantId = @ConsultantId";
                        using (SqlCommand consultantCommand = new SqlCommand(consultantQuery, connection))
                        {
                            consultantCommand.Parameters.AddWithValue("@ConsultantId", id);
                            using (SqlDataReader consultantReader = consultantCommand.ExecuteReader())
                            {
                                if (consultantReader.Read())
                                {
                                    model.Specialty = consultantReader.IsDBNull(0) ? null : consultantReader.GetString(0);
                                    model.ExperienceYears = consultantReader.IsDBNull(1) ? null : consultantReader.GetInt32(1);
                                    model.Description = consultantReader.IsDBNull(2) ? null : consultantReader.GetString(2);
                                    model.ApprovalStatus = consultantReader.IsDBNull(3) ? null : consultantReader.GetString(3);
                                    model.CertificateUrl = consultantReader.IsDBNull(4) ? null : consultantReader.GetString(4);
                                }
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

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        [HttpPost]
        [Route("Consultants/Edit/{id}")]
        public IActionResult EditConsultant(int id, ConsultantEditViewModel model)
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
                        // Cập nhật bảng Users
                        string userQuery = @"
                            UPDATE Users 
                            SET FullName = @FullName, Email = @Email, Phone = @Phone,
                                Sex = @Sex, SecurityQuestion = @SecurityQuestion, SecurityAnswer = @SecurityAnswer
                            WHERE UserId = @UserId";
                        using (SqlCommand userCommand = new SqlCommand(userQuery, connection))
                        {
                            userCommand.Parameters.AddWithValue("@UserId", id);
                            userCommand.Parameters.AddWithValue("@FullName", model.FullName);
                            userCommand.Parameters.AddWithValue("@Email", model.Email);
                            userCommand.Parameters.AddWithValue(
                                "@Phone",
                                model.Phone != null ? (object)model.Phone : DBNull.Value
                            );
                            userCommand.Parameters.AddWithValue("@Sex", model.Sex);
                            userCommand.Parameters.AddWithValue("@SecurityQuestion", model.SecurityQuestion);
                            userCommand.Parameters.AddWithValue("@SecurityAnswer", model.SecurityAnswer);
                            userCommand.ExecuteNonQuery();
                        }

                        // Cập nhật hoặc thêm ConsultantProfiles
                        string checkConsultantQuery = "SELECT COUNT(*) FROM ConsultantProfiles WHERE ConsultantId = @ConsultantId";
                        using (SqlCommand checkCommand = new SqlCommand(checkConsultantQuery, connection))
                        {
                            checkCommand.Parameters.AddWithValue("@ConsultantId", id);
                            int count = (int)checkCommand.ExecuteScalar();
                            if (count == 0)
                            {
                                string insertQuery = "INSERT INTO ConsultantProfiles (ConsultantId, Specialty, ExperienceYears, Description, ApprovalStatus, CertificateUrl) VALUES (@ConsultantId, @Specialty, @ExperienceYears, @Description, @ApprovalStatus, @CertificateUrl)";
                                using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                                {
                                    insertCommand.Parameters.AddWithValue("@ConsultantId", id);
                                    insertCommand.Parameters.AddWithValue("@Specialty", model.Specialty);
                                    insertCommand.Parameters.AddWithValue(
                                        "@ExperienceYears",
                                        model.ExperienceYears.HasValue ? (object)model.ExperienceYears.Value : DBNull.Value
                                    );

                                    insertCommand.Parameters.AddWithValue("@Description", model.Description);
                                    insertCommand.Parameters.AddWithValue("@ApprovalStatus", model.ApprovalStatus);
                                    insertCommand.Parameters.AddWithValue(
                                        "@CertificateUrl",
                                        model.CertificateUrl != null ? (object)model.CertificateUrl : DBNull.Value
                                    );
                                    insertCommand.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                string updateQuery = @"
                                    UPDATE ConsultantProfiles 
                                    SET Specialty = @Specialty, ExperienceYears = @ExperienceYears, Description = @Description, 
                                        ApprovalStatus = @ApprovalStatus, CertificateUrl = @CertificateUrl
                                    WHERE ConsultantId = @ConsultantId";
                                using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                                {
                                    updateCommand.Parameters.AddWithValue("@ConsultantId", id);
                                    updateCommand.Parameters.AddWithValue("@Specialty", model.Specialty);
                                    updateCommand.Parameters.AddWithValue(
                                        "@ExperienceYears",
                                        model.ExperienceYears.HasValue ? (object)model.ExperienceYears.Value : DBNull.Value
                                    );
                                    updateCommand.Parameters.AddWithValue("@Description", model.Description);
                                    updateCommand.Parameters.AddWithValue("@ApprovalStatus", model.ApprovalStatus);
                                    updateCommand.Parameters.AddWithValue(
                                        "@CertificateUrl",
                                        model.CertificateUrl != null ? (object)model.CertificateUrl : DBNull.Value
                                    );
                                    updateCommand.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                    return RedirectToAction("ConsultantList");
                }
                catch (SqlException ex)
                {
                    ModelState.AddModelError("", $"Lỗi cơ sở dữ liệu: {ex.Message}");
                }
            }
            return View(model);
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
                    string query1 = "DELETE FROM Users WHERE UserId = @Id";
                    using (SqlCommand command = new SqlCommand(query1, connection))
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

        [Route("Policies/List")]
        [HttpGet]
        public IActionResult PolicyList()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")) || HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            List<TermsAndPolicy> policies = new List<TermsAndPolicy>();
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT Id, PolicyType, Content, CreatedDate, LastModifiedDate, IsActive FROM TermsAndPolicies";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                policies.Add(new TermsAndPolicy
                                {
                                    Id = reader.GetInt32(0),
                                    PolicyType = reader.GetString(1),
                                    Content = reader.GetString(2),
                                    CreatedDate = reader.GetDateTime(3),
                                    LastModifiedDate = reader.IsDBNull(4) ? null : reader.GetDateTime(4) as DateTime?,
                                    IsActive = reader.GetBoolean(5)
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
            return View(policies);
        }

        [Route("Policies/Edit/{id}")]
        [HttpGet]
        public IActionResult EditPolicy(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")) || HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            TermsAndPolicy? policy = null;
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT Id, PolicyType, Content, CreatedDate, LastModifiedDate, IsActive FROM TermsAndPolicies WHERE Id = @Id";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                policy = new TermsAndPolicy
                                {
                                    Id = reader.GetInt32(0),
                                    PolicyType = reader.GetString(1),
                                    Content = reader.GetString(2),
                                    CreatedDate = reader.GetDateTime(3),
                                    LastModifiedDate = reader.IsDBNull(4) ? null : reader.GetDateTime(4) as DateTime?,
                                    IsActive = reader.GetBoolean(5)
                                };
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError("", $"Lỗi cơ sở dữ liệu: {ex.Message}");
                return RedirectToAction("PolicyList");
            }
            if (policy == null)
            {
                return NotFound();
            }
            return View(policy);
        }

        [Route("Policies/Edit/{id}")]
        [HttpPost]
        public IActionResult EditPolicy(TermsAndPolicy model)
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
                        string query = @"
                            UPDATE TermsAndPolicies 
                            SET Content = @Content, LastModifiedDate = @LastModifiedDate, IsActive = @IsActive 
                            WHERE Id = @Id";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Id", model.Id);
                            command.Parameters.AddWithValue("@Content", model.Content);
                            command.Parameters.AddWithValue("@LastModifiedDate", DateTime.Now);
                            command.Parameters.AddWithValue("@IsActive", model.IsActive);
                            int rowsAffected = command.ExecuteNonQuery();
                            if (rowsAffected == 0)
                            {
                                ModelState.AddModelError("", "Không tìm thấy chính sách để cập nhật.");
                            }
                        }
                    }
                    return RedirectToAction("PolicyList");
                }
                catch (SqlException ex)
                {
                    ModelState.AddModelError("", $"Lỗi cơ sở dữ liệu: {ex.Message}");
                }
            }
            return View(model);
        }

        [Route("Policies/Create")]
        [HttpGet]
        public IActionResult CreatePolicy()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")) || HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            return View(new TermsAndPolicy());
        }

        [Route("Policies/Create")]
        [HttpPost]
        public IActionResult CreatePolicy(TermsAndPolicy model)
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
                        string query = @"
                            INSERT INTO TermsAndPolicies (PolicyType, Content, CreatedDate, LastModifiedDate, IsActive)
                            VALUES (@PolicyType, @Content, @CreatedDate, @LastModifiedDate, @IsActive)";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@PolicyType", model.PolicyType);
                            command.Parameters.AddWithValue("@Content", model.Content);
                            command.Parameters.AddWithValue("@CreatedDate", DateTime.Now);
                            command.Parameters.AddWithValue("@LastModifiedDate", DateTime.Now);
                            command.Parameters.AddWithValue("@IsActive", model.IsActive);
                            command.ExecuteNonQuery();
                        }
                    }
                    return RedirectToAction("PolicyList");
                }
                catch (SqlException ex)
                {
                    ModelState.AddModelError("", $"Lỗi cơ sở dữ liệu: {ex.Message}");
                }
            }
            return View(model);
        }

        [Route("Policies/Delete/{id}")]
        [HttpGet]
        public IActionResult DeletePolicy(int id)
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
                    string query = "DELETE FROM TermsAndPolicies WHERE Id = @Id";
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
            return RedirectToAction("PolicyList");
        }

        [HttpGet("EditAboutUs")]
        public IActionResult EditAboutUs()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")) || HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var model = new AboutUsViewModel();
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT * FROM AboutUs WHERE Id = @Id";
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", 1);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                model.Id = reader.GetInt32(0);
                                model.Title = reader["Title"].ToString();
                                model.HeroHeading = reader["HeroHeading"].ToString();
                                model.HeroDescription = reader["HeroDescription"].ToString();
                                model.HeroImageUrl = reader["HeroImageUrl"].ToString();
                                model.MissionHeading = reader["MissionHeading"].ToString();
                                model.ValuesHeading = reader["ValuesHeading"].ToString();
                                model.CallToActionHeading = reader["CallToActionHeading"].ToString();
                                model.CallToActionDescription = reader["CallToActionDescription"].ToString();
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError("", $"Lỗi cơ sở dữ liệu: {ex.Message}");
            }

            return View(model);
        }

        [HttpPost("EditAboutUs")]
        public async Task<IActionResult> EditAboutUs(AboutUsViewModel model)
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
                        string? heroImageFileName = model.HeroImageUrl;
                        if (model.HeroImageFile != null)
                        {
                            string uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "images");
                            if (!Directory.Exists(uploadsFolder))
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }
                            heroImageFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.HeroImageFile.FileName);
                            string filePath = Path.Combine(uploadsFolder, heroImageFileName);
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await model.HeroImageFile.CopyToAsync(stream);
                            }
                        }

                        string query = @"
                            IF NOT EXISTS (SELECT 1 FROM AboutUs WHERE Id = @Id)
                                INSERT INTO AboutUs (Id, Title, HeroHeading, HeroDescription, HeroImageUrl, MissionHeading, ValuesHeading, CallToActionHeading, CallToActionDescription)
                                VALUES (@Id, @Title, @HeroHeading, @HeroDescription, @HeroImageUrl, @MissionHeading, @ValuesHeading, @CallToActionHeading, @CallToActionDescription)
                            ELSE
                                UPDATE AboutUs 
                                SET Title = @Title, HeroHeading = @HeroHeading, HeroDescription = @HeroDescription, 
                                    HeroImageUrl = @HeroImageUrl, MissionHeading = @MissionHeading, ValuesHeading = @ValuesHeading, 
                                    CallToActionHeading = @CallToActionHeading, CallToActionDescription = @CallToActionDescription
                                WHERE Id = @Id";
                        using (SqlCommand cmd = new SqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@Id", model.Id);
                            cmd.Parameters.AddWithValue("@Title", model.Title);
                            cmd.Parameters.AddWithValue("@HeroHeading", model.HeroHeading);
                            cmd.Parameters.AddWithValue("@HeroDescription", model.HeroDescription);
                            cmd.Parameters.AddWithValue("@HeroImageUrl", heroImageFileName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@MissionHeading", model.MissionHeading);
                            cmd.Parameters.AddWithValue("@ValuesHeading", model.ValuesHeading);
                            cmd.Parameters.AddWithValue("@CallToActionHeading", model.CallToActionHeading);
                            cmd.Parameters.AddWithValue("@CallToActionDescription", model.CallToActionDescription);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    return RedirectToAction("EditAboutUs");
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
    }
}