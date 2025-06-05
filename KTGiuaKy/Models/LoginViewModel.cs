using System.ComponentModel.DataAnnotations;

namespace KTGiuaKy.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Mã sinh viên là bắt buộc")]
        [StringLength(10)]
        public string MaSV { get; set; }
    }
}