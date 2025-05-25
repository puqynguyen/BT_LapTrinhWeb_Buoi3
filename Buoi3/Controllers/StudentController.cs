using Microsoft.AspNetCore.Mvc;
using buoi1C5.Models;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace buoi1C5.Controllers
{
    public class StudentController : Controller
    {
        private static List<Student> Students = new List<Student>();
        private const string FilePath = "students.json";

        // Load dữ liệu từ file khi khởi tạo
        private void LoadStudents()
        {
            if (System.IO.File.Exists(FilePath))
            {
                var json = System.IO.File.ReadAllText(FilePath);
                Students = JsonSerializer.Deserialize<List<Student>>(json) ?? new List<Student>();
            }
        }

        // Lưu dữ liệu vào file
        private void SaveStudents()
        {
            var json = JsonSerializer.Serialize(Students, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(FilePath, json);
        }

        public StudentController()
        {
            LoadStudents();
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(Student student)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem MSSV đã tồn tại chưa
                var existingStudent = Students.Find(s => s.MSSV == student.MSSV);
                if (existingStudent != null)
                {
                    // Nếu MSSV đã tồn tại, cập nhật thông tin
                    existingStudent.HoTen = student.HoTen;
                    existingStudent.DiemTB = student.DiemTB;
                    existingStudent.ChuyenNganh = student.ChuyenNganh;
                    TempData["Message"] = "Đổi thông tin sinh viên thành công!";
                }
                else
                {
                    // Nếu MSSV chưa tồn tại, thêm mới
                    Students.Add(student);
                    TempData["Message"] = "Đăng ký sinh viên mới thành công!";
                }

                // Lưu danh sách vào file JSON
                SaveStudents();

                // Sửa lỗi: Redirect đến "ShowKQ" thay vì "mayonnaise"
                return RedirectToAction("ShowKQ", new { mssv = student.MSSV, hoTen = student.HoTen, chuyenNganh = student.ChuyenNganh });
            }
            return View(student);
        }

        public IActionResult ShowKQ(string mssv, string hoTen, string chuyenNganh)
        {
            int soLuongCungNganh = Students.Count(s => s.ChuyenNganh == chuyenNganh);
            ViewBag.MSSV = mssv;
            ViewBag.HoTen = hoTen;
            ViewBag.ChuyenNganh = chuyenNganh;
            ViewBag.SoLuongCungNganh = soLuongCungNganh;
            return View();
        }

        public IActionResult List()
        {
            return View(Students);
        }
    }
}