using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MentalHealthSupport.Models.ViewModel;

namespace MentalHealthSupport.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly string? _connectionString;

        public ReviewsController(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        [HttpGet]
        public IActionResult Create(int appointmentId)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            using SqlConnection conn = new SqlConnection(_connectionString);
            conn.Open();

            string query = @"
                SELECT a.AppointmentId, a.ConsultantId, u.FullName
                FROM Appointments a
                INNER JOIN Users u ON a.ConsultantId = u.UserId
                WHERE a.AppointmentId = @AppointmentId
                  AND a.UserId = @UserId
                  AND a.Status = 'Completed'
                  AND NOT EXISTS (
                      SELECT 1 FROM Ratings r WHERE r.AppointmentId = a.AppointmentId
                  )";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@AppointmentId", appointmentId);
            cmd.Parameters.AddWithValue("@UserId", userId.Value);

            using SqlDataReader reader = cmd.ExecuteReader();

            if (!reader.Read())
            {
                TempData["ErrorMessage"] = "Bạn chỉ có thể đánh giá sau khi buổi tư vấn hoàn thành và chưa được đánh giá trước đó.";
                return RedirectToAction("MyAppointments", "Appointments");
            }

            var model = new CreateReviewViewModel
            {
                AppointmentId = reader.GetInt32(0),
                ConsultantId = reader.GetInt32(1),
                ConsultantName = reader.GetString(2)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CreateReviewViewModel model)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(model);

            using SqlConnection conn = new SqlConnection(_connectionString);
            conn.Open();

            string checkQuery = @"
                SELECT COUNT(*)
                FROM Appointments a
                WHERE a.AppointmentId = @AppointmentId
                  AND a.UserId = @UserId
                  AND a.Status = 'Completed'
                  AND NOT EXISTS (
                      SELECT 1 FROM Ratings r WHERE r.AppointmentId = a.AppointmentId
                  )";

            using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
            {
                checkCmd.Parameters.AddWithValue("@AppointmentId", model.AppointmentId);
                checkCmd.Parameters.AddWithValue("@UserId", userId.Value);

                int valid = (int)checkCmd.ExecuteScalar();
                if (valid == 0)
                {
                    ModelState.AddModelError("", "Không thể gửi đánh giá cho lịch hẹn này.");
                    return View(model);
                }
            }

            string insertQuery = @"
                INSERT INTO Ratings (AppointmentId, Score, Comment, RatedAt)
                VALUES (@AppointmentId, @Score, @Comment, GETDATE())";

            using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
            {
                cmd.Parameters.AddWithValue("@AppointmentId", model.AppointmentId);
                cmd.Parameters.AddWithValue("@Score", model.Rating);
                cmd.Parameters.AddWithValue("@Comment", (object?)model.Comment ?? DBNull.Value);

                cmd.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] = "Đánh giá của bạn đã được gửi.";
            return RedirectToAction("MyAppointments", "Appointments");
        }

        [HttpGet]
        public IActionResult ConsultantReviews(int consultantId)
        {
            var list = new List<ConsultantReviewViewModel>();

            using SqlConnection conn = new SqlConnection(_connectionString);
            conn.Open();

            string query = @"
                SELECT 
                    r.RatingId,
                    r.AppointmentId,
                    r.Score,
                    r.Comment,
                    r.RatedAt,
                    u.FullName AS UserName,
                    c.FullName AS ConsultantName
                FROM Ratings r
                INNER JOIN Appointments a ON r.AppointmentId = a.AppointmentId
                INNER JOIN Users u ON a.UserId = u.UserId
                INNER JOIN Users c ON a.ConsultantId = c.UserId
                WHERE a.ConsultantId = @ConsultantId
                ORDER BY r.RatedAt DESC";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ConsultantId", consultantId);

            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ConsultantReviewViewModel
                {
                    ReviewId = reader.GetInt32(0),
                    AppointmentId = reader.GetInt32(1),
                    Rating = reader.GetInt32(2),
                    Comment = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    RatedAt = reader.GetDateTime(4),
                    UserName = reader.GetString(5),
                    ConsultantName = reader.GetString(6)
                });
            }

            ViewBag.ConsultantId = consultantId;
            return View(list);
        }
    }
}