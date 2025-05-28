using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MentalHealthSupport.Models.ViewModel;

public class ConsultantsController : Controller
{
    private readonly IConfiguration _config;

    public ConsultantsController(IConfiguration config)
    {
        _config = config;
    }

    // Lấy danh sách chuyên gia kèm thông tin user
    public IActionResult Index()
    {
        List<ConsultantViewModel> consultants = new List<ConsultantViewModel>();
        string? connectionString = _config.GetConnectionString("DefaultConnection");

        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            // JOIN Users và ConsultantProfiles
            SqlCommand cmd = new SqlCommand(@"
                SELECT u.UserId, u.FullName, u.Email, u.Phone, u.Role, u.IsVerified,
                       c.ConsultantId, c.Specialty, c.Description, c.ApprovalStatus, c.AvatarUrl, c.ExperienceYears, c.CertificateUrl
                FROM Users u
                INNER JOIN ConsultantProfiles c ON u.UserId = c.ConsultantId and u.IsVerified = 'True'
                and u.Role = 'Consultant'
            ", conn);

            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                consultants.Add(new ConsultantViewModel
                {
                    UserId = Convert.ToInt32(reader["UserId"]),
                    FullName = reader["FullName"].ToString() ?? string.Empty,
                    Email = reader["Email"].ToString() ?? string.Empty,
                    Phone = reader["Phone"].ToString() ?? string.Empty,
                    Role = reader["Role"].ToString() ?? string.Empty,
                    IsVerified = Convert.ToBoolean(reader["IsVerified"]),

                    ConsultantId = Convert.ToInt32(reader["ConsultantId"]),
                    Specialty = reader["Specialty"].ToString() ?? string.Empty,
                    Description = reader["Description"].ToString() ?? string.Empty,
                    ApprovalStatus = reader["ApprovalStatus"].ToString() ?? string.Empty,
                    AvatarUrl = reader["AvatarUrl"].ToString() ?? string.Empty,
                    ExperienceYears = Convert.ToInt32(reader["ExperienceYears"]),
                    CertificateUrl = reader["CertificateUrl"].ToString() ?? string.Empty
                });
            }
        }
        return View(consultants);
    }

    // Hiển thị form tạo mới chuyên gia
    public IActionResult Create() => View();

    // Xử lý tạo mới chuyên gia
    [HttpPost]
    public IActionResult Create(ConsultantViewModel consultant)
    {
        string? connectionString = _config.GetConnectionString("DefaultConnection");
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();

            // 1. Tạo User trước
            SqlCommand cmdUser = new SqlCommand(@"
                INSERT INTO Users (FullName, Email, Phone, Role, IsVerified, CreatedAt)
                OUTPUT INSERTED.UserId
                VALUES (@FullName, @Email, @Phone, @Role, @IsVerified, @CreatedAt)
            ", conn);
            cmdUser.Parameters.AddWithValue("@FullName", consultant.FullName);
            cmdUser.Parameters.AddWithValue("@Email", consultant.Email);
            cmdUser.Parameters.AddWithValue("@Phone", consultant.Phone ?? "");
            cmdUser.Parameters.AddWithValue("@Role", "Consultant");
            cmdUser.Parameters.AddWithValue("@IsVerified", consultant.IsVerified);
            cmdUser.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

            int newUserId = (int)cmdUser.ExecuteScalar();

            // 2. Tạo ConsultantProfile
            SqlCommand cmdProfile = new SqlCommand(@"
                INSERT INTO ConsultantProfiles (UserId, Specialization, Certificate, Status)
                VALUES (@UserId, @Specialization, @Certificate, @Status)
            ", conn);
            cmdProfile.Parameters.AddWithValue("@UserId", newUserId);
            cmdProfile.Parameters.AddWithValue("@Specialty", consultant.Specialty ?? "");
            cmdProfile.Parameters.AddWithValue("@Description", consultant.Description ?? "");
            cmdProfile.Parameters.AddWithValue("@ApprovalStatus", consultant.ApprovalStatus ?? "Pending");
            cmdProfile.ExecuteNonQuery();
        }
        return RedirectToAction("Index");
    }

    // Hiển thị form chỉnh sửa chuyên gia
    public IActionResult Edit(int id)
    {
        ConsultantViewModel? consultant = null;
        string? connectionString = _config.GetConnectionString("DefaultConnection");
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            SqlCommand cmd = new SqlCommand(@"
                SELECT u.UserId, u.FullName, u.Email, u.Phone, u.Role, u.IsVerified,
                       c.ConsultantId, c.Specialization, c.Certificate, c.Status
                FROM Users u
                INNER JOIN ConsultantProfiles c ON u.UserId = c.UserId
                WHERE c.ConsultantId = @id
            ", conn);
            cmd.Parameters.AddWithValue("@id", id);
            SqlDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                consultant = new ConsultantViewModel
                {
                    UserId = Convert.ToInt32(reader["UserId"]),
                    FullName = reader["FullName"].ToString() ?? string.Empty,
                    Email = reader["Email"].ToString() ?? string.Empty,
                    Phone = reader["Phone"].ToString() ?? string.Empty,
                    Role = reader["Role"].ToString() ?? string.Empty,
                    IsVerified = Convert.ToBoolean(reader["IsVerified"]),

                    ConsultantId = Convert.ToInt32(reader["ConsultantId"]),
                    Specialty = reader["Specialty"].ToString() ?? string.Empty,
                    Description = reader["Description"].ToString() ?? string.Empty,
                    ApprovalStatus = reader["ApprovalStatus"].ToString() ?? string.Empty
                };
            }
        }
        if (consultant == null) return NotFound();
        return View(consultant);
    }

    // Xử lý cập nhật chuyên gia
    [HttpPost]
    public IActionResult Edit(ConsultantViewModel consultant)
    {
        string? connectionString = _config.GetConnectionString("DefaultConnection");
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            // Update Users
            SqlCommand cmdUser = new SqlCommand(@"
                UPDATE Users SET FullName = @FullName, Email = @Email, Phone = @Phone, IsVerified = @IsVerified
                WHERE UserId = @UserId
            ", conn);
            cmdUser.Parameters.AddWithValue("@FullName", consultant.FullName);
            cmdUser.Parameters.AddWithValue("@Email", consultant.Email);
            cmdUser.Parameters.AddWithValue("@Phone", consultant.Phone ?? "");
            cmdUser.Parameters.AddWithValue("@IsVerified", consultant.IsVerified);
            cmdUser.Parameters.AddWithValue("@UserId", consultant.UserId);
            cmdUser.ExecuteNonQuery();

            // Update ConsultantProfiles
            SqlCommand cmdProfile = new SqlCommand(@"
                UPDATE ConsultantProfiles SET Specialization = @Specialization, Certificate = @Certificate, Status = @Status
                WHERE ConsultantId = @ConsultantId
            ", conn);
            cmdProfile.Parameters.AddWithValue("@Specialty", consultant.Specialty ?? "");
            cmdProfile.Parameters.AddWithValue("@Description", consultant.Description ?? "");
            cmdProfile.Parameters.AddWithValue("@ApprovalStatus", consultant.ApprovalStatus ?? "Pending");
            cmdProfile.Parameters.AddWithValue("@ConsultantId", consultant.ConsultantId);
            cmdProfile.ExecuteNonQuery();
        }
        return RedirectToAction("Index");
    }

    // Xóa chuyên gia
    public IActionResult Delete(int id)
    {
        string? connectionString = _config.GetConnectionString("DefaultConnection");
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();

            // Lấy UserId để xóa cả User nếu muốn
            int userId = 0;
            SqlCommand cmdGet = new SqlCommand("SELECT UserId FROM ConsultantProfiles WHERE ConsultantId = @id", conn);
            cmdGet.Parameters.AddWithValue("@id", id);
            var result = cmdGet.ExecuteScalar();
            if (result != null)
                userId = Convert.ToInt32(result);

            // Xóa ConsultantProfile
            SqlCommand cmdProfile = new SqlCommand("DELETE FROM ConsultantProfiles WHERE ConsultantId = @id", conn);
            cmdProfile.Parameters.AddWithValue("@id", id);
            cmdProfile.ExecuteNonQuery();

            // Xóa User
            // SqlCommand cmdUser = new SqlCommand("DELETE FROM Users WHERE UserId = @userId", conn);
            // cmdUser.Parameters.AddWithValue("@userId", userId);
            // cmdUser.ExecuteNonQuery();
        }
        return RedirectToAction("Index");
    }
    
    // Hiển thị chi tiết chuyên gia
    public IActionResult Details(int id)
    {
        ConsultantDetailsViewModel? consultant = null;
        string? connectionString = _config.GetConnectionString("DefaultConnection");

        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();

            // 1. Lấy thông tin cơ bản từ Users và ConsultantProfiles
            SqlCommand cmd = new SqlCommand(@"
                SELECT u.UserId, u.FullName, u.Email, u.Phone, u.Role, u.IsVerified,
                    c.ConsultantId, c.Specialty, c.Description, c.ApprovalStatus, c.AvatarUrl, c.ExperienceYears, c.CertificateUrl
                FROM Users u
                INNER JOIN ConsultantProfiles c ON u.UserId = c.ConsultantId
                WHERE u.UserId = @Id AND u.IsVerified = 'True' AND u.Role = 'Consultant'
            ", conn);
            cmd.Parameters.AddWithValue("@Id", id);

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    consultant = new ConsultantDetailsViewModel
                    {
                        UserId = Convert.ToInt32(reader["UserId"]),
                        FullName = reader["FullName"].ToString() ?? string.Empty,
                        Email = reader["Email"].ToString() ?? string.Empty,
                        Phone = reader["Phone"].ToString() ?? string.Empty,
                        Role = reader["Role"].ToString() ?? string.Empty,
                        IsVerified = Convert.ToBoolean(reader["IsVerified"]),

                        ConsultantId = Convert.ToInt32(reader["ConsultantId"]),
                        Specialty = reader["Specialty"].ToString() ?? string.Empty,
                        Description = reader["Description"].ToString() ?? string.Empty,
                        ApprovalStatus = reader["ApprovalStatus"].ToString() ?? string.Empty,
                        AvatarUrl = reader["AvatarUrl"].ToString() ?? string.Empty,
                        ExperienceYears = Convert.ToInt32(reader["ExperienceYears"]),
                        CertificateUrl = reader["CertificateUrl"].ToString() ?? string.Empty
                    };
                }
            }

            if (consultant == null)
                return NotFound();

            // 2. Lấy lịch làm việc
            SqlCommand scheduleCmd = new SqlCommand(@"
                SELECT DayOfWeek, startTime, endTime
                FROM ConsultantSchedules
                WHERE ConsultantId = @ConsultantId
            ", conn);
            scheduleCmd.Parameters.AddWithValue("@ConsultantId", id);

            using (SqlDataReader reader = scheduleCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    consultant.Schedules.Add(new ConsultantScheduleViewModel
                    {
                        DayOfWeek = Convert.ToDateTime(reader["Date"]),
                        StartTime = reader["TimeRange"].ToString() ?? "",
                        EndTime = reader["TimeRange"].ToString() ?? "",
                    });
                }
            }

            // 3. Lấy đánh giá
            SqlCommand ratingCmd = new SqlCommand(@"
                SELECT u.FullName, r.Score, r.Comment, r.RatedAt
                FROM Ratings r
                INNER JOIN Appointments a ON r.AppointmentId = a.AppointmentId
                INNER JOIN Users u ON a.UserId = u.UserId
                WHERE u.UserId = @ConsultantId
            ", conn);
            ratingCmd.Parameters.AddWithValue("@ConsultantId", id);

            using (SqlDataReader reader = ratingCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    consultant.Ratings.Add(new RatingViewModel
                    {
                        UserName = reader["FullName"].ToString() ?? "",
                        Score = Convert.ToInt32(reader["Score"]),
                        Comment = reader["Comment"].ToString() ?? "",
                        CreatedAt = Convert.ToDateTime(reader["RatedAt"])
                    });
                }
            }

            // 4. Lấy bài viết
            SqlCommand articleCmd = new SqlCommand(@"
                SELECT Title, Content, Category
                FROM Articles a join Users u on u.UserId = a.CreatedBy
                WHERE u.UserId = @ConsultantId
            ", conn);
            articleCmd.Parameters.AddWithValue("@ConsultantId", id);

            using (SqlDataReader reader = articleCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    consultant.Articles.Add(new ArticleViewModel
                    {
                        Title = reader["Title"].ToString() ?? "",
                        Content = reader["Content"].ToString() ?? "",
                        Category = reader["Category"].ToString() ?? "",
                    });
                }
            }
        }

        return View(consultant);
    }

}
