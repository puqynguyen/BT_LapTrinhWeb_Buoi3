using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KTGiuaKy.Models
{
    public class SinhVien
    {
        [Key]
        [StringLength(10)]
        [Required(ErrorMessage = "Mã SV là bắt buộc")]
        public string MaSV { get; set; }

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(50)]
        public string HoTen { get; set; }

        [StringLength(5)]
        [Required(ErrorMessage = "Giới tính là bắt buộc")]
        public string GioiTinh { get; set; }

        [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
        public DateTime NgaySinh { get; set; }

        [StringLength(50)]
        public string Hinh { get; set; }

        [StringLength(4)]
        [Required(ErrorMessage = "Mã ngành là bắt buộc")]
        public string MaNganh { get; set; }

        [ForeignKey("MaNganh")]
        public NganhHoc NganhHoc { get; set; }

        public ICollection<DangKy> DangKys { get; set; } = new List<DangKy>();
    }
}