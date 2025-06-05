using System.ComponentModel.DataAnnotations;

namespace KTGiuaKy.Models
{
    public class HocPhan
    {
        [Key]
        [StringLength(6)]
        public string MaHP { get; set; }

        [Required]
        [StringLength(30)]
        public string TenHP { get; set; }

        public int SoTinChi { get; set; }

        public ICollection<ChiTietDangKy> ChiTietDangKys { get; set; }
    }
}