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

                    string query = @"
                        SELECT Id, Title, Content, CreatedDate, Author
                        FROM News
                        ORDER BY CreatedDate DESC";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            newsList.Add(new News
                            {
                                Id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                                Title = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                Content = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                CreatedDate = reader.IsDBNull(3) ? DateTime.Now : reader.GetDateTime(3),
                                Author = reader.IsDBNull(4) ? "" : reader.GetString(4)
                            });
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Database error at News/Index: {ex.Message}");
                return View("Error");
            }

            return View(newsList);
        }

        [HttpGet]
        public IActionResult Detail(int id)
        {
            News? article = null;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT Id, Title, Content, CreatedDate, Author
                        FROM News
                        WHERE Id = @Id";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                article = new News
                                {
                                    Id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                                    Title = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                    Content = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                    CreatedDate = reader.IsDBNull(3) ? DateTime.Now : reader.GetDateTime(3),
                                    Author = reader.IsDBNull(4) ? "" : reader.GetString(4)
                                };
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Database error at News/Detail: {ex.Message}");
                return View("Error");
            }

            if (article == null)
            {
                return NotFound();
            }

            return View(article);
        }
    }
}