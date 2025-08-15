using Microsoft.AspNetCore.Mvc;
using MentalHealthSupport.Models.ViewModel;
using Microsoft.Data.SqlClient;
using MentalHealthSupport.Models;

namespace MentalHealthSupport.Controllers;

public class HomeController(IConfiguration config) : Controller
{
    private readonly IConfiguration _config = config;

    public IActionResult Index()
    {
        List<ConsultantViewModel> consultants = new List<ConsultantViewModel>();
        string? connectionString = _config.GetConnectionString("DefaultConnection");

        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            SqlCommand cmd = new SqlCommand(@"
                SELECT Top 4 u.UserId, u.FullName, u.Email, u.Phone, u.Role, u.IsVerified,
                    c.ConsultantId, c.Specialty, c.Description, c.ApprovalStatus, c.AvatarUrl, c.ExperienceYears
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
    
    public IActionResult Terms()
        {
            TermsAndPolicy policy = GetPolicyByType("Terms");
            return View(policy);
        }

        public IActionResult Privacy()
        {
            TermsAndPolicy policy = GetPolicyByType("Privacy");
            return View(policy);
        }

        private TermsAndPolicy GetPolicyByType(string policyType)
        {
            string? connectionString = _config.GetConnectionString("DefaultConnection");
            using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT TOP 1 * FROM TermsAndPolicies WHERE PolicyType = @PolicyType AND IsActive = 1";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PolicyType", policyType);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new TermsAndPolicy
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
            return new TermsAndPolicy { PolicyType = policyType, Content = "Nội dung chưa có.", IsActive = true };
        }
}
