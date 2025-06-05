using KTGiuaKy.Models;
using Microsoft.EntityFrameworkCore;

namespace KTGiuaKy.Repositories
{
    public class EFSinhVienRepository : ISinhVienRepository
    {
        private readonly ApplicationDbContext _context;

        public EFSinhVienRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IEnumerable<SinhVien> GetAll()
        {
            return _context.SinhViens.Include(s => s.NganhHoc).ToList();
        }

        public SinhVien GetById(string maSV)
        {
            return _context.SinhViens.Include(s => s.NganhHoc).FirstOrDefault(s => s.MaSV == maSV);
        }

        public void Add(SinhVien sinhVien)
        {
            _context.SinhViens.Add(sinhVien);
            _context.SaveChanges();
        }

        public void Update(SinhVien sinhVien)
        {   
            _context.SinhViens.Update(sinhVien);
            _context.SaveChanges();
        }

        public void Delete(string maSV)
        {
            var sinhVien = _context.SinhViens.Find(maSV);
            if (sinhVien != null)
            {
                _context.SinhViens.Remove(sinhVien);
                _context.SaveChanges();
            }
        }
    }
}