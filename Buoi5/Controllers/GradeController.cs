using Buoi5.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Buoi5.Controllers
{
    public class GradeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GradeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Grade/Index
        public IActionResult Index()
        {
            List<Grade> listGrade = _context.Grades.Include(g => g.Students).ToList();
            return View(listGrade);
        }

        // GET: /Grade/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Grade/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Grade grade)
        {
            if (ModelState.IsValid)
            {
                _context.Add(grade);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(grade);
        }

        // GET: /Grade/Edit/5
        public IActionResult Edit(int? id)
        {
            if (id == null) return NotFound();
            var grade = _context.Grades.Find(id);
            if (grade == null) return NotFound();
            return View(grade);
        }

        // POST: /Grade/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Grade grade)
        {
            if (id != grade.GradeId) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(grade);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(grade);
        }

        // GET: /Grade/Delete/5
        public IActionResult Delete(int? id)
        {
            if (id == null) return NotFound();
            var grade = _context.Grades.Include(g => g.Students).FirstOrDefault(g => g.GradeId == id);
            if (grade == null) return NotFound();
            return View(grade);
        }

        // POST: /Grade/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var grade = _context.Grades.Find(id);
            if (grade != null)
            {
                _context.Grades.Remove(grade);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}