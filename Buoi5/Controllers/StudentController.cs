using Buoi5.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Buoi5.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StudentController(ApplicationDbContext context)
        {
            _context = context;
            Console.WriteLine("StudentController initialized");
        }

        // GET: /Student/Index
        public IActionResult Index()
        {
            Console.WriteLine("Index action called");
            var students = _context.Students.Include(s => s.Grade).ToList();
            Console.WriteLine($"Found {students.Count} students");
            return View(students);
        }

        // GET: /Student/Details/5
        public IActionResult Details(int? id)
        {
            Console.WriteLine($"Details action called with id: {id}");
            if (id == null)
            {
                Console.WriteLine("Id is null");
                return NotFound();
            }
            var student = _context.Students.Include(s => s.Grade).FirstOrDefault(s => s.StudentId == id);
            if (student == null)
            {
                Console.WriteLine("Student not found");
                return NotFound();
            }
            Console.WriteLine($"Found student: {student.FirstName} {student.LastName}");
            return View(student);
        }

        // GET: /Student/Create
        public IActionResult Create()
        {
            Console.WriteLine("Create GET action called");
            ViewBag.Grades = _context.Grades.ToList();
            Console.WriteLine($"Loaded {ViewBag.Grades.Count} grades for dropdown");
            return View();
        }

        // POST: /Student/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Student student)
        {
            Console.WriteLine("Create POST action called");
            Console.WriteLine($"Received student data: {JsonSerializer.Serialize(student)}");

            // Validate GradeId
            if (student.GradeId <= 0 || !_context.Grades.Any(g => g.GradeId == student.GradeId))
            {
                Console.WriteLine($"Invalid GradeId: {student.GradeId}");
                ModelState.AddModelError("GradeId", "Lớp được chọn không hợp lệ.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    Console.WriteLine("Adding student to database...");
                    _context.Add(student);
                    _context.SaveChanges();
                    Console.WriteLine("Student added successfully");
                    return Json(new { success = true, redirectUrl = Url.Action("Index") });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while adding student: {ex.Message}");
                    return Json(new { success = false, message = $"Có lỗi xảy ra khi thêm dữ liệu: {ex.Message}" });
                }
            }
            else
            {
                Console.WriteLine("Model state is invalid");
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                Console.WriteLine($"Validation errors: {JsonSerializer.Serialize(errors)}");
                return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors });
            }
        }

        // GET: /Student/Edit/5
        public IActionResult Edit(int? id)
        {
            Console.WriteLine($"Edit GET action called with id: {id}");
            if (id == null)
            {
                Console.WriteLine("Id is null");
                return NotFound();
            }
            var student = _context.Students.Find(id);
            if (student == null)
            {
                Console.WriteLine("Student not found");
                return NotFound();
            }
            Console.WriteLine($"Found student: {student.FirstName} {student.LastName}");
            ViewBag.Grades = _context.Grades.ToList();
            Console.WriteLine($"Loaded {ViewBag.Grades.Count} grades for dropdown");
            return View(student);
        }

        // POST: /Student/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Student student)
        {
            Console.WriteLine($"Edit POST action called for StudentId: {id}");
            Console.WriteLine($"Received student data: {JsonSerializer.Serialize(student)}");
            Console.WriteLine($"Received GradeId: {student.GradeId}");

            if (id != student.StudentId)
            {
                Console.WriteLine("ID mismatch");
                return Json(new { success = false, message = "ID không khớp" });
            }

            if (student.GradeId <= 0 || !_context.Grades.Any(g => g.GradeId == student.GradeId))
            {
                Console.WriteLine($"GradeId {student.GradeId} does not exist in Grades table");
                ModelState.AddModelError("GradeId", "Lớp được chọn không tồn tại.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingStudent = _context.Students.Find(id);
                    if (existingStudent == null)
                    {
                        return NotFound();
                    }

                    existingStudent.FirstName = student.FirstName;
                    existingStudent.LastName = student.LastName;
                    existingStudent.GradeId = student.GradeId;

                    Console.WriteLine("Updating student in database...");
                    _context.Update(existingStudent);
                    _context.SaveChanges();
                    Console.WriteLine("Student updated successfully");
                    return Json(new { success = true, redirectUrl = Url.Action("Index") });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while updating student: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    return Json(new { success = false, message = $"Có lỗi xảy ra khi lưu dữ liệu: {ex.Message}" });
                }
            }
            else
            {
                var errors = ModelState.Keys
                    .SelectMany(key => ModelState[key].Errors.Select(error => new { Key = key, Error = error.ErrorMessage }))
                    .ToList();
                Console.WriteLine($"Validation errors: {JsonSerializer.Serialize(errors)}");
                return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors = errors.Select(e => e.Error).ToList() });
            }
        }
        // GET: /Student/Delete/5
        public IActionResult Delete(int? id)
        {
            Console.WriteLine($"Delete GET action called with id: {id}");
            if (id == null)
            {
                Console.WriteLine("Id is null");
                return NotFound();
            }
            var student = _context.Students.Include(s => s.Grade).FirstOrDefault(s => s.StudentId == id);
            if (student == null)
            {
                Console.WriteLine("Student not found");
                return NotFound();
            }
            Console.WriteLine($"Found student: {student.FirstName} {student.LastName}");
            return View(student);
        }

        // POST: /Student/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            Console.WriteLine($"DeleteConfirmed POST action called for StudentId: {id}");
            var student = _context.Students.Find(id);
            if (student != null)
            {
                Console.WriteLine("Deleting student...");
                _context.Students.Remove(student);
                _context.SaveChanges();
                Console.WriteLine("Student deleted successfully");
            }
            else
            {
                Console.WriteLine("Student not found");
            }
            return RedirectToAction(nameof(Index));
        }
    }
}