using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MentalHealthSupport.Models;
using System;
using System.Collections.Generic;

public class UsersController : Controller
{
    private readonly string connectionString = "Server=LAPTOPMINHTRI\\SQLEXPRESS;Database=MentalHealthSupportDB;Trusted_Connection=True;TrustServerCertificate=true;";

    public IActionResult Index()
    {
        List<User> users = new List<User>();
        using (SqlConnection con = new SqlConnection(connectionString))
        {
            string query = "SELECT * FROM Users";
            SqlCommand cmd = new SqlCommand(query, con);
            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                User user = new User
                {
                    UserId = (int)reader["UserId"],
                    FullName = reader["FullName"]?.ToString() ?? string.Empty,
                    Email = reader["Email"]?.ToString() ?? string.Empty,
                    Phone = reader["Phone"]?.ToString() ?? string.Empty,
                    Role = reader["Role"]?.ToString() ?? string.Empty,
                    IsVerified = (bool)reader["IsVerified"],
                    CreatedAt = (DateTime)reader["CreatedAt"]
                };
                users.Add(user);
            }
            con.Close();
        }
        return View(users);
    }

    // GET: Users/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Users/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(User user)
    {
        if (ModelState.IsValid)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "INSERT INTO Users (FullName, Email, Phone, Role, IsVerified, CreatedAt) " +
                             "VALUES (@FullName, @Email, @Phone, @Role, @IsVerified, @CreatedAt)";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@FullName", user.FullName);
                cmd.Parameters.AddWithValue("@Email", user.Email);
                cmd.Parameters.AddWithValue("@Phone", user.Phone ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Role", user.Role);
                cmd.Parameters.AddWithValue("@IsVerified", user.IsVerified);
                cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
            }
            return RedirectToAction(nameof(Index));
        }
        return View(user);
    }

    // GET: Users
    public IActionResult Edit(int id)
    {
        User? user = null;
        using (SqlConnection con = new SqlConnection(connectionString))
        {
            string query = "SELECT * FROM Users WHERE UserId = @UserId";
            SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@UserId", id);
            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                user = new User
                {
                    UserId = (int)reader["UserId"],
                    FullName = reader["FullName"] != DBNull.Value ? reader["FullName"].ToString() : string.Empty,
                    Email = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : string.Empty,
                    Phone = reader["Phone"] != DBNull.Value ? reader["Phone"].ToString() : string.Empty,
                    Role = reader["Role"] != DBNull.Value ? reader["Role"].ToString() : string.Empty,
                    IsVerified = (bool)reader["IsVerified"],
                    CreatedAt = (DateTime)reader["CreatedAt"]
                };
            }
            con.Close();
        }

        if (user == null)
        {
            return NotFound();
        }
        return View(user);
    }
    // POST: Users/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, User user)
    {
        if (id != user.UserId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = "UPDATE Users SET FullName = @FullName, Email = @Email, Phone = @Phone, " +
                             "Role = @Role, IsVerified = @IsVerified WHERE UserId = @UserId";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@UserId", user.UserId);
                cmd.Parameters.AddWithValue("@FullName", user.FullName);
                cmd.Parameters.AddWithValue("@Email", user.Email);
                cmd.Parameters.AddWithValue("@Phone", user.Phone ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Role", user.Role);
                cmd.Parameters.AddWithValue("@IsVerified", user.IsVerified);

                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
            }
            return RedirectToAction(nameof(Index));
        }
        return View(user);
    }

    // GET: Users/
    public IActionResult Delete(int id)
    {
        User? user = null;
        using (SqlConnection con = new SqlConnection(connectionString))
        {
            string query = "SELECT * FROM Users WHERE UserId = @UserId";
            SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@UserId", id);
            con.Open();
            SqlDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                user = new User
                {
                    UserId = (int)reader["UserId"],
                    FullName = reader["FullName"]?.ToString() ?? string.Empty,
                    Email = reader["Email"]?.ToString() ?? string.Empty,
                    Phone = reader["Phone"]?.ToString() ?? string.Empty,
                    Role = reader["Role"]?.ToString() ?? string.Empty,
                    IsVerified = (bool)reader["IsVerified"],
                    CreatedAt = (DateTime)reader["CreatedAt"]
                };
            }
            con.Close();
        }

        if (user == null)
        {
            return NotFound();
        }
        return View(user);
    }

    // POST: Users/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        using (SqlConnection con = new SqlConnection(connectionString))
        {
            string query = "DELETE FROM Users WHERE UserId = @UserId";
            SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@UserId", id);
            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();
        }
        return RedirectToAction(nameof(Index));
    }

    // public IActionResult Login()
    // {
    //     return View();
    // }
    
    // [HttpPost]
    // public IActionResult Login(string email, string password)
    // {
    //     using (SqlConnection con = new SqlConnection(connectionString))
    //     {
    //         string query = "SELECT * FROM Users WHERE Email = @Email AND Password = @Password";
    //         SqlCommand cmd = new SqlCommand(query, con);
    //         cmd.Parameters.AddWithValue("@Email", email);
    //         cmd.Parameters.AddWithValue("@Password", password); // nên mã hóa trong thực tế

    //         con.Open();
    //         SqlDataReader reader = cmd.ExecuteReader();

    //         if (reader.Read())
    //         {
    //             // Đăng nhập thành công, lưu thông tin vào Session
    //             HttpContext.Session.SetInt32("UserId", (int)reader["UserId"]);
    //             HttpContext.Session.SetString("FullName", reader["FullName"].ToString() ?? "");
    //             HttpContext.Session.SetString("Role", reader["Role"].ToString() ?? "");

    //             return RedirectToAction("Index", "Home"); // chuyển về trang chính
    //         }

    //         ViewBag.Message = "Email hoặc mật khẩu không đúng!";
    //         return View();
    //     }
    // }

}
