using Microsoft.AspNetCore.Mvc;
using MentalHealthSupport.Models;
using Microsoft.Data.SqlClient;

namespace MentalHealthSupport.Controllers
{
    public class NewsController : Controller
    {
        private readonly string? connectionString;

        public NewsController(IConfiguration config)
        {
            connectionString = config.GetConnectionString("DefaultConnection");
        }

        public IActionResult Index()
        {
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
                // Ghi log lỗi (nếu có hệ thống log)
                Console.WriteLine($"Database error: {ex.Message}");
                return View("Error"); // Hoặc trả về view lỗi tùy chỉnh
            }

            return View(newsList);
        }
    }
}