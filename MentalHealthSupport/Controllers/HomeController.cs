using Microsoft.AspNetCore.Mvc;
using MentalHealthSupport.Models.ViewModel;
using Microsoft.Data.SqlClient;

namespace MentalHealthSupport.Controllers;

public class HomeController(IConfiguration config) : Controller
{
    private readonly IConfiguration _config = config;

    public IActionResult Index()
    {
        List<ConsultantViewModel> consultants = new List<ConsultantViewModel>();
        string ?connectionString = _config.GetConnectionString("DefaultConnection");

        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            // JOIN Users và ConsultantProfiles
            SqlCommand cmd = new SqlCommand(@"
                SELECT u.UserId, u.FullName, u.Role, u.IsVerified,
                       c.ConsultantId, c.Specialty, c.CertificateUrl, c.AvatarUrl, c.ExperienceYears
                FROM Users u
                INNER JOIN ConsultantProfiles c ON u.UserId = c.ConsultantId and u.IsVerified = 'True'
                and u.Role = 'Consultant'
            ", conn);

            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                consultants.Add(item: new ConsultantViewModel
                {
                    UserId = Convert.ToInt32(reader["UserId"]),
                    FullName = reader["FullName"].ToString() ?? string.Empty,
                    Role = reader["Role"].ToString() ?? string.Empty,
                    IsVerified = Convert.ToBoolean(reader["IsVerified"]),

                    ConsultantId = Convert.ToInt32(reader["ConsultantId"]),
                    Specialty = reader["Specialty"].ToString() ?? string.Empty,
                    AvatarUrl = reader["AvatarUrl"].ToString() ?? string.Empty,
                    ExperienceYears = Convert.ToInt32(reader["ExperienceYears"])
                });
            }
        }
        return View(consultants);
    }
}
