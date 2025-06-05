using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KTGiuaKy.Models
{
    public class DangKy
    {
        [Key]
        public int MaDK { get; set; }

        public DateTime NgayDK { get; set; }

        [StringLength(10)]
        public string MaSV { get; set; }

        [ForeignKey("MaSV")]
        public SinhVien SinhVien { get; set; }

        public ICollection<ChiTietDangKy> ChiTietDangKys { get; set; }
    }
}