using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MentalHealthSupport.Models;

namespace MentalHealthSupport.Controllers
{
    public class AdminController : Controller
    {
        private readonly string? connectionString;

        public AdminController(IConfiguration config)
        {
            connectionString = config.GetConnectionString("DefaultConnection");
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        // Quản lý Tin tức
        [HttpGet]
        public IActionResult CreateNews()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [HttpPost]
        public IActionResult CreateNews(News news)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            if (ModelState.IsValid)
            {
                news.CreatedDate = DateTime.Now;
                news.Author = HttpContext.Session.GetString("FullName") ?? "Admin";

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
            return View(news);
        }

        [HttpGet]
        public IActionResult NewsList()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            List<News> newsList = new List<News>();
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
            return View(newsList);
        }

        [HttpGet]
        public IActionResult EditNews(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            News? news = null;
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
            if (news == null)
            {
                return NotFound();
            }
            return View(news);
        }

        [HttpPost]
        public IActionResult EditNews(News news)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            if (ModelState.IsValid)
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "UPDATE News SET Title = @Title, Content = @Content, CreatedDate = @CreatedDate, Author = @Author WHERE Id = @Id";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", news.Id);
                        command.Parameters.AddWithValue("@Title", news.Title);
                        command.Parameters.AddWithValue("@Content", news.Content);
                        command.Parameters.AddWithValue("@CreatedDate", news.CreatedDate);
                        command.Parameters.AddWithValue("@Author", news.Author);
                        command.ExecuteNonQuery();
                    }
                }
                return RedirectToAction("NewsList");
            }
            return View(news);
        }

        [HttpGet]
        public IActionResult DeleteNews(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
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
            return RedirectToAction("NewsList");
        }

        // Quản lý Người dùng
        [HttpGet]
        public IActionResult UserList()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            List<User> userList = new List<User>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT UserId, FullName, Email, PasswordHash, Phone, Role, IsVerified, CreatedAt, Sex FROM Users";
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
                                PasswordHash = reader.GetString(3),
                                Phone = reader.IsDBNull(4) ? null : reader.GetString(4),
                                Role = reader.GetString(5),
                                IsVerified = reader.GetBoolean(6),
                                CreatedAt = reader.GetDateTime(7),
                                Sex = reader.GetBoolean(8)
                            });
                        }
                    }
                }
            }
            return View(userList);
        }

        // Quản lý Chuyên gia
        [HttpGet]
        public IActionResult ConsultantList()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            List<ConsultantProfile> consultantList = new List<ConsultantProfile>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT cp.ConsultantId, cp.Specialty, cp.ExperienceYears, cp.Description, cp.ApprovalStatus " +
                               "FROM ConsultantProfiles cp JOIN Users u ON cp.ConsultantId = u.UserId";
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
            return View(consultantList);
        }

        // Quản lý Lịch hẹn
        [HttpGet]
        public IActionResult AppointmentList()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            List<Appointment> appointmentList = new List<Appointment>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT AppointmentId, UserId, ConsultantId, AppointmentTime, Status, Note, CreatedAt FROM Appointments";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            appointmentList.Add(new Appointment
                            {
                                AppointmentId = reader.GetInt32(0),
                                UserId = reader.GetInt32(1),
                                ConsultantId = reader.GetInt32(2),
                                AppointmentTime = reader.GetDateTime(3),
                                Status = reader.GetString(4),
                                Note = reader.IsDBNull(5) ? null : reader.GetString(5),
                                CreatedAt = reader.GetDateTime(6)
                            });
                        }
                    }
                }
            }
            return View(appointmentList);
        }

        // Quản lý Đánh giá
        [HttpGet]
        public IActionResult RatingList()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            List<Rating> ratingList = new List<Rating>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT RatingId, AppointmentId, Score, Comment, RatedAt FROM Ratings";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ratingList.Add(new Rating
                            {
                                RatingId = reader.GetInt32(0),
                                AppointmentId = reader.GetInt32(1),
                                Score = reader.GetInt32(2),
                                Comment = reader.IsDBNull(3) ? null : reader.GetString(3),
                                RatedAt = reader.GetDateTime(4)
                            });
                        }
                    }
                }
            }
            return View(ratingList);
        }

        // Quản lý Thanh toán
        [HttpGet]
        public IActionResult PaymentList()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            List<Payment> paymentList = new List<Payment>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT PaymentId, AppointmentId, Amount, PaymentMethod, PaymentStatus, PaidAt FROM Payments";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            paymentList.Add(new Payment
                            {
                                PaymentId = reader.GetInt32(0),
                                AppointmentId = reader.GetInt32(1),
                                Amount = reader.GetDecimal(2),
                                PaymentMethod = reader.IsDBNull(3) ? null : reader.GetString(3),
                                PaymentStatus = reader.GetString(4),
                                PaidAt = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5)
                            });
                        }
                    }
                }
            }
            return View(paymentList);
        }

        // Quản lý Báo cáo
        [HttpGet]
        public IActionResult ReportList()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            List<Report> reportList = new List<Report>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT ReportId, ReporterId, ReportedUserId, Message, CreatedAt, Status FROM Reports";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            reportList.Add(new Report
                            {
                                ReportId = reader.GetInt32(0),
                                ReporterId = reader.GetInt32(1),
                                ReportedUserId = reader.GetInt32(2),
                                Message = reader.GetString(3),
                                CreatedAt = reader.GetDateTime(4),
                                Status = reader.GetString(5)
                            });
                        }
                    }
                }
            }
            return View(reportList);
        }

        // Quản lý Bài viết
        [HttpGet]
        public IActionResult ArticleList()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            List<Article> articleList = new List<Article>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT ArticleId, Title, Content, Category, CreatedBy, CreatedAt FROM Articles";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            articleList.Add(new Article
                            {
                                ArticleId = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Content = reader.GetString(2),
                                Category = reader.IsDBNull(3) ? null : reader.GetString(3),
                                CreatedBy = reader.GetInt32(4),
                                CreatedAt = reader.GetDateTime(5)
                            });
                        }
                    }
                }
            }
            return View(articleList);
        }
    }
}