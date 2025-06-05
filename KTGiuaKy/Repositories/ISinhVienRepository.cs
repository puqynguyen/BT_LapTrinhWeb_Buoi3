using KTGiuaKy.Models;

namespace KTGiuaKy.Repositories
{
    public interface ISinhVienRepository
    {
        IEnumerable<SinhVien> GetAll();
        SinhVien GetById(string maSV);
        void Add(SinhVien sinhVien);
        void Update(SinhVien sinhVien);
        void Delete(string maSV);
    }
}