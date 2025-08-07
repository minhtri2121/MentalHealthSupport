using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MentalHealthSupport.Models.ViewModel;

namespace MentalHealthSupport.Controllers
{
    public class AboutController : Controller
    {
        private readonly string? connectionString;

        public AboutController(IConfiguration config)
        {
            connectionString = config.GetConnectionString("DefaultConnection");
        }

        public IActionResult Index()
        {
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
                // Log lỗi ra console (hoặc hệ thống log khác nếu bạn dùng)
                Console.WriteLine("Lỗi SQL: " + ex.Message);

                // Hoặc thêm vào ModelState để hiện lỗi ra View (nếu có dùng @Html.ValidationSummary)
                ModelState.AddModelError("", "Đã xảy ra lỗi khi lấy dữ liệu: " + ex.Message);

                // Giá trị fallback
                model.HeroImageUrl = null;
            }
            return View(model);
        }
    }
}