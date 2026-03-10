using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MentalHealthSupport.Models.ViewModel;

namespace MentalHealthSupport.Controllers
{
    public class AppointmentsController : Controller
    {
        private readonly string? _connectionString;

        public AppointmentsController(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        // Trang đặt lịch
        [HttpGet]
        public IActionResult Create(int consultantId)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var model = new CreateAppointmentViewModel
            {
                ConsultantId = consultantId,
                AppointmentTime = DateTime.Now.AddDays(1)
            };

            return View(model);
        }

        // POST: tạo lịch hẹn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CreateAppointmentViewModel model)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                using SqlConnection conn = new SqlConnection(_connectionString);
                conn.Open();

                // Kiểm tra trùng lịch
                string checkQuery = @"
                    SELECT COUNT(*) 
                    FROM Appointments
                    WHERE ConsultantId = @ConsultantId
                    AND AppointmentTime = @AppointmentTime
                    AND Status IN ('Pending','Confirmed')";

                using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@ConsultantId", model.ConsultantId);
                    checkCmd.Parameters.AddWithValue("@AppointmentTime", model.AppointmentTime);

                    int exists = (int)checkCmd.ExecuteScalar();

                    if (exists > 0)
                    {
                        ModelState.AddModelError("", "Khung giờ này đã được đặt. Vui lòng chọn thời gian khác.");
                        return View(model);
                    }
                }

                string insertQuery = @"
                    INSERT INTO Appointments
                    (UserId, ConsultantId, AppointmentTime, Status, Note, CreatedAt)
                    VALUES
                    (@UserId, @ConsultantId, @AppointmentTime, 'Pending', @Note, GETDATE())";

                using SqlCommand cmd = new SqlCommand(insertQuery, conn);

                cmd.Parameters.AddWithValue("@UserId", userId.Value);
                cmd.Parameters.AddWithValue("@ConsultantId", model.ConsultantId);
                cmd.Parameters.AddWithValue("@AppointmentTime", model.AppointmentTime);
                cmd.Parameters.AddWithValue("@Note", (object?)model.Note ?? DBNull.Value);

                cmd.ExecuteNonQuery();

                TempData["SuccessMessage"] = "Đặt lịch tư vấn thành công.";

                return RedirectToAction("MyAppointments");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi: " + ex.Message);
                return View(model);
            }
        }

        // Danh sách lịch của user
        public IActionResult MyAppointments()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var list = new List<MyAppointmentViewModel>();

            using SqlConnection conn = new SqlConnection(_connectionString);
            conn.Open();

            string query = @"
                SELECT 
                    a.AppointmentId,
                    a.AppointmentTime,
                    a.Status,
                    a.Note,
                    u.FullName AS ConsultantName
                FROM Appointments a
                INNER JOIN Users u ON a.ConsultantId = u.UserId
                WHERE a.UserId = @UserId
                ORDER BY a.AppointmentTime DESC";

            using SqlCommand cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@UserId", userId.Value);

            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new MyAppointmentViewModel
                {
                    AppointmentId = reader.GetInt32(0),
                    AppointmentTime = reader.GetDateTime(1),
                    Status = reader.GetString(2),
                    Note = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    ConsultantName = reader.GetString(4)
                });
            }

            return View(list);
        }

        // Hủy lịch
        [HttpPost]
        public IActionResult Cancel(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            using SqlConnection conn = new SqlConnection(_connectionString);
            conn.Open();

            string query = @"
                UPDATE Appointments
                SET Status = 'Cancelled'
                WHERE AppointmentId = @Id
                AND UserId = @UserId
                AND Status IN ('Pending','Confirmed')";

            using SqlCommand cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@UserId", userId.Value);

            cmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = "Đã hủy lịch hẹn.";

            return RedirectToAction("MyAppointments");
        }

        // Lịch hẹn dành cho chuyên gia
        [HttpGet]
        public IActionResult ConsultantAppointments()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("UserRole");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (role != "Consultant" && role != "Admin")
                return RedirectToAction("Index", "Home");

            var list = new List<ConsultantAppointmentViewModel>();

            using SqlConnection conn = new SqlConnection(_connectionString);
            conn.Open();

            string query = @"
                SELECT 
                    a.AppointmentId,
                    u.FullName AS UserName,
                    a.AppointmentTime,
                    a.Status,
                    a.Note,
                    a.CreatedAt
                FROM Appointments a
                INNER JOIN Users u ON a.UserId = u.UserId
                WHERE a.ConsultantId = @ConsultantId
                ORDER BY a.AppointmentTime DESC";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@ConsultantId", userId.Value);

            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ConsultantAppointmentViewModel
                {
                    AppointmentId = reader.GetInt32(0),
                    UserName = reader.GetString(1),
                    AppointmentTime = reader.GetDateTime(2),
                    Status = reader.GetString(3),
                    Note = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    CreatedAt = reader.GetDateTime(5)
                });
            }

            return View(list);
        }

        // Chuyên gia xác nhận lịch
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Confirm(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("UserRole");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (role != "Consultant" && role != "Admin")
                return RedirectToAction("Index", "Home");

            using SqlConnection conn = new SqlConnection(_connectionString);
            conn.Open();

            string query = @"
                UPDATE Appointments
                SET Status = 'Confirmed'
                WHERE AppointmentId = @Id
                AND ConsultantId = @ConsultantId
                AND Status = 'Pending'";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@ConsultantId", userId.Value);

            int rows = cmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = rows > 0
                ? "Đã xác nhận lịch hẹn."
                : "Không thể xác nhận lịch hẹn.";

            return RedirectToAction("ConsultantAppointments");
        }

        // Chuyên gia từ chối / hủy lịch
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reject(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("UserRole");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (role != "Consultant" && role != "Admin")
                return RedirectToAction("Index", "Home");

            using SqlConnection conn = new SqlConnection(_connectionString);
            conn.Open();

            string query = @"
                UPDATE Appointments
                SET Status = 'Cancelled'
                WHERE AppointmentId = @Id
                AND ConsultantId = @ConsultantId
                AND Status IN ('Pending','Confirmed')";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@ConsultantId", userId.Value);

            int rows = cmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = rows > 0
                ? "Đã từ chối lịch hẹn."
                : "Không thể từ chối lịch hẹn.";

            return RedirectToAction("ConsultantAppointments");
        }

        // Đánh dấu đã hoàn thành
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Complete(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("UserRole");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (role != "Consultant" && role != "Admin")
                return RedirectToAction("Index", "Home");

            using SqlConnection conn = new SqlConnection(_connectionString);
            conn.Open();

            string query = @"
                UPDATE Appointments
                SET Status = 'Completed'
                WHERE AppointmentId = @Id
                AND ConsultantId = @ConsultantId
                AND Status = 'Confirmed'";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@ConsultantId", userId.Value);

            int rows = cmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = rows > 0
                ? "Đã hoàn thành buổi tư vấn."
                : "Không thể cập nhật trạng thái.";

            return RedirectToAction("ConsultantAppointments");
        }
    }
}