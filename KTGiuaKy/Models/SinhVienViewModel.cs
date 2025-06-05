using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace KTGiuaKy.Models
{
    public class SinhVienViewModel
    {
        [Required(ErrorMessage = "Mã SV là bắt buộc")]
        [StringLength(10)]
        public string MaSV { get; set; }

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(50)]
        public string HoTen { get; set; }

        [StringLength(5)]
        [Required(ErrorMessage = "Giới tính là bắt buộc")]
        public string GioiTinh { get; set; }

        [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
        public DateTime NgaySinh { get; set; }

        public string Hinh { get; set; }

        [Required(ErrorMessage = "Mã ngành là bắt buộc")]
        public string MaNganh { get; set; }

        public IEnumerable<SelectListItem> MaNganhList { get; set; }

        [Display(Name = "Hình ảnh")]
        [Required(ErrorMessage = "Hình ảnh là bắt buộc")]
        public IFormFile MainImage { get; set; }
    }
}